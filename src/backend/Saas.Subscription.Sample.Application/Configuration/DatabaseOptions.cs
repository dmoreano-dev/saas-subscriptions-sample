using System.ComponentModel.DataAnnotations;

namespace Saas.Subscription.Sample.Application.Configuration;

/// <summary>PostgreSQL connectivity. The connection string is a secret: it is never logged or echoed in errors.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Connection name Aspire injects locally as <c>ConnectionStrings:appdb</c>; used when <see cref="ConnectionString"/> is not set.</summary>
    public const string AspireConnectionName = "appdb";

    [Required]
    public string? ConnectionString { get; set; }

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>Sized against the hosted project's connection limit (plan §10, Supabase).</summary>
    [Range(1, 100)]
    public int MaxPoolSize { get; set; } = 20;
}
