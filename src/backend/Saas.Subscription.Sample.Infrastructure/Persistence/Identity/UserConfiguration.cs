using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Defense in-depth: EmailNormalizer already canonicalizes in code, but this keeps the unique index
        // from being bypassed by writes that skip the domain (raw SQL, seeds, imports).
        builder.ToTable("users", table =>
            table.HasCheckConstraint(
                "ck_users_normalized_email",
                "normalized_email <> '' AND normalized_email = lower(btrim(normalized_email))"));

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();

        // Explicit name: callers identify a unique violation (23505) by its constraint name.
        builder.HasIndex(user => user.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");

        // PostgreSQL's xmin system column changes on every UPDATE; EF uses it for optimistic concurrency.
        // A shadow property keeps this persistence detail out of the domain entity.
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
