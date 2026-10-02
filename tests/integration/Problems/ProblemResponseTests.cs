using System.Net;
using Saas.Subscription.Sample.Api.Problems;

namespace Saas.Subscription.Sample.IntegrationTests.Problems;

public class ProblemResponseTests(ProblemTestHost host) : IClassFixture<ProblemTestHost>
{
    private const string UnknownPath = "/does-not-exist";
    private const string RetryAfterHeader = "Retry-After";
    private const string ValidCorrelationId = "client-request_42.a";
    private const int GeneratedCorrelationIdLength = 32;

    [Theory]
    [InlineData(ProblemTestHost.Unauthenticated, HttpStatusCode.Unauthorized, ProblemCodes.Unauthenticated)]
    [InlineData(ProblemTestHost.Forbidden, HttpStatusCode.Forbidden, ProblemCodes.Forbidden)]
    [InlineData(ProblemTestHost.CapabilityNotGranted, HttpStatusCode.Forbidden, ProblemCodes.CapabilityNotGranted)]
    [InlineData(ProblemTestHost.NotFound, HttpStatusCode.NotFound, ProblemCodes.NotFound)]
    [InlineData(ProblemTestHost.Conflict, HttpStatusCode.Conflict, ProblemCodes.Conflict)]
    [InlineData(ProblemTestHost.QuotaExceeded, HttpStatusCode.Conflict, ProblemCodes.QuotaExceeded)]
    [InlineData(ProblemTestHost.RateLimited, HttpStatusCode.TooManyRequests, ProblemCodes.RateLimited)]
    [InlineData(ProblemTestHost.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, ProblemCodes.ServiceUnavailable)]
    [InlineData(ProblemTestHost.BadRequest, HttpStatusCode.BadRequest, ProblemCodes.BadRequest)]
    [InlineData(ProblemTestHost.Validation, HttpStatusCode.BadRequest, ProblemCodes.ValidationFailed)]
    [InlineData(ProblemTestHost.BareUnauthorized, HttpStatusCode.Unauthorized, ProblemCodes.Unauthenticated)]
    [InlineData(ProblemTestHost.BareForbidden, HttpStatusCode.Forbidden, ProblemCodes.Forbidden)]
    [InlineData(ProblemTestHost.BareNotFound, HttpStatusCode.NotFound, ProblemCodes.NotFound)]
    [InlineData(ProblemTestHost.BareTooManyRequests, HttpStatusCode.TooManyRequests, ProblemCodes.RateLimited)]
    [InlineData(ProblemTestHost.ThrowDependencyUnavailable, HttpStatusCode.ServiceUnavailable, ProblemCodes.ServiceUnavailable)]
    [InlineData(ProblemTestHost.ThrowUnexpected, HttpStatusCode.InternalServerError, ProblemCodes.InternalError)]
    [InlineData(UnknownPath, HttpStatusCode.NotFound, ProblemCodes.NotFound)]
    public async Task Get_FailingRoute_ReturnsProblemWithStatusAndStableCode(string path, HttpStatusCode expectedStatus, string expectedCode)
    {
        // Arrange
        using var client = host.CreateClient();

        // Act
        using var response = await client.GetAsync(path);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(ProblemResponse.MediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.Equal(expectedCode, problem.Code);
    }

    [Theory]
    [InlineData(ProblemTestHost.ThrowDependencyUnavailable)]
    [InlineData(ProblemTestHost.ThrowUnexpected)]
    public async Task Get_RouteThatThrows_DoesNotLeakExceptionDetails(string path)
    {
        // Arrange
        using var client = host.CreateClient();

        // Act
        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.DoesNotContain(ProblemTestHost.SecretExceptionMessage, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_ValidationRoute_ReturnsFieldErrors()
    {
        // Arrange
        using var client = host.CreateClient();

        // Act
        using var response = await client.GetAsync(ProblemTestHost.Validation);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.True(problem.Root.GetProperty("errors").TryGetProperty(ProblemTestHost.ValidationField, out _));
    }

    [Fact]
    public async Task Get_RateLimitedRoute_SendsRetryAfter()
    {
        // Arrange
        using var client = host.CreateClient();

        // Act
        using var response = await client.GetAsync(ProblemTestHost.RateLimited);

        // Assert
        Assert.Equal(
            ProblemTestHost.RetryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Assert.Single(response.Headers.GetValues(RetryAfterHeader)));
    }

    [Fact]
    public async Task Get_FailingRoute_ReturnsCorrelationIdInHeaderAndBody()
    {
        // Arrange
        using var client = host.CreateClient();

        // Act
        using var response = await client.GetAsync(ProblemTestHost.NotFound);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(problem.CorrelationId, Assert.Single(response.Headers.GetValues(CorrelationId.HeaderName)));
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    [Fact]
    public async Task Get_WithValidCorrelationId_EchoesIt()
    {
        // Arrange
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ProblemTestHost.NotFound);
        request.Headers.Add(CorrelationId.HeaderName, ValidCorrelationId);

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.Equal(ValidCorrelationId, problem.CorrelationId);
        Assert.Equal(ValidCorrelationId, Assert.Single(response.Headers.GetValues(CorrelationId.HeaderName)));
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("semi;colon<script>")]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890123x")]
    public async Task Get_WithInvalidCorrelationId_ReplacesIt(string invalidCorrelationId)
    {
        // Arrange
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ProblemTestHost.NotFound);
        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, invalidCorrelationId);

        // Act
        using var response = await client.SendAsync(request);
        var problem = await ProblemResponse.ReadAsync(response);

        // Assert
        Assert.NotEqual(invalidCorrelationId, problem.CorrelationId);
        Assert.Equal(GeneratedCorrelationIdLength, problem.CorrelationId.Length);
    }

    [Fact]
    public async Task Get_SuccessfulRoute_ReturnsCorrelationIdHeader()
    {
        // Arrange
        using var client = host.CreateClient();

        // Act
        using var response = await client.GetAsync(ProblemTestHost.Ok);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(CorrelationId.HeaderName));
    }
}
