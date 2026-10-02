using Saas.Subscription.Sample.Application.Configuration;
using static Saas.Subscription.Sample.UnitTests.Configuration.OptionsTestData;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class BillingOptionsValidatorTests
{
    [Fact]
    public void Validate_HostedWithSimulatorControls_Fails()
    {
        // Arrange
        var validator = new BillingOptionsValidator(Hosted);
        var options = new BillingOptions { Simulator = new SimulatorOptions { ControlsEnabled = true } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.Contains("ControlsEnabled", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_LocalWithSimulatorControls_Succeeds()
    {
        // Arrange
        var validator = new BillingOptionsValidator(Local);
        var options = new BillingOptions { Simulator = new SimulatorOptions { ControlsEnabled = true } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_HostedSimulatorWithControlsOff_Succeeds()
    {
        // Arrange
        var validator = new BillingOptionsValidator(Hosted);

        // Act
        var result = validator.Validate(null, new BillingOptions());

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(null, WebhookSecret)]
    [InlineData(TestSecretKey, null)]
    [InlineData("not-a-stripe-key", WebhookSecret)]
    [InlineData(TestSecretKey, "not-a-webhook-secret")]
    public void Validate_StripeWithMissingOrMalformedSecrets_Fails(string? secretKey, string? webhookSecret)
    {
        // Arrange
        var validator = new BillingOptionsValidator(Local);
        var options = StripeOptionsWith(secretKey, webhookSecret);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData(BillingProvider.Stripe)]
    [InlineData(BillingProvider.Simulator)]
    public void Validate_LiveSecretKey_FailsWhicheverProviderIsSelected(BillingProvider provider)
    {
        // Arrange
        var validator = new BillingOptionsValidator(Local);
        var options = StripeOptionsWith(LiveSecretKey, WebhookSecret);
        options.Provider = provider;

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.Contains("Sandbox", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_StripeWithSandboxSecrets_Succeeds()
    {
        // Arrange
        var validator = new BillingOptionsValidator(Hosted);
        var options = StripeOptionsWith(TestSecretKey, WebhookSecret);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    private static BillingOptions StripeOptionsWith(string? secretKey, string? webhookSecret) => new()
    {
        Provider = BillingProvider.Stripe,
        Stripe = new StripeOptions { SecretKey = secretKey, WebhookSecret = webhookSecret },
    };
}
