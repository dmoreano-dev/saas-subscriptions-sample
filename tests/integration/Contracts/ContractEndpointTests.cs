using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.IntegrationTests.Configuration;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Contracts;

/// <summary>
/// The real API through <see cref="ApiFactory"/> (no database). The account and project families are still
/// contract-only: behind the bearer token they answer 501 with a problem body. Registration, login and
/// <c>/api/me</c> are implemented and covered against a real database in <c>Identity/</c>.
/// </summary>
public class ContractEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string AccountId = "0197c0de-0000-7000-8000-000000000001";
    private const string ProjectId = "0197c0de-0000-7000-8000-000000000002";
    private const string ProjectJson = """{"name":"p","description":null}""";
    private const string JsonMediaType = "application/json";
    private const string BearerScheme = "Bearer";

    private static HttpRequestMessage CreateRequest(string method, string path, string? json)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
        }

        return request;
    }

    private string IssueToken() => factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(Guid.CreateVersion7()).Value;

    [Theory]
    [InlineData("GET", "/api/accounts", null)]
    [InlineData("GET", $"/api/accounts/{AccountId}/capabilities", null)]
    [InlineData("GET", $"/api/accounts/{AccountId}/projects", null)]
    [InlineData("POST", $"/api/accounts/{AccountId}/projects", ProjectJson)]
    [InlineData("GET", $"/api/accounts/{AccountId}/projects/{ProjectId}", null)]
    [InlineData("DELETE", $"/api/accounts/{AccountId}/projects/{ProjectId}", null)]
    public async Task Send_ContractOnlyEndpointWithAccessToken_ReturnsNotImplementedProblem(string method, string path, string? json)
    {
        // Arrange
        using var client = factory.CreateClient();
        using var request = CreateRequest(method, path, json);
        request.Headers.Authorization = new(BearerScheme, IssueToken());

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        Assert.Equal(ProblemCodes.NotImplemented, problem.Code);
        Assert.False(string.IsNullOrWhiteSpace(problem.CorrelationId));
    }

    [Theory]
    [InlineData("GET", "/api/me", null)]
    [InlineData("GET", "/api/accounts", null)]
    [InlineData("GET", $"/api/accounts/{AccountId}/capabilities", null)]
    [InlineData("GET", $"/api/accounts/{AccountId}/projects", null)]
    [InlineData("POST", $"/api/accounts/{AccountId}/projects", ProjectJson)]
    [InlineData("GET", $"/api/accounts/{AccountId}/projects/{ProjectId}", null)]
    [InlineData("DELETE", $"/api/accounts/{AccountId}/projects/{ProjectId}", null)]
    public async Task Send_ProtectedEndpointWithoutAccessToken_ReturnsUnauthenticatedProblem(string method, string path, string? json)
    {
        // Arrange
        using var client = factory.CreateClient();
        using var request = CreateRequest(method, path, json);

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemCodes.Unauthenticated, problem.Code);
        Assert.Equal(ProblemResponse.MediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(BearerScheme, Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    [Fact]
    public async Task Post_LoginWithMalformedJson_ReturnsBadRequestProblem()
    {
        // Arrange
        using var client = factory.CreateClient();
        using var content = new StringContent("{not json", Encoding.UTF8, JsonMediaType);

        // Act
        using var response = await client.PostAsync("/api/auth/login", content);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemCodes.BadRequest, problem.Code);
    }

    [Fact]
    public async Task Get_UnknownApiRoute_ReturnsNotFoundProblem()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/does-not-exist");
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemCodes.NotFound, problem.Code);
    }
}
