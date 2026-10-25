namespace Saas.Subscription.Sample.Application.Entitlements;

/// <summary>Why paid capabilities were withdrawn in <see cref="EntitlementState.Restricted"/>.</summary>
public enum AccessRestriction
{
    /// <summary>The grace after a failed renewal ended.</summary>
    PaymentOverdue,

    /// <summary>The paid period ended and no renewal was confirmed.</summary>
    AccessExpired,
}
