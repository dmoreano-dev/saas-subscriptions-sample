using System.Security.Cryptography;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

/// <summary>
/// A throwaway ES256 key generated per test run, so no private key is ever committed. Every test host is
/// configured with it unless a test needs a different one.
/// </summary>
public static class TestSigningKey
{
    public const string KeyId = "test-key-1";

    public static ECDsa Key { get; } = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public static string Pem { get; } = Key.ExportPkcs8PrivateKeyPem();

    public static string NewPem()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return key.ExportPkcs8PrivateKeyPem();
    }
}
