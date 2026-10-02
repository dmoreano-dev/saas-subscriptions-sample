using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Saas.Subscription.Sample.IntegrationTests.Contracts;

public class OpenApiDocumentTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DocumentPath = "/openapi/v1.json";
    private const string ProductionEnvironment = "Production";
    private const string RegenerateHint =
        "docs/contracts/openapi.json is out of date. Regenerate it and the TypeScript types with " +
        "`npm run contract:generate` in src/frontend, then commit both.";

    [Fact]
    public async Task GetDocument_InDevelopment_MatchesCommittedContract()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var served = JsonNode.Parse(await client.GetStringAsync(DocumentPath));
        var committed = ContractDocument.Load();

        // Assert
        Assert.True(JsonNode.DeepEquals(committed, served), RegenerateHint);
    }

    [Theory]
    [InlineData(DocumentPath)]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    public async Task Get_DocumentationRoute_OutsideDevelopment_ReturnsNotFound(string path)
    {
        // Arrange
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment(ProductionEnvironment)).CreateClient();

        // Act
        using var response = await client.GetAsync(path);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDocumentationUi_InDevelopment_ReturnsPage()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/scalar/v1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
