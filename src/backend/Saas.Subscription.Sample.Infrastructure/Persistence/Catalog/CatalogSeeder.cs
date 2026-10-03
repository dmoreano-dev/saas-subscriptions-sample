using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Catalog;

/// <summary>
/// Inserts the missing catalog fixtures. It is idempotent and never edits what exists: a published version
/// that differs from the fixtures is reported as a divergence, because changing it needs a new version.
/// </summary>
public static class CatalogSeeder
{
    public static async Task<CatalogSeedResult> SeedAsync(
        AppDbContext context,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var inserted = new List<string>();
        var divergences = new List<string>();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var definitions = await context.FeatureDefinitions.ToDictionaryAsync(d => d.Key, cancellationToken);
        foreach (var feature in CatalogSeed.Features)
        {
            if (!definitions.TryGetValue(feature.Key, out var existing))
            {
                var definition = feature.ValueType == FeatureValueType.Boolean
                    ? FeatureDefinition.Boolean(feature.Key, feature.Description)
                    : FeatureDefinition.Limit(feature.Key, feature.Unit!.Value, feature.Description);
                context.FeatureDefinitions.Add(definition);
                inserted.Add($"feature:{feature.Key}");
            }
            else if (existing.ValueType != feature.ValueType || existing.Unit != feature.Unit)
            {
                divergences.Add($"feature:{feature.Key} type or unit differs from the fixture");
            }
        }

        foreach (var seedPlan in CatalogSeed.Plans)
        {
            var plan = await context.Plans.SingleOrDefaultAsync(p => p.Key == seedPlan.Key, cancellationToken);
            if (plan is null)
            {
                plan = Plan.Create(seedPlan.Key, seedPlan.Name, seedPlan.AccountType, seedPlan.IsBillable, now);
                context.Plans.Add(plan);
                inserted.Add($"plan:{seedPlan.Key}");
            }

            var version = await context.PlanVersions
                .Include(v => v.Capabilities)
                .Include(v => v.Prices)
                .SingleOrDefaultAsync(v => v.PlanId == plan.Id && v.Version == CatalogSeed.InitialVersion, cancellationToken);

            if (version is null)
            {
                version = PlanVersion.CreateDraft(plan.Id, CatalogSeed.InitialVersion, now);
                foreach (var capability in seedPlan.Capabilities)
                {
                    version.AddCapability(capability.BoolValue is { } flag
                        ? PlanCapability.Boolean(capability.Key, flag)
                        : PlanCapability.Limit(capability.Key, capability.LimitValue!.Value));
                }

                foreach (var price in seedPlan.Prices)
                {
                    version.AddPrice(PlanPrice.Create(price.Interval, CatalogSeed.Currency, price.UnitAmountMinor));
                }

                // Rows are written while the version is a draft; publishing then freezes them.
                context.PlanVersions.Add(version);
                await context.SaveChangesAsync(cancellationToken);
                version.Publish(now);
                await context.SaveChangesAsync(cancellationToken);
                inserted.Add($"plan_version:{seedPlan.Key}:v{CatalogSeed.InitialVersion}");
            }
            else if (!Matches(version, seedPlan))
            {
                divergences.Add($"plan_version:{seedPlan.Key}:v{CatalogSeed.InitialVersion} differs from the fixture; publish a new version instead of editing it");
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CatalogSeedResult(inserted, divergences);
    }

    private static bool Matches(PlanVersion version, SeedPlan seedPlan)
    {
        var capabilities = version.Capabilities
            .Select(c => (c.FeatureKey, c.BoolValue, c.LimitValue, c.IsUnlimited))
            .Order();
        var expectedCapabilities = seedPlan.Capabilities
            .Select(c => (c.Key, c.BoolValue, c.LimitValue, false))
            .Order();
        var prices = version.Prices
            .Select(p => (p.Interval, p.Currency, p.UnitAmountMinor))
            .Order();
        var expectedPrices = seedPlan.Prices
            .Select(p => (p.Interval, CatalogSeed.Currency, p.UnitAmountMinor))
            .Order();

        return capabilities.SequenceEqual(expectedCapabilities) && prices.SequenceEqual(expectedPrices);
    }
}

public sealed record CatalogSeedResult(IReadOnlyList<string> Inserted, IReadOnlyList<string> Divergences);
