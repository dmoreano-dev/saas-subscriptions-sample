using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Application.Authorization;

/// <summary>
/// The evaluation order of plan §6 up to the permission: active user, active membership, active account, permission.
/// Nothing from the access token takes part except the user id.
/// </summary>
public static class AccountAccessPolicy
{
    public static AccountAccessOutcome Evaluate(AccountAccessFacts? facts, string? permission)
    {
        if (facts is not { UserStatus: UserStatus.Active })
        {
            return AccountAccessOutcome.Unauthenticated;
        }

        // A closed account is gone for everyone, so it is hidden like an account that never existed.
        if (facts.Membership is not { Status: MembershipStatus.Active } membership
            || membership.AccountStatus == AccountStatus.Closed)
        {
            return AccountAccessOutcome.NotFound;
        }

        if (membership.AccountStatus != AccountStatus.Active)
        {
            return AccountAccessOutcome.AccountSuspended;
        }

        return permission is null || membership.Permissions.Contains(permission)
            ? AccountAccessOutcome.Granted
            : AccountAccessOutcome.Forbidden;
    }

    public static bool IsListed(AccessibleAccount account) =>
        account.MembershipStatus == MembershipStatus.Active && account.Status != AccountStatus.Closed;
}
