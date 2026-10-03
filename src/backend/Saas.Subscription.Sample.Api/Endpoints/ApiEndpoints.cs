namespace Saas.Subscription.Sample.Api.Endpoints;

/// <summary>
/// Maps the endpoint families. A family that is still contract-only answers 501 until the phase that owns it
/// replaces the handlers (accounts/capabilities P02, projects P02). Routes, DTOs and documented responses are the
/// contract and must not change when that happens.
/// </summary>
public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapAuthEndpoints();

        // Everything below needs a valid access token, including the handlers that still answer 501.
        var authenticated = api.MapGroup(string.Empty)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        authenticated.MapMeEndpoints();
        authenticated.MapAccountEndpoints();
        authenticated.MapProjectEndpoints();

        return app;
    }
}
