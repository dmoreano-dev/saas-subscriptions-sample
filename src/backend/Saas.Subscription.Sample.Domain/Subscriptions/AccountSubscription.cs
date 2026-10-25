namespace Saas.Subscription.Sample.Domain.Subscriptions;

/// <summary>
/// The local projection of an account's paid base subscription, read on every entitlement decision so no request
/// ever asks the payment provider. Free is not a row: an account without a current subscription is on Free.
/// PostgreSQL keeps at most one subscription that is not <see cref="SubscriptionStatus.Ended"/> per account.
/// Billing (P03) and the lifecycle (P04) write these rows; nothing in the API creates them yet.
/// </summary>
public sealed class AccountSubscription
{
    private AccountSubscription()
    {
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    /// <summary>The purchased plan version, not "the latest": published versions never change under a subscriber.</summary>
    public Guid PlanVersionId { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public DateTimeOffset CurrentPeriodStart { get; private set; }

    /// <summary>The paid-through instant of the current period.</summary>
    public DateTimeOffset CurrentPeriodEnd { get; private set; }

    /// <summary>
    /// The access deadline: paid capabilities apply in [<see cref="CurrentPeriodStart"/>, <see cref="AccessUntil"/>).
    /// It equals <see cref="CurrentPeriodEnd"/> while active and the end of the grace while past due, so a missed
    /// renewal or a provider outage can never extend paid access past it.
    /// </summary>
    public DateTimeOffset AccessUntil { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
