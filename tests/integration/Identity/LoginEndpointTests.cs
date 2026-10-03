using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.IntegrationTests.Configuration;
using Saas.Subscription.Sample.IntegrationTests.Persistence;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

[Collection(PostgresTestGroup.Name)]
public class LoginEndpointTests(PostgresFixture postgres)
{
    private const string BearerTokenType = "Bearer";
    private const string NoStore = "no-store";
    private const int AttemptsAllowed = 5;
    private const int WindowSeconds = 15 * 60;

    private static readonly string[] ExpectedClaims = ["aud", "exp", "iat", "iss", "jti", "nbf", "sub"];

    private async Task<PostgresApiFactory> CreateFactoryAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        return new PostgresApiFactory(database.ConnectionString);
    }

    [Fact]
    public async Task Post_ValidCredentials_ReturnsBearerTokenAndUserWithoutCaching()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client);

        // Act
        using var response = await AuthApi.LoginAsync(client);
        var body = await AuthApi.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(BearerTokenType, body.GetProperty("tokenType").GetString());
        Assert.Equal(AuthApi.Email, body.GetProperty("user").GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.DoesNotContain("password", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_ValidCredentials_IssuesTokenWithOnlyTheRegisteredClaims()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        await AuthApi.RegisterAsync(client);

        // Act
        using var response = await AuthApi.LoginAsync(client);
        var body = await AuthApi.ReadJsonAsync(response);
        var token = new JsonWebToken(body.GetProperty("accessToken").GetString());

        // Assert
        Assert.Equal(ExpectedClaims, token.Claims.Select(claim => claim.Type).Order(StringComparer.Ordinal));
        Assert.Equal(body.GetProperty("user").GetProperty("id").GetString(), token.Subject);
        Assert.Equal(options.Issuer, token.Issuer);
        Assert.Equal(options.Audience, Assert.Single(token.Audiences));
        Assert.Equal(options.AccessTokenLifetime, token.ValidTo - token.IssuedAt);
        Assert.Equal(body.GetProperty("expiresAt").GetDateTimeOffset().UtcDateTime, token.ValidTo);
        Assert.Equal("ES256", token.Alg);
        Assert.Equal(TestSigningKey.KeyId, token.Kid);
    }

    [Fact]
    public async Task Post_ValidCredentialsTwice_IssuesTokensWithDifferentIds()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client);
        using var first = await AuthApi.LoginAsync(client);
        var firstToken = new JsonWebToken((await AuthApi.ReadJsonAsync(first)).GetProperty("accessToken").GetString());

        // Act
        using var second = await AuthApi.LoginAsync(client);
        var secondToken = new JsonWebToken((await AuthApi.ReadJsonAsync(second)).GetProperty("accessToken").GetString());

        // Assert
        Assert.NotEqual(firstToken.Id, secondToken.Id);
    }

    [Theory]
    [InlineData(AuthApi.Email, AuthApi.WrongPassword)]
    [InlineData(AuthApi.UnknownEmail, AuthApi.Password)]
    [InlineData("not-an-email", AuthApi.Password)]
    public async Task Post_InvalidCredentials_ReturnsTheGenericProblemWithoutAToken(string email, string password)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client);

        // Act
        using var response = await AuthApi.LoginAsync(client, email, password);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.InvalidCredentials, problem.Code);
        Assert.DoesNotContain("token", problem.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_UnknownEmail_ReturnsTheSameBodyShapeAsAWrongPassword()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client);
        using var wrongPassword = await AuthApi.LoginAsync(client, AuthApi.Email, AuthApi.WrongPassword);
        var wrongPasswordProblem = await ProblemResponse.ReadAsync(wrongPassword);

        // Act
        using var unknownEmail = await AuthApi.LoginAsync(client, AuthApi.UnknownEmail, AuthApi.Password);
        var unknownEmailProblem = await ProblemResponse.ReadAsync(unknownEmail);

        // Assert
        Assert.Equal(wrongPassword.StatusCode, unknownEmail.StatusCode);
        Assert.Equal(
            wrongPasswordProblem.Root.EnumerateObject().Select(member => member.Name),
            unknownEmailProblem.Root.EnumerateObject().Select(member => member.Name));
        Assert.Equal(wrongPasswordProblem.Root.GetProperty("title").GetString(), unknownEmailProblem.Root.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Post_DisabledUser_ReturnsTheGenericProblem()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var factory = new PostgresApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client);
        await AuthApi.SetUserStatusAsync(database.ConnectionString, "Disabled");

        // Act
        using var response = await AuthApi.LoginAsync(client);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.InvalidCredentials, problem.Code);
    }

    [Fact]
    public async Task Post_AfterTooManyFailures_ReturnsRateLimitedWithRetryAfterEvenForTheRightPassword()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await AuthApi.RegisterAsync(client);
        await AuthApi.FailLoginsAsync(client, AttemptsAllowed);

        // Act
        using var response = await AuthApi.LoginAsync(client);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(ProblemCodes.RateLimited, problem.Code);
        Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, WindowSeconds);
        Assert.DoesNotContain("token", problem.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, AuthApi.Password, "email")]
    [InlineData(" ", AuthApi.Password, "email")]
    [InlineData(AuthApi.Email, null, "password")]
    [InlineData(AuthApi.Email, "", "password")]
    public async Task Post_MissingField_ReturnsValidationProblemNamingTheField(string? email, string? password, string field)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        // Act
        using var response = await AuthApi.LoginAsync(client, email, password);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemCodes.ValidationFailed, problem.Code);
        Assert.Contains(field, problem.Root.GetProperty("errors").EnumerateObject().Select(error => error.Name));
    }
}
