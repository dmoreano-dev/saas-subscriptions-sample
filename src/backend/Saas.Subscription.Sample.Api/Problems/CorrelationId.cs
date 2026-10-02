using System.Text.RegularExpressions;

namespace Saas.Subscription.Sample.Api.Problems;

/// <summary>
/// Per-request identifier used only for debugging: it lets a person quote an id from an error and lets
/// us find that request in the logs. It is not an authorization or tracing-security mechanism.
/// </summary>
public static partial class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";

    private const int MaxLength = 64;
    private const string ItemKey = "Saas.CorrelationId";

    /// <summary>
    /// A caller-supplied id is client-controlled input that ends up in logs and responses, so only a short,
    /// plain token is accepted; anything else is replaced to prevent log forging and oversized values.
    /// </summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxLength && AllowedCharacters().IsMatch(value);

    public static string Resolve(string? incoming) => IsValid(incoming) ? incoming! : Guid.NewGuid().ToString("N");

    public static void Set(HttpContext context, string correlationId) => context.Items[ItemKey] = correlationId;

    public static string? Get(HttpContext context) => context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex AllowedCharacters();
}
