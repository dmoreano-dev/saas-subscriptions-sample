namespace Saas.Subscription.Sample.Domain.Authorization;

/// <summary>
/// The roles that exist at this stage. Plans (Free, Pro, Max, Enterprise) are never roles: what an account bought
/// is resolved from its subscription, not from who the caller is.
/// </summary>
public static class SystemRoles
{
    /// <summary>Held by the owner of a personal account through their membership.</summary>
    public const string PersonalOwner = "personal_owner";

    /// <summary>Held by a user directly, outside any account.</summary>
    public const string PlatformOperator = "platform_operator";

    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        new(PersonalOwner, PermissionScope.Account, "Personal account owner",
            [Permissions.ProjectsRead, Permissions.ProjectsWrite, Permissions.ReportsGenerate, Permissions.BillingManage]),
        new(PlatformOperator, PermissionScope.Platform, "Platform operator", [Permissions.PlatformOperate]),
    ];
}

public sealed record RoleDefinition(string Key, PermissionScope Scope, string Name, IReadOnlyList<string> Permissions);
