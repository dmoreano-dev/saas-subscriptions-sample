namespace Saas.Subscription.Sample.Application.Configuration;

public enum BillingProvider
{
    Simulator,
    Stripe,
}

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    public BillingProvider Provider { get; set; } = BillingProvider.Simulator;

    public SimulatorOptions Simulator { get; set; } = new();

    public StripeOptions Stripe { get; set; } = new();
}

public sealed class SimulatorOptions
{
    /// <summary>
    /// Enables the scenario controls (failure, duplicate, delayed, out-of-order delivery). Off by default and
    /// rejected at startup when hosted.
    /// </summary>
    public bool ControlsEnabled { get; set; }
}

public sealed class StripeOptions
{
    /// <summary>Secret. Only Sandbox (test-mode) keys are accepted anywhere in the lab.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Secret used to verify webhook signatures.</summary>
    public string? WebhookSecret { get; set; }
}
