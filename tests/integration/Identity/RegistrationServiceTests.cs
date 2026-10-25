using Microsoft.EntityFrameworkCore;
using Npgsql;
using Saas.Subscription.Sample.IntegrationTests.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

[Collection(PostgresTestGroup.Name)]
public class RegistrationServiceTests(PostgresFixture postgres)
{
    private const int ConcurrentAttempts = 8;
    private const string PersonalType = "Personal";
    private const string ForcedFailureCode = "P0001";

    private static readonly string[] EmailVariants =
        ["ana@example.com", "ANA@example.com", " ana@Example.com ", "Ana@EXAMPLE.COM", "ana@example.com ", " Ana@example.com", "ana@Example.com", "ANA@Example.COM"];

    private const string InstallFailingMembershipTrigger = """
        CREATE FUNCTION fail_membership_insert() RETURNS trigger AS $$
        BEGIN RAISE EXCEPTION 'forced failure' USING ERRCODE = 'P0001'; END
        $$ LANGUAGE plpgsql;
        CREATE TRIGGER trg_fail_membership_insert BEFORE INSERT ON memberships
            FOR EACH ROW EXECUTE FUNCTION fail_membership_insert();
        """;

    private static async Task<long> CountAsync(NpgsqlConnection connection, string table) =>
        await Sql.ScalarAsync<long>(connection, $"SELECT count(*) FROM {table}");

    private static async Task<(long Users, long Credentials, long Accounts, long Memberships)> CountAllAsync(string connectionString)
    {
        await using var connection = await Sql.OpenAsync(connectionString);
        return (
            await CountAsync(connection, "users"),
            await CountAsync(connection, "password_credentials"),
            await CountAsync(connection, "accounts"),
            await CountAsync(connection, "memberships"));
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_PersistsUserCredentialPersonalAccountAndOwnerMembership()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);

        // Act
        var result = await harness.RegisterAsync();

        // Assert
        Assert.True(result.Created);
        await using var connection = await Sql.OpenAsync(harness.ConnectionString);
        Assert.Equal((1L, 1L, 1L, 1L), await CountAllAsync(harness.ConnectionString));
        Assert.Equal(
            1L,
            await Sql.ScalarAsync<long>(
                connection,
                "SELECT count(*) FROM accounts a JOIN memberships m ON m.account_id = a.id AND m.user_id = a.personal_owner_user_id " +
                "WHERE a.id = @account AND a.type = @type AND a.personal_owner_user_id = @user AND a.status = 'Active'",
                ("account", result.User.PersonalAccountId), ("type", PersonalType), ("user", result.User.UserId)));
        Assert.Equal(IdentityHarness.Email, result.User.Email);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_GivesTheOwnerMembershipThePersonalOwnerRole()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);

        // Act
        var result = await harness.RegisterAsync();

        // Assert
        await using var connection = await Sql.OpenAsync(harness.ConnectionString);
        var roles = await Sql.ColumnAsync(
            connection,
            "SELECT r.role_key || ':' || m.status FROM membership_roles r JOIN memberships m ON m.id = r.membership_id " +
            $"WHERE m.account_id = '{result.User.PersonalAccountId}' AND m.user_id = '{result.User.UserId}'");
        Assert.Equal(["personal_owner:Active"], roles);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_StoresAHashAndNeverThePassword()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);

        // Act
        await harness.RegisterAsync();

        // Assert
        await using var connection = await Sql.OpenAsync(harness.ConnectionString);
        var stored = await Sql.ScalarAsync<string>(connection, "SELECT encoded_hash FROM password_credentials");
        Assert.StartsWith("$argon2id$", stored, StringComparison.Ordinal);
        Assert.DoesNotContain(IdentityHarness.Password, stored, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_FailureWhileInsertingTheMembership_RollsBackEveryRow()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        await using (var connection = await Sql.OpenAsync(harness.ConnectionString))
        {
            await Sql.ExecuteAsync(connection, InstallFailingMembershipTrigger);
        }

        // Act
        var failure = await Record.ExceptionAsync(() => harness.RegisterAsync());

        // Assert
        var update = Assert.IsType<DbUpdateException>(failure);
        Assert.Equal(ForcedFailureCode, Assert.IsType<PostgresException>(update.InnerException).SqlState);
        Assert.Equal((0L, 0L, 0L, 0L), await CountAllAsync(harness.ConnectionString));
    }

    [Theory]
    [InlineData("ana@example.com")]
    [InlineData("ANA@Example.com")]
    [InlineData("  ana@example.com ")]
    public async Task RegisterAsync_ExistingEmail_ReturnsADecoyAndChangesNothing(string duplicate)
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        var original = await harness.RegisterAsync();

        // Act
        var result = await harness.RegisterAsync(duplicate, IdentityHarness.OtherPassword);

        // Assert
        Assert.False(result.Created);
        Assert.NotEqual(original.User.UserId, result.User.UserId);
        Assert.NotEqual(original.User.PersonalAccountId, result.User.PersonalAccountId);
        Assert.Equal(original.User.PersonalAccountName, result.User.PersonalAccountName);
        Assert.Equal((1L, 1L, 1L, 1L), await CountAllAsync(harness.ConnectionString));
        var login = await harness.LoginAsync(IdentityHarness.Email, IdentityHarness.Password);
        Assert.Equal(original.User.UserId, login.Identity?.UserId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RegisterAsync_NewOrExistingEmail_HashesThePasswordExactlyOnce(bool emailExists)
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        if (emailExists)
        {
            await harness.RegisterAsync();
        }

        var hashesBefore = harness.Hasher.HashCalls;

        // Act
        await harness.RegisterAsync(emailExists ? IdentityHarness.Email : "other@example.com");

        // Assert
        Assert.Equal(1, harness.Hasher.HashCalls - hashesBefore);
    }

    [Fact]
    public async Task RegisterAsync_ConcurrentRequestsForOneEmail_CreateExactlyOneCompleteAccount()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        var gate = new TaskCompletionSource();
        var attempts = EmailVariants.Take(ConcurrentAttempts)
            .Select(async email =>
            {
                await gate.Task;
                return await harness.RegisterAsync(email);
            })
            .ToList();

        // Act
        gate.SetResult();
        var results = await Task.WhenAll(attempts);

        // Assert
        Assert.Single(results, result => result.Created);
        Assert.Equal((1L, 1L, 1L, 1L), await CountAllAsync(harness.ConnectionString));
        Assert.Equal(ConcurrentAttempts, results.Select(result => result.User.UserId).Distinct().Count());
        var winner = Assert.Single(results, result => result.Created);
        var login = await harness.LoginAsync();
        Assert.Equal(winner.User.UserId, login.Identity?.UserId);
    }
}
