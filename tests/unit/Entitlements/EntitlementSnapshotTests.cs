using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Application.Entitlements;
using Saas.Subscription.Sample.Domain.Catalog;
using static Saas.Subscription.Sample.UnitTests.Entitlements.EntitlementTestData;

namespace Saas.Subscription.Sample.UnitTests.Entitlements;

public class EntitlementSnapshotTests
{
    private const string UndefinedFeature = "reports.unlimited_everything";

    private static EntitlementSnapshot SnapshotOf(PlanEntitlements plan) =>
        new(AccountId, MidPeriod, EntitlementState.Free, plan, EffectiveFrom: null, ValidUntil: null, Restriction: null, PendingChange: null);

    public static TheoryData<CapabilityEntry, bool> DeclaredCapabilities => new()
    {
        { Flag(FeatureKeys.ReportsBasic, true), true },
        { Flag(FeatureKeys.ReportsBasic, false), false },
        { Limit(FeatureKeys.ProjectsMaxCount, 3), true },
        { Limit(FeatureKeys.ProjectsMaxCount, 0), false },
        { Unlimited(FeatureKeys.ProjectsMaxCount), true },
    };

    [Theory]
    [MemberData(nameof(DeclaredCapabilities))]
    public void IsGranted_DeclaredCapability_DependsOnItsValue(CapabilityEntry capability, bool expected)
    {
        // Act
        var granted = EntitlementSnapshot.IsGranted(capability);

        // Assert
        Assert.Equal(expected, granted);
    }

    [Theory]
    [InlineData(FeatureKeys.ReportsMonthlyLimit)]
    [InlineData(FeatureKeys.SsoEnabled)]
    [InlineData(UndefinedFeature)]
    public void IsGranted_KeyTheFreePlanDoesNotDeclare_ReturnsFalse(string featureKey)
    {
        // Arrange
        var snapshot = SnapshotOf(Free);

        // Act
        var granted = snapshot.IsGranted(featureKey);

        // Assert
        Assert.False(granted);
    }

    [Fact]
    public void Find_UndefinedKey_ReturnsNull()
    {
        // Arrange
        var snapshot = SnapshotOf(Max);

        // Act
        var capability = snapshot.Find(UndefinedFeature);

        // Assert
        Assert.Null(capability);
    }
}
