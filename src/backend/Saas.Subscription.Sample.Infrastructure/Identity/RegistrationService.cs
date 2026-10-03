using Microsoft.EntityFrameworkCore;
using Npgsql;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public sealed class RegistrationService(AppDbContext db, IPasswordHasher hasher, TimeProvider time) : IRegistrationService
{
    public const string PersonalAccountName = "Personal";

    private const string UniqueViolation = "23505";
    private const string UniqueEmailIndex = "ux_users_normalized_email";

    public async Task<RegistrationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        // The expensive hash runs before any database work, in the created and in the duplicate path alike.
        var hash = hasher.Hash(password);

        var user = User.Create(email, now);
        var account = Account.CreatePersonal(user.Id, PersonalAccountName, now);
        db.Users.Add(user);
        db.PasswordCredentials.Add(PasswordCredential.Create(user.Id, hash, now));
        db.Accounts.Add(account);
        db.Memberships.Add(Membership.Create(account.Id, user.Id, now));

        try
        {
            // One SaveChanges is one transaction: either all four rows exist or none does. There is no
            // existence check first (it would race); the unique index on the normalized email decides.
            await db.SaveChangesAsync(cancellationToken);
            return new RegistrationResult(ToResult(user, account), Created: true);
        }
        catch (DbUpdateException exception) when (IsDuplicateEmail(exception))
        {
            return new RegistrationResult(Decoy(email, now), Created: false);
        }
    }

    private static bool IsDuplicateEmail(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation, ConstraintName: UniqueEmailIndex };

    private static RegisteredUser ToResult(User user, Account account) =>
        new(user.Id, user.Email, user.CreatedAt, account.Id, account.Name);

    /// <summary>Same shape and id format as a real result, referring to nothing that was stored.</summary>
    private static RegisteredUser Decoy(string email, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), email.Trim(), now, Guid.CreateVersion7(now), PersonalAccountName);
}
