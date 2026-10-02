namespace Saas.Subscription.Sample.Api.OpenApi;

/// <summary>
/// Endpoint metadata that documents "requires a bearer token" in the OpenAPI document. It enforces nothing:
/// P01 replaces it with real authentication and <c>RequireAuthorization()</c>.
/// </summary>
public sealed class BearerAuthenticationRequirement
{
    public const string SchemeName = "bearerAuth";

    public static BearerAuthenticationRequirement Instance { get; } = new();
}
