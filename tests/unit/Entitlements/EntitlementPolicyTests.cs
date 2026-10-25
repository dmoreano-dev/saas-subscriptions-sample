using Saas.Subscription.Sample.Application.Entitlements;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;
using static Saas.Subscription.Sample.UnitTests.Entitlements.EntitlementTestData;

namespace Saas.Subscription.Sample.UnitTests.Entitlements;

public class EntitlementPolicyTests
{
    [Fact]
    public void Resolve_PersonalAccountWithoutSubscription_ReturnsFreeWithoutBoundary()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(), MidPeriod);

        // Assert
        Assert.Equal(EntitlementState.Free, snapshot.State);
        Assert.Equal(Free, snapshot.Plan);
        Assert.Null(snapshot.ValidUntil);
        Assert.Null(snapshot.EffectiveFrom);
        Assert.Null(snapshot.Restriction);
        Assert.Null(snapshot.PendingChange);
    }

    [Fact]
    public void Resolve_AnyState_CarriesAccountAndTime()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PaidSubscription()), MidPeriod);

        // Assert
        Assert.Equal(AccountId, snapshot.AccountId);
        Assert.Equal(MidPeriod, snapshot.CalculatedAt);
    }

    [Fact]
    public void Resolve_EndedSubscription_ReturnsFree()
    {
        // Arrange
        var facts = Personal(PaidSubscription(SubscriptionStatus.Ended));

        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, facts, MidPeriod);

        // Assert
        Assert.Equal(EntitlementState.Free, snapshot.State);
        Assert.Equal(PlanKeys.Free, snapshot.Plan.PlanKey);
    }

    [Fact]
    public void Resolve_ActiveSubscriptionWithinPeriod_ReturnsPaidPlanValidUntilTheDeadline()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PaidSubscription()), MidPeriod);

        // Assert
        Assert.Equal(EntitlementState.Active, snapshot.State);
        Assert.Equal(Pro, snapshot.Plan);
        Assert.Equal(PeriodStart, snapshot.EffectiveFrom);
        Assert.Equal(PeriodEnd, snapshot.ValidUntil);
    }

    [Theory]
    [InlineData(-1, EntitlementState.Active, PlanKeys.Pro)]
    [InlineData(0, EntitlementState.Restricted, PlanKeys.Free)]
    [InlineData(1, EntitlementState.Restricted, PlanKeys.Free)]
    public void Resolve_ActiveSubscriptionAroundTheAccessDeadline_WithdrawsPaidAccessAtTheDeadline(
        int secondsFromDeadline,
        EntitlementState expectedState,
        string expectedPlan)
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PaidSubscription()), PeriodEnd.AddSeconds(secondsFromDeadline));

        // Assert
        Assert.Equal(expectedState, snapshot.State);
        Assert.Equal(expectedPlan, snapshot.Plan.PlanKey);
    }

    [Fact]
    public void Resolve_ActiveSubscriptionPastTheDeadline_ReportsAccessExpiredSinceTheDeadline()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PaidSubscription()), PeriodEnd);

        // Assert
        Assert.Equal(AccessRestriction.AccessExpired, snapshot.Restriction);
        Assert.Equal(PeriodEnd, snapshot.EffectiveFrom);
        Assert.Null(snapshot.ValidUntil);
    }

    [Theory]
    [InlineData(-1, EntitlementState.Grace, PlanKeys.Pro)]
    [InlineData(0, EntitlementState.Restricted, PlanKeys.Free)]
    public void Resolve_PastDueSubscriptionAroundTheGraceDeadline_KeepsThePaidPlanOnlyDuringGrace(
        int secondsFromGraceEnd,
        EntitlementState expectedState,
        string expectedPlan)
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PastDue()), GraceEnd.AddSeconds(secondsFromGraceEnd));

        // Assert
        Assert.Equal(expectedState, snapshot.State);
        Assert.Equal(expectedPlan, snapshot.Plan.PlanKey);
    }

    [Fact]
    public void Resolve_PastDueSubscriptionDuringGrace_IsValidUntilTheGraceDeadline()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PastDue()), PeriodEnd);

        // Assert
        Assert.Equal(GraceEnd, snapshot.ValidUntil);
        Assert.Null(snapshot.Restriction);
    }

    [Fact]
    public void Resolve_PastDueSubscriptionAfterGrace_ReportsPaymentOverdue()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PastDue()), GraceEnd);

        // Assert
        Assert.Equal(AccessRestriction.PaymentOverdue, snapshot.Restriction);
    }

    [Theory]
    [InlineData(-1, PlanKeys.Max)]
    [InlineData(0, PlanKeys.Pro)]
    [InlineData(1, PlanKeys.Pro)]
    public void Resolve_ScheduledDowngradeAroundItsInstant_AppliesTheTargetFromTheInstant(int secondsFromChange, string expectedPlan)
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(MaxDowngradingToProMidPeriod()), MidPeriod.AddSeconds(secondsFromChange));

        // Assert
        Assert.Equal(EntitlementState.Active, snapshot.State);
        Assert.Equal(expectedPlan, snapshot.Plan.PlanKey);
    }

    [Fact]
    public void Resolve_DowngradeNotYetEffective_ReportsItAndIsValidUntilItsInstant()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(MaxDowngradingToProMidPeriod()), PeriodStart);

        // Assert
        Assert.Equal(SubscriptionChangeKind.PlanChange, snapshot.PendingChange!.Kind);
        Assert.Equal(Pro, snapshot.PendingChange.Target);
        Assert.Equal(MidPeriod, snapshot.ValidUntil);
    }

    [Fact]
    public void Resolve_DowngradeAlreadyEffectiveButNotApplied_ReportsNoPendingChangeAndKeepsTheDeadline()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(MaxDowngradingToProMidPeriod()), MidPeriod);

        // Assert
        Assert.Null(snapshot.PendingChange);
        Assert.Equal(MidPeriod, snapshot.EffectiveFrom);
        Assert.Equal(PeriodEnd, snapshot.ValidUntil);
    }

    [Fact]
    public void Resolve_ChangeScheduledAfterTheDeadline_IsValidOnlyUntilTheDeadline()
    {
        // Arrange
        var change = new PendingChangeFacts(SubscriptionChangeKind.PlanChange, GraceEnd, Free);
        var facts = Personal(PaidSubscription(change: change));

        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, facts, MidPeriod);

        // Assert
        Assert.Equal(PeriodEnd, snapshot.ValidUntil);
    }

    [Theory]
    [InlineData(-1, EntitlementState.Active, PlanKeys.Pro)]
    [InlineData(0, EntitlementState.Free, PlanKeys.Free)]
    [InlineData(1, EntitlementState.Free, PlanKeys.Free)]
    public void Resolve_ScheduledCancellationAroundItsInstant_FallsBackToFreeFromTheInstant(
        int secondsFromChange,
        EntitlementState expectedState,
        string expectedPlan)
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(ProCancellingMidPeriod()), MidPeriod.AddSeconds(secondsFromChange));

        // Assert
        Assert.Equal(expectedState, snapshot.State);
        Assert.Equal(expectedPlan, snapshot.Plan.PlanKey);
    }

    [Fact]
    public void Resolve_CancellationAlreadyEffective_IsFreeWithoutRestriction()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(ProCancellingMidPeriod()), PeriodEnd);

        // Assert
        Assert.Equal(EntitlementState.Free, snapshot.State);
        Assert.Null(snapshot.Restriction);
        Assert.Equal(MidPeriod, snapshot.EffectiveFrom);
    }

    [Fact]
    public void Resolve_SubscriptionBeforeItsPeriodStarts_ReturnsFreeUntilTheStart()
    {
        // Act
        var snapshot = EntitlementPolicy.Resolve(AccountId, Personal(PaidSubscription()), PeriodStart.AddSeconds(-1));

        // Assert
        Assert.Equal(EntitlementState.Free, snapshot.State);
        Assert.Equal(PeriodStart, snapshot.ValidUntil);
    }
}
