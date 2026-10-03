namespace Saas.Subscription.Sample.Application.Identity;

public interface ILoginService
{
    /// <summary>
    /// Verifies the credentials of one login attempt. Unknown email, wrong password, a user without a password
    /// and a disabled user are the same <see cref="LoginOutcome.InvalidCredentials"/>. No token is issued here.
    /// </summary>
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
}

public enum LoginOutcome
{
    Succeeded,
    InvalidCredentials,
    Throttled,
}

public sealed record LoginResult(
    LoginOutcome Outcome,
    AuthenticatedIdentity? Identity = null,
    TimeSpan? RetryAfter = null)
{
    public static LoginResult InvalidCredentials { get; } = new(LoginOutcome.InvalidCredentials);

    public static LoginResult Succeeded(AuthenticatedIdentity identity) => new(LoginOutcome.Succeeded, identity);

    public static LoginResult Throttled(TimeSpan retryAfter) => new(LoginOutcome.Throttled, RetryAfter: retryAfter);
}
