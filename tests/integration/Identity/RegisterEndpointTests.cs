using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.IntegrationTests.Persistence;
using Saas.Subscription.Sample.IntegrationTests.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

[Collection(PostgresTestGroup.Name)]
public class RegisterEndpointTests(PostgresFixture postgres)
{
    private const string Route = "/api/auth/register";
    private const string Email = "ana@example.com";
    private const string Password = "right-password";
    private const string PersonalAccountType = "personal";
    private const string ActiveStatus = "active";

    private async Task<PostgresApiFactory> CreateFactoryAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        return new PostgresApiFactory(database.ConnectionString);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string? email, string? password) =>
        client.PostAsJsonAsync(Route, new { email, password });

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private static List<string> ShapeOf(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().SelectMany(property => ShapeOf(property.Value).Select(path => $"{property.Name}/{path}").DefaultIfEmpty(property.Name)).Order(StringComparer.Ordinal).ToList()
            : [];

    [Fact]
    public async Task Post_NewEmail_ReturnsCreatedUserAndPersonalAccountWithoutAToken()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        // Act
        using var response = await PostAsync(client, Email, Password);
        var body = await ReadBodyAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Email, body.GetProperty("user").GetProperty("email").GetString());
        Assert.Equal(PersonalAccountType, body.GetProperty("personalAccount").GetProperty("type").GetString());
        Assert.Equal(ActiveStatus, body.GetProperty("personalAccount").GetProperty("status").GetString());
        Assert.DoesNotContain("token", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_ExistingEmail_ReturnsTheSameStatusAndShapeAsANewEmail()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        using var first = await PostAsync(client, Email, Password);
        var firstBody = await ReadBodyAsync(first);

        // Act
        using var second = await PostAsync(client, Email, "another-password");
        var secondBody = await ReadBodyAsync(second);

        // Assert
        Assert.Equal(first.StatusCode, second.StatusCode);
        Assert.Equal(first.Content.Headers.ContentType, second.Content.Headers.ContentType);
        Assert.Equal(ShapeOf(firstBody), ShapeOf(secondBody));
        Assert.NotEqual(firstBody.GetProperty("user").GetProperty("id"), secondBody.GetProperty("user").GetProperty("id"));
    }

    [Theory]
    [InlineData(null, Password, "email")]
    [InlineData("not-an-email", Password, "email")]
    [InlineData(Email, null, "password")]
    [InlineData(Email, "abcd", "password")]
    public async Task Post_InvalidField_ReturnsValidationProblemNamingTheField(string? email, string? password, string field)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        // Act
        using var response = await PostAsync(client, email, password);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemCodes.ValidationFailed, problem.Code);
        Assert.Contains(field, problem.Root.GetProperty("errors").EnumerateObject().Select(error => error.Name));
    }
}
