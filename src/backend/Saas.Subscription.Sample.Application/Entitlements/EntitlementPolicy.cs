using Saas.Subscription.Sample.Domain.Subscriptions;

namespace Saas.Subscription.Sample.Application.Entitlements;

/// <summary>
/// Turns the database facts and the current time into effective entitlements. The same facts give different answers
/// at different times, so the decision cannot be stored; it is recomputed on every request. Every interval is
/// half-open [start, end): at the access deadline or a change's effective instant the new state already applies. A
/// change whose instant has passed governs access even if no worker has applied it yet. Nothing from the access token
/// takes part, so a stale plan label in a JWT cannot override this.
/// </summary>
public static class EntitlementPolicy
{
    public static EntitlementSnapshot Resolve(Guid accountId, EntitlementFacts facts, DateTimeOffset now)
    {
        var subscription = facts.Subscription;
        if (subscription is null || subscription.Status == SubscriptionStatus.Ended)
        {
            return Free(accountId, facts, now, effectiveFrom: null, validUntil: null, restriction: null);
        }

        if (now < subscription.CurrentPeriodStart)
        {
            return Free(accountId, facts, now, effectiveFrom: null, validUntil: subscription.CurrentPeriodStart, restriction: null);
        }

        var change = subscription.PendingChange;
        var changeTookEffect = change is not null && now >= change.EffectiveAt;

        if (changeTookEffect && change!.Kind == SubscriptionChangeKind.Cancel)
        {
            return Free(accountId, facts, now, effectiveFrom: change.EffectiveAt, validUntil: null, restriction: null);
        }

        if (now >= subscription.AccessUntil)
        {
            var restriction = subscription.Status == SubscriptionStatus.PastDue
                ? AccessRestriction.PaymentOverdue
                : AccessRestriction.AccessExpired;
            return Free(accountId, facts, now, effectiveFrom: subscription.AccessUntil, validUntil: null, restriction);
        }

        var pending = changeTookEffect ? null : change;
        return new EntitlementSnapshot(
            accountId,
            now,
            subscription.Status == SubscriptionStatus.PastDue ? EntitlementState.Grace : EntitlementState.Active,
            // A plan change always names its target (a CHECK constraint guarantees it).
            changeTookEffect ? change!.Target! : subscription.Plan,
            changeTookEffect ? change!.EffectiveAt : subscription.CurrentPeriodStart,
            Earliest(subscription.AccessUntil, pending?.EffectiveAt),
            Restriction: null,
            pending);
    }

    private static EntitlementSnapshot Free(
        Guid accountId,
        EntitlementFacts facts,
        DateTimeOffset now,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? validUntil,
        AccessRestriction? restriction) =>
        new(
            accountId,
            now,
            restriction is null ? EntitlementState.Free : EntitlementState.Restricted,
            facts.Free,
            effectiveFrom,
            validUntil,
            restriction,
            PendingChange: null);

    private static DateTimeOffset Earliest(DateTimeOffset deadline, DateTimeOffset? next) =>
        next is { } instant && instant < deadline ? instant : deadline;
}
