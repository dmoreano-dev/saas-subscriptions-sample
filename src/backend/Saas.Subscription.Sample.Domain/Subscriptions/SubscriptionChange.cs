namespace Saas.Subscription.Sample.Domain.Subscriptions;

/// <summary>
/// A transition scheduled for <see cref="EffectiveAt"/>. A subscription has at most one pending change; scheduling
/// another means cancelling the first. It carries the account id so its account boundary is checked by a composite
/// foreign key to the subscription, not trusted.
/// </summary>
public sealed class SubscriptionChange
{
    private SubscriptionChange()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public Guid AccountId { get; private set; }

    public SubscriptionChangeKind Kind { get; private set; }

    /// <summary>Set for <see cref="SubscriptionChangeKind.PlanChange"/> only.</summary>
    public Guid? TargetPlanVersionId { get; private set; }

    public DateTimeOffset EffectiveAt { get; private set; }

    public SubscriptionChangeStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>When the change was applied or cancelled; null while pending.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }
}
