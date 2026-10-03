using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public sealed partial class LoginService(
    AppDbContext db,
    IPasswordHasher hasher,
    DummyPasswordHash dummyHash,
    ILoginThrottle throttle,
    TimeProvider time,
    ILogger<LoginService> logger) : ILoginService
{
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        // Input that could never belong to a user is rejected before it costs a hash or a throttle slot.
        if (string.IsNullOrWhiteSpace(email)
            || email.Length > EmailPolicy.MaxLength
            || string.IsNullOrEmpty(password)
            || PasswordPolicy.Check(password) == PasswordPolicyResult.TooLong)
        {
            return LoginResult.InvalidCredentials;
        }

        var key = EmailNormalizer.Normalize(email);

        var decision = throttle.TryBeginAttempt(key);
        if (!decision.Allowed)
        {
            return LoginResult.Throttled(decision.RetryAfter);
        }

        var candidate = await FindAsync(key, cancellationToken);

        // Without a real credential the password is checked against a dummy hash, so every outcome pays for one
        // verification and the generic result is the only thing a caller can observe.
        var stored = candidate?.Hash ?? dummyHash.Value;
        var verification = hasher.Verify(password, stored);

        if (candidate is null
            || candidate.Hash is null
            || candidate.Status != UserStatus.Active
            || verification == PasswordVerificationResult.Failed)
        {
            return LoginResult.InvalidCredentials;
        }

        throttle.RecordSuccess(key);

        if (verification == PasswordVerificationResult.SucceededRehashNeeded)
        {
            await TryRehashAsync(candidate.UserId, stored, password, cancellationToken);
        }

        return LoginResult.Succeeded(new AuthenticatedIdentity(candidate.UserId, candidate.Email, candidate.CreatedAt));
    }

    private async Task<Candidate?> FindAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        // Left join: a user without a password row still comes back, with null algorithm and hash. The `!` only
        // silences the compiler, because `credential` is null in that case and EF translates it to NULL columns.
        var row = await db.Users.AsNoTracking()
            .Where(user => user.NormalizedEmail == normalizedEmail)
            .LeftJoin(
                db.PasswordCredentials.AsNoTracking(),
                user => user.Id,
                credential => credential.UserId,
                (user, credential) => new
                {
                    user.Id,
                    user.Email,
                    user.Status,
                    user.CreatedAt,
                    Algorithm = credential!.Algorithm,
                    EncodedHash = credential.EncodedHash,
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var hash = row.Algorithm is not null && row.EncodedHash is not null
            ? new StoredPasswordHash(row.Algorithm, row.EncodedHash)
            : null;
        return new Candidate(row.Id, row.Email, row.Status, row.CreatedAt, hash);
    }

    /// <summary>
    /// Upgrades the stored hash after a successful login. It only replaces the exact hash that was verified, so a
    /// concurrent password change is never overwritten, and it never fails the login.
    /// </summary>
    private async Task TryRehashAsync(Guid userId, StoredPasswordHash verified, string password, CancellationToken cancellationToken)
    {
        try
        {
            var upgraded = hasher.Hash(password);
            var now = time.GetUtcNow();
            await db.PasswordCredentials
                .Where(credential => credential.UserId == userId && credential.EncodedHash == verified.EncodedHash)
                .ExecuteUpdateAsync(
                    update => update
                        .SetProperty(credential => credential.Algorithm, upgraded.Algorithm)
                        .SetProperty(credential => credential.EncodedHash, upgraded.EncodedHash)
                        .SetProperty(credential => credential.UpdatedAt, now),
                    cancellationToken);
        }
        catch (Exception exception) when (exception is ArgumentException or DbException or InvalidOperationException)
        {
            // Only the type is logged: messages may echo values. The login itself already succeeded.
            LogRehashFailed(userId, exception.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Password rehash after login failed for user {UserId} ({ExceptionType}).")]
    private partial void LogRehashFailed(Guid userId, string exceptionType);

    private sealed record Candidate(Guid UserId, string Email, UserStatus Status, DateTimeOffset CreatedAt, StoredPasswordHash? Hash);
}
