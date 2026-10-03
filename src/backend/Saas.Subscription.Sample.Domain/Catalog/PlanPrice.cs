namespace Saas.Subscription.Sample.Domain.Catalog;

/// <summary>
/// A price of a plan version for one interval and currency. It is separate from the capabilities, so
/// monthly and annual prices share one capability package. <see cref="ExternalPriceId"/> is the optional
/// payment-provider reference, filled in only when a provider is wired (never required by the catalog).
/// </summary>
public sealed class PlanPrice
{
    private PlanPrice()
    {
    }

    public Guid Id { get; private set; }

    public Guid PlanVersionId { get; private set; }

    public BillingInterval Interval { get; private set; }

    public string Currency { get; private set; } = null!;

    public int UnitAmountMinor { get; private set; }

    public string? ExternalPriceId { get; private set; }

    public static PlanPrice Create(BillingInterval interval, string currency, int unitAmountMinor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitAmountMinor);
        return new()
        {
            Id = Guid.CreateVersion7(),
            Interval = interval,
            Currency = currency,
            UnitAmountMinor = unitAmountMinor,
        };
    }
}
