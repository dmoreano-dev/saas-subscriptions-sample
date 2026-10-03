namespace Saas.Subscription.Sample.Infrastructure.Identity;

/// <summary>The registered claim names (RFC 7519) an access token carries beyond issuer, audience and times.</summary>
public static class AccessTokenClaims
{
    /// <summary>The user id.</summary>
    public const string Subject = "sub";

    /// <summary>A unique id per token. Nothing stores or checks it at this checkpoint (no revocation until P06).</summary>
    public const string TokenId = "jti";
}
