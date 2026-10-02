using Microsoft.Extensions.Time.Testing;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

/// <summary>
/// Business rules take <c>now</c> as a parameter; callers get it from <see cref="TimeProvider"/>, so a
/// <see cref="FakeTimeProvider"/> makes every timestamp deterministic.
/// </summary>
public class FakeClockTests
{
    private static readonly DateTimeOffset Start = new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly TimeSpan Elapsed = TimeSpan.FromDays(3);
    private const string AnyEmail = "a@example.test";
    private const string OtherEmail = "b@example.test";
    private const string AnyName = "Any";

    [Fact]
    public void Create_ClockAtFixedInstant_StampsThatInstant()
    {
        // Arrange
        var clock = new FakeTimeProvider(Start);

        // Act
        var user = User.Create(AnyEmail, clock.GetUtcNow());

        // Assert
        Assert.Equal(Start, user.CreatedAt);
    }

    [Fact]
    public void ChangeEmail_ClockAdvanced_UpdatesOnlyUpdatedAt()
    {
        // Arrange
        var clock = new FakeTimeProvider(Start);
        var user = User.Create(AnyEmail, clock.GetUtcNow());
        clock.Advance(Elapsed);

        // Act
        user.ChangeEmail(OtherEmail, clock.GetUtcNow());

        // Assert
        Assert.Equal(Start, user.CreatedAt);
        Assert.Equal(Start + Elapsed, user.UpdatedAt);
    }

    [Fact]
    public void CreateOrganization_ClockAtFixedInstant_StampsThatInstant()
    {
        // Arrange
        var clock = new FakeTimeProvider(Start);

        // Act
        var account = Account.CreateOrganization(AnyName, clock.GetUtcNow());

        // Assert
        Assert.Equal(Start, account.CreatedAt);
    }
}
