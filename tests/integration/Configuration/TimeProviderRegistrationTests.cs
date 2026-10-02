using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

public class TimeProviderRegistrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateTimeOffset FixedNow = new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public void Resolve_TimeProvider_ReturnsSystemClockByDefault()
    {
        // Act
        var timeProvider = factory.Services.GetRequiredService<TimeProvider>();

        // Assert
        Assert.Same(TimeProvider.System, timeProvider);
    }

    [Fact]
    public void Resolve_TimeProvider_ReturnsFakeClockWhenReplaced()
    {
        // Arrange
        var fake = new FakeTimeProvider(FixedNow);
        using var replaced = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(fake))));

        // Act
        var timeProvider = replaced.Services.GetRequiredService<TimeProvider>();

        // Assert
        Assert.Equal(FixedNow, timeProvider.GetUtcNow());
    }

    [Fact]
    public void GetUtcNow_ReplacedClockAdvanced_ReflectsTheAdvance()
    {
        // Arrange
        var fake = new FakeTimeProvider(FixedNow);
        var advance = TimeSpan.FromDays(3);
        using var replaced = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(fake))));
        var timeProvider = replaced.Services.GetRequiredService<TimeProvider>();

        // Act
        fake.Advance(advance);

        // Assert
        Assert.Equal(FixedNow + advance, timeProvider.GetUtcNow());
    }
}
