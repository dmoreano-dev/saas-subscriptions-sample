using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>
/// The JWT checkpoint (P01): every route under <c>/api</c> except register and login refuses a request without a
/// valid token, and a valid token only reaches handlers that exist at this stage. Accounts and projects are still
/// contract-only, so a valid token meets 501 there, never data.
/// </summary>
public sealed class ProtectedSurfaceTests(FixedClockApiFixture api) : IClassFixture<FixedClockApiFixture>
{
    private const string ApiPrefix = "/api";
    private const string PublicPrefix = "/api/auth/";
    private const string JsonBody = "{}";
    private const string JsonContentType = "application/json";
    private const string AnyId = "00000000-0000-7000-8000-000000000001";
    private static readonly Regex RouteParameter = new(@"\{[^}]+\}", RegexOptions.Compiled);

    /// <summary>The protected routes at this stage; <see cref="ProtectedRoutes_AreExactlyTheOnesListedForThisStage"/> keeps it honest.</summary>
    public static readonly TheoryData<string, string> StageProtectedRoutes = new()
    {
        { "GET", "/api/me" },
        { "GET", "/api/accounts" },
        { "GET", "/api/accounts/{accountId:guid}/capabilities" },
        { "GET", "/api/accounts/{accountId:guid}/projects" },
        { "POST", "/api/accounts/{accountId:guid}/projects" },
        { "GET", "/api/accounts/{accountId:guid}/projects/{projectId:guid}" },
        { "DELETE", "/api/accounts/{accountId:guid}/projects/{projectId:guid}" },
    };

    /// <summary>The same routes minus <c>/api/me</c>, which reads the user from PostgreSQL (see <c>MeEndpointTests</c>).</summary>
    public static readonly TheoryData<string, string> StageContractOnlyRoutes = new()
    {
        { "GET", "/api/accounts" },
        { "GET", "/api/accounts/{accountId:guid}/capabilities" },
        { "GET", "/api/accounts/{accountId:guid}/projects" },
        { "POST", "/api/accounts/{accountId:guid}/projects" },
        { "GET", "/api/accounts/{accountId:guid}/projects/{projectId:guid}" },
        { "DELETE", "/api/accounts/{accountId:guid}/projects/{projectId:guid}" },
    };

    [Fact]
    public void ProtectedRoutes_AreExactlyTheOnesListedForThisStage()
    {
        // Arrange
        var expected = StageProtectedRoutes.Select(row => $"{row[0]} {row[1]}").Order().ToArray();

        // Act
        var actual = ApiEndpointsRequiringAuthorization().Select(entry => $"{entry.Method} {entry.Template}").Order().ToArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnonymousRoutes_AreOnlyRegisterAndLogin()
    {
        // Arrange
        var expected = new[] { "POST /api/auth/login", "POST /api/auth/register" };

        // Act
        var actual = ApiEndpoints()
            .Where(entry => !entry.RequiresAuthorization)
            .Select(entry => $"{entry.Method} {entry.Template}")
            .Order()
            .ToArray();

        // Assert
        Assert.Equal(expected, actual);
        Assert.All(actual, route => Assert.Contains(PublicPrefix, route, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(StageProtectedRoutes))]
    public async Task Send_ProtectedRouteWithoutToken_ReturnsUnauthenticatedProblem(string method, string template)
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        using var request = Build(method, template);

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.Unauthenticated, problem.Code);
    }

    [Theory]
    [MemberData(nameof(StageContractOnlyRoutes))]
    public async Task Send_ContractOnlyRouteWithValidToken_ReachesOnlyTheNotImplementedHandler(string method, string template)
    {
        // Arrange
        using var client = api.Factory.CreateClient();
        var token = api.Factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(Guid.CreateVersion7());
        using var request = Build(method, template);
        request.Headers.Authorization = new("Bearer", token.Value);

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        Assert.Equal(ProblemCodes.NotImplemented, problem.Code);
    }

    private static HttpRequestMessage Build(string method, string template)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), RouteParameter.Replace(template, AnyId));
        if (method == HttpMethod.Post.Method)
        {
            request.Content = new StringContent(JsonBody, System.Text.Encoding.UTF8, JsonContentType);
        }

        return request;
    }

    private IEnumerable<ApiEndpoint> ApiEndpointsRequiringAuthorization() =>
        ApiEndpoints().Where(entry => entry.RequiresAuthorization);

    private IEnumerable<ApiEndpoint> ApiEndpoints() =>
        api.Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith(ApiPrefix, StringComparison.Ordinal) == true)
            .SelectMany(endpoint => (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                .Select(method => new ApiEndpoint(
                    method,
                    endpoint.RoutePattern.RawText!.TrimEnd('/'),
                    endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null)));

    private sealed record ApiEndpoint(string Method, string Template, bool RequiresAuthorization);
}
