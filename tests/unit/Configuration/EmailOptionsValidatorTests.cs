using Saas.Subscription.Sample.Application.Configuration;
using static Saas.Subscription.Sample.UnitTests.Configuration.OptionsTestData;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class EmailOptionsValidatorTests
{
    private const string AnyHost = "localhost";
    private const string AnyApiKey = "key";

    [Fact]
    public void Validate_HostedWithSmtp_Fails()
    {
        // Arrange
        var validator = new EmailOptionsValidator(Hosted);
        var options = new EmailOptions { Provider = EmailProvider.Smtp, Smtp = new SmtpOptions { Host = AnyHost } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.Contains("Https", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_LocalWithSmtpHost_Succeeds()
    {
        // Arrange
        var validator = new EmailOptionsValidator(Local);
        var options = new EmailOptions { Provider = EmailProvider.Smtp, Smtp = new SmtpOptions { Host = AnyHost } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(null, 1025)]
    [InlineData(AnyHost, 0)]
    [InlineData(AnyHost, 65536)]
    public void Validate_SmtpWithMissingHostOrInvalidPort_Fails(string? host, int port)
    {
        // Arrange
        var validator = new EmailOptionsValidator(Local);
        var options = new EmailOptions { Provider = EmailProvider.Smtp, Smtp = new SmtpOptions { Host = host, Port = port } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_HttpsWithoutApiKey_Fails(string? apiKey)
    {
        // Arrange
        var validator = new EmailOptionsValidator(Hosted);
        var options = new EmailOptions { Provider = EmailProvider.Https, Https = new HttpsEmailOptions { ApiKey = apiKey } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_HostedHttpsWithApiKey_Succeeds()
    {
        // Arrange
        var validator = new EmailOptionsValidator(Hosted);
        var options = new EmailOptions { Provider = EmailProvider.Https, Https = new HttpsEmailOptions { ApiKey = AnyApiKey } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }
}
