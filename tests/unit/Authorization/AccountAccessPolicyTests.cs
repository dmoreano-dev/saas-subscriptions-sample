using Saas.Subscription.Sample.Application.Authorization;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.UnitTests.Authorization;

public class AccountAccessPolicyTests
{
    private const string UnknownPermission = "projects.destroy_everything";

    private static AccountAccessFacts Facts(
        UserStatus user = UserStatus.Active,
        MembershipStatus membership = MembershipStatus.Active,
        AccountStatus account = AccountStatus.Active,
        params string[] permissions) =>
        new(user, new MembershipFacts(membership, account, permissions.ToHashSet()));

    [Fact]
    public void Evaluate_UserDoesNotExist_ReturnsUnauthenticated()
    {
        // Act
        var outcome = AccountAccessPolicy.Evaluate(facts: null, Permissions.ProjectsRead);

        // Assert
        Assert.Equal(AccountAccessOutcome.Unauthenticated, outcome);
    }

    [Fact]
    public void Evaluate_DisabledUserWithEveryPermission_ReturnsUnauthenticated()
    {
        // Arrange
        var facts = Facts(user: UserStatus.Disabled, permissions: Permissions.ProjectsRead);

        // Act
        var outcome = AccountAccessPolicy.Evaluate(facts, Permissions.ProjectsRead);

        // Assert
        Assert.Equal(AccountAccessOutcome.Unauthenticated, outcome);
    }

    [Fact]
    public void Evaluate_NoMembership_ReturnsNotFound()
    {
        // Arrange
        var facts = new AccountAccessFacts(UserStatus.Active, Membership: null);

        // Act
        var outcome = AccountAccessPolicy.Evaluate(facts, permission: null);

        // Assert
        Assert.Equal(AccountAccessOutcome.NotFound, outcome);
    }

    [Theory]
    [InlineData(MembershipStatus.Suspended, AccountStatus.Active, AccountAccessOutcome.NotFound)]
    [InlineData(MembershipStatus.Suspended, AccountStatus.Suspended, AccountAccessOutcome.NotFound)]
    [InlineData(MembershipStatus.Active, AccountStatus.Closed, AccountAccessOutcome.NotFound)]
    [InlineData(MembershipStatus.Active, AccountStatus.Suspended, AccountAccessOutcome.AccountSuspended)]
    [InlineData(MembershipStatus.Active, AccountStatus.Active, AccountAccessOutcome.Granted)]
    public void Evaluate_MemberHoldingThePermission_DependsOnMembershipAndAccountStatus(
        MembershipStatus membership,
        AccountStatus account,
        AccountAccessOutcome expected)
    {
        // Arrange
        var facts = Facts(membership: membership, account: account, permissions: Permissions.ProjectsRead);

        // Act
        var outcome = AccountAccessPolicy.Evaluate(facts, Permissions.ProjectsRead);

        // Assert
        Assert.Equal(expected, outcome);
    }

    [Theory]
    [InlineData(Permissions.ProjectsWrite)]
    [InlineData(Permissions.PlatformOperate)]
    [InlineData(UnknownPermission)]
    public void Evaluate_ActiveMemberWithoutThePermission_ReturnsForbidden(string permission)
    {
        // Arrange
        var facts = Facts(permissions: Permissions.ProjectsRead);

        // Act
        var outcome = AccountAccessPolicy.Evaluate(facts, permission);

        // Assert
        Assert.Equal(AccountAccessOutcome.Forbidden, outcome);
    }

    [Fact]
    public void Evaluate_ActiveMemberWithoutRolesWhenOnlyMembershipIsNeeded_ReturnsGranted()
    {
        // Arrange
        var facts = Facts();

        // Act
        var outcome = AccountAccessPolicy.Evaluate(facts, permission: null);

        // Assert
        Assert.Equal(AccountAccessOutcome.Granted, outcome);
    }

    [Theory]
    [InlineData(MembershipStatus.Active, AccountStatus.Active, true)]
    [InlineData(MembershipStatus.Active, AccountStatus.Suspended, true)]
    [InlineData(MembershipStatus.Active, AccountStatus.Closed, false)]
    [InlineData(MembershipStatus.Suspended, AccountStatus.Active, false)]
    public void IsListed_Account_DependsOnMembershipAndAccountStatus(MembershipStatus membership, AccountStatus status, bool expected)
    {
        // Arrange
        var account = new AccessibleAccount(Guid.CreateVersion7(), "Any", AccountType.Personal, status, membership);

        // Act
        var listed = AccountAccessPolicy.IsListed(account);

        // Assert
        Assert.Equal(expected, listed);
    }
}
