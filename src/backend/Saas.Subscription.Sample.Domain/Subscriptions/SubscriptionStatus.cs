namespace Saas.Subscription.Sample.Domain.Subscriptions;

/// <summary>
/// The local billing decision for a subscription. Whether it grants paid access right now also depends on
/// <see cref="AccountSubscription.AccessUntil"/> and the current time, never on this value alone.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Paid and in good standing until <see cref="AccountSubscription.AccessUntil"/>.</summary>
    Active,

    /// <summary>A renewal failed; paid access continues only during the grace that ends at <see cref="AccountSubscription.AccessUntil"/>.</summary>
    PastDue,

    /// <summary>Finished. The row stays as history; the account no longer has this subscription.</summary>
    Ended,
}
