using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Application.Authorization;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Authorization;

/// <summary>
/// Reads only memberships and their account roles. Platform role assignments are deliberately not consulted: a
/// platform operator has no access to an account they are not a member of.
/// </summary>
public sealed class AccountAccessReader(AppDbContext db) : IAccountAccessReader
{
    public async Task<AccountAccessFacts?> FindAsync(Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        var row = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.Status,
                Membership = db.Memberships
                    .Where(membership => membership.UserId == user.Id && membership.AccountId == accountId)
                    .Join(db.Accounts, membership => membership.AccountId, account => account.Id, (membership, account) => new
                    {
                        membership.Id,
                        MembershipStatus = membership.Status,
                        AccountStatus = account.Status,
                    })
                    .FirstOrDefault(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        if (row.Membership is null)
        {
            return new AccountAccessFacts(row.Status, Membership: null);
        }

        var membershipId = row.Membership.Id;
        var permissions = await db.MembershipRoles.AsNoTracking()
            .Where(assignment => assignment.MembershipId == membershipId)
            .Join(db.RolePermissions, assignment => assignment.RoleKey, grant => grant.RoleKey, (_, grant) => grant.PermissionKey)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new AccountAccessFacts(
            row.Status,
            new MembershipFacts(row.Membership.MembershipStatus, row.Membership.AccountStatus, permissions.ToHashSet(StringComparer.Ordinal)));
    }

    public async Task<UserMemberships?> ListMembershipsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var status = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => (Domain.Identity.UserStatus?)user.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (status is null)
        {
            return null;
        }

        var accounts = await db.Memberships.AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Join(db.Accounts, membership => membership.AccountId, account => account.Id, (membership, account) => new { membership, account })
            .OrderBy(row => row.account.CreatedAt)
            .ThenBy(row => row.account.Id)
            .Select(row => new AccessibleAccount(row.account.Id, row.account.Name, row.account.Type, row.account.Status, row.membership.Status))
            .ToListAsync(cancellationToken);

        return new UserMemberships(status.Value, accounts);
    }
}
