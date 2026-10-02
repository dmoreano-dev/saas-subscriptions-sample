using Saas.Subscription.Sample.Api.Contracts.Users;
using Saas.Subscription.Sample.Api.Problems;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/me", () => ApiProblems.NotImplemented())
            .WithTags("Me")
            .WithName("GetMe")
            .WithSummary("The authenticated caller's profile.")
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
