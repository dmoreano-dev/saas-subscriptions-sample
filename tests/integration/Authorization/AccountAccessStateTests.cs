using System.Net;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.IntegrationTests.Identity;
using Saas.Subscription.Sample.IntegrationTests.Persistence;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Authorization;

/// <summary>
/// The decision is taken from PostgreSQL on every request: a change of membership, account, role or user status
/// applies to the token the caller already holds, and nothing the token says beyond the user id is believed.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public class AccountAccessStateTests(PostgresFixture postgres)
{
    private const string ReadOnlyRole = "test_read_only";
    private const string AccountScopeName = nameof(PermissionScope.Account);

    private const string SuspendMembership = "UPDATE memberships SET status = 'Suspended' WHERE account_id = @account";
    private const string SetAccountStatus = "UPDATE accounts SET status = @status WHERE id = @account";
    private const string DisableUser = "UPDATE users SET status = 'Disabled' WHERE id = @user";
    private const string RemoveRoles = "DELETE FROM membership_roles WHERE membership_id IN (SELECT id FROM memberships WHERE account_id = @account)";
    private const string RestoreOwnerRole = """
        INSERT INTO membership_roles (membership_id, role_key, role_scope, created_at)
        SELECT id, 'personal_owner', 'Account', now() FROM memberships WHERE account_id = @account
        """;

    private static string OwnProject(TwoAccounts world) => TwoAccounts.ProjectRoute(world.Ana.AccountId, world.AnaProjectId);

    /// <summary>Replaces Ana's owner role in her account with a role that can only read projects.</summary>
    private static async Task MakeAnaReadOnlyAsync(TwoAccounts world)
    {
        await using var connection = await world.OpenAsync();
        await Sql.InsertRoleAsync(connection, ReadOnlyRole, AccountScopeName);
        await Sql.InsertRolePermissionAsync(connection, ReadOnlyRole, Permissions.ProjectsRead, AccountScopeName);
        await Sql.ExecuteAsync(
            connection,
            "UPDATE membership_roles SET role_key = @role WHERE membership_id IN (SELECT id FROM memberships WHERE account_id = @account)",
            ("role", ReadOnlyRole), ("account", world.Ana.AccountId));
    }

    [Fact]
    public async Task Get_OwnProjectAfterTheMembershipWasSuspended_ReturnsNotFoundProblem()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(SuspendMembership, ("account", world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, OwnProject(world), world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task Send_OwnProjectAfterTheAccountWasSuspended_ReturnsAccountSuspendedProblem(string method)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(SetAccountStatus, ("status", "Suspended"), ("account", world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(new HttpMethod(method), OwnProject(world), world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProblemCodes.AccountSuspended, problem.Code);
        Assert.Equal(1L, await world.CountProjectsAsync(world.AnaProjectId));
    }

    [Fact]
    public async Task Get_ProjectOfASuspendedForeignAccount_ReturnsNotFoundProblem()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(SetAccountStatus, ("status", "Suspended"), ("account", world.Bob.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, TwoAccounts.ProjectRoute(world.Bob.AccountId, world.BobProjectId), world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
    }

    [Fact]
    public async Task Get_OwnProjectAfterTheAccountWasClosed_ReturnsNotFoundProblem()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(SetAccountStatus, ("status", "Closed"), ("account", world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, OwnProject(world), world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
    }

    [Theory]
    [InlineData("Suspended", 1)]
    [InlineData("Closed", 0)]
    public async Task ListAccounts_AfterTheAccountStatusChanged_ListsSuspendedButNotClosedAccounts(string status, int expectedCount)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(SetAccountStatus, ("status", status), ("account", world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, TwoAccounts.AccountsRoute, world.Ana.Token);
        var items = (await AuthApi.ReadJsonAsync(response)).GetProperty("items").EnumerateArray().ToList();

        // Assert
        Assert.Equal(expectedCount, items.Count);
    }

    [Fact]
    public async Task ListAccounts_AfterTheMembershipWasSuspended_ListsNothing()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(SuspendMembership, ("account", world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, TwoAccounts.AccountsRoute, world.Ana.Token);
        var items = (await AuthApi.ReadJsonAsync(response)).GetProperty("items").EnumerateArray().ToList();

        // Assert
        Assert.Empty(items);
    }

    [Theory]
    [InlineData("/api/accounts")]
    [InlineData("/api/accounts/{0}/projects")]
    public async Task Get_AfterTheUserWasDisabled_ReturnsUnauthenticatedProblem(string routeTemplate)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        var route = string.Format(System.Globalization.CultureInfo.InvariantCulture, routeTemplate, world.Ana.AccountId);
        await world.ExecuteAsync(DisableUser, ("user", world.Ana.UserId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, route, world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.Unauthenticated, problem.Code);
    }

    [Fact]
    public async Task Get_OwnProjectAfterEveryRoleWasRemoved_ReturnsForbiddenProblem()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(RemoveRoles, ("account", world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, OwnProject(world), world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProblemCodes.Forbidden, problem.Code);
        Assert.DoesNotContain(TwoAccounts.AnaProjectName, problem.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_OwnProjectAfterTheRoleWasGrantedAgain_SucceedsWithTheSameToken()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await world.ExecuteAsync(RemoveRoles, ("account", world.Ana.AccountId));
        await world.ExecuteAsync(RestoreOwnerRole, ("account", world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, OwnProject(world), world.Ana.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", HttpStatusCode.OK)]
    [InlineData("DELETE", HttpStatusCode.Forbidden)]
    public async Task Send_OwnProjectWithARoleThatOnlyReads_AllowsReadingAndForbidsWriting(string method, HttpStatusCode expected)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await MakeAnaReadOnlyAsync(world);

        // Act
        using var response = await world.SendAsync(new HttpMethod(method), OwnProject(world), world.Ana.Token);

        // Assert
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(1L, await world.CountProjectsAsync(world.AnaProjectId));
    }

    [Fact]
    public async Task Delete_OwnProjectWithAReadOnlyRoleAndATokenClaimingMore_ReturnsForbiddenProblem()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await MakeAnaReadOnlyAsync(world);
        var token = TokenForge.CreateWithExtraClaims(world.TokenOptions, DateTimeOffset.UtcNow, world.Ana.UserId, InflatedClaims(world.Ana.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Delete, OwnProject(world), token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProblemCodes.Forbidden, problem.Code);
        Assert.Equal(1L, await world.CountProjectsAsync(world.AnaProjectId));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task Send_BobsProjectWithATokenClaimingBobsAccountPlanAndRoles_ReturnsNotFoundProblem(string method)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        var token = TokenForge.CreateWithExtraClaims(world.TokenOptions, DateTimeOffset.UtcNow, world.Ana.UserId, InflatedClaims(world.Bob.AccountId));

        // Act
        using var response = await world.SendAsync(new HttpMethod(method), TwoAccounts.ProjectRoute(world.Bob.AccountId, world.BobProjectId), token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
        Assert.Equal(1L, await world.CountProjectsAsync(world.BobProjectId));
    }

    [Fact]
    public async Task Get_OwnProjectWithATokenCarryingExtraClaims_StillSucceeds()
    {
        // Arrange: the control for the two tests above, proving the forged token itself is accepted.
        await using var world = await TwoAccounts.CreateAsync(postgres);
        var token = TokenForge.CreateWithExtraClaims(world.TokenOptions, DateTimeOffset.UtcNow, world.Ana.UserId, InflatedClaims(world.Bob.AccountId));

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, OwnProject(world), token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task Send_BobsProjectAsAPlatformOperatorWithoutMembership_ReturnsNotFoundProblem(string method)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        await using (var connection = await world.OpenAsync())
        {
            await Sql.InsertPlatformRoleAssignmentAsync(connection, world.Ana.UserId, SystemRoles.PlatformOperator);
        }

        // Act
        using var response = await world.SendAsync(new HttpMethod(method), TwoAccounts.ProjectRoute(world.Bob.AccountId, world.BobProjectId), world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
        Assert.Equal(1L, await world.CountProjectsAsync(world.BobProjectId));
    }

    /// <summary>Everything a token could say to look more powerful; the API reads none of it.</summary>
    private static Dictionary<string, object> InflatedClaims(Guid accountId) => new()
    {
        ["plan"] = PlanKeys.Max,
        ["role"] = SystemRoles.PersonalOwner,
        ["roles"] = new[] { SystemRoles.PersonalOwner, SystemRoles.PlatformOperator },
        ["permissions"] = new[] { Permissions.ProjectsRead, Permissions.ProjectsWrite },
        ["account_id"] = accountId.ToString(),
    };
}
