using System.Text.Json.Nodes;
using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

/// <summary>Keeps <c>appsettings.example.json</c> in step with the typed options so the placeholders never drift.</summary>
public class ExampleConfigurationTests
{
    private const string SolutionFileName = "Saas.Subscription.Sample.slnx";

    public static TheoryData<string, Type> Sections => new()
    {
        { DatabaseOptions.SectionName, typeof(DatabaseOptions) },
        { AuthenticationOptions.SectionName, typeof(AuthenticationOptions) },
        { BillingOptions.SectionName, typeof(BillingOptions) },
        { EmailOptions.SectionName, typeof(EmailOptions) },
        { CacheOptions.SectionName, typeof(CacheOptions) },
        { FrontendOptions.SectionName, typeof(FrontendOptions) },
        { BackgroundWorkOptions.SectionName, typeof(BackgroundWorkOptions) },
    };

    [Theory]
    [MemberData(nameof(Sections))]
    public void Example_Section_ListsExactlyTheOptionsProperties(string section, Type optionsType)
    {
        // Arrange
        var example = LoadExample()[section]!.AsObject();

        // Act
        var documented = LeafPaths(example).Order().ToArray();
        var declared = PropertyPaths(optionsType).Order().ToArray();

        // Assert
        Assert.Equal(declared, documented);
    }

    [Fact]
    public void Example_Root_ContainsOnlyOptionsSections()
    {
        // Arrange
        var expected = Sections.Select(row => (string)row[0]).Order().ToArray();

        // Act
        var actual = LoadExample().AsObject().Select(property => property.Key).Order().ToArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    private static JsonNode LoadExample() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "src", "backend", "Saas.Subscription.Sample.Api", "appsettings.example.json")))!;

    private static IEnumerable<string> LeafPaths(JsonObject node, string prefix = "") =>
        node.SelectMany(property => property.Value is JsonObject child
            ? LeafPaths(child, $"{prefix}{property.Key}.")
            : [$"{prefix}{property.Key}"]);

    private static IEnumerable<string> PropertyPaths(Type type, string prefix = "") =>
        type.GetProperties().SelectMany(property => property.PropertyType.Namespace == type.Namespace
            && property.PropertyType.IsClass
            ? PropertyPaths(property.PropertyType, $"{prefix}{property.Name}.")
            : [$"{prefix}{property.Name}"]);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles(SolutionFileName).Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException($"Could not find {SolutionFileName}.");
    }
}
