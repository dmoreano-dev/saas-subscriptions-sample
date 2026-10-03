using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Saas.Subscription.Sample.Application.Identity;

/// <summary>
/// Reads the configured access-token signing key: an unencrypted PEM private key of the size ES256 needs
/// (NIST P-256). Nothing here logs or echoes the value.
/// </summary>
public static class SigningKeyPem
{
    private const int Es256KeySize = 256;

    /// <summary>
    /// Returns false for anything that cannot sign with ES256. Line breaks may be written as the two characters
    /// <c>\n</c>, because some hosts only accept single-line environment values.
    /// </summary>
    public static bool TryImport(string? value, [NotNullWhen(true)] out ECDsa? key)
    {
        key = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = ECDsa.Create();
        try
        {
            candidate.ImportFromPem(value.Replace("\\n", "\n", StringComparison.Ordinal));
            if (candidate.KeySize == Es256KeySize)
            {
                // A public key imports without error; only signing shows that the private half is missing.
                candidate.SignData([], HashAlgorithmName.SHA256);
                key = candidate;
                return true;
            }
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            // Not PEM, not an EC key, encrypted or public-only: all the same "not usable" answer.
        }

        candidate.Dispose();
        return false;
    }
}
