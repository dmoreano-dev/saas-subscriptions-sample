using Microsoft.Extensions.Time.Testing;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Infrastructure.Identity;

namespace Saas.Subscription.Sample.UnitTests.Identity;

public class InMemoryLoginThrottleTests
{
    private const string Key = "a@example.com";
    private const string OtherKey = "b@example.com";
    private const int MaxAttempts = 3;
    private const int MaxKeys = 3;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly LoginThrottlePolicy Policy = new(MaxAttempts, Window, MaxKeys);

    private static InMemoryLoginThrottle CreateThrottle(FakeTimeProvider time) => new(time, Policy);

    private static void ConsumeAttempts(InMemoryLoginThrottle throttle, string key, int count)
    {
        for (var i = 0; i < count; i++)
        {
            throttle.TryBeginAttempt(key);
        }
    }

    [Fact]
    public void TryBeginAttempt_UpToTheLimit_AllowsEveryAttempt()
    {
        // Arrange
        var throttle = CreateThrottle(new FakeTimeProvider());

        // Act
        var decisions = Enumerable.Range(0, MaxAttempts).Select(_ => throttle.TryBeginAttempt(Key).Allowed).ToList();

        // Assert
        Assert.All(decisions, Assert.True);
    }

    [Fact]
    public void TryBeginAttempt_BeyondTheLimit_BlocksWithTimeLeftInTheWindow()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var throttle = CreateThrottle(time);
        ConsumeAttempts(throttle, Key, MaxAttempts);
        var elapsed = TimeSpan.FromMinutes(5);
        time.Advance(elapsed);

        // Act
        var decision = throttle.TryBeginAttempt(Key);

        // Assert
        Assert.False(decision.Allowed);
        Assert.Equal(Window - elapsed, decision.RetryAfter);
    }

    [Fact]
    public void TryBeginAttempt_BlockedAttemptsInsideTheWindow_DoNotExtendIt()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var throttle = CreateThrottle(time);
        ConsumeAttempts(throttle, Key, MaxAttempts);
        time.Advance(Window - TimeSpan.FromMinutes(1));
        throttle.TryBeginAttempt(Key);
        time.Advance(TimeSpan.FromMinutes(1));

        // Act
        var decision = throttle.TryBeginAttempt(Key);

        // Assert
        Assert.True(decision.Allowed);
    }

    [Fact]
    public void TryBeginAttempt_AfterTheWindow_StartsAFreshWindow()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var throttle = CreateThrottle(time);
        ConsumeAttempts(throttle, Key, MaxAttempts);
        time.Advance(Window);

        // Act
        var decisions = Enumerable.Range(0, MaxAttempts + 1).Select(_ => throttle.TryBeginAttempt(Key).Allowed).ToList();

        // Assert
        Assert.Equal([true, true, true, false], decisions);
    }

    [Fact]
    public void TryBeginAttempt_OtherKey_IsNotAffected()
    {
        // Arrange
        var throttle = CreateThrottle(new FakeTimeProvider());
        ConsumeAttempts(throttle, Key, MaxAttempts + 1);

        // Act
        var decision = throttle.TryBeginAttempt(OtherKey);

        // Assert
        Assert.True(decision.Allowed);
    }

    [Fact]
    public void RecordSuccess_AfterFailures_ReleasesTheKey()
    {
        // Arrange
        var throttle = CreateThrottle(new FakeTimeProvider());
        ConsumeAttempts(throttle, Key, MaxAttempts - 1);
        throttle.RecordSuccess(Key);

        // Act
        var decisions = Enumerable.Range(0, MaxAttempts).Select(_ => throttle.TryBeginAttempt(Key).Allowed).ToList();

        // Assert
        Assert.All(decisions, Assert.True);
    }

    [Fact]
    public void TryBeginAttempt_ManyDistinctKeys_NeverTracksMoreThanTheCap()
    {
        // Arrange
        var throttle = CreateThrottle(new FakeTimeProvider());

        // Act
        for (var i = 0; i < MaxKeys * 4; i++)
        {
            throttle.TryBeginAttempt($"user{i}@example.com");
        }

        // Assert
        Assert.Equal(MaxKeys, throttle.TrackedKeyCount);
    }

    [Fact]
    public void TryBeginAttempt_FullTable_DropsExpiredWindowsBeforeEvictingLiveOnes()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var throttle = CreateThrottle(time);
        throttle.TryBeginAttempt("old1@example.com");
        throttle.TryBeginAttempt("old2@example.com");
        time.Advance(Window);
        ConsumeAttempts(throttle, Key, MaxAttempts);

        // Act
        throttle.TryBeginAttempt(OtherKey);

        // Assert
        Assert.Equal(2, throttle.TrackedKeyCount);
        Assert.False(throttle.TryBeginAttempt(Key).Allowed);
    }
}
