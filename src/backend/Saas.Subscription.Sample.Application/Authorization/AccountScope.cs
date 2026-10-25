namespace Saas.Subscription.Sample.Application.Authorization;

/// <summary>
/// Proof that a caller was authorized for one account. Only <see cref="AccountAccessService"/> can create it, so
/// anything that takes an <see cref="AccountScope"/> instead of an account id (queries, writes, raw SQL, and later
/// background jobs) cannot be handed an id that nobody verified. Request payloads never supply
/// <see cref="AccountId"/>.
/// </summary>
public sealed class AccountScope
{
    private readonly IReadOnlySet<string> _permissions;

    internal AccountScope(Guid accountId, Guid userId, IReadOnlySet<string> permissions)
    {
        AccountId = accountId;
        UserId = userId;
        _permissions = permissions;
    }

    public Guid AccountId { get; }

    public Guid UserId { get; }

    public bool Has(string permission) => _permissions.Contains(permission);
}
