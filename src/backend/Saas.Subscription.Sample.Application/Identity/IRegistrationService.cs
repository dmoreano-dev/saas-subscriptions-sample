namespace Saas.Subscription.Sample.Application.Identity;

public interface IRegistrationService
{
    /// <summary>
    /// Creates the user, its password credential, its personal account and the owner membership in one
    /// transaction. The caller has already validated <paramref name="email"/> (<see cref="EmailPolicy"/>) and
    /// <paramref name="password"/> (<see cref="PasswordPolicy"/>).
    /// </summary>
    Task<RegistrationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="Created"/> is false when the email already belonged to a user: nothing was written and
/// <see cref="User"/> is a decoy shaped like a real result, so the HTTP response cannot reveal the difference
/// (account enumeration, ADR 0008). Only the server side (and tests) may look at <see cref="Created"/>.
/// </summary>
public sealed record RegistrationResult(RegisteredUser User, bool Created);

public sealed record RegisteredUser(
    Guid UserId,
    string Email,
    DateTimeOffset CreatedAt,
    Guid PersonalAccountId,
    string PersonalAccountName);
