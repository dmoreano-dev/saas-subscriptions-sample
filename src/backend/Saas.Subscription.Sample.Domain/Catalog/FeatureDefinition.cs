namespace Saas.Subscription.Sample.Domain.Catalog;

/// <summary>
/// Declares a capability key, how its value is typed and, for limits, what the number counts.
/// The type and unit never change once defined: published plan versions depend on them.
/// </summary>
public sealed class FeatureDefinition
{
    private FeatureDefinition()
    {
    }

    public string Key { get; private set; } = null!;

    public FeatureValueType ValueType { get; private set; }

    public FeatureUnit? Unit { get; private set; }

    public string Description { get; private set; } = null!;

    public static FeatureDefinition Boolean(string key, string description) => new()
    {
        Key = key,
        ValueType = FeatureValueType.Boolean,
        Description = description,
    };

    public static FeatureDefinition Limit(string key, FeatureUnit unit, string description) => new()
    {
        Key = key,
        ValueType = FeatureValueType.Limit,
        Unit = unit,
        Description = description,
    };
}
