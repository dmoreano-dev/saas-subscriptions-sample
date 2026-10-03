namespace Saas.Subscription.Sample.Application.Identity;

/// <summary>
/// Limits login attempts per email so a guessing attack cannot also burn the memory-hard hash. The key is the
/// normalized email as typed, whether or not it belongs to a user, so throttling never reveals which exist.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>
    /// Takes one attempt slot atomically before the password is checked (so concurrent attempts cannot all
    /// pass a check-then-record gap). A blocked attempt does not extend the window.
    /// </summary>
    LoginThrottleDecision TryBeginAttempt(string key);

    /// <summary>A correct password releases the slots held by the key.</summary>
    void RecordSuccess(string key);
}

public readonly record struct LoginThrottleDecision(bool Allowed, TimeSpan RetryAfter);

/// <summary>Lab values (see ADR 0008); JWT-05 owns the full abuse-control policy.</summary>
public sealed record LoginThrottlePolicy(int MaxAttempts, TimeSpan Window, int MaxTrackedKeys)
{
    public static LoginThrottlePolicy Default { get; } = new(MaxAttempts: 5, Window: TimeSpan.FromMinutes(15), MaxTrackedKeys: 10_000);
}
