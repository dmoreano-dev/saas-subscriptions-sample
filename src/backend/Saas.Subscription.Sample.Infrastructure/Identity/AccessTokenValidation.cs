using Microsoft.IdentityModel.Tokens;
using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public static class AccessTokenValidation
{
    /// <summary>
    /// Grace period added to <c>exp</c> and subtracted from <c>nbf</c> for clock differences between machines.
    /// Explicit because the library default is 5 minutes, which would silently turn a 10-minute token into 15.
    /// </summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    /// <summary>
    /// What a token must satisfy to be accepted: signed with ES256 by the configured key (matched by <c>kid</c>),
    /// issued by this API for this API, inside its lifetime. Subject and token id are checked by the caller,
    /// because the library has no "required claim" setting.
    /// </summary>
    public static TokenValidationParameters CreateParameters(
        AuthenticationOptions options,
        AccessTokenSigningKeys keys,
        TimeProvider time) => new()
        {
            // An allow-list, so `none`, HMAC (public key used as a shared secret) and other curves are all refused.
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            RequireSignedTokens = true,
            IssuerSigningKey = keys.Validation,
            ValidateIssuerSigningKey = true,
            TryAllIssuerSigningKeys = false,

            ValidateIssuer = true,
            ValidIssuer = options.Issuer,

            ValidateAudience = true,
            RequireAudience = true,
            ValidAudience = options.Audience,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = ClockSkew,
            LifetimeValidator = (notBefore, expires, _, _) => IsWithinLifetime(notBefore, expires, time.GetUtcNow().UtcDateTime),

            NameClaimType = AccessTokenClaims.Subject,
        };

    /// <summary>
    /// The library's own lifetime rules, evaluated against the injected clock (it reads the system clock and has no
    /// public seam for another one): an expiry is mandatory, and the token is valid from <c>nbf - skew</c> up to
    /// and including <c>exp + skew</c>.
    /// </summary>
    public static bool IsWithinLifetime(DateTime? notBefore, DateTime? expires, DateTime utcNow)
    {
        if (expires is null || notBefore > expires)
        {
            return false;
        }

        if (notBefore is not null && notBefore.Value > utcNow + ClockSkew)
        {
            return false;
        }

        return expires.Value >= utcNow - ClockSkew;
    }
}
