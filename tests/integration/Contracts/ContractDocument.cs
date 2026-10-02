using System.Text.Json.Nodes;

namespace Saas.Subscription.Sample.IntegrationTests.Contracts;

/// <summary>Access to the committed OpenAPI document, the contract the frontend types are generated from.</summary>
public static class ContractDocument
{
    private const string SolutionFileName = "Saas.Subscription.Sample.slnx";
    private static readonly string[] HttpMethods = ["get", "post", "put", "patch", "delete"];

    public static string Path { get; } = System.IO.Path.Combine(FindRepositoryRoot(), "docs", "contracts", "openapi.json");

    public static JsonNode Load() => JsonNode.Parse(File.ReadAllText(Path))!;

    public static IEnumerable<object[]> Operations() =>
        Load()["paths"]!.AsObject().SelectMany(path =>
            path.Value!.AsObject()
                .Where(operation => HttpMethods.Contains(operation.Key))
                .Select(operation => new object[] { operation.Key, path.Key }));

    public static JsonNode Operation(string method, string path) => Load()["paths"]![path]![method]!;

    public static IEnumerable<object[]> SchemaNames() =>
        Load()["components"]!["schemas"]!.AsObject().Select(schema => new object[] { schema.Key });

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles(SolutionFileName).Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException($"Could not find {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
