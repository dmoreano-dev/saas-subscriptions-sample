using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Catalog;
using Saas.Subscription.Sample.Domain.Subscriptions;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Subscriptions;

internal sealed class AccountSubscriptionConfiguration : IEntityTypeConfiguration<AccountSubscription>
{
    public void Configure(EntityTypeBuilder<AccountSubscription> builder)
    {
        builder.ToTable("subscriptions", table =>
        {
            table.HasCheckConstraint("ck_subscriptions_status", "status IN ('Active', 'PastDue', 'Ended')");
            table.HasCheckConstraint("ck_subscriptions_period", "current_period_end > current_period_start");
            table.HasCheckConstraint("ck_subscriptions_access_until", "access_until > current_period_start");
            table.HasCheckConstraint("ck_subscriptions_ended_at", "(status = 'Ended') = (ended_at IS NOT NULL)");
        });

        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).ValueGeneratedNever();
        builder.Property(subscription => subscription.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(subscription => subscription.CurrentPeriodStart).IsRequired();
        builder.Property(subscription => subscription.CurrentPeriodEnd).IsRequired();
        builder.Property(subscription => subscription.AccessUntil).IsRequired();
        builder.Property(subscription => subscription.StartedAt).IsRequired();
        builder.Property(subscription => subscription.CreatedAt).IsRequired();
        builder.Property(subscription => subscription.UpdatedAt).IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(subscription => subscription.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Only a published version can be referenced; a trigger in the migration enforces it.
        builder.HasOne<PlanVersion>()
            .WithMany()
            .HasForeignKey(subscription => subscription.PlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Invariant 6: at most one base subscription per account that has not ended; ended ones stay as history.
        builder.HasIndex(subscription => subscription.AccountId)
            .IsUnique()
            .HasFilter("status <> 'Ended'")
            .HasDatabaseName("ux_subscriptions_account_id_current");

        // Changes reference a subscription with (account_id, subscription_id) against this key, so a change of one
        // account can never point at the subscription of another.
        builder.HasAlternateKey(subscription => new { subscription.AccountId, subscription.Id });

        builder.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class SubscriptionChangeConfiguration : IEntityTypeConfiguration<SubscriptionChange>
{
    public void Configure(EntityTypeBuilder<SubscriptionChange> builder)
    {
        builder.ToTable("subscription_changes", table =>
        {
            table.HasCheckConstraint("ck_subscription_changes_kind", "kind IN ('PlanChange', 'Cancel')");
            table.HasCheckConstraint("ck_subscription_changes_status", "status IN ('Pending', 'Applied', 'Cancelled')");

            // A plan change names its target version; a cancellation never does.
            table.HasCheckConstraint("ck_subscription_changes_target", "(kind = 'PlanChange') = (target_plan_version_id IS NOT NULL)");
            table.HasCheckConstraint("ck_subscription_changes_resolved_at", "(status = 'Pending') = (resolved_at IS NULL)");
        });

        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(change => change.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(change => change.EffectiveAt).IsRequired();
        builder.Property(change => change.RequestedAt).IsRequired();

        builder.HasOne<AccountSubscription>()
            .WithMany()
            .HasForeignKey(change => new { change.AccountId, change.SubscriptionId })
            .HasPrincipalKey(subscription => new { subscription.AccountId, subscription.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PlanVersion>()
            .WithMany()
            .HasForeignKey(change => change.TargetPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        // One pending change per subscription: scheduling another means cancelling the first.
        builder.HasIndex(change => change.SubscriptionId)
            .IsUnique()
            .HasFilter("status = 'Pending'")
            .HasDatabaseName("ux_subscription_changes_subscription_id_pending");

        builder.Property<uint>("xmin").IsRowVersion();
    }
}
