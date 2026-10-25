using System.Security.Claims;
using Saas.Subscription.Sample.Api.Authentication;
using Saas.Subscription.Sample.Api.Authorization;
using Saas.Subscription.Sample.Api.Contracts.Accounts;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Authorization;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this RouteGroupBuilder api)
    {
        var accounts = api.MapGroup("/accounts")
            .WithTags("Accounts")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        accounts.MapGet("/", ListAccountsAsync)
            .WithName("ListAccounts")
            .WithSummary("Accounts the caller is a member of; accounts they cannot access are never listed.")
            .Produces<AccountListResponse>();

        // accountId is only a selector. Membership is verified server-side; a missing or foreign account is 404.
        accounts.MapGet("/{accountId:guid}/capabilities", (Guid accountId) => ApiProblems.NotImplemented())
            .RequireAccountAccess()
            .WithName("GetAccountCapabilities")
            .WithSummary("Effective capabilities of the account now. Guides the UI; the API re-authorizes every action.")
            .Produces<CapabilitiesResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListAccountsAsync(
        ClaimsPrincipal caller,
        IAccountAccessService access,
        CancellationToken cancellationToken)
    {
        if (!caller.TryGetUserId(out var userId))
        {
            return ApiProblems.Unauthenticated();
        }

        var accounts = await access.ListAccountsAsync(userId, cancellationToken);
        return accounts is null
            ? ApiProblems.Unauthenticated()
            : Results.Ok(new AccountListResponse([.. accounts.Select(account =>
                new AccountSummary(account.Id, account.Name, (AccountType)account.Type, (AccountStatus)account.Status))]));
    }
}
