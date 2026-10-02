using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class BackgroundWorkOptionsValidatorTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_LeaseNotLongerThanPollInterval_Fails(int leaseSeconds)
    {
        // Arrange
        var validator = new BackgroundWorkOptionsValidator();
        var options = new BackgroundWorkOptions { PollInterval = Poll, LeaseDuration = TimeSpan.FromSeconds(leaseSeconds) };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_Defaults_Succeeds()
    {
        // Arrange
        var validator = new BackgroundWorkOptionsValidator();

        // Act
        var result = validator.Validate(null, new BackgroundWorkOptions());

        // Assert
        Assert.True(result.Succeeded);
    }
}
