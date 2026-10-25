namespace Saas.Subscription.Sample.Domain.Subscriptions;

public enum SubscriptionChangeKind
{
    /// <summary>Move to another plan version at the effective instant, for example a downgrade at period end.</summary>
    PlanChange,

    /// <summary>Do not renew: paid access ends at the effective instant and the account falls back to Free.</summary>
    Cancel,
}
