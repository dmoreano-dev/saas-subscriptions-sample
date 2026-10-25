namespace Saas.Subscription.Sample.Domain.Authorization;

public sealed class PermissionDefinition
{
    private PermissionDefinition()
    {
    }

    public string Key { get; private set; } = null!;

    public PermissionScope Scope { get; private set; }

    public string Description { get; private set; } = null!;

    public static PermissionDefinition Create(string key, PermissionScope scope, string description) => new()
    {
        Key = key,
        Scope = scope,
        Description = description,
    };
}
