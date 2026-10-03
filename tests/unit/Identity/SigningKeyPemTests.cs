using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.UnitTests.Identity;

public class SigningKeyPemTests
{
    public static TheoryData<string> UsableKeys => new()
    {
        TestKeys.P256Pkcs8Pem,
        TestKeys.P256Sec1Pem,
        TestKeys.P256Pkcs8Pem.ReplaceLineEndings("\\n"),
        $"  {TestKeys.P256Pkcs8Pem}\n",
    };

    public static TheoryData<string?> UnusableKeys => new()
    {
        null,
        string.Empty,
        "   ",
        "not-a-pem-key",
        "-----BEGIN PRIVATE KEY-----\nAAAA\n-----END PRIVATE KEY-----",
        TestKeys.P256PublicPem,
        TestKeys.P256EncryptedPem,
        TestKeys.P384Pem,
        TestKeys.RsaPem,
    };

    [Theory]
    [MemberData(nameof(UsableKeys))]
    public void TryImport_P256PrivateKey_ReturnsAKeyThatCanSign(string pem)
    {
        // Act
        var imported = SigningKeyPem.TryImport(pem, out var key);

        // Assert
        Assert.True(imported);
        using (key)
        {
            Assert.NotEmpty(key!.SignData([1, 2, 3], System.Security.Cryptography.HashAlgorithmName.SHA256));
        }
    }

    [Theory]
    [MemberData(nameof(UnusableKeys))]
    public void TryImport_AnythingElse_ReturnsFalseAndNoKey(string? value)
    {
        // Act
        var imported = SigningKeyPem.TryImport(value, out var key);

        // Assert
        Assert.False(imported);
        Assert.Null(key);
    }
}
