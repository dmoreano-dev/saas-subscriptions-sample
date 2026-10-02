using Saas.Subscription.Sample.Application.Configuration;
using static Saas.Subscription.Sample.UnitTests.Configuration.OptionsTestData;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class DatabaseOptionsValidatorTests
{
    private const string PlainConnectionString = "Host=h;Database=d;Username=u;Password=p";

    [Theory]
    [InlineData("SSL Mode=VerifyFull")]
    [InlineData("SslMode=VerifyFull")]
    [InlineData("SSL Mode=Verify-Full")]
    public void Validate_HostedWithVerifiedTls_Succeeds(string tlsSetting)
    {
        // Arrange
        var validator = new DatabaseOptionsValidator(Hosted);
        var options = new DatabaseOptions { ConnectionString = $"{PlainConnectionString};{tlsSetting}" };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData(";SSL Mode=Require")]
    [InlineData(";SSL Mode=Disable")]
    public void Validate_HostedWithoutVerifiedTls_Fails(string tlsSetting)
    {
        // Arrange
        var validator = new DatabaseOptionsValidator(Hosted);
        var options = new DatabaseOptions { ConnectionString = PlainConnectionString + tlsSetting };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_LocalWithoutTls_Succeeds()
    {
        // Arrange
        var validator = new DatabaseOptionsValidator(Local);
        var options = new DatabaseOptions { ConnectionString = PlainConnectionString };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_HostedWithMalformedConnectionString_FailsWithoutEchoingIt()
    {
        // Arrange
        const string malformed = "this is not a connection string password=hunter2 =";
        var validator = new DatabaseOptionsValidator(Hosted);
        var options = new DatabaseOptions { ConnectionString = malformed };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.DoesNotContain("hunter2", result.FailureMessage, StringComparison.Ordinal);
    }
}
