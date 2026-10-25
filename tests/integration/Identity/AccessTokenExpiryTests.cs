using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>An issued token over time: each test owns its host, because it moves the clock.</summary>
public sealed class AccessTokenExpiryTests
{
    private const int LifetimeSeconds = 600;
    private const int ClockSkewSeconds = 30;

    [Theory]
    [InlineData(0, HttpStatusCode.OK)]
    [InlineData(LifetimeSeconds, HttpStatusCode.OK)]
    [InlineData(LifetimeSeconds + ClockSkewSeconds, HttpStatusCode.OK)]
    [InlineData(LifetimeSeconds + ClockSkewSeconds + 1, HttpStatusCode.Unauthorized)]
    public async Task Get_ProtectedRouteSomeTimeAfterIssue_AcceptsTheTokenOnlyUntilExpiryPlusClockSkew(
        int secondsAfterIssue,
        HttpStatusCode expected)
    {
        // Arrange
        using var api = new FixedClockApiFixture();
        using var client = api.Factory.CreateClient();
        var token = api.Factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(Guid.CreateVersion7());
        using var request = FixedClockApiFixture.ProtectedRequest(token.Value);
        api.Clock.Advance(TimeSpan.FromSeconds(secondsAfterIssue));

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }
}
