namespace Saas.Subscription.Sample.Application.Entitlements;

public enum EntitlementState
{
    /// <summary>No current paid subscription; the Free plan applies.</summary>
    Free,

    /// <summary>A paid subscription in good standing.</summary>
    Active,

    /// <summary>A renewal failed and the paid plan still applies until the grace deadline.</summary>
    Grace,

    /// <summary>The access deadline passed without a renewal: the Free plan applies again, data is kept.</summary>
    Restricted,
}
