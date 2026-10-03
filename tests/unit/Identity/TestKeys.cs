using System.Security.Cryptography;

namespace Saas.Subscription.Sample.UnitTests.Identity;

/// <summary>Throwaway keys generated per test run, so no private key is ever committed.</summary>
internal static class TestKeys
{
    public const string KeyId = "unit-test-key";

    public static string P256Pkcs8Pem { get; } = Export(ECCurve.NamedCurves.nistP256, key => key.ExportPkcs8PrivateKeyPem());

    public static string P256Sec1Pem { get; } = Export(ECCurve.NamedCurves.nistP256, key => key.ExportECPrivateKeyPem());

    public static string P256PublicPem { get; } = Export(ECCurve.NamedCurves.nistP256, key => key.ExportSubjectPublicKeyInfoPem());

    public static string P256EncryptedPem { get; } = Export(
        ECCurve.NamedCurves.nistP256,
        key => key.ExportEncryptedPkcs8PrivateKeyPem("passphrase", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1)));

    public static string P384Pem { get; } = Export(ECCurve.NamedCurves.nistP384, key => key.ExportPkcs8PrivateKeyPem());

    public static string RsaPem { get; } = ExportRsa();

    private static string Export(ECCurve curve, Func<ECDsa, string> export)
    {
        using var key = ECDsa.Create(curve);
        return export(key);
    }

    private static string ExportRsa()
    {
        using var key = RSA.Create(2048);
        return key.ExportPkcs8PrivateKeyPem();
    }
}
