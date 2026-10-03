using System.Security.Claims;
using Saas.Subscription.Sample.Infrastructure.Identity;

namespace Saas.Subscription.Sample.Api.Authentication;

public static class CallerClaims
{
    /// <summary>The user id in the subject claim; false when it is missing or not a user id.</summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParseExact(principal.FindFirstValue(AccessTokenClaims.Subject), "D", out userId);

    /// <summary>Plan §5A: a token without a subject or a token id is not accepted, however valid its signature.</summary>
    public static bool HasRequiredAccessTokenClaims(this ClaimsPrincipal principal) =>
        principal.TryGetUserId(out _)
        && !string.IsNullOrWhiteSpace(principal.FindFirstValue(AccessTokenClaims.TokenId));
}
