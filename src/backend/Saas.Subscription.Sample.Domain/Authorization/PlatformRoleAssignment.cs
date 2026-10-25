namespace Saas.Subscription.Sample.Domain.Authorization;

/// <summary>A platform role held by a user. It is not a membership and gives no access to any account.</summary>
public sealed class PlatformRoleAssignment
{
    private PlatformRoleAssignment()
    {
    }

    public Guid UserId { get; private set; }

    public string RoleKey { get; private set; } = null!;

    /// <summary>Always <see cref="PermissionScope.Platform"/>; stored so the database can refuse an account role here.</summary>
    public PermissionScope RoleScope { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static PlatformRoleAssignment Create(Guid userId, string roleKey, DateTimeOffset now) => new()
    {
        UserId = userId,
        RoleKey = roleKey,
        RoleScope = PermissionScope.Platform,
        CreatedAt = now,
    };
}
