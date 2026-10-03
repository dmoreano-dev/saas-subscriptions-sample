using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.UnitTests.Identity;
using static Saas.Subscription.Sample.UnitTests.Configuration.OptionsTestData;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class AuthenticationOptionsValidatorTests
{
    private const string UnusableKey = "do-not-leak-this-signing-key";

    public static TheoryData<bool, string?, string?> MissingSigningMaterial => new()
    {
        { true, null, TestKeys.KeyId },
        { true, "", TestKeys.KeyId },
        { true, TestKeys.P256Pkcs8Pem, null },
        { true, TestKeys.P256Pkcs8Pem, " " },
        { false, null, TestKeys.KeyId },
        { false, TestKeys.P256Pkcs8Pem, null },
        { false, null, null },
    };

    public static TheoryData<bool, string> UnusableSigningKeys => new()
    {
        { true, UnusableKey },
        { false, UnusableKey },
        { true, TestKeys.P384Pem },
        { true, TestKeys.RsaPem },
        { true, TestKeys.P256PublicPem },
    };

    private static AuthenticationOptionsValidator CreateValidator(bool hosted) => new(hosted ? Hosted : Local);

    [Theory]
    [MemberData(nameof(MissingSigningMaterial))]
    public void Validate_WithoutSigningMaterial_FailsInEveryEnvironment(bool hosted, string? signingKey, string? signingKeyId)
    {
        // Arrange
        var validator = CreateValidator(hosted);
        var options = new AuthenticationOptions { SigningKey = signingKey, SigningKeyId = signingKeyId };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Theory]
    [MemberData(nameof(UnusableSigningKeys))]
    public void Validate_SigningKeyThatIsNotAP256PrivateKey_FailsWithoutEchoingTheKey(bool hosted, string signingKey)
    {
        // Arrange
        var validator = CreateValidator(hosted);
        var options = new AuthenticationOptions { SigningKey = signingKey, SigningKeyId = TestKeys.KeyId };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.DoesNotContain(signingKey, result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_P256PrivateKeyAndKeyId_Succeeds(bool hosted)
    {
        // Arrange
        var validator = CreateValidator(hosted);
        var options = new AuthenticationOptions { SigningKey = TestKeys.P256Pkcs8Pem, SigningKeyId = TestKeys.KeyId };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }
}
