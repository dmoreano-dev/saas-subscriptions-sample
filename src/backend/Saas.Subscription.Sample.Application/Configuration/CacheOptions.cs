using System.ComponentModel.DataAnnotations;

namespace Saas.Subscription.Sample.Application.Configuration;

public enum CacheProvider
{
    Memory,
    Redis,
}

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>Selected explicitly; deploying does not imply Redis (plan §10).</summary>
    public CacheProvider Provider { get; set; } = CacheProvider.Memory;

    /// <summary>Namespaces entries per environment so deployments never share keys.</summary>
    [Required]
    public string? KeyPrefix { get; set; }

    public RedisOptions Redis { get; set; } = new();
}

public sealed class RedisOptions
{
    /// <summary>Secret.</summary>
    public string? ConnectionString { get; set; }
}
