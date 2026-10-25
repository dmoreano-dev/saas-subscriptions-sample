namespace Saas.Subscription.Sample.Domain.Authorization;

/// <summary>An account role held by one membership, so it is valid in that account only.</summary>
public sealed class MembershipRole
{
    private MembershipRole()
    {
    }

    public Guid MembershipId { get; private set; }

    public string RoleKey { get; private set; } = null!;

    /// <summary>Always <see cref="PermissionScope.Account"/>; stored so the database can refuse a platform role here.</summary>
    public PermissionScope RoleScope { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static MembershipRole Create(Guid membershipId, string roleKey, DateTimeOffset now) => new()
    {
        MembershipId = membershipId,
        RoleKey = roleKey,
        RoleScope = PermissionScope.Account,
        CreatedAt = now,
    };
}
