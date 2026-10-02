namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

/// <summary>
/// What the account can do now. <see cref="Limit"/> is null for features. A capability that is absent from
/// the response is denied. This only guides the UI; the API re-authorizes every execution.
/// </summary>
public sealed record CapabilityDto(string Key, CapabilityKind Kind, bool Granted, CapabilityLimitDto? Limit);
