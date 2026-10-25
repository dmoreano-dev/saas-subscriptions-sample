using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Application.Entitlements;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Subscriptions;

public sealed class EntitlementReader(AppDbContext db) : IEntitlementReader
{
    public async Task<EntitlementFacts> ReadAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var free = await LoadLatestFreeAsync(cancellationToken);

        var subscription = await db.Subscriptions.AsNoTracking()
            .SingleOrDefaultAsync(
                subscription => subscription.AccountId == accountId && subscription.Status != SubscriptionStatus.Ended,
                cancellationToken);

        if (subscription is null)
        {
            return new EntitlementFacts(free, Subscription: null);
        }

        var change = await db.SubscriptionChanges.AsNoTracking()
            .SingleOrDefaultAsync(
                change => change.SubscriptionId == subscription.Id && change.Status == SubscriptionChangeStatus.Pending,
                cancellationToken);

        var plan = await LoadPlanAsync(subscription.PlanVersionId, cancellationToken);
        var target = change?.TargetPlanVersionId is { } targetId ? await LoadPlanAsync(targetId, cancellationToken) : null;

        return new EntitlementFacts(
            free,
            new SubscriptionFacts(
                subscription.Status,
                subscription.CurrentPeriodStart,
                subscription.AccessUntil,
                plan,
                change is null ? null : new PendingChangeFacts(change.Kind, change.EffectiveAt, target)));
    }

    private async Task<PlanEntitlements> LoadLatestFreeAsync(CancellationToken cancellationToken)
    {
        // version.Plan.Key without Include: inside a query, EF turns the navigation into a join.
        var freeVersionId = await db.PlanVersions
            .Where(version => version.Status == PlanVersionStatus.Published && version.Plan.Key == PlanKeys.Free)
            .OrderByDescending(version => version.Version)
            .Select(version => (Guid?)version.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No published Free plan version; run the Migrator catalog seed.");

        return await LoadPlanAsync(freeVersionId, cancellationToken);
    }

    /// <summary>
    /// One plan version with its plan key and the capabilities it declares, ordered by key. A projection: EF joins the
    /// four tables through the navigations and reads only these columns; no entity is loaded.
    /// </summary>
    private Task<PlanEntitlements> LoadPlanAsync(Guid versionId, CancellationToken cancellationToken) =>
        db.PlanVersions
            .Where(version => version.Id == versionId)
            .Select(version => new PlanEntitlements(
                version.Plan.Key,
                version.Version,
                version.Capabilities
                    .OrderBy(capability => capability.FeatureKey)
                    .Select(capability => new CapabilityEntry(
                        capability.FeatureKey,
                        capability.Definition.ValueType,
                        capability.Definition.Unit,
                        capability.BoolValue,
                        capability.LimitValue,
                        capability.IsUnlimited))
                    .ToList()))
            .SingleAsync(cancellationToken);
}
