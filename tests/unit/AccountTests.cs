using Saas.Subscription.Sample.Domain.Accounts;

namespace Saas.Subscription.Sample.UnitTests;

public class AccountTests
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.UnixEpoch;
    private const string AnyName = "Any";

    [Fact]
    public void CreatePersonal_ValidOwner_ReturnsActivePersonalAccountOwnedByThatUser()
    {
        // Arrange
        var ownerUserId = Guid.CreateVersion7();

        // Act
        var account = Account.CreatePersonal(ownerUserId, AnyName, FixedNow);

        // Assert
        Assert.Equal(AccountType.Personal, account.Type);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(ownerUserId, account.PersonalOwnerUserId);
    }

    [Fact]
    public void CreateOrganization_ValidName_ReturnsActiveOrganizationWithoutPersonalOwner()
    {
        // Act
        var account = Account.CreateOrganization(AnyName, FixedNow);

        // Assert
        Assert.Equal(AccountType.Organization, account.Type);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.PersonalOwnerUserId);
    }

    [Fact]
    public void CreateOrganization_CalledTwice_ReturnsDistinctIds()
    {
        // Arrange
        var first = Account.CreateOrganization(AnyName, FixedNow);

        // Act
        var second = Account.CreateOrganization(AnyName, FixedNow);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void CreateOrganization_AnyName_GeneratesUuidV7Id()
    {
        // Act
        var account = Account.CreateOrganization(AnyName, FixedNow);

        // Assert
        Assert.Equal(7, account.Id.Version);
    }
}
