using Saas.Subscription.Sample.Application.Configuration;
using static Saas.Subscription.Sample.UnitTests.Configuration.OptionsTestData;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class FrontendOptionsValidatorTests
{
    private const string LocalOrigin = "http://localhost:5173";

    [Fact]
    public void Validate_HostedHttpsOrigins_Succeeds()
    {
        // Arrange
        var validator = new FrontendOptionsValidator(Hosted);
        var options = new FrontendOptions { BaseUrl = HttpsOrigin, AllowedOrigins = [HttpsOrigin] };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(LocalOrigin)]
    [InlineData("http://app.example.test")]
    [InlineData("https://localhost")]
    [InlineData("https://127.0.0.1")]
    public void Validate_HostedWithInsecureOrLocalBaseUrl_Fails(string baseUrl)
    {
        // Arrange
        var validator = new FrontendOptionsValidator(Hosted);
        var options = new FrontendOptions { BaseUrl = baseUrl };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_LocalHttpOrigin_Succeeds()
    {
        // Arrange
        var validator = new FrontendOptionsValidator(Local);
        var options = new FrontendOptions { BaseUrl = LocalOrigin, AllowedOrigins = [LocalOrigin] };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.test")]
    [InlineData("https://app.example.test/path")]
    [InlineData("https://app.example.test/?q=1")]
    [InlineData("ftp://app.example.test")]
    [InlineData("not a url")]
    public void Validate_AllowedOriginThatIsNotAnExactOrigin_Fails(string origin)
    {
        // Arrange
        var validator = new FrontendOptionsValidator(Local);
        var options = new FrontendOptions { BaseUrl = LocalOrigin, AllowedOrigins = [origin] };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }
}
