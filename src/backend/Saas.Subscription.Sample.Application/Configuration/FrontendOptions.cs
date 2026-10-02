using System.ComponentModel.DataAnnotations;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>The exact public origin of the React app, e.g. <c>https://app.example.com</c>.</summary>
    [Required]
    public string? BaseUrl { get; set; }

    /// <summary>Exact origins allowed for intentional direct cross-origin calls. Wildcards are rejected.</summary>
    public IReadOnlyList<string> AllowedOrigins { get; set; } = [];
}
