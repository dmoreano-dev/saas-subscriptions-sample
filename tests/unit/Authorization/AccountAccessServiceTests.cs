using Saas.Subscription.Sample.Application.Authorization;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.UnitTests.Authorization;

public class AccountAccessServiceTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid AccountId = Guid.CreateVersion7();

    private static AccountAccessService ServiceFor(AccountAccessFacts? facts) => new(new FixedFactsReader(facts));

    private static AccountAccessFacts OwnerOf(AccountStatus account) =>
        new(UserStatus.Active, new MembershipFacts(MembershipStatus.Active, account, new HashSet<string> { Permissions.ProjectsRead }));

    [Fact]
    public async Task AuthorizeAsync_Granted_ReturnsAScopeForThatAccountUserAndPermissions()
    {
        // Arrange
        var service = ServiceFor(OwnerOf(AccountStatus.Active));

        // Act
        var result = await service.AuthorizeAsync(UserId, AccountId, Permissions.ProjectsRead, CancellationToken.None);

        // Assert
        Assert.Equal(AccountAccessOutcome.Granted, result.Outcome);
        Assert.Equal(AccountId, result.Scope!.AccountId);
        Assert.Equal(UserId, result.Scope.UserId);
        Assert.True(result.Scope.Has(Permissions.ProjectsRead));
        Assert.False(result.Scope.Has(Permissions.ProjectsWrite));
    }

    [Fact]
    public async Task AuthorizeAsync_Denied_ReturnsNoScope()
    {
        // Arrange
        var service = ServiceFor(OwnerOf(AccountStatus.Suspended));

        // Act
        var result = await service.AuthorizeAsync(UserId, AccountId, Permissions.ProjectsRead, CancellationToken.None);

        // Assert
        Assert.Equal(AccountAccessOutcome.AccountSuspended, result.Outcome);
        Assert.Null(result.Scope);
    }

    [Fact]
    public void AccountScope_PublicConstructors_DoNotExist()
    {
        // Act
        var constructors = typeof(AccountScope).GetConstructors();

        // Assert
        Assert.Empty(constructors);
    }

    private sealed class FixedFactsReader(AccountAccessFacts? facts) : IAccountAccessReader
    {
        public Task<AccountAccessFacts?> FindAsync(Guid userId, Guid accountId, CancellationToken cancellationToken) => Task.FromResult(facts);

        public Task<UserMemberships?> ListMembershipsAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<UserMemberships?>(null);
    }
}
