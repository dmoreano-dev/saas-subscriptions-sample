namespace Saas.Subscription.Sample.Domain.Subscriptions;

public enum SubscriptionChangeStatus
{
    /// <summary>
    /// Scheduled. Once its effective instant has passed it already governs access, even if no worker has
    /// marked it <see cref="Applied"/> yet.
    /// </summary>
    Pending,

    Applied,

    /// <summary>Undone before it took effect; kept as history.</summary>
    Cancelled,
}
