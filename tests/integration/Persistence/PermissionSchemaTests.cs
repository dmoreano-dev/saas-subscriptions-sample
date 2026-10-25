using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

/// <summary>What PostgreSQL itself refuses, whatever code path (EF, raw SQL, a future job) attempts it.</summary>
[Collection(PostgresTestGroup.Name)]
public class PermissionSchemaTests(PostgresFixture postgres)
{
    private const string AnyEmail = "a@x.com";
    private const string OtherEmail = "b@x.com";
    private const string AnyName = "Any";
    private const string PersonalType = "Personal";
    private const string AccountScope = nameof(PermissionScope.Account);
    private const string PlatformScope = nameof(PermissionScope.Platform);
    private const string MigrationBeforeRoles = "AddPlanCatalog";

    // Stands for any later table that belongs to a project (reports arrive in ENT-06): it must reference the
    // project together with its account.
    private const string CreateChildTable = """
        CREATE TABLE test_project_notes (
            id uuid PRIMARY KEY,
            account_id uuid NOT NULL,
            project_id uuid NOT NULL,
            FOREIGN KEY (account_id, project_id) REFERENCES projects (account_id, id)
        )
        """;

    private const string InsertChild = "INSERT INTO test_project_notes (id, account_id, project_id) VALUES (@id, @account, @project)";

    private async Task<NpgsqlConnection> OpenMigratedConnectionAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        return await Sql.OpenAsync(database.ConnectionString);
    }

    /// <summary>A user with their personal account and membership, as raw rows (no role).</summary>
    private static async Task<(Guid UserId, Guid AccountId, Guid MembershipId)> InsertOwnerAsync(NpgsqlConnection connection, string email)
    {
        var userId = Sql.NewId();
        var accountId = Sql.NewId();
        await using var transaction = await connection.BeginTransactionAsync();
        await Sql.InsertUserAsync(connection, userId, email, transaction: transaction);
        await Sql.InsertAccountAsync(connection, accountId, PersonalType, userId, transaction: transaction);
        var membershipId = await Sql.InsertMembershipAsync(connection, accountId, userId, transaction: transaction);
        await transaction.CommitAsync();
        return (userId, accountId, membershipId);
    }

    private static Task<int> InsertChildAsync(NpgsqlConnection connection, Guid accountId, Guid projectId) =>
        Sql.ExecuteAsync(connection, InsertChild, ("id", Sql.NewId()), ("account", accountId), ("project", projectId));

    // ---- cross-account references ----

    [Fact]
    public async Task InsertChild_ProjectOfAnotherAccount_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var ana = await InsertOwnerAsync(connection, AnyEmail);
        var bob = await InsertOwnerAsync(connection, OtherEmail);
        var bobsProject = await Sql.InsertProjectAsync(connection, bob.AccountId, AnyName);
        await Sql.ExecuteAsync(connection, CreateChildTable);

        // Act
        var act = () => InsertChildAsync(connection, ana.AccountId, bobsProject);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task InsertChild_ProjectOfTheSameAccount_Succeeds()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var ana = await InsertOwnerAsync(connection, AnyEmail);
        var anasProject = await Sql.InsertProjectAsync(connection, ana.AccountId, AnyName);
        await Sql.ExecuteAsync(connection, CreateChildTable);

        // Act
        var inserted = await InsertChildAsync(connection, ana.AccountId, anasProject);

        // Assert
        Assert.Equal(1, inserted);
    }

    [Fact]
    public async Task UpdateProject_MovingAReferencedProjectToAnotherAccount_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var ana = await InsertOwnerAsync(connection, AnyEmail);
        var bob = await InsertOwnerAsync(connection, OtherEmail);
        var anasProject = await Sql.InsertProjectAsync(connection, ana.AccountId, AnyName);
        await Sql.ExecuteAsync(connection, CreateChildTable);
        await InsertChildAsync(connection, ana.AccountId, anasProject);

        // Act
        var act = () => Sql.ExecuteAsync(
            connection,
            "UPDATE projects SET account_id = @account WHERE id = @id",
            ("account", bob.AccountId), ("id", anasProject));

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task InsertProject_UnknownAccount_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var act = () => Sql.InsertProjectAsync(connection, Sql.NewId(), AnyName);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    // ---- account roles and platform roles stay apart ----

    [Theory]
    [InlineData(AccountScope, PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PlatformScope, PostgresErrorCodes.CheckViolation)]
    public async Task InsertMembershipRole_PlatformRole_IsRejected(string declaredScope, string expectedSqlState)
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var ana = await InsertOwnerAsync(connection, AnyEmail);

        // Act
        var act = () => Sql.InsertMembershipRoleAsync(connection, ana.MembershipId, SystemRoles.PlatformOperator, declaredScope);

        // Assert
        await Sql.AssertSqlStateAsync(expectedSqlState, act);
    }

    [Theory]
    [InlineData(PlatformScope, PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(AccountScope, PostgresErrorCodes.CheckViolation)]
    public async Task InsertPlatformRoleAssignment_AccountRole_IsRejected(string declaredScope, string expectedSqlState)
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var ana = await InsertOwnerAsync(connection, AnyEmail);

        // Act
        var act = () => Sql.InsertPlatformRoleAssignmentAsync(connection, ana.UserId, SystemRoles.PersonalOwner, declaredScope);

        // Assert
        await Sql.AssertSqlStateAsync(expectedSqlState, act);
    }

    [Theory]
    [InlineData(SystemRoles.PersonalOwner, Permissions.PlatformOperate, AccountScope)]
    [InlineData(SystemRoles.PersonalOwner, Permissions.PlatformOperate, PlatformScope)]
    [InlineData(SystemRoles.PlatformOperator, Permissions.ProjectsRead, PlatformScope)]
    [InlineData(SystemRoles.PlatformOperator, Permissions.ProjectsRead, AccountScope)]
    public async Task InsertRolePermission_PermissionOfAnotherScope_ThrowsForeignKeyViolation(string roleKey, string permissionKey, string declaredScope)
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var act = () => Sql.InsertRolePermissionAsync(connection, roleKey, permissionKey, declaredScope);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task UpdateMembership_UnknownStatus_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        await InsertOwnerAsync(connection, AnyEmail);

        // Act
        var act = () => Sql.ExecuteAsync(connection, "UPDATE memberships SET status = 'Removed'");

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    // ---- seeded roles ----

    [Fact]
    public async Task MigratedSchema_Roles_AreTheSystemRolesAndNeverAPlan()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        string[] planKeys = [PlanKeys.Free, PlanKeys.Pro, PlanKeys.Max, PlanKeys.Enterprise];

        // Act
        var roles = await Sql.ColumnAsync(connection, "SELECT key || ':' || scope FROM roles ORDER BY key");
        var plansUsedAsRoleOrPermission = await Sql.ScalarAsync<long>(
            connection,
            "SELECT (SELECT count(*) FROM roles WHERE lower(key) = ANY(@plans)) + (SELECT count(*) FROM permissions WHERE lower(key) = ANY(@plans))",
            ("plans", planKeys));

        // Assert
        Assert.Equal(["personal_owner:Account", "platform_operator:Platform"], roles);
        Assert.Equal(0L, plansUsedAsRoleOrPermission);
    }

    [Fact]
    public async Task MigratedSchema_PersonalOwnerPermissions_AreTheAccountPermissionsOnly()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var permissions = await Sql.ColumnAsync(
            connection,
            "SELECT permission_key FROM role_permissions WHERE role_key = 'personal_owner' ORDER BY permission_key");

        // Assert
        Assert.Equal([Permissions.BillingManage, Permissions.ProjectsRead, Permissions.ProjectsWrite, Permissions.ReportsGenerate], permissions);
    }

    // ---- owners registered before roles existed ----

    [Fact]
    public async Task MigrateAsync_OwnerRegisteredBeforeRolesExisted_ReceivesThePersonalOwnerRole()
    {
        // Arrange
        var database = await postgres.CreateEmptyDatabaseAsync();
        await using (var context = new AppDbContext(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), database.ConnectionString).Options))
        {
            await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeRoles);
        }

        Guid membershipId;
        await using (var before = await Sql.OpenAsync(database.ConnectionString))
        {
            (_, _, membershipId) = await InsertOwnerAsync(before, AnyEmail);
        }

        // Act
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        // Assert
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var roles = await Sql.ColumnAsync(connection, $"SELECT role_key FROM membership_roles WHERE membership_id = '{membershipId}'");
        var status = await Sql.ColumnAsync(connection, "SELECT status FROM memberships");
        Assert.Equal([SystemRoles.PersonalOwner], roles);
        Assert.Equal(["Active"], status);
    }
}
