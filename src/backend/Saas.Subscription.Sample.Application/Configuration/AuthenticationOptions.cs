using System.ComponentModel.DataAnnotations;

namespace Saas.Subscription.Sample.Application.Configuration;

/// <summary>
/// Token settings. Only the options exist at this checkpoint; issuance and validation arrive with JWT-03.
/// </summary>
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    [Required]
    public string? Issuer { get; set; }

    [Required]
    public string? Audience { get; set; }

    /// <summary>10-minute baseline (plan §5A).</summary>
    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Secret signing material. Opaque until JWT-03 chooses the algorithm; the plan prefers an asymmetric
    /// persistent key. Never regenerated on startup, never logged.
    /// </summary>
    public string? SigningKey { get; set; }

    public string? SigningKeyId { get; set; }
}
