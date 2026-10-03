namespace Saas.Subscription.Sample.Application.Identity;

public interface IAccessTokenIssuer
{
    /// <summary>
    /// Issues a short-lived signed access token whose subject is <paramref name="userId"/>. The token says who the
    /// caller is and nothing else: no email, account, role or plan (those are resolved on the server per request).
    /// </summary>
    AccessToken Issue(Guid userId);
}

/// <param name="Value">The compact serialized token. A bearer secret: never logged.</param>
/// <param name="ExpiresAt">The instant in the token's <c>exp</c> claim.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
