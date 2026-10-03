using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Infrastructure.Persistence.Catalog;

namespace Saas.Subscription.Sample.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext(connectionString);
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>Inserts the missing plan catalog fixtures; safe to repeat. Run after <see cref="MigrateAsync"/>.</summary>
    public static async Task<CatalogSeedResult> SeedCatalogAsync(
        string connectionString,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext(connectionString);
        return await CatalogSeeder.SeedAsync(context, timeProvider ?? TimeProvider.System, cancellationToken);
    }

    private static AppDbContext CreateContext(string connectionString) =>
        new(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString).Options);
}
