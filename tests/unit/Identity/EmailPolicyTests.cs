using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.UnitTests.Identity;

public class EmailPolicyTests
{
    private const string Domain = "@example.com";

    [Theory]
    [InlineData("a@example.com")]
    [InlineData("  A.B+tag@Sub.Example.COM  ")]
    public void Check_WellFormedAddress_ReturnsValid(string email)
    {
        // Arrange / Act
        var result = EmailPolicy.Check(email);

        // Assert
        Assert.Equal(EmailPolicyResult.Valid, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Check_BlankInput_ReturnsEmpty(string? email)
    {
        // Arrange / Act
        var result = EmailPolicy.Check(email);

        // Assert
        Assert.Equal(EmailPolicyResult.Empty, result);
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("@example.com")]
    [InlineData("a@")]
    [InlineData("a@@example.com")]
    [InlineData("a@b@example.com")]
    [InlineData("a b@example.com")]
    [InlineData("a@exa mple.com")]
    [InlineData("a@localhost")]
    [InlineData("a@.example.com")]
    [InlineData("a@example.com.")]
    [InlineData("a@example..com")]
    public void Check_MalformedAddress_ReturnsInvalidFormat(string email)
    {
        // Arrange / Act
        var result = EmailPolicy.Check(email);

        // Assert
        Assert.Equal(EmailPolicyResult.InvalidFormat, result);
    }

    [Fact]
    public void Check_LongerThanLimit_ReturnsTooLong()
    {
        // Arrange
        var email = new string('a', EmailPolicy.MaxLength - Domain.Length + 1) + Domain;

        // Act
        var result = EmailPolicy.Check(email);

        // Assert
        Assert.Equal(EmailPolicyResult.TooLong, result);
    }

    [Fact]
    public void Check_LocalPartLongerThanLimit_ReturnsInvalidFormat()
    {
        // Arrange
        var email = new string('a', EmailPolicy.MaxLocalPartLength + 1) + Domain;

        // Act
        var result = EmailPolicy.Check(email);

        // Assert
        Assert.Equal(EmailPolicyResult.InvalidFormat, result);
    }
}
