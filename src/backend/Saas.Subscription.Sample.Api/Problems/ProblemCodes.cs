using System.Reflection;

namespace Saas.Subscription.Sample.Api.Problems;

/// <summary>
/// Stable, English, machine-readable error codes returned in the <c>code</c> member of every problem
/// response. Clients branch on these, never on titles or details. Adding a code is additive; renaming or
/// removing one is a breaking contract change.
/// </summary>
public static class ProblemCodes
{
    // 400
    public const string BadRequest = "bad_request";
    public const string ValidationFailed = "validation_failed";
    public const string RegistrationFailed = "registration_failed";

    // 401
    public const string Unauthenticated = "unauthenticated";
    public const string InvalidCredentials = "invalid_credentials";

    // 403
    public const string Forbidden = "forbidden";
    public const string CapabilityNotGranted = "capability_not_granted";

    // 404
    public const string NotFound = "not_found";

    // 409
    public const string Conflict = "conflict";
    public const string QuotaExceeded = "quota_exceeded";

    // 429
    public const string RateLimited = "rate_limited";

    // 5xx
    public const string InternalError = "internal_error";
    public const string NotImplemented = "not_implemented";
    public const string ServiceUnavailable = "service_unavailable";

    /// <summary>Every declared code, published as the enum of the <c>code</c> member in the OpenAPI document.</summary>
    public static IReadOnlyList<string> All { get; } = typeof(ProblemCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field is { IsLiteral: true, FieldType.Name: nameof(String) })
        .Select(field => (string)field.GetRawConstantValue()!)
        .Order(StringComparer.Ordinal)
        .ToArray();

    /// <summary>The code used when a response only carries a status code and no explicit code.</summary>
    public static string DefaultFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status401Unauthorized => Unauthenticated,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status409Conflict => Conflict,
        StatusCodes.Status429TooManyRequests => RateLimited,
        StatusCodes.Status501NotImplemented => NotImplemented,
        StatusCodes.Status503ServiceUnavailable => ServiceUnavailable,
        >= 500 => InternalError,
        _ => BadRequest,
    };
}
