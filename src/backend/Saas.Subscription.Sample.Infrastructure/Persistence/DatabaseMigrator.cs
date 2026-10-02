using Microsoft.EntityFrameworkCore;

namespace Saas.Subscription.Sample.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString).Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
