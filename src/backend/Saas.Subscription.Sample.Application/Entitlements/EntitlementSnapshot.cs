using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.Application.Entitlements;

/// <summary>
/// The answer to "what may this account use at <see cref="CalculatedAt"/>?", computed from the database facts and the
/// time. It stays true until <see cref="ValidUntil"/> (the next known transition; null when none is known) unless the
/// facts change. Absence denies: a key the effective plan version does not declare is not granted.
/// </summary>
/// <param name="Plan">The plan version in effect.</param>
/// <param name="EffectiveFrom">The instant this result took effect, when known.</param>
/// <param name="PendingChange">A scheduled change that has not taken effect yet.</param>
public sealed record EntitlementSnapshot(
    Guid AccountId,
    DateTimeOffset CalculatedAt,
    EntitlementState State,
    PlanEntitlements Plan,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? ValidUntil,
    AccessRestriction? Restriction,
    PendingChangeFacts? PendingChange)
{
    /// <summary>The capability as the effective plan version declares it; null when it is not declared (denied).</summary>
    public CapabilityEntry? Find(string featureKey) =>
        Plan.Capabilities.FirstOrDefault(capability => string.Equals(capability.Key, featureKey, StringComparison.Ordinal));

    /// <summary>False for any key that is not declared, including keys nobody defined.</summary>
    public bool IsGranted(string featureKey) => Find(featureKey) is { } capability && IsGranted(capability);

    /// <summary>A feature is granted when it is on; a limit when it is unlimited or above zero.</summary>
    public static bool IsGranted(CapabilityEntry capability) => capability.ValueType == FeatureValueType.Boolean
        ? capability.BoolValue == true
        : capability.IsUnlimited || capability.LimitValue > 0;
}
