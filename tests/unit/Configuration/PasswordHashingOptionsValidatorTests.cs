using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class PasswordHashingOptionsValidatorTests
{
    [Fact]
    public void Validate_Defaults_Succeeds()
    {
        // Arrange
        var validator = new PasswordHashingOptionsValidator();

        // Act
        var result = validator.Validate(null, new PasswordHashingOptions());

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(PasswordHashingOptions.MinMemoryMebibytes - 1)]
    [InlineData(PasswordHashingOptions.MaxMemoryMebibytes + 1)]
    public void Validate_MemoryOutsideTheBounds_FailsNamingTheKey(int memoryMebibytes)
    {
        // Arrange
        var validator = new PasswordHashingOptionsValidator();
        var options = new PasswordHashingOptions { MemoryMebibytes = memoryMebibytes };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("PasswordHashing:MemoryMebibytes", result.FailureMessage);
    }

    [Theory]
    [InlineData(PasswordHashingOptions.MinIterations - 1)]
    [InlineData(PasswordHashingOptions.MaxIterations + 1)]
    public void Validate_IterationsOutsideTheBounds_FailsNamingTheKey(int iterations)
    {
        // Arrange
        var validator = new PasswordHashingOptionsValidator();
        var options = new PasswordHashingOptions { Iterations = iterations };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("PasswordHashing:Iterations", result.FailureMessage);
    }

    [Fact]
    public void Validate_BothOutsideTheBounds_ReportsBothKeys()
    {
        // Arrange
        var validator = new PasswordHashingOptionsValidator();
        var options = new PasswordHashingOptions { MemoryMebibytes = 0, Iterations = 0 };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.Contains("PasswordHashing:MemoryMebibytes", result.FailureMessage);
        Assert.Contains("PasswordHashing:Iterations", result.FailureMessage);
    }
}
