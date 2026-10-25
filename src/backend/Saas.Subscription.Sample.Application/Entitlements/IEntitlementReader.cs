using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;

namespace Saas.Subscription.Sample.Application.Entitlements;

/// <summary>
/// Reads the persisted facts about one account's access from PostgreSQL. It decides nothing (it does not know the
/// time); <see cref="EntitlementPolicy"/> does. It never calls the payment provider.
/// </summary>
public interface IEntitlementReader
{
    Task<EntitlementFacts> ReadAsync(Guid accountId, CancellationToken cancellationToken);
}

/// <param name="Free">The latest published Free version: what the account has without a paid subscription.</param>
/// <param name="Subscription">The subscription that has not ended, if any; there is at most one.</param>
public sealed record EntitlementFacts(PlanEntitlements Free, SubscriptionFacts? Subscription);

/// <param name="Plan">The purchased plan version.</param>
/// <param name="PendingChange">The single pending change of this subscription, if any, whether or not its instant has passed.</param>
public sealed record SubscriptionFacts(
    SubscriptionStatus Status,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset AccessUntil,
    PlanEntitlements Plan,
    PendingChangeFacts? PendingChange);

/// <param name="Target">The plan version a <see cref="SubscriptionChangeKind.PlanChange"/> moves to; null for a cancellation.</param>
public sealed record PendingChangeFacts(SubscriptionChangeKind Kind, DateTimeOffset EffectiveAt, PlanEntitlements? Target);

/// <summary>One published plan version and the capabilities it declares. A capability it does not declare is denied.</summary>
public sealed record PlanEntitlements(string PlanKey, int Version, IReadOnlyList<CapabilityEntry> Capabilities);
