namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

/// <summary>
/// Effective capabilities of one account. <see cref="ValidUntil"/> is the known instant at which the result
/// stops being true (for example a scheduled plan change); null means no known boundary.
/// </summary>
public sealed record CapabilitiesResponse(
    Guid AccountId,
    DateTimeOffset CalculatedAt,
    DateTimeOffset? ValidUntil,
    IReadOnlyList<CapabilityDto> Capabilities);
