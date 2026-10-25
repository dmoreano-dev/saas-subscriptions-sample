using System.Net;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.IntegrationTests.Identity;
using Saas.Subscription.Sample.IntegrationTests.Persistence;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Authorization;

/// <summary>A way Ana, with her own valid token, tries to reach something of Bob's.</summary>
public enum ForeignRequest
{
    ListProjectsOfBobsAccount,
    GetBobsProjectThroughBobsAccount,
    GetBobsProjectThroughHerOwnAccount,
    DeleteBobsProjectThroughBobsAccount,
    DeleteBobsProjectThroughHerOwnAccount,
    CreateProjectInBobsAccount,
    GetCapabilitiesOfBobsAccount,
    ListProjectsOfAnAccountThatDoesNotExist,
}

/// <summary>
/// Broken object level authorization (BOLA): two users, two accounts, real PostgreSQL. A valid token identifies
/// the caller and nothing else; ids in the URL only select, and every foreign id is answered like an unknown one.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public class AccountIsolationTests(PostgresFixture postgres)
{
    private static (HttpMethod Method, string Route) Build(ForeignRequest request, TwoAccounts world) => request switch
    {
        ForeignRequest.ListProjectsOfBobsAccount => (HttpMethod.Get, TwoAccounts.ProjectsRoute(world.Bob.AccountId)),
        ForeignRequest.GetBobsProjectThroughBobsAccount => (HttpMethod.Get, TwoAccounts.ProjectRoute(world.Bob.AccountId, world.BobProjectId)),
        ForeignRequest.GetBobsProjectThroughHerOwnAccount => (HttpMethod.Get, TwoAccounts.ProjectRoute(world.Ana.AccountId, world.BobProjectId)),
        ForeignRequest.DeleteBobsProjectThroughBobsAccount => (HttpMethod.Delete, TwoAccounts.ProjectRoute(world.Bob.AccountId, world.BobProjectId)),
        ForeignRequest.DeleteBobsProjectThroughHerOwnAccount => (HttpMethod.Delete, TwoAccounts.ProjectRoute(world.Ana.AccountId, world.BobProjectId)),
        ForeignRequest.CreateProjectInBobsAccount => (HttpMethod.Post, TwoAccounts.ProjectsRoute(world.Bob.AccountId)),
        ForeignRequest.GetCapabilitiesOfBobsAccount => (HttpMethod.Get, TwoAccounts.CapabilitiesRoute(world.Bob.AccountId)),
        _ => (HttpMethod.Get, TwoAccounts.ProjectsRoute(Sql.NewId())),
    };

    [Theory]
    [InlineData(ForeignRequest.ListProjectsOfBobsAccount)]
    [InlineData(ForeignRequest.GetBobsProjectThroughBobsAccount)]
    [InlineData(ForeignRequest.GetBobsProjectThroughHerOwnAccount)]
    [InlineData(ForeignRequest.DeleteBobsProjectThroughBobsAccount)]
    [InlineData(ForeignRequest.DeleteBobsProjectThroughHerOwnAccount)]
    [InlineData(ForeignRequest.CreateProjectInBobsAccount)]
    [InlineData(ForeignRequest.GetCapabilitiesOfBobsAccount)]
    [InlineData(ForeignRequest.ListProjectsOfAnAccountThatDoesNotExist)]
    public async Task Send_ForeignOrUnknownResourceWithOwnValidToken_ReturnsNotFoundProblemWithoutBobsData(ForeignRequest foreign)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        var (method, route) = Build(foreign, world);

        // Act
        using var response = await world.SendAsync(method, route, world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
        Assert.DoesNotContain(TwoAccounts.BobProjectName, problem.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ForeignRequest.DeleteBobsProjectThroughBobsAccount)]
    [InlineData(ForeignRequest.DeleteBobsProjectThroughHerOwnAccount)]
    public async Task Delete_BobsProjectWithAnasToken_LeavesTheProjectInPlace(ForeignRequest foreign)
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);
        var (method, route) = Build(foreign, world);

        // Act
        using var response = await world.SendAsync(method, route, world.Ana.Token);

        // Assert
        Assert.Equal(1L, await world.CountProjectsAsync(world.BobProjectId));
    }

    [Fact]
    public async Task List_OwnAccount_ReturnsOnlyOwnProjects()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, TwoAccounts.ProjectsRoute(world.Ana.AccountId), world.Ana.Token);
        var items = (await AuthApi.ReadJsonAsync(response)).GetProperty("items").EnumerateArray().ToList();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(items);
        Assert.Equal(world.AnaProjectId, item.GetProperty("id").GetGuid());
        Assert.Equal(world.Ana.AccountId, item.GetProperty("accountId").GetGuid());
    }

    [Fact]
    public async Task Get_OwnProject_ReturnsIt()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, TwoAccounts.ProjectRoute(world.Ana.AccountId, world.AnaProjectId), world.Ana.Token);
        var body = await AuthApi.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TwoAccounts.AnaProjectName, body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Delete_OwnProject_RemovesOnlyThatProject()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);

        // Act
        using var response = await world.SendAsync(HttpMethod.Delete, TwoAccounts.ProjectRoute(world.Ana.AccountId, world.AnaProjectId), world.Ana.Token);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0L, await world.CountProjectsAsync(world.AnaProjectId));
        Assert.Equal(1L, await world.CountProjectsAsync(world.BobProjectId));
    }

    [Fact]
    public async Task ListAccounts_WithOwnToken_ReturnsOnlyTheCallersAccount()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);

        // Act
        using var response = await world.SendAsync(HttpMethod.Get, TwoAccounts.AccountsRoute, world.Ana.Token);
        var items = (await AuthApi.ReadJsonAsync(response)).GetProperty("items").EnumerateArray().ToList();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(world.Ana.AccountId, Assert.Single(items).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ProjectOfOwnAccount_ReachesTheNotImplementedHandler()
    {
        // Arrange
        await using var world = await TwoAccounts.CreateAsync(postgres);

        // Act
        using var response = await world.SendAsync(HttpMethod.Post, TwoAccounts.ProjectsRoute(world.Ana.AccountId), world.Ana.Token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        Assert.Equal(ProblemCodes.NotImplemented, problem.Code);
    }
}
