namespace Saas.Subscription.Sample.Api.Contracts.Accounts;

/// <summary>
/// Effective capabilities of one account, decided from the server's current state (never from the access token).
/// <see cref="ValidUntil"/> is the known instant at which the result stops being true (an access deadline or a
/// scheduled change); null means no known boundary.
/// </summary>
/// <param name="Plan">The plan version in effect (Free when there is no current paid subscription).</param>
public sealed record CapabilitiesResponse(
    Guid AccountId,
    DateTimeOffset CalculatedAt,
    PlanVersionRefDto Plan,
    DateTimeOffset? ValidUntil,
    IReadOnlyList<CapabilityDto> Capabilities);
