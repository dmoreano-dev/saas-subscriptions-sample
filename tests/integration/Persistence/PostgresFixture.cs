using Npgsql;
using Saas.Subscription.Sample.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Saas.Subscription.Sample.IntegrationTests.Persistence;

/// <summary>
/// One real PostgreSQL container shared by every relational test (no in-memory EF, no external
/// credentials; Docker is the only requirement). Each test gets its own database, so tests stay isolated.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Keep in sync with WithImageTag(...) in the AppHost: PostgreSQL major 17 (see README "Pinned toolchain").
    private const string Image = "postgres:17";
    private const string AdminPassword = "integration-test-admin";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image)
        .WithUsername("postgres")
        .WithPassword(AdminPassword)
        .Build();

    private readonly Lazy<Task> _migratedTemplate;

    public PostgresFixture() => _migratedTemplate = new Lazy<Task>(CreateMigratedTemplateAsync);

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception exception)
        {
            // Fail loudly: a silently skipped relational test would look like passing evidence.
            throw new InvalidOperationException(
                "PostgreSQL integration tests need a running Docker daemon (Testcontainers could not start " +
                $"'{Image}'). Start Docker and re-run; these tests are intentionally not skipped.",
                exception);
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>A new database with nothing in it (no schemas, no tables).</summary>
    public async Task<TestDatabase> CreateEmptyDatabaseAsync()
    {
        var name = $"test_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(ConnectionString("postgres", "postgres", AdminPassword));
        await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin).ExecuteNonQueryAsync();
        return new TestDatabase(ConnectionString(name, "postgres", AdminPassword));
    }

    /// <summary>A new database with every migration applied (cloned from a migrated template).</summary>
    public async Task<TestDatabase> CreateMigratedDatabaseAsync()
    {
        await _migratedTemplate.Value;

        var name = $"test_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(ConnectionString("postgres", "postgres", AdminPassword));
        await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE \"{name}\" TEMPLATE migrated_template", admin).ExecuteNonQueryAsync();
        return new TestDatabase(ConnectionString(name, "postgres", AdminPassword));
    }

    private async Task CreateMigratedTemplateAsync()
    {
        await using (var admin = new NpgsqlConnection(ConnectionString("postgres", "postgres", AdminPassword)))
        {
            await admin.OpenAsync();
            await new NpgsqlCommand("CREATE DATABASE migrated_template", admin).ExecuteNonQueryAsync();
        }

        await DatabaseMigrator.MigrateAsync(ConnectionString("migrated_template", "postgres", AdminPassword));
    }

    private string ConnectionString(string database, string username, string password) =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = database,
            Username = username,
            Password = password,
            Pooling = false,
        }.ConnectionString;
}

public sealed record TestDatabase(string ConnectionString);

[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
