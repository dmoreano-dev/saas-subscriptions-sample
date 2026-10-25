namespace Saas.Subscription.Sample.Application.Authorization;

/// <summary>
/// The single place that decides whether a user may act in an account. Every account-scoped entry point calls it
/// with the account id it was given (a selector, never proof) and works only with the returned scope.
/// </summary>
public interface IAccountAccessService
{
    /// <param name="permission">The permission the action needs; null when being an active member is enough.</param>
    Task<AccountAccessResult> AuthorizeAsync(Guid userId, Guid accountId, string? permission, CancellationToken cancellationToken);

    /// <summary>The accounts the user can open; null when the user does not exist or is not active.</summary>
    Task<IReadOnlyList<AccessibleAccount>?> ListAccountsAsync(Guid userId, CancellationToken cancellationToken);
}

public enum AccountAccessOutcome
{
    Granted,

    /// <summary>The caller's user does not exist or is not active.</summary>
    Unauthenticated,

    /// <summary>No such account for this caller. An unknown account and one the caller is not an active member of look the same.</summary>
    NotFound,

    /// <summary>The caller is an active member, but the account is suspended.</summary>
    AccountSuspended,

    /// <summary>The caller is an active member of an active account without the needed permission.</summary>
    Forbidden,
}

public sealed record AccountAccessResult(AccountAccessOutcome Outcome, AccountScope? Scope);
