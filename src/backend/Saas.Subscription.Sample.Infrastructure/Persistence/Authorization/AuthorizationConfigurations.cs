using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Authorization;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Authorization;

// Roles and permissions are part of the code (handlers name them), so they are migration data, not Migrator
// fixtures: a database at a given migration always has exactly the rows that code expects.

internal sealed class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
    public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
    {
        builder.ToTable("permissions", table =>
            table.HasCheckConstraint("ck_permissions_scope", "scope IN ('Account', 'Platform')"));

        builder.HasKey(permission => permission.Key);
        builder.Property(permission => permission.Key).HasMaxLength(64).ValueGeneratedNever();
        builder.Property(permission => permission.Scope).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(permission => permission.Description).HasMaxLength(200).IsRequired();

        // Target of the composite foreign key that keeps a role and its permissions in one scope.
        builder.HasAlternateKey(permission => new { permission.Key, permission.Scope });

        builder.HasData(Permissions.All);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", table =>
            table.HasCheckConstraint("ck_roles_scope", "scope IN ('Account', 'Platform')"));

        builder.HasKey(role => role.Key);
        builder.Property(role => role.Key).HasMaxLength(64).ValueGeneratedNever();
        builder.Property(role => role.Scope).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(100).IsRequired();

        builder.HasAlternateKey(role => new { role.Key, role.Scope });

        builder.HasData(SystemRoles.All.Select(role => new { role.Key, role.Scope, role.Name }));
    }
}

internal sealed class RoleGrantConfiguration : IEntityTypeConfiguration<RoleGrant>
{
    public void Configure(EntityTypeBuilder<RoleGrant> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(grant => new { grant.RoleKey, grant.PermissionKey });
        builder.Property(grant => grant.RoleKey).HasMaxLength(64);
        builder.Property(grant => grant.PermissionKey).HasMaxLength(64);
        builder.Property(grant => grant.Scope).HasConversion<string>().HasMaxLength(16).IsRequired();

        // Both foreign keys carry the scope, so an account role cannot be given a platform permission or vice versa.
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(grant => new { grant.RoleKey, grant.Scope })
            .HasPrincipalKey(role => new { role.Key, role.Scope })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PermissionDefinition>()
            .WithMany()
            .HasForeignKey(grant => new { grant.PermissionKey, grant.Scope })
            .HasPrincipalKey(permission => new { permission.Key, permission.Scope })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SystemRoles.All.SelectMany(role =>
            role.Permissions.Select(permission => new { RoleKey = role.Key, PermissionKey = permission, role.Scope })));
    }
}

internal sealed class MembershipRoleConfiguration : IEntityTypeConfiguration<MembershipRole>
{
    public void Configure(EntityTypeBuilder<MembershipRole> builder)
    {
        // The constant scope plus the composite foreign key make a platform role impossible to attach to a membership.
        builder.ToTable("membership_roles", table =>
            table.HasCheckConstraint("ck_membership_roles_role_scope", "role_scope = 'Account'"));

        builder.HasKey(assignment => new { assignment.MembershipId, assignment.RoleKey });
        builder.Property(assignment => assignment.RoleKey).HasMaxLength(64);
        builder.Property(assignment => assignment.RoleScope).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(assignment => assignment.CreatedAt).IsRequired();

        builder.HasOne<Membership>()
            .WithMany()
            .HasForeignKey(assignment => assignment.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(assignment => new { assignment.RoleKey, assignment.RoleScope })
            .HasPrincipalKey(role => new { role.Key, role.Scope })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PlatformRoleAssignmentConfiguration : IEntityTypeConfiguration<PlatformRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlatformRoleAssignment> builder)
    {
        builder.ToTable("platform_role_assignments", table =>
            table.HasCheckConstraint("ck_platform_role_assignments_role_scope", "role_scope = 'Platform'"));

        builder.HasKey(assignment => new { assignment.UserId, assignment.RoleKey });
        builder.Property(assignment => assignment.RoleKey).HasMaxLength(64);
        builder.Property(assignment => assignment.RoleScope).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(assignment => assignment.CreatedAt).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(assignment => new { assignment.RoleKey, assignment.RoleScope })
            .HasPrincipalKey(role => new { role.Key, role.Scope })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
