using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Application.Authorization;

/// <summary>Reads the current facts from the database on every call. It decides nothing; <see cref="AccountAccessPolicy"/> does.</summary>
public interface IAccountAccessReader
{
    /// <summary>Null when the user does not exist.</summary>
    Task<AccountAccessFacts?> FindAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);

    /// <summary>Null when the user does not exist.</summary>
    Task<UserMemberships?> ListMembershipsAsync(Guid userId, CancellationToken cancellationToken);
}

/// <param name="Membership">Null when the account does not exist or the user has no membership in it.</param>
public sealed record AccountAccessFacts(UserStatus UserStatus, MembershipFacts? Membership);

/// <param name="Permissions">The account permissions granted by the roles of this membership.</param>
public sealed record MembershipFacts(MembershipStatus Status, AccountStatus AccountStatus, IReadOnlySet<string> Permissions);

public sealed record UserMemberships(UserStatus UserStatus, IReadOnlyList<AccessibleAccount> Accounts);

public sealed record AccessibleAccount(Guid Id, string Name, AccountType Type, AccountStatus Status, MembershipStatus MembershipStatus);
