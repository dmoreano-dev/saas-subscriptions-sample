using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

public class CacheOptionsValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_RedisWithoutConnectionString_Fails(string? connectionString)
    {
        // Arrange
        var validator = new CacheOptionsValidator();
        var options = new CacheOptions { Provider = CacheProvider.Redis, Redis = new RedisOptions { ConnectionString = connectionString } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_RedisWithConnectionString_Succeeds()
    {
        // Arrange
        var validator = new CacheOptionsValidator();
        var options = new CacheOptions { Provider = CacheProvider.Redis, Redis = new RedisOptions { ConnectionString = "localhost:6379" } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_MemoryWithoutRedisSettings_Succeeds()
    {
        // Arrange
        var validator = new CacheOptionsValidator();

        // Act
        var result = validator.Validate(null, new CacheOptions());

        // Assert
        Assert.True(result.Succeeded);
    }
}
