namespace Saas.Subscription.Sample.Api.Problems;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationId.Resolve(context.Request.Headers[CorrelationId.HeaderName].FirstOrDefault());
        CorrelationId.Set(context, correlationId);
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { [ProblemDetailsMembers.CorrelationId] = correlationId }))
        {
            await next(context);
        }
    }
}
