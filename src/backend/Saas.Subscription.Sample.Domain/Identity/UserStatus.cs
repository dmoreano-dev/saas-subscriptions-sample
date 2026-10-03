namespace Saas.Subscription.Sample.Domain.Identity;

public enum UserStatus
{
    Active,

    /// <summary>Reserved for the account-disable flow (P06); no code path sets it yet.</summary>
    Disabled,
}
