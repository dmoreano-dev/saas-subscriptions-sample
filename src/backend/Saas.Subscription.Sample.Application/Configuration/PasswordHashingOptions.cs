namespace Saas.Subscription.Sample.Application.Configuration;

/// <summary>
/// Work factor of the current password hash (Argon2id). Raising it takes effect for new hashes immediately and for
/// existing ones as people sign in (rehash on success). Calibration: <c>docs/decisions/0007-password-hashing-and-credential-storage.md</c>.
/// </summary>
public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    /// <summary>OWASP minimum for Argon2id is 19 MiB with 2 iterations; the lab default is higher.</summary>
    public const int MinMemoryMebibytes = 19;

    /// <summary>
    /// OWASP minimum
    /// </summary>
    public const int MinIterations = 2;

    /// <summary>Sanity ceilings: an accidental huge value would exhaust the host on the first login.</summary>
    public const int MaxMemoryMebibytes = 1024;

    public const int MaxIterations = 20;

    public int MemoryMebibytes { get; set; } = 64;

    public int Iterations { get; set; } = 3;
}
