namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

/// <summary>Rules that reject a setting in every environment, including local Development.</summary>
public class EnvironmentIndependentStartupTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void Start_DevelopmentDefaults_Succeeds()
    {
        // Act
        var failure = StartupFailure.Describe(() => factory.CreateClient().Dispose());

        // Assert
        Assert.Empty(failure);
    }

    [Theory]
    [InlineData("Cache:Provider", "Redis", "Cache:Redis:ConnectionString")]
    [InlineData("Billing:Provider", "Stripe", "Billing:Stripe:SecretKey")]
    [InlineData("Billing:Stripe:SecretKey", "sk_live_example", "Billing:Stripe:SecretKey")]
    [InlineData("BackgroundWork:PollInterval", "00:02:00", "LeaseDuration")]
    public void Start_InvalidSettingInDevelopment_FailsWithTheOffendingKey(string key, string value, string expectedFailure)
    {
        // Arrange
        using var development = factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

        // Act
        var failure = StartupFailure.Describe(() => development.CreateClient().Dispose());

        // Assert
        Assert.Contains(expectedFailure, failure, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Billing:Simulator:ControlsEnabled", "true")]
    [InlineData("Email:Provider", "Smtp")]
    public void Start_LocalOnlySettingInDevelopment_Succeeds(string key, string value)
    {
        // Arrange
        using var development = factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

        // Act
        var failure = StartupFailure.Describe(() => development.CreateClient().Dispose());

        // Assert
        Assert.Empty(failure);
    }
}
