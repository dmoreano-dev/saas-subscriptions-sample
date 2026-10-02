using Saas.Subscription.Sample.Application.Configuration;
using static Saas.Subscription.Sample.UnitTests.Configuration.OptionsTestData;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class AuthenticationOptionsValidatorTests
{
    private const string AnyKey = "key";

    [Theory]
    [InlineData(null, AnyKey)]
    [InlineData("", AnyKey)]
    [InlineData(AnyKey, null)]
    [InlineData(AnyKey, " ")]
    public void Validate_HostedWithoutSigningMaterial_Fails(string? signingKey, string? signingKeyId)
    {
        // Arrange
        var validator = new AuthenticationOptionsValidator(Hosted);
        var options = new AuthenticationOptions { SigningKey = signingKey, SigningKeyId = signingKeyId };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_HostedWithSigningMaterial_Succeeds()
    {
        // Arrange
        var validator = new AuthenticationOptionsValidator(Hosted);
        var options = new AuthenticationOptions { SigningKey = AnyKey, SigningKeyId = AnyKey };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_LocalWithoutSigningMaterial_Succeeds()
    {
        // Arrange
        var validator = new AuthenticationOptionsValidator(Local);

        // Act
        var result = validator.Validate(null, new AuthenticationOptions());

        // Assert
        Assert.True(result.Succeeded);
    }
}
