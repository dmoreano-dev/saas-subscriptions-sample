namespace Saas.Subscription.Sample.Domain.Catalog;

/// <summary>
/// One capability of a plan version: exactly one of a boolean value, a limit, or "unlimited".
/// Unlimited is a deliberate state, never a very large number.
/// </summary>
public sealed class PlanCapability
{
    private PlanCapability()
    {
    }

    public Guid PlanVersionId { get; private set; }

    public string FeatureKey { get; private set; } = null!;

    /// <summary>How the capability is typed (flag or limit); loaded only when a query includes it.</summary>
    public FeatureDefinition Definition { get; private set; } = null!;

    public bool? BoolValue { get; private set; }

    public int? LimitValue { get; private set; }

    public bool IsUnlimited { get; private set; }

    public static PlanCapability Boolean(string featureKey, bool value) => new()
    {
        FeatureKey = featureKey,
        BoolValue = value,
    };

    public static PlanCapability Limit(string featureKey, int limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        return new() { FeatureKey = featureKey, LimitValue = limit };
    }

    public static PlanCapability Unlimited(string featureKey) => new()
    {
        FeatureKey = featureKey,
        IsUnlimited = true,
    };
}
