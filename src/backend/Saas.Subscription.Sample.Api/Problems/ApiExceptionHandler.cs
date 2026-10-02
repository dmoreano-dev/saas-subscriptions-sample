using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Saas.Subscription.Sample.Application.Common;

namespace Saas.Subscription.Sample.Api.Problems;

/// <summary>
/// Last line of defence: turns an unhandled exception into a generic problem response. The exception is
/// logged by the exception-handler middleware; its message, type and stack trace are never returned.
/// </summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, code) = exception switch
        {
            DependencyUnavailableException => (StatusCodes.Status503ServiceUnavailable, ProblemCodes.ServiceUnavailable),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, ProblemCodes.BadRequest),
            _ => (StatusCodes.Status500InternalServerError, ProblemCodes.InternalError),
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Extensions = { [ProblemDetailsMembers.Code] = code },
            },
        });
    }
}
