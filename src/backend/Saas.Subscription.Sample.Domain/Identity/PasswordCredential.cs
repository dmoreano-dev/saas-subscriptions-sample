namespace Saas.Subscription.Sample.Domain.Identity;

/// <summary>
/// The password login method of a user, kept apart from <see cref="User"/> so the hash is never loaded or
/// serialized by accident and so users without a password (corporate SSO later) simply have no row.
/// </summary>
public sealed class PasswordCredential
{
    private PasswordCredential()
    {
    }

    /// <summary>One credential per user; also the primary key.</summary>
    public Guid UserId { get; private set; }

    public string Algorithm { get; private set; } = null!;

    public string EncodedHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static PasswordCredential Create(Guid userId, StoredPasswordHash hash, DateTimeOffset now)
    {
        var credential = new PasswordCredential
        {
            UserId = userId,
            CreatedAt = now,
        };
        credential.Apply(hash, now);
        return credential;
    }

    /// <summary>Replaces the stored hash of the same password, e.g. after the work factor was raised.</summary>
    public void Rehash(StoredPasswordHash hash, DateTimeOffset now) => Apply(hash, now);

    public StoredPasswordHash ToStoredHash() => new(Algorithm, EncodedHash);

    private void Apply(StoredPasswordHash hash, DateTimeOffset now)
    {
        Algorithm = hash.Algorithm;
        EncodedHash = hash.EncodedHash;
        UpdatedAt = now;
    }
}
