using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.UnitTests.Identity;

public class PasswordPolicyTests
{
    public static TheoryData<string?, PasswordPolicyResult> Cases => new()
    {
        { null, PasswordPolicyResult.TooShort },
        { "", PasswordPolicyResult.TooShort },
        { new string('a', PasswordPolicy.MinLength - 1), PasswordPolicyResult.TooShort },
        { new string('a', PasswordPolicy.MinLength), PasswordPolicyResult.Valid },
        { new string('a', PasswordPolicy.MaxLength), PasswordPolicyResult.Valid },
        { new string('a', PasswordPolicy.MaxLength + 1), PasswordPolicyResult.TooLong },
        { new string('a', 100_000), PasswordPolicyResult.TooLong },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Check_LengthAroundTheBounds_ReturnsExpectedResult(string? password, PasswordPolicyResult expected)
    {
        // Act
        var result = PasswordPolicy.Check(password);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Check_MaxLengthOfSurrogatePairCharacters_IsValid()
    {
        // Arrange: each emoji is two UTF-16 units but one character.
        var password = string.Concat(Enumerable.Repeat("😀", PasswordPolicy.MaxLength));

        // Act
        var result = PasswordPolicy.Check(password);

        // Assert
        Assert.Equal(PasswordPolicyResult.Valid, result);
    }

    [Fact]
    public void Check_OneSurrogatePairCharacterOverTheMax_IsTooLong()
    {
        // Arrange
        var password = string.Concat(Enumerable.Repeat("😀", PasswordPolicy.MaxLength + 1));

        // Act
        var result = PasswordPolicy.Check(password);

        // Assert
        Assert.Equal(PasswordPolicyResult.TooLong, result);
    }
}
