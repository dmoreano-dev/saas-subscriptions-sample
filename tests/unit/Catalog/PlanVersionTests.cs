using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.UnitTests.Catalog;

public class PlanVersionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;
    private static readonly Guid PlanId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const int FirstVersion = 1;
    private const int Limit = 3;
    private const int Price = 1000;
    private const string Usd = "USD";

    [Fact]
    public void Publish_DraftVersion_BecomesPublishedWithTimestamp()
    {
        // Arrange
        var version = PlanVersion.CreateDraft(PlanId, FirstVersion, Now);

        // Act
        version.Publish(Now);

        // Assert
        Assert.Equal(PlanVersionStatus.Published, version.Status);
        Assert.Equal(Now, version.PublishedAt);
    }

    [Fact]
    public void AddCapability_PublishedVersion_Throws()
    {
        // Arrange
        var version = PlanVersion.CreateDraft(PlanId, FirstVersion, Now);
        version.Publish(Now);

        // Act
        var act = () => version.AddCapability(PlanCapability.Limit(FeatureKeys.ProjectsMaxCount, Limit));

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void AddPrice_PublishedVersion_Throws()
    {
        // Arrange
        var version = PlanVersion.CreateDraft(PlanId, FirstVersion, Now);
        version.Publish(Now);

        // Act
        var act = () => version.AddPrice(PlanPrice.Create(BillingInterval.Month, Usd, Price));

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Publish_AlreadyPublished_Throws()
    {
        // Arrange
        var version = PlanVersion.CreateDraft(PlanId, FirstVersion, Now);
        version.Publish(Now);

        // Act
        var act = () => version.Publish(Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void CreateDraft_VersionBelowOne_Throws()
    {
        // Act
        var act = () => PlanVersion.CreateDraft(PlanId, 0, Now);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Limit_NegativeValue_Throws()
    {
        // Act
        var act = () => PlanCapability.Limit(FeatureKeys.ProjectsMaxCount, -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Unlimited_Capability_HasNoNumericLimit()
    {
        // Act
        var capability = PlanCapability.Unlimited(FeatureKeys.ProjectsMaxCount);

        // Assert
        Assert.True(capability.IsUnlimited);
        Assert.Null(capability.LimitValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositivePrice_Throws(int amount)
    {
        // Act
        var act = () => PlanPrice.Create(BillingInterval.Month, Usd, amount);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}
