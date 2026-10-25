namespace Saas.Subscription.Sample.Api.Endpoints;

/// <summary>
/// Maps the endpoint families. An operation that is still contract-only answers 501 until the item that owns it
/// replaces the handler (capabilities ENT-03, project creation ENT-04). Account-scoped routes are authorized first,
/// so even a 501 is only reachable by a member of the account.
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
