using Npgsql;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

/// <summary>The subscription projection's invariants, enforced by PostgreSQL itself so no writer can bypass them.</summary>
[Collection(PostgresTestGroup.Name)]
public class SubscriptionSchemaTests(PostgresFixture postgres)
{
    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";
    private const string CheckViolation = "23514";
    private const string NotPublishedState = "55000";
    private const int DraftVersion = 2;
    private const string Active = nameof(SubscriptionStatus.Active);
    private const string Ended = nameof(SubscriptionStatus.Ended);
    private const string PlanChange = nameof(SubscriptionChangeKind.PlanChange);
    private const string Cancel = nameof(SubscriptionChangeKind.Cancel);
    private const string Applied = nameof(SubscriptionChangeStatus.Applied);
    private const string UnknownStatus = "Paused";
    private const string OrganizationType = nameof(AccountType.Organization);

    private static readonly DateTimeOffset PeriodStart = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    private async Task<NpgsqlConnection> OpenSeededConnectionAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        await DatabaseMigrator.SeedCatalogAsync(database.ConnectionString);
        return await Sql.OpenAsync(database.ConnectionString);
    }

    private static async Task<Guid> InsertAccountAsync(NpgsqlConnection connection)
    {
        var id = Sql.NewId();
        await Sql.InsertAccountAsync(connection, id, OrganizationType, personalOwnerUserId: null);
        return id;
    }

    private static async Task<Guid> InsertDraftProVersionAsync(NpgsqlConnection connection)
    {
        var id = Sql.NewId();
        await Sql.ExecuteAsync(
            connection,
            "INSERT INTO plan_versions (id, plan_id, version, status, created_at) SELECT @id, id, @version, 'Draft', now() FROM plans WHERE key = 'pro'",
            ("id", id), ("version", DraftVersion));
        return id;
    }

    private static async Task<(Guid AccountId, Guid SubscriptionId)> InsertProSubscriptionAsync(NpgsqlConnection connection)
    {
        var accountId = await InsertAccountAsync(connection);
        var pro = await Sql.PlanVersionIdAsync(connection, PlanKeys.Pro);
        return (accountId, await Sql.InsertSubscriptionAsync(connection, accountId, pro, PeriodStart, PeriodEnd));
    }

    [Fact]
    public async Task InsertSubscription_SecondCurrentOneForTheSameAccount_ThrowsUniqueViolation()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var (accountId, _) = await InsertProSubscriptionAsync(connection);
        var max = await Sql.PlanVersionIdAsync(connection, PlanKeys.Max);

        // Act
        var act = () => Sql.InsertSubscriptionAsync(connection, accountId, max, PeriodStart, PeriodEnd);

        // Assert
        await Sql.AssertSqlStateAsync(UniqueViolation, act);
    }

    [Fact]
    public async Task InsertSubscription_AfterThePreviousOneEnded_IsAccepted()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var accountId = await InsertAccountAsync(connection);
        var pro = await Sql.PlanVersionIdAsync(connection, PlanKeys.Pro);
        await Sql.InsertSubscriptionAsync(connection, accountId, pro, PeriodStart, PeriodEnd, Ended, endedAt: PeriodEnd);

        // Act
        await Sql.InsertSubscriptionAsync(connection, accountId, pro, PeriodEnd, PeriodEnd.AddMonths(1));

        // Assert
        Assert.Equal(2L, await Sql.ScalarAsync<long>(connection, "SELECT count(*) FROM subscriptions WHERE account_id = @id", ("id", accountId)));
    }

    [Fact]
    public async Task InsertSubscription_ReferencingADraftVersion_IsRejected()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var accountId = await InsertAccountAsync(connection);
        var draft = await InsertDraftProVersionAsync(connection);

        // Act
        var act = () => Sql.InsertSubscriptionAsync(connection, accountId, draft, PeriodStart, PeriodEnd);

        // Assert
        await Sql.AssertSqlStateAsync(NotPublishedState, act);
    }

    [Fact]
    public async Task UpdateSubscription_MovingToADraftVersion_IsRejected()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var (_, subscriptionId) = await InsertProSubscriptionAsync(connection);
        var draft = await InsertDraftProVersionAsync(connection);

        // Act
        var act = () => Sql.ExecuteAsync(connection, "UPDATE subscriptions SET plan_version_id = @draft WHERE id = @id", ("draft", draft), ("id", subscriptionId));

        // Assert
        await Sql.AssertSqlStateAsync(NotPublishedState, act);
    }

    public static TheoryData<string, DateTimeOffset?, DateTimeOffset> InconsistentSubscriptions => new()
    {
        { Active, PeriodEnd, PeriodEnd },
        { Ended, null, PeriodEnd },
        { Active, null, PeriodStart },
        { UnknownStatus, null, PeriodEnd },
    };

    [Theory]
    [MemberData(nameof(InconsistentSubscriptions))]
    public async Task InsertSubscription_InconsistentStatusEndOrDeadline_ThrowsCheckViolation(string status, DateTimeOffset? endedAt, DateTimeOffset accessUntil)
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var accountId = await InsertAccountAsync(connection);
        var pro = await Sql.PlanVersionIdAsync(connection, PlanKeys.Pro);

        // Act
        var act = () => Sql.InsertSubscriptionAsync(connection, accountId, pro, PeriodStart, PeriodEnd, status, accessUntil, endedAt);

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Fact]
    public async Task InsertSubscription_PeriodEndingAtItsStart_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var accountId = await InsertAccountAsync(connection);
        var pro = await Sql.PlanVersionIdAsync(connection, PlanKeys.Pro);

        // Act
        var act = () => Sql.InsertSubscriptionAsync(connection, accountId, pro, PeriodStart, PeriodStart, Active, PeriodEnd);

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }

    [Fact]
    public async Task InsertChange_ForTheSubscriptionOfAnotherAccount_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var (_, subscriptionId) = await InsertProSubscriptionAsync(connection);
        var otherAccountId = await InsertAccountAsync(connection);

        // Act
        var act = () => Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, otherAccountId, Cancel, null, PeriodEnd);

        // Assert
        await Sql.AssertSqlStateAsync(ForeignKeyViolation, act);
    }

    [Fact]
    public async Task InsertChange_SecondPendingChange_ThrowsUniqueViolation()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var (accountId, subscriptionId) = await InsertProSubscriptionAsync(connection);
        await Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, accountId, Cancel, null, PeriodEnd);

        // Act
        var act = () => Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, accountId, Cancel, null, PeriodEnd);

        // Assert
        await Sql.AssertSqlStateAsync(UniqueViolation, act);
    }

    [Fact]
    public async Task InsertChange_PendingAfterAnAppliedOne_IsAccepted()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var (accountId, subscriptionId) = await InsertProSubscriptionAsync(connection);
        await Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, accountId, Cancel, null, PeriodStart.AddDays(1), Applied);

        // Act
        await Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, accountId, Cancel, null, PeriodEnd);

        // Assert
        Assert.Equal(2L, await Sql.ScalarAsync<long>(connection, "SELECT count(*) FROM subscription_changes WHERE subscription_id = @id", ("id", subscriptionId)));
    }

    [Fact]
    public async Task InsertChange_TargetingADraftVersion_IsRejected()
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var (accountId, subscriptionId) = await InsertProSubscriptionAsync(connection);
        var draft = await InsertDraftProVersionAsync(connection);

        // Act
        var act = () => Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, accountId, PlanChange, draft, PeriodEnd);

        // Assert
        await Sql.AssertSqlStateAsync(NotPublishedState, act);
    }

    [Theory]
    [InlineData(PlanChange, false)]
    [InlineData(Cancel, true)]
    public async Task InsertChange_TargetNotMatchingTheKind_ThrowsCheckViolation(string kind, bool withTarget)
    {
        // Arrange
        await using var connection = await OpenSeededConnectionAsync();
        var (accountId, subscriptionId) = await InsertProSubscriptionAsync(connection);
        Guid? target = withTarget ? await Sql.PlanVersionIdAsync(connection, PlanKeys.Max) : null;

        // Act
        var act = () => Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, accountId, kind, target, PeriodEnd);

        // Assert
        await Sql.AssertSqlStateAsync(CheckViolation, act);
    }
}
