using Microsoft.EntityFrameworkCore;
using Npgsql;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

[Collection(PostgresTestGroup.Name)]
public class AccountConstraintTests(PostgresFixture postgres)
{
    private const string AnyEmail = "a@x.com";
    private const string UppercaseAnyEmail = "A@x.com";
    private const string OtherEmail = "b@x.com";
    private const string AnyName = "Any";
    private const string PersonalType = nameof(AccountType.Personal);
    private const string OrganizationType = nameof(AccountType.Organization);
    private const string ActiveStatus = nameof(AccountStatus.Active);
    private const string UnknownType = "Enterprise";
    private const string UnknownStatus = "Deleted";
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.UnixEpoch;

    private static AppDbContext CreateContext(string connectionString) =>
        new(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString).Options);

    private async Task<NpgsqlConnection> OpenMigratedConnectionAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        return await Sql.OpenAsync(database.ConnectionString);
    }

    /// <summary>Inserts a user, its personal account and the owner membership in one transaction.</summary>
    private static async Task<(Guid UserId, Guid AccountId)> InsertRegisteredUserAsync(NpgsqlConnection connection, string email)
    {
        var userId = Sql.NewId();
        var accountId = Sql.NewId();
        await using var transaction = await connection.BeginTransactionAsync();
        await Sql.InsertUserAsync(connection, userId, email, transaction: transaction);
        await Sql.InsertAccountAsync(connection, accountId, PersonalType, userId, transaction: transaction);
        await Sql.InsertMembershipAsync(connection, accountId, userId, transaction: transaction);
        await transaction.CommitAsync();
        return (userId, accountId);
    }

    /// <summary>Adds an organization account with a membership for the given user.</summary>
    private static async Task InsertOrganizationMembershipAsync(NpgsqlConnection connection, Guid userId)
    {
        var organizationId = Sql.NewId();
        await Sql.InsertAccountAsync(connection, organizationId, OrganizationType, personalOwnerUserId: null);
        await Sql.InsertMembershipAsync(connection, organizationId, userId);
    }

    // ---- users ----

    [Fact]
    public async Task InsertUser_EmailDifferingOnlyByCase_ThrowsUniqueViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        await Sql.InsertUserAsync(connection, Sql.NewId(), AnyEmail);

        // Act
        var act = () => Sql.InsertUserAsync(connection, Sql.NewId(), UppercaseAnyEmail);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.UniqueViolation, act);
    }

    [Theory]
    [InlineData("A@x.com")]
    [InlineData(" a@x.com")]
    [InlineData("")]
    public async Task InsertUser_NonCanonicalNormalizedEmail_ThrowsCheckViolation(string normalizedEmail)
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var act = () => Sql.InsertUserAsync(connection, Sql.NewId(), AnyEmail, normalizedEmail);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    [Fact]
    public async Task SaveChangesAsync_UserChangedByAnotherWriter_ThrowsConcurrencyException()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var user = User.Create(AnyEmail, FixedNow);
        await using (var seed = CreateContext(database.ConnectionString))
        {
            seed.Users.Add(user);
            await seed.SaveChangesAsync();
        }

        await using var winner = CreateContext(database.ConnectionString);
        await using var loser = CreateContext(database.ConnectionString);
        var userLoadedByWinner = await winner.Users.SingleAsync(u => u.Id == user.Id);
        var userLoadedByLoser = await loser.Users.SingleAsync(u => u.Id == user.Id);
        userLoadedByWinner.ChangeEmail(OtherEmail, FixedNow);
        await winner.SaveChangesAsync();
        userLoadedByLoser.ChangeEmail(OtherEmail, FixedNow);

        // Act
        var act = () => loser.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(act);
    }

    [Fact]
    public async Task DeleteUser_UserOwnsAccount_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var (userId, _) = await InsertRegisteredUserAsync(connection, AnyEmail);

        // Act
        var act = () => Sql.DeleteUserAsync(connection, userId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    // ---- personal account invariant ----

    [Fact]
    public async Task SaveChangesAsync_UserPersonalAccountAndMembership_PersistsTheOwnerMembership()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var user = User.Create(AnyEmail, FixedNow);
        var account = Account.CreatePersonal(user.Id, AnyName, FixedNow);
        var membership = Membership.Create(account.Id, user.Id, FixedNow);
        await using var context = CreateContext(database.ConnectionString);
        context.AddRange(user, account, membership);

        // Act
        await context.SaveChangesAsync();

        // Assert
        await using var verification = CreateContext(database.ConnectionString);
        var storedAccount = await verification.Accounts.SingleAsync(a => a.Id == account.Id);
        var storedMembership = await verification.Memberships.SingleAsync(m => m.AccountId == account.Id);
        Assert.Equal(user.Id, storedAccount.PersonalOwnerUserId);
        Assert.Equal(user.Id, storedMembership.UserId);
    }

    [Fact]
    public async Task InsertAccount_SecondPersonalAccountForSameUser_ThrowsUniqueViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var (userId, _) = await InsertRegisteredUserAsync(connection, AnyEmail);

        // Act
        var act = () => Sql.InsertAccountAsync(connection, Sql.NewId(), PersonalType, userId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.UniqueViolation, act);
    }

    [Fact]
    public async Task InsertAccount_SecondOrganizationForSameUser_Succeeds()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var (userId, _) = await InsertRegisteredUserAsync(connection, AnyEmail);
        await InsertOrganizationMembershipAsync(connection, userId);

        // Act
        await InsertOrganizationMembershipAsync(connection, userId);

        // Assert
        Assert.Equal(3, await Sql.CountMembershipsAsync(connection, userId));
    }

    [Fact]
    public async Task Commit_PersonalAccountWithoutOwnerMembership_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail, transaction: transaction);
        await Sql.InsertAccountAsync(connection, Sql.NewId(), PersonalType, userId, transaction: transaction);

        // Act
        var act = () => transaction.CommitAsync();

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task Commit_PersonalAccountWhoseOnlyMemberIsAnotherUser_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var ownerId = Sql.NewId();
        var strangerId = Sql.NewId();
        var accountId = Sql.NewId();
        await Sql.InsertUserAsync(connection, ownerId, AnyEmail, transaction: transaction);
        await Sql.InsertUserAsync(connection, strangerId, OtherEmail, transaction: transaction);
        await Sql.InsertAccountAsync(connection, accountId, PersonalType, ownerId, transaction: transaction);
        await Sql.InsertMembershipAsync(connection, accountId, strangerId, transaction: transaction);

        // Act
        var act = () => transaction.CommitAsync();

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task InsertAccount_PersonalWithoutOwner_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var act = () => Sql.InsertAccountAsync(connection, Sql.NewId(), PersonalType, personalOwnerUserId: null);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    [Fact]
    public async Task InsertAccount_OrganizationWithPersonalOwner_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail);

        // Act
        var act = () => Sql.InsertAccountAsync(connection, Sql.NewId(), OrganizationType, userId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    [Fact]
    public async Task InsertAccount_UnknownType_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var act = () => Sql.InsertAccountAsync(connection, Sql.NewId(), UnknownType, personalOwnerUserId: null);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    [Fact]
    public async Task InsertAccount_UnknownStatus_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var act = () => Sql.InsertAccountAsync(connection, Sql.NewId(), OrganizationType, personalOwnerUserId: null, UnknownStatus);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    // ---- memberships ----

    [Fact]
    public async Task InsertMembership_UnknownAccount_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail);

        // Act
        var act = () => Sql.InsertMembershipAsync(connection, Sql.NewId(), userId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task InsertMembership_UnknownUser_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var accountId = Sql.NewId();
        await Sql.InsertAccountAsync(connection, accountId, OrganizationType, personalOwnerUserId: null);

        // Act
        var act = () => Sql.InsertMembershipAsync(connection, accountId, Sql.NewId());

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task InsertMembership_UserAlreadyInAccount_ThrowsUniqueViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var (userId, accountId) = await InsertRegisteredUserAsync(connection, AnyEmail);

        // Act
        var act = () => Sql.InsertMembershipAsync(connection, accountId, userId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.UniqueViolation, act);
    }

    [Fact]
    public async Task DeleteAccount_AccountHasMemberships_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var (_, accountId) = await InsertRegisteredUserAsync(connection, AnyEmail);

        // Act
        var act = () => Sql.DeleteAccountAsync(connection, accountId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }
}
