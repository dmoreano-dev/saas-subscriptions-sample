using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Saas.Subscription.Sample.Infrastructure.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tools, which need a context to read the model. They never open a
/// connection (adding a migration only writes files), so the connection string is intentionally empty:
/// it just selects the Npgsql provider.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), connectionString: string.Empty).Options);
}
