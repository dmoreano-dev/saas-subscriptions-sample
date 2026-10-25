using Saas.Subscription.Sample.Api.Authentication;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Authorization;

namespace Saas.Subscription.Sample.Api.Authorization;

/// <summary>Declares what an account-scoped endpoint needs; null means being an active member is enough.</summary>
public sealed record AccountAccessRequirement(string? Permission);

public static class AccountAccess
{
    public const string AccountIdRouteParameter = "accountId";

    private static readonly object ScopeKey = new();

    /// <summary>
    /// Every route with an <c>{accountId}</c> must call this. The account id in the route is only a selector: the
    /// filter verifies it against the database before the handler runs, and the handler works with
    /// <see cref="GetAccountScope"/>, never with the route value.
    /// </summary>
    public static RouteHandlerBuilder RequireAccountAccess(this RouteHandlerBuilder builder, string? permission = null) =>
        builder
            .WithMetadata(new AccountAccessRequirement(permission))
            .AddEndpointFilter(AuthorizeAsync);

    /// <summary>The verified account of this request. Throws when the endpoint did not call <see cref="RequireAccountAccess"/>.</summary>
    public static AccountScope GetAccountScope(this HttpContext httpContext) =>
        httpContext.Items[ScopeKey] as AccountScope
        ?? throw new InvalidOperationException("The endpoint has no verified account scope; map it with RequireAccountAccess.");

    private static async ValueTask<object?> AuthorizeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var requirement = httpContext.GetEndpoint()?.Metadata.GetMetadata<AccountAccessRequirement>()
            ?? throw new InvalidOperationException("The endpoint declares no account access requirement.");

        if (!httpContext.User.TryGetUserId(out var userId))
        {
            return ApiProblems.Unauthenticated();
        }

        if (!Guid.TryParse(httpContext.Request.RouteValues[AccountIdRouteParameter] as string, out var accountId))
        {
            return ApiProblems.NotFound();
        }

        var access = httpContext.RequestServices.GetRequiredService<IAccountAccessService>();
        var result = await access.AuthorizeAsync(userId, accountId, requirement.Permission, httpContext.RequestAborted);

        switch (result.Outcome)
        {
            case AccountAccessOutcome.Granted when result.Scope is not null:
                httpContext.Items[ScopeKey] = result.Scope;
                return await next(context);
            case AccountAccessOutcome.Unauthenticated:
                return ApiProblems.Unauthenticated();
            case AccountAccessOutcome.AccountSuspended:
                return ApiProblems.Forbidden(ProblemCodes.AccountSuspended);
            case AccountAccessOutcome.Forbidden:
                return ApiProblems.Forbidden();
            default:
                return ApiProblems.NotFound();
        }
    }
}
