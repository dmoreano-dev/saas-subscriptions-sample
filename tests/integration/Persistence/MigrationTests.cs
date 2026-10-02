using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

[Collection(PostgresTestGroup.Name)]
public class MigrationTests(PostgresFixture postgres)
{
    // The model is only inspected, never queried, so this connection string is never opened.
    private const string NeverOpenedConnectionString = "Host=localhost;Database=never_opened";

    private const string TableNamesQuery =
        "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name COLLATE \"C\"";

    private const string AppliedMigrationsCountQuery = "SELECT count(*) FROM \"__EFMigrationsHistory\"";

    private const string KeyColumnsThatAreNotUuidQuery = """
        SELECT table_name || '.' || column_name || ':' || data_type
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
          AND (column_name = 'id' OR column_name LIKE '%\_id')
          AND data_type <> 'uuid'
        """;

    private const string TimestampColumnsThatAreNotTimestamptzQuery = """
        SELECT table_name || '.' || column_name || ':' || data_type
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
          AND column_name LIKE '%\_at'
          AND data_type <> 'timestamp with time zone'
        """;

    private const string ColumnsThatAreNotSnakeCaseQuery = """
        SELECT table_name || '.' || column_name
        FROM information_schema.columns
        WHERE table_schema = 'public' AND column_name !~ '^[a-z][a-z0-9_]*$'
        """;

    private static AppDbContext CreateContext(string connectionString) =>
        new(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString).Options);

    private static async Task<long> CountAppliedMigrationsAsync(string connectionString)
    {
        await using var connection = await Sql.OpenAsync(connectionString);
        return await Sql.ScalarAsync<long>(connection, AppliedMigrationsCountQuery);
    }

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_CreatesExpectedTables()
    {
        // Arrange
        var database = await postgres.CreateEmptyDatabaseAsync();

        // Act
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        // Assert
        await using var connection = await Sql.OpenAsync(database.ConnectionString);
        var tables = await Sql.ColumnAsync(connection, TableNamesQuery);
        Assert.Equal(["__EFMigrationsHistory", "accounts", "memberships", "users"], tables);
    }

    [Fact]
    public async Task MigrateAsync_EmptyDatabase_LeavesNoPendingMigrations()
    {
        // Arrange
        var database = await postgres.CreateEmptyDatabaseAsync();

        // Act
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        // Assert
        await using var context = CreateContext(database.ConnectionString);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task MigrateAsync_AlreadyMigratedDatabase_AppliesNothingAgain()
    {
        // Arrange
        var database = await postgres.CreateEmptyDatabaseAsync();
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);
        var appliedBefore = await CountAppliedMigrationsAsync(database.ConnectionString);

        // Act
        await DatabaseMigrator.MigrateAsync(database.ConnectionString);

        // Assert
        Assert.Equal(appliedBefore, await CountAppliedMigrationsAsync(database.ConnectionString));
    }

    [Fact]
    public async Task MigrateAsync_ConcurrentRuns_AppliesEachMigrationOnce()
    {
        // Arrange
        var database = await postgres.CreateEmptyDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        var migrationCount = context.Database.GetMigrations().Count();

        // Act
        var first = DatabaseMigrator.MigrateAsync(database.ConnectionString);
        var second = DatabaseMigrator.MigrateAsync(database.ConnectionString);
        var third = DatabaseMigrator.MigrateAsync(database.ConnectionString);
        await Task.WhenAll(first, second, third);

        // Assert
        Assert.Equal(migrationCount, await CountAppliedMigrationsAsync(database.ConnectionString));
    }

    [Fact]
    public void HasPendingModelChanges_CurrentModel_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext(NeverOpenedConnectionString);

        // Act
        var hasPendingChanges = context.Database.HasPendingModelChanges();

        // Assert
        Assert.False(
            hasPendingChanges,
            "The model changed without a migration. Run `dotnet tool run dotnet-ef migrations add <Name>`.");
    }

    [Fact]
    public async Task MigratedSchema_KeyColumns_AreUuid()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var offenders = await Sql.ColumnAsync(connection, KeyColumnsThatAreNotUuidQuery);

        // Assert
        Assert.Empty(offenders);
    }

    [Fact]
    public async Task MigratedSchema_TimestampColumns_AreTimestamptz()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var offenders = await Sql.ColumnAsync(connection, TimestampColumnsThatAreNotTimestamptzQuery);

        // Assert
        Assert.Empty(offenders);
    }

    [Fact]
    public async Task MigratedSchema_ColumnNames_AreSnakeCase()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        await using var connection = await Sql.OpenAsync(database.ConnectionString);

        // Act
        var offenders = await Sql.ColumnAsync(connection, ColumnsThatAreNotSnakeCaseQuery);

        // Assert
        Assert.Empty(offenders);
    }
}
