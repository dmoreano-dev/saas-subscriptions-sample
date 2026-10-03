using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

/// <summary>
/// The configured ES256 key pair, loaded once per process. Signing uses the private key; validation gets a
/// separate public-only key, so the code path that reads untrusted tokens never holds the private half.
/// </summary>
public sealed class AccessTokenSigningKeys : IDisposable
{
    private readonly ECDsa _privateKey;
    private readonly ECDsa _publicKey;

    public AccessTokenSigningKeys(IOptions<AuthenticationOptions> options)
    {
        var settings = options.Value;

        // Startup validation already rejected an unusable key; this only guards a caller that skipped it.
        if (!SigningKeyPem.TryImport(settings.SigningKey, out var privateKey) || string.IsNullOrWhiteSpace(settings.SigningKeyId))
        {
            throw new InvalidOperationException("The access-token signing key is not configured.");
        }

        _privateKey = privateKey;
        _publicKey = ECDsa.Create(privateKey.ExportParameters(includePrivateParameters: false));

        // The library's default factory caches signature providers process-wide, per key. That cache outlives this
        // object: after a dispose, a new instance with the same key would be handed a provider bound to the disposed one.
        var uncached = new CryptoProviderFactory { CacheSignatureProviders = false };

        Signing = new SigningCredentials(
            new ECDsaSecurityKey(_privateKey) { KeyId = settings.SigningKeyId, CryptoProviderFactory = uncached },
            SecurityAlgorithms.EcdsaSha256);
        Validation = new ECDsaSecurityKey(_publicKey) { KeyId = settings.SigningKeyId, CryptoProviderFactory = uncached };
    }

    public SigningCredentials Signing { get; }

    public SecurityKey Validation { get; }

    public void Dispose()
    {
        _privateKey.Dispose();
        _publicKey.Dispose();
    }
}
