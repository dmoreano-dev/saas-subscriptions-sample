namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

public enum CapabilityKind
{
    /// <summary>On/off functionality, for example <c>reports.advanced</c>.</summary>
    Feature,

    /// <summary>Numeric allowance, for example <c>projects.max_count</c>; see <see cref="CapabilityLimitDto"/>.</summary>
    Limit,
}
