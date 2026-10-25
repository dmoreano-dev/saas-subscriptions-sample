namespace Saas.Subscription.Sample.Domain.Authorization;

/// <summary>
/// A permission granted by a role. <see cref="Scope"/> is shared with both sides, so a role can only hold
/// permissions of its own scope.
/// </summary>
public sealed class RoleGrant
{
    private RoleGrant()
    {
    }

    public string RoleKey { get; private set; } = null!;

    public string PermissionKey { get; private set; } = null!;

    public PermissionScope Scope { get; private set; }
}
