using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>The negative token matrix, through the real authentication pipeline of the API.</summary>
public sealed class AccessTokenValidationTests(FixedClockApiFixture api) : IClassFixture<FixedClockApiFixture>
{
    private const string BearerScheme = "Bearer";

    private string Forge(Forgery forgery) => TokenForge.Create(forgery, api.Options, FixedClockApiFixture.Start);

    [Theory]
    [InlineData(Forgery.None)]
    [InlineData(Forgery.ExpiredInsideClockSkew)]
    [InlineData(Forgery.NotYetValidInsideClockSkew)]
    public async Task Get_ProtectedRouteWithAcceptableToken_ReachesTheHandler(Forgery forgery)
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        using var request = FixedClockApiFixture.ProtectedRequest(Forge(forgery));

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Theory]
    [InlineData(Forgery.AlgNoneWithoutSignature)]
    [InlineData(Forgery.SignatureRemoved)]
    [InlineData(Forgery.SignatureTampered)]
    [InlineData(Forgery.PayloadTampered)]
    [InlineData(Forgery.SignedWithAnotherKey)]
    [InlineData(Forgery.UnknownKeyId)]
    [InlineData(Forgery.MissingKeyId)]
    [InlineData(Forgery.HmacWithThePublicKeyAsSecret)]
    [InlineData(Forgery.Es384FromAnotherCurve)]
    [InlineData(Forgery.WrongIssuer)]
    [InlineData(Forgery.MissingIssuer)]
    [InlineData(Forgery.WrongAudience)]
    [InlineData(Forgery.MissingAudience)]
    [InlineData(Forgery.Expired)]
    [InlineData(Forgery.MissingExpiry)]
    [InlineData(Forgery.NotYetValid)]
    [InlineData(Forgery.NotBeforeAfterExpiry)]
    [InlineData(Forgery.MissingSubject)]
    [InlineData(Forgery.SubjectIsNotAUserId)]
    [InlineData(Forgery.MissingTokenId)]
    public async Task Get_ProtectedRouteWithForgedToken_ReturnsUnauthenticatedProblem(Forgery forgery)
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        using var request = FixedClockApiFixture.ProtectedRequest(Forge(forgery));

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.Unauthenticated, problem.Code);
    }

    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("a.b.c")]
    [InlineData("")]
    public async Task Get_ProtectedRouteWithGarbageToken_ReturnsUnauthenticatedProblem(string token)
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, FixedClockApiFixture.ProtectedRoute);
        request.Headers.TryAddWithoutValidation("Authorization", $"{BearerScheme} {token}");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ProtectedRouteWithValidTokenInAnotherScheme_ReturnsUnauthenticatedProblem()
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, FixedClockApiFixture.ProtectedRoute);
        request.Headers.Authorization = new("Basic", Forge(Forgery.None));

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ProtectedRouteWithExpiredToken_DoesNotSayWhyItWasRefused()
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        using var request = FixedClockApiFixture.ProtectedRequest(Forge(Forgery.Expired));

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal(BearerScheme, challenge.Scheme);
        Assert.Null(challenge.Parameter);
        Assert.DoesNotContain("expire", problem.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_ProtectedRouteWithIssuedToken_ReachesTheHandler()
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        var token = api.Factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(Guid.CreateVersion7());
        using var request = FixedClockApiFixture.ProtectedRequest(token.Value);

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }
}
