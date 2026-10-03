namespace Saas.Subscription.Sample.Application.Identity;

/// <summary>
/// The application identity proven by a successful credential check: the only thing JWT-03 needs to issue a
/// token. It is not a token, a session or a claim set, and carries no hash, status or account.
/// </summary>
public sealed record AuthenticatedIdentity(Guid UserId, string Email, DateTimeOffset CreatedAt);
