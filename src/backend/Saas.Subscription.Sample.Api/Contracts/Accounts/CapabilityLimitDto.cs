namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

/// <summary>
/// A numeric allowance. "Unlimited" is stated explicitly by <see cref="IsUnlimited"/> (and then
/// <see cref="Value"/> is null) instead of being encoded as an arbitrarily large number.
/// </summary>
public sealed record CapabilityLimitDto(bool IsUnlimited, int? Value);
