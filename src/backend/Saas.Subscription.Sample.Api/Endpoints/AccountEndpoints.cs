using Saas.Subscription.Sample.Api.Contracts.Accounts;
using Saas.Subscription.Sample.Api.Problems;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this RouteGroupBuilder api)
    {
        var accounts = api.MapGroup("/accounts")
            .WithTags("Accounts")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        accounts.MapGet("/", () => ApiProblems.NotImplemented())
            .WithName("ListAccounts")
            .WithSummary("Accounts the caller is a member of; accounts they cannot access are never listed.")
            .Produces<AccountListResponse>();

        // accountId is only a selector. Membership is verified server-side; a missing or foreign account is 404.
        accounts.MapGet("/{accountId:guid}/capabilities", (Guid accountId) => ApiProblems.NotImplemented())
            .WithName("GetAccountCapabilities")
            .WithSummary("Effective capabilities of the account now. Guides the UI; the API re-authorizes every action.")
            .Produces<CapabilitiesResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
