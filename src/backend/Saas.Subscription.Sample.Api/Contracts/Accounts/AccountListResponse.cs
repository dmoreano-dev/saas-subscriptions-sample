namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

public sealed record AccountListResponse(IReadOnlyList<AccountSummary> Items);
