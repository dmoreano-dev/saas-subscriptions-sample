using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.Application.Catalog;

public interface ICatalogReader
{
    Task<IReadOnlyList<PlanCatalogEntry>> GetPublishedPlansAsync(CancellationToken cancellationToken);
}

public sealed record PlanCatalogEntry(
    string PlanKey,
    string Name,
    AccountType AccountType,
    bool IsBillable,
    int Version,
    DateTimeOffset PublishedAt,
    IReadOnlyList<CapabilityEntry> Capabilities,
    IReadOnlyList<PriceEntry> Prices);

public sealed record CapabilityEntry(
    string Key,
    FeatureValueType ValueType,
    FeatureUnit? Unit,
    bool? BoolValue,
    int? LimitValue,
    bool IsUnlimited);

public sealed record PriceEntry(BillingInterval Interval, string Currency, int UnitAmountMinor, string? ExternalPriceId);
