using Saas.Subscription.Sample.Api.OpenApi;

namespace Saas.Subscription.Sample.Api.Endpoints;

/// <summary>
/// Maps the contract-only endpoint families. Handlers answer 501 until the phase that owns each family
/// replaces them (identity P01, accounts/capabilities P02, projects P02). Routes, DTOs and documented
/// responses are the contract and must not change when that happens.
/// </summary>
public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapAuthEndpoints();

        // Everything below needs a bearer token (documentation only until P01 enforces it).
        var authenticated = api.MapGroup(string.Empty)
            .WithMetadata(BearerAuthenticationRequirement.Instance)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        authenticated.MapMeEndpoints();
        authenticated.MapAccountEndpoints();
        authenticated.MapProjectEndpoints();

        return app;
    }
}
