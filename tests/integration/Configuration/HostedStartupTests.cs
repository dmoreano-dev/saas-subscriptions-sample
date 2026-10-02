using System.Net;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

public class HostedStartupTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string LivenessPath = "/health/live";

    [Fact]
    public async Task Start_ValidHostedConfiguration_ServesRequests()
    {
        // Arrange
        using var hosted = HostedProfile.Create(factory);

        // Act
        using var client = hosted.CreateClient();
        using var response = await client.GetAsync(LivenessPath);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Billing:Simulator:ControlsEnabled", "true", "Billing:Simulator:ControlsEnabled")]
    [InlineData("Authentication:SigningKey", "", "Authentication:SigningKey")]
    [InlineData("Authentication:SigningKeyId", "", "Authentication:SigningKeyId")]
    [InlineData("Database:ConnectionString", "Host=h;Database=d;Username=u;Password=p", "SSL Mode=VerifyFull")]
    [InlineData("Database:ConnectionString", "Host=h;Database=d;Username=u;Password=p;SSL Mode=Require", "SSL Mode=VerifyFull")]
    [InlineData("Database:ConnectionString", "", "ConnectionString")]
    [InlineData("Email:Provider", "Smtp", "Email:Provider")]
    [InlineData("Email:Https:ApiKey", "", "Email:Https:ApiKey")]
    [InlineData("Frontend:BaseUrl", "http://app.example.test", "Frontend:BaseUrl")]
    [InlineData("Frontend:BaseUrl", "https://localhost", "Frontend:BaseUrl")]
    [InlineData("Frontend:AllowedOrigins:0", "*", "Frontend:AllowedOrigins")]
    public void Start_InvalidHostedSetting_FailsWithTheOffendingKey(string key, string value, string expectedFailure)
    {
        // Arrange
        using var hosted = HostedProfile.Create(factory, (key, value));

        // Act
        var failure = StartupFailure.Describe(() => hosted.CreateClient().Dispose());

        // Assert
        Assert.Contains(expectedFailure, failure, StringComparison.Ordinal);
    }

    [Fact]
    public void Start_InvalidHostedSetting_DoesNotEchoTheConnectionString()
    {
        // Arrange
        const string password = "do-not-leak-this-password";
        using var hosted = HostedProfile.Create(factory, ("Database:ConnectionString", $"Host=h;Password={password}"));

        // Act
        var failure = StartupFailure.Describe(() => hosted.CreateClient().Dispose());

        // Assert
        Assert.DoesNotContain(password, failure, StringComparison.Ordinal);
    }
}
