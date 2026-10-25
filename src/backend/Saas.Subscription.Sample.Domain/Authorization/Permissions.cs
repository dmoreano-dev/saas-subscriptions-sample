namespace Saas.Subscription.Sample.Domain.Authorization;

/// <summary>
/// The operations that can be allowed. A permission answers "may this member do this in this account"; whether the
/// account's plan includes the feature is a capability, decided separately.
/// </summary>
public static class Permissions
{
    public const string ProjectsRead = "projects.read";
    public const string ProjectsWrite = "projects.write";
    public const string ReportsGenerate = "reports.generate";
    public const string BillingManage = "billing.manage";

    public const string PlatformOperate = "platform.operate";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        PermissionDefinition.Create(ProjectsRead, PermissionScope.Account, "Read the account's projects"),
        PermissionDefinition.Create(ProjectsWrite, PermissionScope.Account, "Create, change and delete the account's projects"),
        PermissionDefinition.Create(ReportsGenerate, PermissionScope.Account, "Generate and download the account's reports"),
        PermissionDefinition.Create(BillingManage, PermissionScope.Account, "Manage the account's subscription and billing"),
        PermissionDefinition.Create(PlatformOperate, PermissionScope.Platform, "Use the platform operator tools"),
    ];
}
