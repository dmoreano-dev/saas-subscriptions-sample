namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

/// <summary>An account the caller is a member of. Accounts the caller cannot access are never listed.</summary>
public sealed record AccountSummary(Guid Id, string Name, AccountType Type, AccountStatus Status);
