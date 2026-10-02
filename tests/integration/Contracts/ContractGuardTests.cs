using System.Text.Json.Nodes;
using Saas.Subscription.Sample.Api.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Contracts;

/// <summary>Structural rules every operation and DTO of the committed contract must satisfy.</summary>
public class ContractGuardTests
{
    private const string ProblemMediaType = "application/problem+json";
    private const string ProblemSchemaName = "ProblemDetails";
    private const string ValidationProblemSchemaName = "HttpValidationProblemDetails";
    private const string BearerSchemeName = "bearerAuth";

    private static readonly string[] AnonymousPaths = ["/api/auth/register", "/api/auth/login"];

    // Plan §9: 401, 403, 404, 409, 429 and 503, plus 400 for malformed or invalid input.
    private static readonly string[] AllowedErrorStatuses = ["400", "401", "403", "404", "409", "429", "503"];

    // Fragments that must never appear in a property of a response DTO (requests legitimately carry a password).
    private static readonly string[] ForbiddenResponseFragments =
        ["password", "hash", "salt", "secret", "refreshtoken", "apikey", "securitystamp", "accessversion", "normalized"];

    public static IEnumerable<object[]> Operations() => ContractDocument.Operations();

    public static IEnumerable<object[]> OperationsWithSecurity() =>
        ContractDocument.Operations().Select(operation =>
            new object[] { operation[0], operation[1], !AnonymousPaths.Contains((string)operation[1]) });

    public static IEnumerable<object[]> ResponseSchemaNames() =>
        ContractDocument.SchemaNames().Where(schema => !((string)schema[0]).EndsWith("Request", StringComparison.Ordinal));

    [Theory]
    [MemberData(nameof(Operations))]
    public void Operation_HasUniqueOperationId(string method, string path)
    {
        // Arrange
        var operationId = (string?)ContractDocument.Operation(method, path)["operationId"];
        var allIds = ContractDocument.Operations()
            .Select(operation => (string?)ContractDocument.Operation((string)operation[0], (string)operation[1])["operationId"])
            .ToList();

        // Act
        var occurrences = allIds.Count(id => id == operationId);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(operationId));
        Assert.Equal(1, occurrences);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public void Operation_ErrorResponses_UseProblemDetailsWithinAllowedStatuses(string method, string path)
    {
        // Arrange
        var responses = ContractDocument.Operation(method, path)["responses"]!.AsObject();

        // Act
        var violations = responses
            .Where(response => int.Parse(response.Key, System.Globalization.CultureInfo.InvariantCulture) >= 400)
            .Where(response => !AllowedErrorStatuses.Contains(response.Key) || !UsesProblemSchema(response.Value!))
            .Select(response => response.Key)
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(OperationsWithSecurity))]
    public void Operation_Security_MatchesAnonymousRoutes(string method, string path, bool requiresBearer)
    {
        // Arrange
        var security = ContractDocument.Operation(method, path)["security"]?.AsArray();

        // Act
        var declaresBearer = security?.Any(requirement => requirement!.AsObject().ContainsKey(BearerSchemeName)) ?? false;

        // Assert
        Assert.Equal(requiresBearer, declaresBearer);
    }

    [Theory]
    [MemberData(nameof(ResponseSchemaNames))]
    public void ResponseSchema_Properties_DoNotExposeSecretOrInternalFields(string schemaName)
    {
        // Arrange
        var properties = ContractDocument.Load()["components"]!["schemas"]![schemaName]!["properties"]?.AsObject();

        // Act
        var leaked = (properties?.Select(property => property.Key) ?? [])
            .Where(name => ForbiddenResponseFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Assert
        Assert.Empty(leaked);
    }

    [Fact]
    public void ProblemSchema_CodeEnum_MatchesProblemCodes()
    {
        // Arrange
        var schema = ContractDocument.Load()["components"]!["schemas"]![ProblemSchemaName]!;

        // Act
        var documented = schema["properties"]!["code"]!["enum"]!.AsArray().Select(code => (string)code!).Order(StringComparer.Ordinal);

        // Assert
        Assert.Equal(ProblemCodes.All, documented);
    }

    [Theory]
    [InlineData(ProblemSchemaName)]
    [InlineData(ValidationProblemSchemaName)]
    public void ProblemSchema_Requires_CodeAndCorrelationId(string schemaName)
    {
        // Arrange
        var schema = ContractDocument.Load()["components"]!["schemas"]![schemaName]!;

        // Act
        var required = schema["required"]!.AsArray().Select(name => (string)name!).ToList();

        // Assert
        Assert.Contains(ProblemDetailsMembers.Code, required);
        Assert.Contains(ProblemDetailsMembers.CorrelationId, required);
    }

    private static bool UsesProblemSchema(JsonNode response)
    {
        var reference = (string?)response["content"]?[ProblemMediaType]?["schema"]?["$ref"];
        return reference is not null
            && (reference.EndsWith($"/{ProblemSchemaName}", StringComparison.Ordinal)
                || reference.EndsWith($"/{ValidationProblemSchemaName}", StringComparison.Ordinal));
    }
}
