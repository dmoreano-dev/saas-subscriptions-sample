using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.UnitTests;

public class UserTests
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Create_PaddedMixedCaseEmail_KeepsTypedAddress()
    {
        // Act
        var user = User.Create(" A@X.com ", FixedNow);

        // Assert
        Assert.Equal("A@X.com", user.Email);
    }

    [Fact]
    public void Create_PaddedMixedCaseEmail_StoresNormalizedAddress()
    {
        // Act
        var user = User.Create(" A@X.com ", FixedNow);

        // Assert
        Assert.Equal("a@x.com", user.NormalizedEmail);
    }
}
