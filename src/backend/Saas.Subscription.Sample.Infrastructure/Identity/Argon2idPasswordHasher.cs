using System.Security.Cryptography;
using System.Text;
using Geralt;
using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

/// <summary>
/// Argon2id through libsodium (Geralt). libsodium draws the 16-byte salt from the operating system CSPRNG,
/// encodes <c>$argon2id$v=19$m=…,t=…,p=1$salt$hash</c> (so the hash carries its own parameters) and compares
/// hashes in constant time. Parallelism is fixed at 1 by libsodium.
/// </summary>
/// <remarks>
/// There is one algorithm today. The algorithm name is still stored with every hash so a second one can be added
/// later (extract an interface then, not before): a stored algorithm this class does not know is a failed
/// verification.
/// </remarks>
public sealed class Argon2idPasswordHasher(IOptions<PasswordHashingOptions> options) : IPasswordHasher
{
    public const string Algorithm = "argon2id";
    private const int BytesPerMebibyte = 1024 * 1024;

    public StoredPasswordHash Hash(string password)
    {
        if (PasswordPolicy.Check(password) != PasswordPolicyResult.Valid)
        {
            // The message never includes the password.
            throw new ArgumentException("The password does not satisfy the password policy.", nameof(password));
        }

        var (iterations, memoryBytes) = CurrentCost();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var buffer = new char[Argon2id.HashSize];
        try
        {
            Argon2id.ComputeHash(buffer, passwordBytes, iterations, memoryBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }

        var encoded = new string(buffer.AsSpan().TrimEnd('\0'));
        return new StoredPasswordHash(Algorithm, encoded);
    }

    public PasswordVerificationResult Verify(string password, StoredPasswordHash stored)
    {
        // Only the upper bound: lowering the minimum later must not lock out existing people, but an oversized
        // input must not reach the expensive hash.
        if (string.IsNullOrEmpty(password)
            || PasswordPolicy.Check(password) == PasswordPolicyResult.TooLong
            || stored.Algorithm != Algorithm
            || !TryToBuffer(stored.EncodedHash, out var buffer))
        {
            return PasswordVerificationResult.Failed;
        }

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            if (!Argon2id.VerifyHash(buffer, passwordBytes))
            {
                return PasswordVerificationResult.Failed;
            }

            var (iterations, memoryBytes) = CurrentCost();
            return Argon2id.NeedsRehash(buffer, iterations, memoryBytes)
                ? PasswordVerificationResult.SucceededRehashNeeded
                : PasswordVerificationResult.Succeeded;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            // A corrupt or foreign encoding is a failed verification, not a crash.
            return PasswordVerificationResult.Failed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private (int Iterations, int MemoryBytes) CurrentCost()
    {
        var value = options.Value;
        return (value.Iterations, value.MemoryMebibytes * BytesPerMebibyte);
    }

    /// <summary>libsodium expects the encoded hash in a fixed, zero-padded buffer.</summary>
    private static bool TryToBuffer(string encodedHash, out char[] buffer)
    {
        buffer = new char[Argon2id.HashSize];
        if (encodedHash.Length >= buffer.Length)
        {
            return false;
        }

        encodedHash.CopyTo(buffer);
        return true;
    }
}
