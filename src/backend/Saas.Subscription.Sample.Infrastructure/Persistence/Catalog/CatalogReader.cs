using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Catalog;

public sealed class CatalogReader(AppDbContext context) : ICatalogReader
{
    public async Task<IReadOnlyList<PlanCatalogEntry>> GetPublishedPlansAsync(CancellationToken cancellationToken)
    {
        var plans = await context.Plans.AsNoTracking().OrderBy(plan => plan.Key).ToListAsync(cancellationToken);
        var versions = await context.PlanVersions.AsNoTracking()
            .Include(version => version.Capabilities)
            .Include(version => version.Prices)
            .Where(version => version.Status == PlanVersionStatus.Published)
            .ToListAsync(cancellationToken);
        var definitions = await context.FeatureDefinitions.AsNoTracking()
            .ToDictionaryAsync(definition => definition.Key, cancellationToken);

        var entries = new List<PlanCatalogEntry>();
        foreach (var plan in plans)
        {
            var latest = versions.Where(version => version.PlanId == plan.Id).MaxBy(version => version.Version);
            if (latest is null)
            {
                continue;
            }

            entries.Add(new PlanCatalogEntry(
                plan.Key,
                plan.Name,
                plan.AccountType,
                plan.IsBillable,
                latest.Version,
                latest.PublishedAt!.Value,
                latest.Capabilities
                    .OrderBy(capability => capability.FeatureKey, StringComparer.Ordinal)
                    .Select(capability => new CapabilityEntry(
                        capability.FeatureKey,
                        definitions[capability.FeatureKey].ValueType,
                        definitions[capability.FeatureKey].Unit,
                        capability.BoolValue,
                        capability.LimitValue,
                        capability.IsUnlimited))
                    .ToList(),
                latest.Prices
                    .OrderBy(price => price.Interval)
                    .ThenBy(price => price.Currency, StringComparer.Ordinal)
                    .Select(price => new PriceEntry(price.Interval, price.Currency, price.UnitAmountMinor, price.ExternalPriceId))
                    .ToList()));
        }

        return entries;
    }
}
