using System.Net;
using Saas.Subscription.Sample.IntegrationTests.Configuration;

namespace Saas.Subscription.Sample.IntegrationTests;

public class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string LivenessPath = "/health/live";
    private const string UnknownPath = "/does-not-exist";
    private const string HealthyBody = "Healthy";

    [Fact]
    public async Task GetLive_WithoutAuthentication_ReturnsHealthy()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(LivenessPath);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HealthyBody, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_UnknownRoute_ReturnsNotFound()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(UnknownPath);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
