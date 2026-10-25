namespace Saas.Subscription.Sample.Domain.Authorization;

public sealed class Role
{
    private Role()
    {
    }

    public string Key { get; private set; } = null!;

    public PermissionScope Scope { get; private set; }

    public string Name { get; private set; } = null!;
}
