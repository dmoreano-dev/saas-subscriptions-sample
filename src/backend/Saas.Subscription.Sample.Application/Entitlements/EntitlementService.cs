using Saas.Subscription.Sample.Application.Authorization;

namespace Saas.Subscription.Sample.Application.Entitlements;

public sealed class EntitlementService(IEntitlementReader reader, TimeProvider timeProvider) : IEntitlementService
{
    public async Task<EntitlementSnapshot> GetAsync(AccountScope scope, CancellationToken cancellationToken)
    {
        var facts = await reader.ReadAsync(scope.AccountId, cancellationToken);
        return EntitlementPolicy.Resolve(scope.AccountId, facts, timeProvider.GetUtcNow());
    }
}
