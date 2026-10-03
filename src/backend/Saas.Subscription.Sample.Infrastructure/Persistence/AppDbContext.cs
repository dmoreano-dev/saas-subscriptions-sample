using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<PasswordCredential> PasswordCredentials => Set<PasswordCredential>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<FeatureDefinition> FeatureDefinitions => Set<FeatureDefinition>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<PlanVersion> PlanVersions => Set<PlanVersion>();

    public DbSet<PlanCapability> PlanCapabilities => Set<PlanCapability>();

    public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();

    public static DbContextOptionsBuilder<AppDbContext> ConfigureOptions(
        DbContextOptionsBuilder<AppDbContext> builder,
        string connectionString) =>
        builder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
