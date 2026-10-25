using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.UnitTests.Authorization;

public class SystemRolesTests
{
    [Theory]
    [InlineData(PlanKeys.Free)]
    [InlineData(PlanKeys.Pro)]
    [InlineData(PlanKeys.Max)]
    [InlineData(PlanKeys.Enterprise)]
    public void All_PlanKey_IsNeitherARoleNorAPermission(string planKey)
    {
        // Act
        var names = SystemRoles.All.Select(role => role.Key).Concat(Permissions.All.Select(permission => permission.Key));

        // Assert
        Assert.DoesNotContain(names, name => string.Equals(name, planKey, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void All_EveryRole_GrantsOnlyPermissionsOfItsOwnScope()
    {
        // Arrange
        var scopes = Permissions.All.ToDictionary(permission => permission.Key, permission => permission.Scope);

        // Act
        var mismatches = SystemRoles.All
            .SelectMany(role => role.Permissions.Where(permission => scopes[permission] != role.Scope).Select(permission => $"{role.Key}:{permission}"))
            .ToList();

        // Assert
        Assert.Empty(mismatches);
    }
}
