namespace Saas.Subscription.Sample.Domain.Catalog;

public enum FeatureValueType
{
    /// <summary>
    /// The capability is either granted or not; it carries no quantity.
    /// </summary>
    Boolean,

    /// <summary>
    /// A non-negative integer limit measured in a <see cref="FeatureUnit"/>, or intentionally unlimited.
    /// </summary>
    Limit,
}
