using Microsoft.EntityFrameworkCore;
using Npgsql;
using Saas.Subscription.Sample.Application.Entitlements;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;
using Saas.Subscription.Sample.Infrastructure.Persistence;
using Saas.Subscription.Sample.Infrastructure.Persistence.Subscriptions;
using Saas.Subscription.Sample.IntegrationTests.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Entitlements;

/// <summary>
/// What <see cref="EntitlementReader"/> loads from a real, seeded PostgreSQL database, and the effective boundaries
/// the policy draws from that persisted state.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public class EntitlementReaderTests(PostgresFixture postgres)
{
    private const string AnyEmail = "a@x.com";
    private const string PersonalType = nameof(AccountType.Personal);
    private const string PastDue = nameof(SubscriptionStatus.PastDue);
    private const string Ended = nameof(SubscriptionStatus.Ended);
    private const string PlanChange = nameof(SubscriptionChangeKind.PlanChange);
    private const int SeededVersion = 1;
    private const int NewerVersion = 2;
    private const int NewerProjectLimit = 5;

    private static readonly DateTimeOffset PeriodStart = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MidPeriod = new(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GraceEnd = new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] SeededFreeKeys = [FeatureKeys.ProjectsMaxCount, FeatureKeys.ReportsAdvanced, FeatureKeys.ReportsBasic];

    private sealed record World(string ConnectionString, Guid AccountId, NpgsqlConnection Connection) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }

    private async Task<World> CreatePersonalAccountAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        await DatabaseMigrator.SeedCatalogAsync(database.ConnectionString);
        var connection = await Sql.OpenAsync(database.ConnectionString);

        var userId = Sql.NewId();
        var accountId = Sql.NewId();
        await using var transaction = await connection.BeginTransactionAsync();
        await Sql.InsertUserAsync(connection, userId, AnyEmail, transaction: transaction);
        await Sql.InsertAccountAsync(connection, accountId, PersonalType, userId, transaction: transaction);
        await Sql.InsertMembershipAsync(connection, accountId, userId, transaction: transaction);
        await transaction.CommitAsync();

        return new World(database.ConnectionString, accountId, connection);
    }

    private static async Task<EntitlementFacts> ReadAsync(string connectionString, Guid accountId)
    {
        await using var context = new AppDbContext(
            AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString).Options);
        return await new EntitlementReader(context).ReadAsync(accountId, CancellationToken.None);
    }

    private static Task<EntitlementFacts> ReadAsync(World world) => ReadAsync(world.ConnectionString, world.AccountId);

    private static async Task<Guid> InsertSubscriptionAsync(World world, string planKey, string status = "Active", DateTimeOffset? accessUntil = null) =>
        await Sql.InsertSubscriptionAsync(
            world.Connection, world.AccountId, await Sql.PlanVersionIdAsync(world.Connection, planKey), PeriodStart, PeriodEnd, status, accessUntil);

    /// <summary>Publishes version 2 of a seeded plan with a single project limit.</summary>
    private static async Task PublishNewerVersionAsync(World world, string planKey, int projectLimit)
    {
        var id = Sql.NewId();
        await Sql.ExecuteAsync(
            world.Connection,
            "INSERT INTO plan_versions (id, plan_id, version, status, created_at) SELECT @id, id, @version, 'Draft', now() FROM plans WHERE key = @key",
            ("id", id), ("version", NewerVersion), ("key", planKey));
        await Sql.ExecuteAsync(
            world.Connection,
            "INSERT INTO plan_capabilities (plan_version_id, feature_key, limit_value, is_unlimited) VALUES (@id, @feature, @limit, false)",
            ("id", id), ("feature", FeatureKeys.ProjectsMaxCount), ("limit", projectLimit));
        await Sql.ExecuteAsync(world.Connection, "UPDATE plan_versions SET status = 'Published', published_at = now() WHERE id = @id", ("id", id));
    }

    [Fact]
    public async Task ReadAsync_NoPublishedFreeVersion_ThrowsInsteadOfGrantingAnything()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();

        // Act
        var act = () => ReadAsync(database.ConnectionString, Sql.NewId());

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ReadAsync_AccountWithoutSubscription_ReturnsTheSeededFreeVersion()
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();

        // Act
        var facts = await ReadAsync(world);

        // Assert
        Assert.Null(facts.Subscription);
        Assert.Equal(PlanKeys.Free, facts.Free.PlanKey);
        Assert.Equal(SeededVersion, facts.Free.Version);
        Assert.Equal(SeededFreeKeys, facts.Free.Capabilities.Select(capability => capability.Key));
    }

    [Fact]
    public async Task ReadAsync_NewerFreeVersionPublished_ReturnsTheLatest()
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();
        await PublishNewerVersionAsync(world, PlanKeys.Free, NewerProjectLimit);

        // Act
        var facts = await ReadAsync(world);

        // Assert
        Assert.Equal(NewerVersion, facts.Free.Version);
        Assert.Equal(NewerProjectLimit, Assert.Single(facts.Free.Capabilities).LimitValue);
    }

    [Fact]
    public async Task ReadAsync_NewerVersionOfThePurchasedPlanPublished_KeepsThePurchasedVersion()
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();
        await InsertSubscriptionAsync(world, PlanKeys.Pro);
        await PublishNewerVersionAsync(world, PlanKeys.Pro, NewerProjectLimit);

        // Act
        var facts = await ReadAsync(world);

        // Assert
        Assert.Equal(PlanKeys.Pro, facts.Subscription!.Plan.PlanKey);
        Assert.Equal(SeededVersion, facts.Subscription.Plan.Version);
    }

    [Fact]
    public async Task ReadAsync_OnlyAnEndedSubscription_ReturnsNoSubscription()
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();
        var pro = await Sql.PlanVersionIdAsync(world.Connection, PlanKeys.Pro);
        await Sql.InsertSubscriptionAsync(world.Connection, world.AccountId, pro, PeriodStart, PeriodEnd, Ended, endedAt: PeriodEnd);

        // Act
        var facts = await ReadAsync(world);

        // Assert
        Assert.Null(facts.Subscription);
    }

    [Fact]
    public async Task ReadAsync_PendingDowngrade_IsReadWithItsTarget()
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();
        var subscriptionId = await InsertSubscriptionAsync(world, PlanKeys.Max);
        var pro = await Sql.PlanVersionIdAsync(world.Connection, PlanKeys.Pro);
        await Sql.InsertSubscriptionChangeAsync(world.Connection, subscriptionId, world.AccountId, PlanChange, pro, MidPeriod);

        // Act
        var facts = await ReadAsync(world);

        // Assert
        var change = facts.Subscription!.PendingChange!;
        Assert.Equal(SubscriptionChangeKind.PlanChange, change.Kind);
        Assert.Equal(MidPeriod, change.EffectiveAt);
        Assert.Equal(PlanKeys.Pro, change.Target!.PlanKey);
    }

    [Theory]
    [InlineData("Applied")]
    [InlineData("Cancelled")]
    public async Task ReadAsync_ResolvedChange_IsNotPending(string status)
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();
        var subscriptionId = await InsertSubscriptionAsync(world, PlanKeys.Max);
        var pro = await Sql.PlanVersionIdAsync(world.Connection, PlanKeys.Pro);
        await Sql.InsertSubscriptionChangeAsync(world.Connection, subscriptionId, world.AccountId, PlanChange, pro, MidPeriod, status);

        // Act
        var facts = await ReadAsync(world);

        // Assert
        Assert.Null(facts.Subscription!.PendingChange);
    }

    [Theory]
    [InlineData(-1, EntitlementState.Grace, PlanKeys.Pro)]
    [InlineData(0, EntitlementState.Restricted, PlanKeys.Free)]
    public async Task Resolve_PersistedPastDueSubscriptionAroundTheGraceDeadline_SwitchesAtTheDeadline(
        int secondsFromGraceEnd,
        EntitlementState expectedState,
        string expectedPlan)
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();
        await InsertSubscriptionAsync(world, PlanKeys.Pro, PastDue, GraceEnd);
        var facts = await ReadAsync(world);

        // Act
        var snapshot = EntitlementPolicy.Resolve(world.AccountId, facts, GraceEnd.AddSeconds(secondsFromGraceEnd));

        // Assert
        Assert.Equal(expectedState, snapshot.State);
        Assert.Equal(expectedPlan, snapshot.Plan!.PlanKey);
    }

    [Theory]
    [InlineData(-1, PlanKeys.Max)]
    [InlineData(0, PlanKeys.Pro)]
    public async Task Resolve_PersistedDowngradeNobodyAppliedAroundItsInstant_AppliesFromTheInstant(int secondsFromChange, string expectedPlan)
    {
        // Arrange
        await using var world = await CreatePersonalAccountAsync();
        var subscriptionId = await InsertSubscriptionAsync(world, PlanKeys.Max);
        var pro = await Sql.PlanVersionIdAsync(world.Connection, PlanKeys.Pro);
        await Sql.InsertSubscriptionChangeAsync(world.Connection, subscriptionId, world.AccountId, PlanChange, pro, MidPeriod);
        var facts = await ReadAsync(world);

        // Act
        var snapshot = EntitlementPolicy.Resolve(world.AccountId, facts, MidPeriod.AddSeconds(secondsFromChange));

        // Assert
        Assert.Equal(expectedPlan, snapshot.Plan!.PlanKey);
    }
}
