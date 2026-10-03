using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Infrastructure.Identity;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

[Collection(PostgresTestGroup.Name)]
public class PasswordCredentialTests(PostgresFixture postgres)
{
    private const string AnyEmail = "a@x.com";
    private const string ValidPassword = "right-password";
    private const string OtherPassword = "wrong-password";
    private const string UnknownAlgorithm = "md5";
    private const string UnknownStatus = "Deleted";
    private const string EmptyHash = "";
    private const string UniqueEmailIndex = "ux_users_normalized_email";
    private const int ConcurrentAttempts = 8;
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.UnixEpoch;

    private static readonly string[] EmailVariants =
        ["a@x.com", "A@x.com", " a@X.com ", "A@X.COM", "a@x.com ", " A@x.com", "a@X.com", "A@X.com"];

    private const string PasswordCredentialColumnsQuery = """
        SELECT column_name || ':' || data_type || ':' || is_nullable
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'password_credentials'
        ORDER BY column_name COLLATE "C"
        """;

    private const string UserStatusColumnQuery = """
        SELECT data_type || ':' || is_nullable || ':' || column_default
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'users' AND column_name = 'status'
        """;

    private const string ConstraintsQuery = """
        SELECT description
        FROM (
            SELECT conrelid::regclass::text || '.' || conname || ':' || contype::text AS description
            FROM pg_constraint
            WHERE connamespace = 'public'::regnamespace
              AND (conrelid = 'password_credentials'::regclass OR conname = 'ck_users_status')
        ) constraints
        ORDER BY description COLLATE "C"
        """;

    private const string PasswordColumnsInOtherTablesQuery = """
        SELECT table_name || '.' || column_name
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name <> 'password_credentials'
          AND (column_name LIKE '%password%' OR column_name LIKE '%hash%')
        """;

    private static AppDbContext CreateContext(string connectionString) =>
        new(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString).Options);

    private static Argon2idPasswordHasher CreateHasher() =>
        new(Options.Create(new PasswordHashingOptions
        {
            MemoryMebibytes = PasswordHashingOptions.MinMemoryMebibytes,
            Iterations = PasswordHashingOptions.MinIterations,
        }));

    private async Task<NpgsqlConnection> OpenMigratedConnectionAsync()
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        return await Sql.OpenAsync(database.ConnectionString);
    }

    /// <summary>Saves a user with its own context (one connection each) after the start gate opens; returns the failure, if any.</summary>
    private static async Task<Exception?> TryRegisterAsync(string connectionString, string email, TaskCompletionSource startGate)
    {
        await using var context = CreateContext(connectionString);
        context.Users.Add(User.Create(email, FixedNow));
        await startGate.Task;
        try
        {
            await context.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException exception)
        {
            return exception;
        }
    }

    // ---- schema inspection ----

    [Fact]
    public async Task Schema_PasswordCredentials_HasExpectedColumns()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var columns = await Sql.ColumnAsync(connection, PasswordCredentialColumnsQuery);

        // Assert
        Assert.Equal(
            [
                "algorithm:character varying:NO",
                "created_at:timestamp with time zone:NO",
                "encoded_hash:character varying:NO",
                "updated_at:timestamp with time zone:NO",
                "user_id:uuid:NO",
            ],
            columns);
    }

    [Fact]
    public async Task Schema_PasswordCredentials_HasKeyForeignKeyAndChecks()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var constraints = await Sql.ColumnAsync(connection, ConstraintsQuery);

        // Assert
        Assert.Equal(
            [
                "password_credentials.ck_password_credentials_algorithm:c",
                "password_credentials.ck_password_credentials_encoded_hash:c",
                "password_credentials.fk_password_credentials_users_user_id:f",
                "password_credentials.pk_password_credentials:p",
                "users.ck_users_status:c",
            ],
            constraints);
    }

    [Fact]
    public async Task Schema_UsersStatus_IsRequiredWithActiveDefault()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var column = await Sql.ScalarAsync<string>(connection, UserStatusColumnQuery);

        // Assert
        Assert.Equal("character varying:NO:'Active'::character varying", column);
    }

    [Fact]
    public async Task Schema_OtherTables_HoldNoPasswordOrHashColumns()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var columns = await Sql.ColumnAsync(connection, PasswordColumnsInOtherTablesQuery);

        // Assert
        Assert.Empty(columns);
    }

    // ---- constraints ----

    [Fact]
    public async Task InsertUser_WithoutStatus_DefaultsToActive()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();

        // Act
        await Sql.InsertUserAsync(connection, userId, AnyEmail);

        // Assert
        var status = await Sql.ScalarAsync<string>(connection, "SELECT status FROM users WHERE id = @id", ("id", userId));
        Assert.Equal(nameof(UserStatus.Active), status);
    }

    [Fact]
    public async Task UpdateUser_UnknownStatus_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail);

        // Act
        var act = () => Sql.ExecuteAsync(connection, "UPDATE users SET status = @status WHERE id = @id", ("status", UnknownStatus), ("id", userId));

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    [Fact]
    public async Task InsertPasswordCredential_UnknownAlgorithm_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail);

        // Act
        var act = () => Sql.InsertPasswordCredentialAsync(connection, userId, algorithm: UnknownAlgorithm);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    [Fact]
    public async Task InsertPasswordCredential_EmptyEncodedHash_ThrowsCheckViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail);

        // Act
        var act = () => Sql.InsertPasswordCredentialAsync(connection, userId, encodedHash: EmptyHash);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.CheckViolation, act);
    }

    [Fact]
    public async Task InsertPasswordCredential_UnknownUser_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();

        // Act
        var act = () => Sql.InsertPasswordCredentialAsync(connection, Sql.NewId());

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    [Fact]
    public async Task InsertPasswordCredential_SecondCredentialForSameUser_ThrowsUniqueViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail);
        await Sql.InsertPasswordCredentialAsync(connection, userId);

        // Act
        var act = () => Sql.InsertPasswordCredentialAsync(connection, userId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.UniqueViolation, act);
    }

    [Fact]
    public async Task DeleteUser_WithPasswordCredential_ThrowsForeignKeyViolation()
    {
        // Arrange
        await using var connection = await OpenMigratedConnectionAsync();
        var userId = Sql.NewId();
        await Sql.InsertUserAsync(connection, userId, AnyEmail);
        await Sql.InsertPasswordCredentialAsync(connection, userId);

        // Act
        var act = () => Sql.DeleteUserAsync(connection, userId);

        // Assert
        await Sql.AssertSqlStateAsync(PostgresErrorCodes.ForeignKeyViolation, act);
    }

    // ---- round trip through EF with the real hasher ----

    [Fact]
    public async Task SaveChanges_UserWithHashedPassword_VerifiesAfterReload()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var hasher = CreateHasher();
        var user = User.Create(AnyEmail, FixedNow);
        await using (var writer = CreateContext(database.ConnectionString))
        {
            writer.Users.Add(user);
            writer.PasswordCredentials.Add(PasswordCredential.Create(user.Id, hasher.Hash(ValidPassword), FixedNow));
            await writer.SaveChangesAsync();
        }

        await using var reader = CreateContext(database.ConnectionString);
        var stored = (await reader.PasswordCredentials.SingleAsync(credential => credential.UserId == user.Id)).ToStoredHash();

        // Act
        var result = hasher.Verify(ValidPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Succeeded, result);
    }

    [Fact]
    public async Task SaveChanges_UserWithHashedPassword_RejectsAWrongPasswordAfterReload()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var hasher = CreateHasher();
        var user = User.Create(AnyEmail, FixedNow);
        await using (var writer = CreateContext(database.ConnectionString))
        {
            writer.Users.Add(user);
            writer.PasswordCredentials.Add(PasswordCredential.Create(user.Id, hasher.Hash(ValidPassword), FixedNow));
            await writer.SaveChangesAsync();
        }

        await using var reader = CreateContext(database.ConnectionString);
        var stored = (await reader.PasswordCredentials.SingleAsync(credential => credential.UserId == user.Id)).ToStoredHash();

        // Act
        var result = hasher.Verify(OtherPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public async Task SaveChanges_HashedPassword_NeverStoresThePasswordOrItsHashOutsideTheCredential()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var hasher = CreateHasher();
        var user = User.Create(AnyEmail, FixedNow);

        // Act
        await using (var writer = CreateContext(database.ConnectionString))
        {
            writer.Users.Add(user);
            writer.PasswordCredentials.Add(PasswordCredential.Create(user.Id, hasher.Hash(ValidPassword), FixedNow));
            await writer.SaveChangesAsync();
        }

        // Assert
        await using var inspection = await Sql.OpenAsync(database.ConnectionString);
        var rowsContainingPassword = await Sql.ScalarAsync<long>(
            inspection,
            "SELECT count(*) FROM password_credentials WHERE encoded_hash LIKE '%' || @password || '%'",
            ("password", ValidPassword));
        Assert.Equal(0, rowsContainingPassword);
    }

    [Fact]
    public async Task SaveChanges_RehashedCredential_PersistsTheNewHash()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var hasher = CreateHasher();
        var user = User.Create(AnyEmail, FixedNow);
        await using (var writer = CreateContext(database.ConnectionString))
        {
            writer.Users.Add(user);
            writer.PasswordCredentials.Add(PasswordCredential.Create(user.Id, hasher.Hash(ValidPassword), FixedNow));
            await writer.SaveChangesAsync();
        }

        var newHash = hasher.Hash(ValidPassword);

        // Act
        await using (var updater = CreateContext(database.ConnectionString))
        {
            var credential = await updater.PasswordCredentials.SingleAsync(row => row.UserId == user.Id);
            credential.Rehash(newHash, FixedNow.AddDays(1));
            await updater.SaveChangesAsync();
        }

        // Assert
        await using var reader = CreateContext(database.ConnectionString);
        var stored = await reader.PasswordCredentials.SingleAsync(row => row.UserId == user.Id);
        Assert.Equal(newHash.EncodedHash, stored.EncodedHash);
    }

    // ---- duplicate email under concurrency ----

    [Fact]
    public async Task SaveChanges_ConcurrentUsersWithTheSameNormalizedEmail_PersistsExactlyOne()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = EmailVariants
            .Select(email => TryRegisterAsync(database.ConnectionString, email, startGate))
            .ToArray();

        // Act
        startGate.SetResult();
        var failures = await Task.WhenAll(attempts);

        // Assert
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        Assert.Equal(1, await Sql.ScalarAsync<long>(connection, "SELECT count(*) FROM users"));
        Assert.Equal(ConcurrentAttempts - 1, failures.Count(failure => failure is not null));
    }

    [Fact]
    public async Task SaveChanges_ConcurrentUsersWithTheSameNormalizedEmail_LosersFailOnTheUniqueIndex()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = EmailVariants
            .Select(email => TryRegisterAsync(database.ConnectionString, email, startGate))
            .ToArray();

        // Act
        startGate.SetResult();
        var failures = await Task.WhenAll(attempts);

        // Assert
        var violations = failures.OfType<DbUpdateException>().Select(failure => Assert.IsType<PostgresException>(failure.InnerException)).ToArray();
        Assert.All(violations, violation => Assert.Equal(PostgresErrorCodes.UniqueViolation, violation.SqlState));
        Assert.All(violations, violation => Assert.Equal(UniqueEmailIndex, violation.ConstraintName));
    }
}
