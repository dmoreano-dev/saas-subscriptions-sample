using Microsoft.EntityFrameworkCore;
using Npgsql;
using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Infrastructure.Persistence;
using Saas.Subscription.Sample.Infrastructure.Persistence.Catalog;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

[Collection(PostgresTestGroup.Name)]
public class PlanCatalogTests(PostgresFixture postgres)
{
    private const string ImmutableState = "55000";
    private const string CheckViolation = "23514";
    private const string UniqueViolation = "23505";
    private const string DraftPlanKey = "draft-plan";
    private const string UnknownCurrency = "usd";
    private const string ProviderPriceId = "price_provider_ref";
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.UnixEpoch;
    private static readonly TimeProvider FixedClock = new FixedTimeProvider(FixedNow);

    private const string CatalogCountsQuery = """
        SELECT (SELECT count(*) FROM feature_definitions) || '/' || (SELECT count(*) FROM plans) || '/' ||
               (SELECT count(*) FROM plan_versions) || '/' || (SELECT count(*) FROM plan_capabilities) || '/' ||
               (SELECT count(*) FROM plan_prices)
        """;

    private const string CatalogFingerprintQuery = """
        SELECT md5(string_agg(row_text, '|' ORDER BY row_text)) FROM (
            SELECT 'v:' || id || status || coalesce(published_at::text, '') AS row_text FROM plan_versions
            UNION ALL SELECT 'c:' || plan_version_id || feature_key || coalesce(bool_value::text, '') || coalesce(limit_value::text, '') || is_unlimited FROM plan_capabilities
            UNION ALL SELECT 'p:' || id || plan_version_id || interval || currency || unit_amount_minor FROM plan_prices
        ) rows
        """;

    private const string FreePriceCountQuery = """
        SELECT count(*) FROM plan_prices pr
        JOIN plan_versions pv ON pv.id = pr.plan_version_id
        JOIN plans p ON p.id = pv.plan_id WHERE p.key = 'free'
        """;

    private static AppDbContext CreateContext(string connectionString) =>
        new(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString).Options);

    private async Task<TestDatabase> CreateSeededDatabaseAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        await DatabaseMigrator.SeedCatalogAsync(database.ConnectionString, FixedClock);
        return database;
    }

    private static async Task<Guid> PublishedVersionIdAsync(NpgsqlConnection connection, string planKey) =>
        await Sql.ScalarAsync<Guid>(
            connection,
            "SELECT pv.id FROM plan_versions pv JOIN plans p ON p.id = pv.plan_id WHERE p.key = @key",
            ("key", planKey));

    private static async Task<Guid> InsertPlanAsync(NpgsqlConnection connection, string key, bool isBillable)
    {
        var id = Sql.NewId();
        await Sql.ExecuteAsync(
            connection,
            "INSERT INTO plans (id, key, name, account_type, is_billable, created_at) VALUES (@id, @key, @key, 'Personal', @billable, now())",
            ("id", id), ("key", key), ("billable", isBillable));
        return id;
    }

    private static async Task<Guid> InsertDraftVersionAsync(NpgsqlConnection connection, Guid planId, int version = 1)
    {
        var id = Sql.NewId();
        await Sql.ExecuteAsync(
            connection,
            "INSERT INTO plan_versions (id, plan_id, version, status, created_at) VALUES (@id, @plan, @version, 'Draft', now())",
            ("id", id), ("plan", planId), ("version", version));
        return id;
    }

    private static Task<int> PublishAsync(NpgsqlConnection connection, Guid versionId) =>
        Sql.ExecuteAsync(
            connection,
            "UPDATE plan_versions SET status = 'Published', published_at = now() WHERE id = @id",
            ("id", versionId));

    private static Task<int> InsertPriceAsync(NpgsqlConnection connection, Guid versionId, string interval = "Month", string currency = "USD", int amount = 1000, string? externalId = null) =>
        Sql.ExecuteAsync(
            connection,
            "INSERT INTO plan_prices (id, plan_version_id, interval, currency, unit_amount_minor, external_price_id) VALUES (@id, @version, @interval, @currency, @amount, @external)",
            ("id", Sql.NewId()), ("version", versionId), ("interval", interval), ("currency", currency), ("amount", amount), ("external", externalId));

    private static Task<int> InsertCapabilityAsync(NpgsqlConnection connection, Guid versionId, string key, bool? boolValue, int? limit, bool unlimited = false) =>
        Sql.ExecuteAsync(
            connection,
            "INSERT INTO plan_capabilities (plan_version_id, feature_key, bool_value, limit_value, is_unlimited) VALUES (@version, @key, @bool, @limit, @unlimited)",
            ("version", versionId), ("key", key), ("bool", boolValue), ("limit", limit), ("unlimited", unlimited));

    // ---- seed idempotency ----

    [Fact]
    public async Task SeedCatalog_EmptyCatalog_InsertsFixtures()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();

        // Act
        var result = await DatabaseMigrator.SeedCatalogAsync(database.ConnectionString, FixedClock);

        // Assert
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        Assert.Equal("7/4/4/18/3", await Sql.ScalarAsync<string>(connection, CatalogCountsQuery));
        Assert.Empty(result.Divergences);
        Assert.Equal(7 + 4 + 4, result.Inserted.Count);
    }

    [Fact]
    public async Task SeedCatalog_RunTwice_DoesNotDuplicateNorChangeAnything()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var countsBefore = await Sql.ScalarAsync<string>(connection, CatalogCountsQuery);
        var fingerprintBefore = await Sql.ScalarAsync<string>(connection, CatalogFingerprintQuery);

        // Act
        var result = await DatabaseMigrator.SeedCatalogAsync(database.ConnectionString, new FixedTimeProvider(FixedNow.AddYears(1)));

        // Assert
        Assert.Empty(result.Inserted);
        Assert.Empty(result.Divergences);
        Assert.Equal(countsBefore, await Sql.ScalarAsync<string>(connection, CatalogCountsQuery));
        Assert.Equal(fingerprintBefore, await Sql.ScalarAsync<string>(connection, CatalogFingerprintQuery));
    }

    [Fact]
    public async Task SeedCatalog_PublishedVersionWasChanged_ReportsDivergenceAndKeepsPublishedData()
    {
        // Arrange: bypass the immutability trigger as a superuser would, then re-seed.
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        await Sql.ExecuteAsync(connection, "ALTER TABLE plan_capabilities DISABLE TRIGGER trg_plan_capabilities_immutable");
        await Sql.ExecuteAsync(connection, "UPDATE plan_capabilities SET limit_value = 999 WHERE feature_key = 'projects.max_count' AND limit_value = 30");
        await Sql.ExecuteAsync(connection, "ALTER TABLE plan_capabilities ENABLE TRIGGER trg_plan_capabilities_immutable");

        // Act
        var result = await DatabaseMigrator.SeedCatalogAsync(database.ConnectionString, FixedClock);

        // Assert
        Assert.Single(result.Divergences);
        Assert.Empty(result.Inserted);
        Assert.Equal(999, await Sql.ScalarAsync<int>(connection, "SELECT max(limit_value) FILTER (WHERE limit_value = 999) FROM plan_capabilities"));
    }

    // ---- immutability ----

    [Fact]
    public async Task PlanVersion_PublishedThenUpdated_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var act = () => Sql.ExecuteAsync(connection, "UPDATE plan_versions SET created_at = now() WHERE status = 'Published'");

        // Assert
        await Sql.AssertSqlStateAsync(ImmutableState, act);
    }

    [Fact]
    public async Task PlanVersion_PublishedThenDeleted_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var act = () => Sql.ExecuteAsync(connection, "DELETE FROM plan_versions");

        // Assert
        await Sql.AssertSqlStateAsync(ImmutableState, act);
    }

    [Theory]
    [InlineData("UPDATE plan_capabilities SET limit_value = 1 WHERE feature_key = 'projects.max_count'")]
    [InlineData("DELETE FROM plan_capabilities")]
    [InlineData("UPDATE plan_prices SET unit_amount_minor = 1")]
    [InlineData("DELETE FROM plan_prices")]
    public async Task PublishedVersionRows_UpdatedOrDeleted_AreRejected(string statement)
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var act = () => Sql.ExecuteAsync(connection, statement);

        // Assert
        await Sql.AssertSqlStateAsync(ImmutableState, act);
    }

    [Fact]
    public async Task PlanCapability_InsertedIntoPublishedVersion_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var proVersion = await PublishedVersionIdAsync(connection, PlanKeys.Pro);

        // Act
        var act = () => InsertCapabilityAsync(connection, proVersion, FeatureKeys.SsoEnabled, true, null);

        // Assert
        await Sql.AssertSqlStateAsync(ImmutableState, act);
    }

    [Fact]
    public async Task PlanPrice_InsertedIntoPublishedVersion_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var proVersion = await PublishedVersionIdAsync(connection, PlanKeys.Pro);

        // Act
        var act = () => InsertPriceAsync(connection, proVersion, interval: "Year", amount: 10000);

        // Assert
        await Sql.AssertSqlStateAsync(ImmutableState, act);
    }

    [Fact]
    public async Task PlanCapability_MovedAwayFromPublishedVersion_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var draft = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));
        var proVersion = await PublishedVersionIdAsync(connection, PlanKeys.Pro);

        // Act
        var act = () => Sql.ExecuteAsync(
            connection,
            "UPDATE plan_capabilities SET plan_version_id = @draft WHERE plan_version_id = @pro AND feature_key = 'projects.max_count'",
            ("draft", draft), ("pro", proVersion));

        // Assert
        await Sql.AssertSqlStateAsync(ImmutableState, act);
    }

    [Fact]
    public async Task DraftVersion_RowsEditedThenPublished_AreFrozenAfterPublish()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));
        await InsertCapabilityAsync(connection, version, FeatureKeys.ProjectsMaxCount, null, 5);
        await Sql.ExecuteAsync(connection, "UPDATE plan_capabilities SET limit_value = 6 WHERE plan_version_id = @id", ("id", version));
        await PublishAsync(connection, version);

        // Act
        var act = () => Sql.ExecuteAsync(connection, "UPDATE plan_capabilities SET limit_value = 7 WHERE plan_version_id = @id", ("id", version));

        // Assert
        await Sql.AssertSqlStateAsync(ImmutableState, act);
    }

    [Fact]
    public async Task PlanVersion_NewVersionOfSamePlan_LeavesPublishedVersionUntouched()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var fingerprintBefore = await Sql.ScalarAsync<string>(connection, CatalogFingerprintQuery);
        var planId = await Sql.ScalarAsync<Guid>(connection, "SELECT id FROM plans WHERE key = 'pro'");

        // Act
        var version = await InsertDraftVersionAsync(connection, planId, version: 2);
        await InsertCapabilityAsync(connection, version, FeatureKeys.ProjectsMaxCount, null, 40);

        // Assert
        var v1Rows = await Sql.ScalarAsync<string>(
            connection,
            "SELECT string_agg(limit_value::text, ',') FROM plan_capabilities c JOIN plan_versions v ON v.id = c.plan_version_id JOIN plans p ON p.id = v.plan_id WHERE p.key = 'pro' AND v.version = 1 AND c.feature_key = 'projects.max_count'");
        Assert.Equal("30", v1Rows);
        Assert.NotEqual(fingerprintBefore, await Sql.ScalarAsync<string>(connection, CatalogFingerprintQuery));
    }

    // ---- constraints ----

    [Fact]
    public async Task PlanVersion_DuplicateVersionNumber_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var planId = await Sql.ScalarAsync<Guid>(connection, "SELECT id FROM plans WHERE key = 'pro'");

        // Act
        var act = () => InsertDraftVersionAsync(connection, planId, version: 1);

        // Assert
        await Sql.AssertSqlStateAsync(UniqueViolation, act);
    }

    [Fact]
    public async Task PlanVersion_PublishedWithoutTimestamp_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));

        // Act
        var act = () => Sql.ExecuteAsync(connection, "UPDATE plan_versions SET status = 'Published' WHERE id = @id", ("id", version));

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Theory]
    [InlineData(true, 3, false)]
    [InlineData(null, 3, true)]
    [InlineData(null, null, false)]
    [InlineData(true, null, true)]
    public async Task PlanCapability_NotExactlyOneValue_IsRejected(bool? boolValue, int? limit, bool unlimited)
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));

        // Act
        var act = () => InsertCapabilityAsync(connection, version, FeatureKeys.ProjectsMaxCount, boolValue, limit, unlimited);

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Fact]
    public async Task PlanCapability_NegativeLimit_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));

        // Act
        var act = () => InsertCapabilityAsync(connection, version, FeatureKeys.ProjectsMaxCount, null, -1);

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Fact]
    public async Task PlanCapability_UnlimitedLimit_IsAccepted()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));

        // Act
        await InsertCapabilityAsync(connection, version, FeatureKeys.ProjectsMaxCount, null, null, unlimited: true);

        // Assert
        Assert.Equal(1, await Sql.ScalarAsync<long>(connection, "SELECT count(*) FROM plan_capabilities WHERE plan_version_id = @id AND is_unlimited", ("id", version)));
    }

    [Fact]
    public async Task FeatureDefinition_LimitWithoutUnit_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var act = () => Sql.ExecuteAsync(
            connection,
            "INSERT INTO feature_definitions (key, value_type, unit, description) VALUES ('x.limit', 'Limit', NULL, 'x')");

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Fact]
    public async Task FeatureDefinition_BooleanWithUnit_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var act = () => Sql.ExecuteAsync(
            connection,
            "INSERT INTO feature_definitions (key, value_type, unit, description) VALUES ('x.flag', 'Boolean', 'Seats', 'x')");

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Theory]
    [InlineData("Month", UnknownCurrency, 1000)]
    [InlineData("Week", "USD", 1000)]
    [InlineData("Month", "USD", 0)]
    public async Task PlanPrice_InvalidIntervalCurrencyOrAmount_IsRejected(string interval, string currency, int amount)
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));

        // Act
        var act = () => InsertPriceAsync(connection, version, interval, currency, amount);

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Fact]
    public async Task PlanPrice_DuplicateIntervalAndCurrency_IsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));
        await InsertPriceAsync(connection, version);

        // Act
        var act = () => InsertPriceAsync(connection, version);

        // Assert
        await Sql.AssertSqlStateAsync(UniqueViolation, act);
    }

    [Fact]
    public async Task PlanPrice_MonthlyAndAnnualForSameVersion_ShareOneCapabilityPackage()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));
        await InsertCapabilityAsync(connection, version, FeatureKeys.ProjectsMaxCount, null, 5);

        // Act
        await InsertPriceAsync(connection, version, "Month", amount: 1000);
        await InsertPriceAsync(connection, version, "Year", amount: 10000);

        // Assert
        Assert.Equal(1, await Sql.ScalarAsync<long>(connection, "SELECT count(*) FROM plan_capabilities WHERE plan_version_id = @id", ("id", version)));
        Assert.Equal(2, await Sql.ScalarAsync<long>(connection, "SELECT count(*) FROM plan_prices WHERE plan_version_id = @id", ("id", version)));
    }

    [Fact]
    public async Task PlanPrice_ExternalPriceIdNullOrSet_IsAcceptedButEmptyIsRejected()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var version = await InsertDraftVersionAsync(connection, await InsertPlanAsync(connection, DraftPlanKey, true));
        await InsertPriceAsync(connection, version, "Month", externalId: null);
        await InsertPriceAsync(connection, version, "Year", externalId: ProviderPriceId);

        // Act
        var act = () => InsertPriceAsync(connection, version, "Month", currency: "EUR", externalId: "");

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    // ---- catalog queries ----

    [Fact]
    public async Task Seed_FreePlan_HasNoPricesAndNoProviderColumnsAnywhere()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var freePrices = await Sql.ScalarAsync<long>(connection, FreePriceCountQuery);
        var providerColumns = await Sql.ColumnAsync(
            connection,
            // The only "subscription" column allowed is the local foreign key of the subscription projection (ADR 0012).
            """
            SELECT table_name || '.' || column_name FROM information_schema.columns
            WHERE table_schema = 'public'
              AND (column_name LIKE '%stripe%' OR column_name LIKE '%customer%' OR column_name LIKE '%subscription%')
              AND (table_name, column_name) <> ('subscription_changes', 'subscription_id')
            """);

        // Assert
        Assert.Equal(0, freePrices);
        Assert.Empty(providerColumns);
    }

    [Fact]
    public async Task GetPublishedPlans_SeededCatalog_ReturnsEveryPlanWithCapabilitiesAndPrices()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        var reader = new CatalogReader(context);

        // Act
        var plans = await reader.GetPublishedPlansAsync(CancellationToken.None);

        // Assert
        Assert.Equal([PlanKeys.Enterprise, PlanKeys.Free, PlanKeys.Max, PlanKeys.Pro], plans.Select(p => p.PlanKey));
        Assert.All(plans, plan => Assert.Equal(1, plan.Version));
        var free = plans.Single(p => p.PlanKey == PlanKeys.Free);
        Assert.False(free.IsBillable);
        Assert.Empty(free.Prices);
        Assert.DoesNotContain(free.Capabilities, c => c.Key == FeatureKeys.ReportsMonthlyLimit);
        Assert.Equal(3, Limit(free, FeatureKeys.ProjectsMaxCount));
        Assert.Equal(AccountType.Organization, plans.Single(p => p.PlanKey == PlanKeys.Enterprise).AccountType);
    }

    [Theory]
    [InlineData(PlanKeys.Pro, 30, 100, false)]
    [InlineData(PlanKeys.Max, 300, 1000, true)]
    [InlineData(PlanKeys.Enterprise, 500, 2000, true)]
    public async Task GetPublishedPlans_PaidPlans_ReturnTheDocumentedLimits(string planKey, int projects, int reports, bool advanced)
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        var reader = new CatalogReader(context);

        // Act
        var plan = (await reader.GetPublishedPlansAsync(CancellationToken.None)).Single(p => p.PlanKey == planKey);

        // Assert
        Assert.Equal(projects, Limit(plan, FeatureKeys.ProjectsMaxCount));
        Assert.Equal(reports, Limit(plan, FeatureKeys.ReportsMonthlyLimit));
        Assert.Equal(advanced, plan.Capabilities.Single(c => c.Key == FeatureKeys.ReportsAdvanced).BoolValue);
        Assert.Equal(FeatureUnit.Reports, plan.Capabilities.Single(c => c.Key == FeatureKeys.ReportsMonthlyLimit).Unit);
    }

    [Fact]
    public async Task GetPublishedPlans_PaidPlans_ReturnMonthlyPricesWithoutProviderIds()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        var reader = new CatalogReader(context);

        // Act
        var plans = await reader.GetPublishedPlansAsync(CancellationToken.None);

        // Assert
        var pro = Assert.Single(plans.Single(p => p.PlanKey == PlanKeys.Pro).Prices);
        Assert.Equal(new PriceEntry(BillingInterval.Month, "USD", 1000, null), pro);
        Assert.Equal(3000, Assert.Single(plans.Single(p => p.PlanKey == PlanKeys.Max).Prices).UnitAmountMinor);
        Assert.Equal(20000, Assert.Single(plans.Single(p => p.PlanKey == PlanKeys.Enterprise).Prices).UnitAmountMinor);
    }

    [Fact]
    public async Task GetPublishedPlans_DraftVersionExists_ReturnsOnlyPublishedVersion()
    {
        // Arrange
        var database = await CreateSeededDatabaseAsync();
        await using (var connection = await Sql.OpenAsync(database.ConnectionString))
        {
            var planId = await Sql.ScalarAsync<Guid>(connection, "SELECT id FROM plans WHERE key = 'pro'");
            var draft = await InsertDraftVersionAsync(connection, planId, version: 2);
            await InsertCapabilityAsync(connection, draft, FeatureKeys.ProjectsMaxCount, null, 99);
        }

        await using var context = CreateContext(database.ConnectionString);
        var reader = new CatalogReader(context);

        // Act
        var pro = (await reader.GetPublishedPlansAsync(CancellationToken.None)).Single(p => p.PlanKey == PlanKeys.Pro);

        // Assert
        Assert.Equal(1, pro.Version);
        Assert.Equal(30, Limit(pro, FeatureKeys.ProjectsMaxCount));
    }

    private static int? Limit(PlanCatalogEntry plan, string key) => plan.Capabilities.Single(c => c.Key == key).LimitValue;

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
