using System.Net;
using System.Text.Json;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;
using Saas.Subscription.Sample.IntegrationTests.Authorization;
using Saas.Subscription.Sample.IntegrationTests.Identity;
using Saas.Subscription.Sample.IntegrationTests.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Entitlements;

/// <summary>
/// <c>GET /api/accounts/{id}/capabilities</c> through the real API over real PostgreSQL. Every token used here was
/// issued before the subscription rows were written, so each answer also shows that the decision comes from the
/// database on every request and never from the token.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public class CapabilitiesEndpointTests(PostgresFixture postgres)
{
    private const string PastDue = nameof(SubscriptionStatus.PastDue);
    private const string PlanChange = nameof(SubscriptionChangeKind.PlanChange);
    private const string LimitKind = "limit";
    private const string FeatureKind = "feature";
    private const int SeededVersion = 1;
    private const int FreeProjectLimit = 3;
    private static readonly string[] FreeCapabilityKeys = [FeatureKeys.ProjectsMaxCount, FeatureKeys.ReportsAdvanced, FeatureKeys.ReportsBasic];

    // Whole days keep the values identical after a round trip through PostgreSQL (microsecond precision).
    private static readonly DateTimeOffset Today = new(DateTime.UtcNow.Date, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = Today.AddDays(-1);
    private static readonly DateTimeOffset ChangeAt = Today.AddDays(10);
    private static readonly DateTimeOffset PeriodEnd = Today.AddDays(29);

    private static async Task<JsonElement> GetCapabilitiesAsync(TwoAccounts world, string? token = null)
    {
        using var response = await world.SendAsync(HttpMethod.Get, TwoAccounts.CapabilitiesRoute(world.Ana.AccountId), token ?? world.Ana.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await AuthApi.ReadJsonAsync(response);
    }

    private static string? PlanKeyOf(JsonElement body) => body.GetProperty("plan").GetProperty("key").GetString();

    private static JsonElement CapabilityOf(JsonElement body, string key) =>
        body.GetProperty("capabilities").EnumerateArray().Single(capability => capability.GetProperty("key").GetString() == key);

    private static async Task<Guid> InsertSubscriptionAsync(TwoAccounts world, Guid accountId, string planKey)
    {
        await using var connection = await world.OpenAsync();
        return await Sql.InsertSubscriptionAsync(connection, accountId, await Sql.PlanVersionIdAsync(connection, planKey), PeriodStart, PeriodEnd);
    }

    [Fact]
    public async Task Get_OwnAccountWithoutSubscription_ReturnsTheFreePlanVersionWithoutBoundary()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);

        // Act
        var body = await GetCapabilitiesAsync(world);

        // Assert
        Assert.Equal(world.Ana.AccountId, body.GetProperty("accountId").GetGuid());
        Assert.Equal(PlanKeys.Free, PlanKeyOf(body));
        Assert.Equal(SeededVersion, body.GetProperty("plan").GetProperty("version").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("validUntil").ValueKind);
    }

    [Fact]
    public async Task Get_OwnAccountWithoutSubscription_ListsOnlyTheCapabilitiesTheFreePlanDeclares()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);

        // Act
        var body = await GetCapabilitiesAsync(world);

        // Assert
        Assert.Equal(FreeCapabilityKeys, body.GetProperty("capabilities").EnumerateArray().Select(capability => capability.GetProperty("key").GetString()));
        var projects = CapabilityOf(body, FeatureKeys.ProjectsMaxCount);
        Assert.Equal(LimitKind, projects.GetProperty("kind").GetString());
        Assert.True(projects.GetProperty("granted").GetBoolean());
        Assert.False(projects.GetProperty("limit").GetProperty("isUnlimited").GetBoolean());
        Assert.Equal(FreeProjectLimit, projects.GetProperty("limit").GetProperty("value").GetInt32());
        var basicReports = CapabilityOf(body, FeatureKeys.ReportsBasic);
        Assert.Equal(FeatureKind, basicReports.GetProperty("kind").GetString());
        Assert.False(basicReports.GetProperty("granted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, basicReports.GetProperty("limit").ValueKind);
    }

    [Fact]
    public async Task Get_WithATokenClaimingTheMaxPlan_ReturnsTheFreePlanFromTheDatabase()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        var claims = new Dictionary<string, object> { ["plan"] = PlanKeys.Max, ["capabilities"] = new[] { FeatureKeys.ReportsAdvanced } };
        var token = TokenForge.CreateWithExtraClaims(world.TokenOptions, DateTimeOffset.UtcNow, world.Ana.UserId, claims);

        // Act
        var body = await GetCapabilitiesAsync(world, token);

        // Assert
        Assert.Equal(PlanKeys.Free, PlanKeyOf(body));
        Assert.False(CapabilityOf(body, FeatureKeys.ReportsAdvanced).GetProperty("granted").GetBoolean());
    }

    [Fact]
    public async Task Get_ActivePaidSubscription_ReturnsItsPlanValidUntilTheDeadline()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await InsertSubscriptionAsync(world, world.Ana.AccountId, PlanKeys.Pro);

        // Act
        var body = await GetCapabilitiesAsync(world);

        // Assert
        Assert.Equal(PlanKeys.Pro, PlanKeyOf(body));
        Assert.Equal(PeriodEnd, body.GetProperty("validUntil").GetDateTimeOffset());
        Assert.True(CapabilityOf(body, FeatureKeys.ReportsBasic).GetProperty("granted").GetBoolean());
    }

    [Fact]
    public async Task Get_PastDueSubscriptionAfterItsGrace_ReturnsTheFreePlan()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await using (var connection = await world.OpenAsync())
        {
            var pro = await Sql.PlanVersionIdAsync(connection, PlanKeys.Pro);
            await Sql.InsertSubscriptionAsync(connection, world.Ana.AccountId, pro, Today.AddDays(-40), Today.AddDays(-10), PastDue, Today.AddDays(-7));
        }

        // Act
        var body = await GetCapabilitiesAsync(world);

        // Assert
        Assert.Equal(PlanKeys.Free, PlanKeyOf(body));
        Assert.False(CapabilityOf(body, FeatureKeys.ReportsBasic).GetProperty("granted").GetBoolean());
    }

    [Fact]
    public async Task Get_DowngradeScheduledBeforeTheDeadline_IsValidOnlyUntilTheDowngrade()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        var subscriptionId = await InsertSubscriptionAsync(world, world.Ana.AccountId, PlanKeys.Max);
        await using (var connection = await world.OpenAsync())
        {
            var pro = await Sql.PlanVersionIdAsync(connection, PlanKeys.Pro);
            await Sql.InsertSubscriptionChangeAsync(connection, subscriptionId, world.Ana.AccountId, PlanChange, pro, ChangeAt);
        }

        // Act
        var body = await GetCapabilitiesAsync(world);

        // Assert
        Assert.Equal(PlanKeys.Max, PlanKeyOf(body));
        Assert.Equal(ChangeAt, body.GetProperty("validUntil").GetDateTimeOffset());
    }

    [Fact]
    public async Task Get_OwnAccountWhileBobHasMax_IsNotAffectedByBobsSubscription()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await InsertSubscriptionAsync(world, world.Bob.AccountId, PlanKeys.Max);

        // Act
        var body = await GetCapabilitiesAsync(world);

        // Assert
        Assert.Equal(PlanKeys.Free, PlanKeyOf(body));
    }
}
