using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Application.Entitlements;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;

namespace Saas.Subscription.Sample.UnitTests.Entitlements;

/// <summary>A one-month paid period with a 3-day grace and a change scheduled mid-period, plus the seeded plans in miniature.</summary>
internal static class EntitlementTestData
{
    public const int Version = 1;

    public static readonly Guid AccountId = Guid.CreateVersion7();

    public static readonly DateTimeOffset PeriodStart = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset MidPeriod = new(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset PeriodEnd = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset GraceEnd = new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);

    public static readonly PlanEntitlements Free = new(PlanKeys.Free, Version,
    [
        Limit(FeatureKeys.ProjectsMaxCount, 3),
        Flag(FeatureKeys.ReportsAdvanced, false),
        Flag(FeatureKeys.ReportsBasic, false),
    ]);

    public static readonly PlanEntitlements Pro = new(PlanKeys.Pro, Version,
    [
        Limit(FeatureKeys.ProjectsMaxCount, 30),
        Flag(FeatureKeys.ReportsAdvanced, false),
        Flag(FeatureKeys.ReportsBasic, true),
        Limit(FeatureKeys.ReportsMonthlyLimit, 100),
    ]);

    public static readonly PlanEntitlements Max = new(PlanKeys.Max, Version,
    [
        Limit(FeatureKeys.ProjectsMaxCount, 300),
        Flag(FeatureKeys.ReportsAdvanced, true),
        Flag(FeatureKeys.ReportsBasic, true),
        Limit(FeatureKeys.ReportsMonthlyLimit, 1000),
    ]);

    public static CapabilityEntry Flag(string key, bool value) =>
        new(key, FeatureValueType.Boolean, Unit: null, BoolValue: value, LimitValue: null, IsUnlimited: false);

    public static CapabilityEntry Limit(string key, int value) =>
        new(key, FeatureValueType.Limit, FeatureUnit.Projects, BoolValue: null, LimitValue: value, IsUnlimited: false);

    public static CapabilityEntry Unlimited(string key) =>
        new(key, FeatureValueType.Limit, FeatureUnit.Projects, BoolValue: null, LimitValue: null, IsUnlimited: true);

    public static EntitlementFacts Personal(SubscriptionFacts? subscription = null) => new(Free, subscription);

    public static SubscriptionFacts PaidSubscription(
        SubscriptionStatus status = SubscriptionStatus.Active,
        PlanEntitlements? plan = null,
        DateTimeOffset? accessUntil = null,
        PendingChangeFacts? change = null) =>
        new(status, PeriodStart, accessUntil ?? PeriodEnd, plan ?? Pro, change);

    public static SubscriptionFacts PastDue() => PaidSubscription(SubscriptionStatus.PastDue, accessUntil: GraceEnd);

    public static SubscriptionFacts MaxDowngradingToProMidPeriod() =>
        PaidSubscription(plan: Max, change: new PendingChangeFacts(SubscriptionChangeKind.PlanChange, MidPeriod, Pro));

    public static SubscriptionFacts ProCancellingMidPeriod() =>
        PaidSubscription(change: new PendingChangeFacts(SubscriptionChangeKind.Cancel, MidPeriod, Target: null));
}
