using Saas.Subscription.Sample.Infrastructure.Identity;

namespace Saas.Subscription.Sample.UnitTests.Identity;

public class AccessTokenValidationTests
{
    private const int SkewSeconds = 30;

    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(-600, 0, true)]
    [InlineData(-600, 1, true)]
    [InlineData(-600, -SkewSeconds, true)]
    [InlineData(-600, -SkewSeconds - 1, false)]
    [InlineData(SkewSeconds, 600, true)]
    [InlineData(SkewSeconds + 1, 600, false)]
    [InlineData(10, 5, false)]
    public void IsWithinLifetime_NotBeforeAndExpiryRelativeToNow_AllowsOnlyTheClockSkew(
        int notBeforeOffsetSeconds,
        int expiresOffsetSeconds,
        bool expected)
    {
        // Arrange
        var notBefore = Now.AddSeconds(notBeforeOffsetSeconds);
        var expires = Now.AddSeconds(expiresOffsetSeconds);

        // Act
        var valid = AccessTokenValidation.IsWithinLifetime(notBefore, expires, Now);

        // Assert
        Assert.Equal(expected, valid);
    }

    [Fact]
    public void IsWithinLifetime_NoExpiry_IsRejected()
    {
        // Act
        var valid = AccessTokenValidation.IsWithinLifetime(Now.AddMinutes(-1), expires: null, Now);

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public void IsWithinLifetime_NoNotBefore_DependsOnlyOnTheExpiry()
    {
        // Act
        var valid = AccessTokenValidation.IsWithinLifetime(notBefore: null, Now.AddMinutes(1), Now);

        // Assert
        Assert.True(valid);
    }
}
