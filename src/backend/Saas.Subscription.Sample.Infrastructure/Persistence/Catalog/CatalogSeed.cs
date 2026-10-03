using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Catalog;

/// <summary>The lab fixtures from docs/plan.md §2 (baseline lab values). Version 1 of every plan.</summary>
internal static class CatalogSeed
{
    public const int InitialVersion = 1;
    public const string Currency = "USD";

    public static readonly IReadOnlyList<SeedFeature> Features =
    [
        new(FeatureKeys.ProjectsMaxCount, FeatureValueType.Limit, FeatureUnit.Projects, "Maximum active projects"),
        new(FeatureKeys.ReportsBasic, FeatureValueType.Boolean, null, "Basic reports"),
        new(FeatureKeys.ReportsAdvanced, FeatureValueType.Boolean, null, "Advanced reports"),
        new(FeatureKeys.ReportsMonthlyLimit, FeatureValueType.Limit, FeatureUnit.Reports, "Successful reports per quota period"),
        new(FeatureKeys.SeatsMaxCount, FeatureValueType.Limit, FeatureUnit.Seats, "Licensed seats"),
        new(FeatureKeys.SsoEnabled, FeatureValueType.Boolean, null, "Corporate single sign-on"),
        new(FeatureKeys.ScimEnabled, FeatureValueType.Boolean, null, "SCIM provisioning"),
    ];

    // Free has no report quota (absent, so it denies) and no prices. Annual fixtures are added later.
    public static readonly IReadOnlyList<SeedPlan> Plans =
    [
        new(PlanKeys.Free, "Free", AccountType.Personal, false,
            [
                Limit(FeatureKeys.ProjectsMaxCount, 3),
                Flag(FeatureKeys.ReportsBasic, false),
                Flag(FeatureKeys.ReportsAdvanced, false),
            ],
            []),
        new(PlanKeys.Pro, "Pro", AccountType.Personal, true,
            [
                Limit(FeatureKeys.ProjectsMaxCount, 30),
                Flag(FeatureKeys.ReportsBasic, true),
                Flag(FeatureKeys.ReportsAdvanced, false),
                Limit(FeatureKeys.ReportsMonthlyLimit, 100),
            ],
            [new(BillingInterval.Month, 1000)]),
        new(PlanKeys.Max, "Max", AccountType.Personal, true,
            [
                Limit(FeatureKeys.ProjectsMaxCount, 300),
                Flag(FeatureKeys.ReportsBasic, true),
                Flag(FeatureKeys.ReportsAdvanced, true),
                Limit(FeatureKeys.ReportsMonthlyLimit, 1000),
            ],
            [new(BillingInterval.Month, 3000)]),
        new(PlanKeys.Enterprise, "Enterprise", AccountType.Organization, true,
            [
                Limit(FeatureKeys.ProjectsMaxCount, 500),
                Flag(FeatureKeys.ReportsBasic, true),
                Flag(FeatureKeys.ReportsAdvanced, true),
                Limit(FeatureKeys.ReportsMonthlyLimit, 2000),
                Limit(FeatureKeys.SeatsMaxCount, 10),
                Flag(FeatureKeys.SsoEnabled, true),
                Flag(FeatureKeys.ScimEnabled, true),
            ],
            [new(BillingInterval.Month, 20000)]),
    ];

    private static SeedCapability Limit(string key, int limit) => new(key, null, limit);

    private static SeedCapability Flag(string key, bool value) => new(key, value, null);
}

internal sealed record SeedFeature(string Key, FeatureValueType ValueType, FeatureUnit? Unit, string Description);

internal sealed record SeedCapability(string Key, bool? BoolValue, int? LimitValue);

internal sealed record SeedPrice(BillingInterval Interval, int UnitAmountMinor);

internal sealed record SeedPlan(
    string Key,
    string Name,
    AccountType AccountType,
    bool IsBillable,
    IReadOnlyList<SeedCapability> Capabilities,
    IReadOnlyList<SeedPrice> Prices);
