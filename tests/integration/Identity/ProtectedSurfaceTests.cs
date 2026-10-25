using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Api.Authorization;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>
/// Every route under <c>/api</c> except register and login refuses a request without a valid token (P01), and a
/// valid token alone opens no account (ENT-02): each route that selects an account declares the access it demands
/// and answers 404 to a caller who is not a member. The host has no database; the caller exists and has no accounts.
/// </summary>
public sealed class ProtectedSurfaceTests(FixedClockApiFixture api) : IClassFixture<FixedClockApiFixture>
{
    private const string ApiPrefix = "/api";
    private const string PublicPrefix = "/api/auth/";
    private const string JsonBody = """{"name":"p","description":null}""";
    private const string AccountIdSegment = "{accountId";
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

    /// <summary>Every route that selects an account, with the permission it must demand (null: active membership only).</summary>
    public static readonly TheoryData<string, string, string?> AccountScopedRoutes = new()
    {
        { "GET", "/api/accounts/{accountId:guid}/capabilities", null },
        { "GET", "/api/accounts/{accountId:guid}/projects", Permissions.ProjectsRead },
        { "POST", "/api/accounts/{accountId:guid}/projects", Permissions.ProjectsWrite },
        { "GET", "/api/accounts/{accountId:guid}/projects/{projectId:guid}", Permissions.ProjectsRead },
        { "DELETE", "/api/accounts/{accountId:guid}/projects/{projectId:guid}", Permissions.ProjectsWrite },
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

    [Fact]
    public void RoutesWithAnAccountId_AreExactlyTheOnesWithADeclaredAccessRequirement()
    {
        // Arrange
        var expected = AccountScopedRoutes.Select(row => $"{row[0]} {row[1]}").Order().ToArray();

        // Act
        var actual = ApiEndpoints()
            .Where(entry => entry.Template.Contains(AccountIdSegment, StringComparison.OrdinalIgnoreCase))
            .Select(entry => $"{entry.Method} {entry.Template}")
            .Order()
            .ToArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(AccountScopedRoutes))]
    public void AccountScopedRoute_Metadata_DemandsVerifiedAccessWithItsPermission(string method, string template, string? permission)
    {
        // Arrange
        var endpoint = ApiEndpoints().Single(entry => entry.Method == method && entry.Template == template);

        // Act
        var requirement = endpoint.AccessRequirement;

        // Assert
        Assert.Equal(new AccountAccessRequirement(permission), requirement);
    }

    [Theory]
    [MemberData(nameof(AccountScopedRoutes))]
    public async Task Send_AccountScopedRouteWithValidTokenOfANonMember_ReturnsNotFoundProblem(string method, string template, string? _)
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
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
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
                    endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null,
                    endpoint.Metadata.GetMetadata<AccountAccessRequirement>())));

    private sealed record ApiEndpoint(string Method, string Template, bool RequiresAuthorization, AccountAccessRequirement? AccessRequirement);
}
