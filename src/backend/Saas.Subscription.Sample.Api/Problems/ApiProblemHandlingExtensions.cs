using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Saas.Subscription.Sample.Api.Problems;

public static class ApiProblemHandlingExtensions
{
    /// <summary>Registers problem responses, the exception handler and the JSON conventions shared by all DTOs.</summary>
    public static IServiceCollection AddApiProblemHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = Customize);
        services.AddExceptionHandler<ApiExceptionHandler>();

        services.ConfigureHttpJsonOptions(options =>
        {
            // Enums travel as camelCase strings (never numbers) so the contract survives reordering members.
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            // Numbers are numbers: the web defaults would also accept "42" and document it as integer|string.
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });

        return services;
    }

    /// <summary>
    /// Order matters: the correlation id first so every later response carries it, then the exception handler,
    /// then status-code pages, which give a problem body to bare 4xx/5xx results (including the 401/403 that
    /// the authentication middleware will produce in P01).
    /// </summary>
    public static IApplicationBuilder UseApiProblemHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    private static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;

        problem.Status = status;
        problem.Extensions.TryAdd(ProblemDetailsMembers.Code, ProblemCodes.DefaultFor(status));
        problem.Extensions[ProblemDetailsMembers.CorrelationId] =
            CorrelationId.Get(context.HttpContext) ?? CorrelationId.Resolve(null);
        problem.Extensions[ProblemDetailsMembers.TraceId] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    }
}
