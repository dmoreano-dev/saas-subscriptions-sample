using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public sealed class JwtAccessTokenIssuer(
    IOptions<AuthenticationOptions> options,
    AccessTokenSigningKeys keys,
    TimeProvider time) : IAccessTokenIssuer
{
    // Every time claim is set explicitly from the injected clock; the handler must not fill any from the system clock.
    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    public AccessToken Issue(Guid userId)
    {
        var settings = options.Value;

        // JWT times are whole seconds; truncating first keeps the returned expiry equal to the `exp` claim.
        var now = time.GetUtcNow();
        var issuedAt = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
        var expiresAt = issuedAt.Add(settings.AccessTokenLifetime);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = keys.Signing,
            Claims = new Dictionary<string, object>
            {
                [AccessTokenClaims.Subject] = userId.ToString("D"),
                [AccessTokenClaims.TokenId] = Guid.NewGuid().ToString("N"),
            },
        });

        return new AccessToken(token, expiresAt);
    }
}
