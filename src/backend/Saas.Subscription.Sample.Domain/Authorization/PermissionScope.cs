namespace Saas.Subscription.Sample.Domain.Authorization;

/// <summary>
/// Where a role or permission applies. Account roles are held through a membership of one account; platform roles
/// are held by a user directly and never grant access to an account's data.
/// </summary>
public enum PermissionScope
{
    Account,
    Platform,
}
