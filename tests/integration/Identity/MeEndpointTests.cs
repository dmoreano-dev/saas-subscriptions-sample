using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.IntegrationTests.Configuration;
using Saas.Subscription.Sample.IntegrationTests.Persistence;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

[Collection(PostgresTestGroup.Name)]
public class MeEndpointTests(PostgresFixture postgres)
{
    private const string DisabledStatus = "Disabled";

    [Fact]
    public async Task Get_WithTheTokenFromLogin_ReturnsTheCallersProfile()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var factory = new PostgresApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var token = await AuthApi.RegisterAndLoginAsync(client);

        // Act
        using var response = await AuthApi.GetMeAsync(client, token);
        var body = await AuthApi.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AuthApi.Email, body.GetProperty("email").GetString());
        Assert.Equal(["createdAt", "email", "id"], body.EnumerateObject().Select(member => member.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Get_WithATokenForAnotherUser_ReturnsOnlyThatUsersProfile()
    {
        // Arrange
        const string otherEmail = "bob@example.com";
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var factory = new PostgresApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client, otherEmail);
        var token = await AuthApi.RegisterAndLoginAsync(client);

        // Act
        using var response = await AuthApi.GetMeAsync(client, token);
        var body = await AuthApi.ReadJsonAsync(response);

        // Assert
        Assert.Equal(AuthApi.Email, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Get_WithATokenWhoseUserWasDisabled_ReturnsUnauthenticatedProblem()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var factory = new PostgresApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var token = await AuthApi.RegisterAndLoginAsync(client);
        await AuthApi.SetUserStatusAsync(database.ConnectionString, DisabledStatus);

        // Act
        using var response = await AuthApi.GetMeAsync(client, token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.Unauthenticated, problem.Code);
    }

    [Fact]
    public async Task Get_WithAValidTokenWhoseUserDoesNotExist_ReturnsUnauthenticatedProblem()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var factory = new PostgresApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client);
        var token = factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(Guid.CreateVersion7()).Value;

        // Act
        using var response = await AuthApi.GetMeAsync(client, token);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.Unauthenticated, problem.Code);
    }

    [Fact]
    public async Task Get_AfterTheHostRestartsWithTheSameSigningKey_StillAcceptsTheToken()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var signingKey = TestSigningKey.NewPem();
        string token;
        await using (var firstHost = new PostgresApiFactory(database.ConnectionString, signingKey))
        {
            using var firstClient = firstHost.CreateClient();
            token = await AuthApi.RegisterAndLoginAsync(firstClient);
        }

        await using var restartedHost = new PostgresApiFactory(database.ConnectionString, signingKey);
        using var client = restartedHost.CreateClient();

        // Act
        using var response = await AuthApi.GetMeAsync(client, token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_AfterTheHostRestartsWithANewSigningKey_RejectsTheToken()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        string token;
        await using (var firstHost = new PostgresApiFactory(database.ConnectionString, TestSigningKey.NewPem()))
        {
            using var firstClient = firstHost.CreateClient();
            token = await AuthApi.RegisterAndLoginAsync(firstClient);
        }

        await using var restartedHost = new PostgresApiFactory(database.ConnectionString, TestSigningKey.NewPem());
        using var client = restartedHost.CreateClient();

        // Act
        using var response = await AuthApi.GetMeAsync(client, token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
