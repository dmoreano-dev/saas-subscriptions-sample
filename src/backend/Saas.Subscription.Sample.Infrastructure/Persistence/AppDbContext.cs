using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Domain.Projects;
using Saas.Subscription.Sample.Domain.Subscriptions;

namespace Saas.Subscription.Sample.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<PasswordCredential> PasswordCredentials => Set<PasswordCredential>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<PermissionDefinition> Permissions => Set<PermissionDefinition>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RoleGrant> RolePermissions => Set<RoleGrant>();

    public DbSet<MembershipRole> MembershipRoles => Set<MembershipRole>();

    public DbSet<PlatformRoleAssignment> PlatformRoleAssignments => Set<PlatformRoleAssignment>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<FeatureDefinition> FeatureDefinitions => Set<FeatureDefinition>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<PlanVersion> PlanVersions => Set<PlanVersion>();

    public DbSet<PlanCapability> PlanCapabilities => Set<PlanCapability>();

    public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();

    public DbSet<AccountSubscription> Subscriptions => Set<AccountSubscription>();

    public DbSet<SubscriptionChange> SubscriptionChanges => Set<SubscriptionChange>();

    public static DbContextOptionsBuilder<AppDbContext> ConfigureOptions(
        DbContextOptionsBuilder<AppDbContext> builder,
        string connectionString) =>
        builder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
