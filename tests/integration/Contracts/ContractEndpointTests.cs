using System.Net;
using System.Text;
using Saas.Subscription.Sample.IntegrationTests.Configuration;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Contracts;

/// <summary>
/// The real API through <see cref="ApiFactory"/>. Every endpoint is contract-only
/// until its phase, so each answers 501 with a problem body; no endpoint touches the database yet.
/// </summary>
public class ContractEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string AccountId = "0197c0de-0000-7000-8000-000000000001";
    private const string ProjectId = "0197c0de-0000-7000-8000-000000000002";
    private const string CredentialsJson = """{"email":"a@example.com","password":"x"}""";
    private const string ProjectJson = """{"name":"p","description":null}""";
    private const string JsonMediaType = "application/json";

    [Theory]
    [InlineData("POST", "/api/auth/register", CredentialsJson)]
    [InlineData("POST", "/api/auth/login", CredentialsJson)]
    [InlineData("GET", "/api/me", null)]
    [InlineData("GET", "/api/accounts", null)]
    [InlineData("GET", $"/api/accounts/{AccountId}/capabilities", null)]
    [InlineData("GET", $"/api/accounts/{AccountId}/projects", null)]
    [InlineData("POST", $"/api/accounts/{AccountId}/projects", ProjectJson)]
    [InlineData("GET", $"/api/accounts/{AccountId}/projects/{ProjectId}", null)]
    [InlineData("DELETE", $"/api/accounts/{AccountId}/projects/{ProjectId}", null)]
    public async Task Send_ContractOnlyEndpoint_ReturnsNotImplementedProblem(string method, string path, string? json)
    {
        // Arrange
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
        }

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        Assert.Equal(ProblemCodes.NotImplemented, problem.Code);
        Assert.False(string.IsNullOrWhiteSpace(problem.CorrelationId));
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
