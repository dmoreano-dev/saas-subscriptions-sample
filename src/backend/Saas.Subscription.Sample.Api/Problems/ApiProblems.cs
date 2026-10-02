namespace Saas.Subscription.Sample.Api.Problems;

/// <summary>
/// The only way endpoints produce error responses, so status, code and shape stay consistent.
/// Details are fixed, generic strings: never exception messages, SQL, or data about other accounts.
/// </summary>
public static class ApiProblems
{
    public static IResult BadRequest(string code = ProblemCodes.BadRequest, string? detail = null) =>
        Create(StatusCodes.Status400BadRequest, code, detail);

    public static IResult Validation(IDictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, extensions: CodeExtension(ProblemCodes.ValidationFailed));

    public static IResult Unauthenticated(string code = ProblemCodes.Unauthenticated) =>
        Create(StatusCodes.Status401Unauthorized, code);

    public static IResult Forbidden(string code = ProblemCodes.Forbidden) =>
        Create(StatusCodes.Status403Forbidden, code);

    /// <summary>Also used for a resource or account the caller may not know exists (never a 403).</summary>
    public static IResult NotFound() => Create(StatusCodes.Status404NotFound, ProblemCodes.NotFound);

    public static IResult Conflict(string code = ProblemCodes.Conflict) =>
        Create(StatusCodes.Status409Conflict, code);

    /// <summary>A contractual limit (for example a monthly report quota) is a business conflict, not throttling.</summary>
    public static IResult QuotaExceeded() => Create(StatusCodes.Status409Conflict, ProblemCodes.QuotaExceeded);

    public static IResult RateLimited(TimeSpan retryAfter) =>
        new RetryAfterResult(Create(StatusCodes.Status429TooManyRequests, ProblemCodes.RateLimited), retryAfter);

    public static IResult ServiceUnavailable() =>
        Create(StatusCodes.Status503ServiceUnavailable, ProblemCodes.ServiceUnavailable);

    /// <summary>Temporary body of contract-only endpoints until their phase implements them.</summary>
    public static IResult NotImplemented() =>
        Create(StatusCodes.Status501NotImplemented, ProblemCodes.NotImplemented);

    private static IResult Create(int statusCode, string code, string? detail = null) =>
        Results.Problem(statusCode: statusCode, detail: detail, extensions: CodeExtension(code));

    private static Dictionary<string, object?> CodeExtension(string code) => new() { [ProblemDetailsMembers.Code] = code };

    private sealed class RetryAfterResult(IResult inner, TimeSpan retryAfter) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return inner.ExecuteAsync(httpContext);
        }
    }
}
