namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

/// <summary>An immutable published plan version, for example <c>pro</c> version 1.</summary>
public sealed record PlanVersionRefDto(string Key, int Version);
