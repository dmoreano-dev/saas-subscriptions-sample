using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.UnitTests;

public class EmailNormalizerTests
{
    [Theory]
    [InlineData("A@X.com", "a@x.com")]
    [InlineData(" a@x.com ", "a@x.com")]
    public void Normalize_MixedCaseOrPaddedEmail_ReturnsTrimmedLowercase(string email, string expected)
    {
        // Act
        var actual = EmailNormalizer.Normalize(email);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Normalize_BlankEmail_ThrowsArgumentException(string blankEmail)
    {
        // Act
        var act = () => EmailNormalizer.Normalize(blankEmail);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}
