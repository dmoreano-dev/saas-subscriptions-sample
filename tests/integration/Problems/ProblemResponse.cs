using System.Text.Json;

namespace Saas.Subscription.Sample.IntegrationTests.Problems;

/// <summary>The members of a problem response that the contract guarantees.</summary>
public sealed record ProblemResponse(
    int Status,
    string Code,
    string CorrelationId,
    string? TraceId,
    string Body,
    JsonElement Root)
{
    public const string MediaType = "application/problem+json";

    public static async Task<ProblemResponse> ReadAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement.Clone();

        return new ProblemResponse(
            root.GetProperty("status").GetInt32(),
            root.GetProperty("code").GetString()!,
            root.GetProperty("correlationId").GetString()!,
            root.TryGetProperty("traceId", out var traceId) ? traceId.GetString() : null,
            body,
            root);
    }
}
