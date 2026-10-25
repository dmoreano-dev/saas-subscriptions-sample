using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Application.Authorization;

public sealed class AccountAccessService(IAccountAccessReader reader) : IAccountAccessService
{
    public async Task<AccountAccessResult> AuthorizeAsync(Guid userId, Guid accountId, string? permission, CancellationToken cancellationToken)
    {
        var facts = await reader.FindAsync(userId, accountId, cancellationToken);
        var outcome = AccountAccessPolicy.Evaluate(facts, permission);

        return outcome == AccountAccessOutcome.Granted
            ? new AccountAccessResult(outcome, new AccountScope(accountId, userId, facts!.Membership!.Permissions))
            : new AccountAccessResult(outcome, Scope: null);
    }

    public async Task<IReadOnlyList<AccessibleAccount>?> ListAccountsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var memberships = await reader.ListMembershipsAsync(userId, cancellationToken);

        return memberships is { UserStatus: UserStatus.Active }
            ? memberships.Accounts.Where(AccountAccessPolicy.IsListed).ToList()
            : null;
    }
}
