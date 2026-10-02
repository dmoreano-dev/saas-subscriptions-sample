using System.ComponentModel.DataAnnotations;

namespace Saas.Subscription.Sample.Application.Configuration;

/// <summary>
/// Polling worker settings for the durable inbox/outbox (plan §7). Only the options exist at this
/// checkpoint; the worker itself arrives with the billing phase.
/// </summary>
public sealed class BackgroundWorkOptions
{
    public const string SectionName = "BackgroundWork";

    public bool Enabled { get; set; } = true;

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How long a claimed row stays owned by a worker before another may take it over.</summary>
    [Range(typeof(TimeSpan), "00:00:05", "01:00:00")]
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(60);

    [Range(1, 100)]
    public int BatchSize { get; set; } = 10;

    /// <summary>Bounded retries before a row moves to the failed state.</summary>
    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 5;
}
