using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class BillingOptionsValidator(AppEnvironment environment) : IValidateOptions<BillingOptions>
{
    private static readonly string[] TestSecretKeyPrefixes = ["sk_test_", "rk_test_"];
    private static readonly string[] LiveKeyPrefixes = ["sk_live_", "rk_live_", "pk_live_"];
    private const string WebhookSecretPrefix = "whsec_";

    private const string SecretKeyPath = $"{BillingOptions.SectionName}:{nameof(BillingOptions.Stripe)}:{nameof(StripeOptions.SecretKey)}";
    private const string WebhookSecretPath = $"{BillingOptions.SectionName}:{nameof(BillingOptions.Stripe)}:{nameof(StripeOptions.WebhookSecret)}";

    public ValidateOptionsResult Validate(string? name, BillingOptions options)
    {
        var failures = new List<string>();

        if (environment.IsHosted && options.Simulator.ControlsEnabled)
        {
            failures.Add(
                $"{BillingOptions.SectionName}:{nameof(BillingOptions.Simulator)}:{nameof(SimulatorOptions.ControlsEnabled)} must be false when hosted.");
        }

        // No real-money key is accepted in the lab profile, whichever provider is selected.
        if (HasPrefix(options.Stripe.SecretKey, LiveKeyPrefixes))
        {
            failures.Add($"{SecretKeyPath} must be a Stripe Sandbox (test-mode) key.");
        }

        if (options.Provider == BillingProvider.Stripe)
        {
            ValidateStripe(options.Stripe, failures);
        }

        return failures.ToResult();
    }

    private static void ValidateStripe(StripeOptions stripe, List<string> failures)
    {
        if (OptionsValidation.IsBlank(stripe.SecretKey))
        {
            failures.Add($"{SecretKeyPath} is required when {BillingOptions.SectionName}:{nameof(BillingOptions.Provider)} is Stripe.");
        }
        else if (!HasPrefix(stripe.SecretKey, TestSecretKeyPrefixes) && !HasPrefix(stripe.SecretKey, LiveKeyPrefixes))
        {
            failures.Add($"{SecretKeyPath} must be a Stripe Sandbox (test-mode) key.");
        }

        if (OptionsValidation.IsBlank(stripe.WebhookSecret))
        {
            failures.Add($"{WebhookSecretPath} is required when {BillingOptions.SectionName}:{nameof(BillingOptions.Provider)} is Stripe.");
        }
        else if (!stripe.WebhookSecret!.StartsWith(WebhookSecretPrefix, StringComparison.Ordinal))
        {
            failures.Add($"{WebhookSecretPath} must be a Stripe webhook signing secret.");
        }
    }

    private static bool HasPrefix(string? value, string[] prefixes) =>
        value is not null && prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
}
