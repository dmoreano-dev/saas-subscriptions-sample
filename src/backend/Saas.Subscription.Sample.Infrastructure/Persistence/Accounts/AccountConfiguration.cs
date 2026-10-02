using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Accounts;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", table =>
        {
            table.HasCheckConstraint("ck_accounts_type", "type IN ('Personal', 'Organization')");
            table.HasCheckConstraint("ck_accounts_status", "status IN ('Active', 'Suspended', 'Closed')");

            // A personal account has a personal owner and an organization account does not.
            table.HasCheckConstraint(
                "ck_accounts_personal_owner",
                "(type = 'Personal') = (personal_owner_user_id IS NOT NULL)");
        });

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(account => account.Name).HasMaxLength(200).IsRequired();
        builder.Property(account => account.CreatedAt).IsRequired();
        builder.Property(account => account.UpdatedAt).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(account => account.PersonalOwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Invariant 1: at most one personal account per user.
        builder.HasIndex(account => account.PersonalOwnerUserId)
            .IsUnique()
            .HasFilter("type = 'Personal'")
            .HasDatabaseName("ux_accounts_personal_owner_user_id");

        builder.Property<uint>("xmin").IsRowVersion();

        // The deferrable composite foreign key (id, personal_owner_user_id) -> memberships
        // (account_id, user_id) cannot be expressed in EF and would make the model cyclic;
        // it is created with SQL in the initial migration.
    }
}
