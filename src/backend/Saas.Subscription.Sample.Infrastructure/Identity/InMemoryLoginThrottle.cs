using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

/// <summary>
/// A fixed window per key held in this process: it is lost on restart and not shared between instances, which
/// is acceptable for the single-instance lab and documented in ADR 0008. Nothing runs in the background: a
/// window "unlocks" because the next attempt finds <c>now &gt;= windowStart + window</c> and starts a new one.
/// The number of tracked keys is capped, because the key is attacker-controlled text.
/// </summary>
public sealed class InMemoryLoginThrottle(TimeProvider time, LoginThrottlePolicy policy) : ILoginThrottle
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Window> _windows = [];

    /// <summary>Keys currently held; never above <see cref="LoginThrottlePolicy.MaxTrackedKeys"/>.</summary>
    public int TrackedKeyCount
    {
        get
        {
            lock (_gate)
            {
                return _windows.Count;
            }
        }
    }

    public LoginThrottleDecision TryBeginAttempt(string key)
    {
        var now = time.GetUtcNow();
        lock (_gate)
        {
            if (_windows.TryGetValue(key, out var current) && now < current.Start + policy.Window)
            {
                if (current.Attempts >= policy.MaxAttempts)
                {
                    return new LoginThrottleDecision(Allowed: false, current.Start + policy.Window - now);
                }

                _windows[key] = current with { Attempts = current.Attempts + 1 };
                return new LoginThrottleDecision(Allowed: true, TimeSpan.Zero);
            }

            if (!_windows.ContainsKey(key) && _windows.Count >= policy.MaxTrackedKeys)
            {
                MakeRoom(now);
            }

            _windows[key] = new Window(now, Attempts: 1);
            return new LoginThrottleDecision(Allowed: true, TimeSpan.Zero);
        }
    }

    public void RecordSuccess(string key)
    {
        lock (_gate)
        {
            _windows.Remove(key);
        }
    }

    /// <summary>Drops expired windows first; if the table is still full, drops the oldest one.</summary>
    private void MakeRoom(DateTimeOffset now)
    {
        foreach (var expired in _windows.Where(entry => now >= entry.Value.Start + policy.Window).Select(entry => entry.Key).ToList())
        {
            _windows.Remove(expired);
        }

        if (_windows.Count >= policy.MaxTrackedKeys)
        {
            _windows.Remove(_windows.MinBy(entry => entry.Value.Start).Key);
        }
    }

    private readonly record struct Window(DateTimeOffset Start, int Attempts);
}
