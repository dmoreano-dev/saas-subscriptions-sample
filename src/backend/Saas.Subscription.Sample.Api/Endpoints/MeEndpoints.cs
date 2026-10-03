using System.Security.Claims;
using Saas.Subscription.Sample.Api.Authentication;
using Saas.Subscription.Sample.Api.Contracts.Users;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/me", GetMeAsync)
            .WithTags("Me")
            .WithName("GetMe")
            .WithSummary("The authenticated caller's profile.")
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// The token only names the caller; the profile is read from the database. A token whose user no longer exists
    /// or is disabled is answered like no token at all. This is a lookup, not a session: nothing is stored per token.
    /// </summary>
    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal caller,
        IUserProfileReader users,
        CancellationToken cancellationToken)
    {
        if (!caller.TryGetUserId(out var userId))
        {
            return ApiProblems.Unauthenticated();
        }

        var profile = await users.FindActiveAsync(userId, cancellationToken);
        return profile is null
            ? ApiProblems.Unauthenticated()
            : Results.Ok(new UserDto(profile.UserId, profile.Email, profile.CreatedAt));
    }
}
