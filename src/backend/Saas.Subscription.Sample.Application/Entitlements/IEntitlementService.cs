using Saas.Subscription.Sample.Application.Authorization;

namespace Saas.Subscription.Sample.Application.Entitlements;

/// <summary>
/// The account entitlement seam of plan §3: effective capabilities decided from PostgreSQL and the current time.
/// It takes a verified <see cref="AccountScope"/>, so it never answers for an account nobody authorized.
/// </summary>
public interface IEntitlementService
{
    Task<EntitlementSnapshot> GetAsync(AccountScope scope, CancellationToken cancellationToken);
}
