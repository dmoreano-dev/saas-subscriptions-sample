using Saas.Subscription.Sample.Api.Contracts.Auth;
using Saas.Subscription.Sample.Api.Problems;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth")
            .WithTags("Auth")
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        // Duplicate emails must not be distinguishable from other registration failures (account enumeration):
        // the only documented failures are field validation errors and one generic registration_failed.
        auth.MapPost("/register", (RegisterRequest request) => ApiProblems.NotImplemented())
            .WithName("Register")
            .WithSummary("Creates a user and their personal account. Does not sign the user in.")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        auth.MapPost("/login", (LoginRequest request) => ApiProblems.NotImplemented())
            .WithName("Login")
            .WithSummary("Exchanges credentials for a short-lived access token. Failures are always the generic invalid_credentials.")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
