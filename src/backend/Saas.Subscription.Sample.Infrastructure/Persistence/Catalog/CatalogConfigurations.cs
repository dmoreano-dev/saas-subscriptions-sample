using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saas.Subscription.Sample.Domain.Catalog;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Catalog;

internal sealed class FeatureDefinitionConfiguration : IEntityTypeConfiguration<FeatureDefinition>
{
    public void Configure(EntityTypeBuilder<FeatureDefinition> builder)
    {
        builder.ToTable("feature_definitions", table =>
        {
            table.HasCheckConstraint("ck_feature_definitions_value_type", "value_type IN ('Boolean', 'Limit')");
            table.HasCheckConstraint("ck_feature_definitions_unit", "unit IS NULL OR unit IN ('Projects', 'Reports', 'Seats')");

            // Only limits have a unit; booleans never do.
            table.HasCheckConstraint("ck_feature_definitions_limit_has_unit", "(value_type = 'Limit') = (unit IS NOT NULL)");
        });

        builder.HasKey(definition => definition.Key);
        builder.Property(definition => definition.Key).HasMaxLength(100).ValueGeneratedNever();
        builder.Property(definition => definition.ValueType).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(definition => definition.Unit).HasConversion<string>().HasMaxLength(16);
        builder.Property(definition => definition.Description).HasMaxLength(500).IsRequired();
    }
}

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans", table =>
        {
            table.HasCheckConstraint("ck_plans_account_type", "account_type IN ('Personal', 'Organization')");
        });

        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();
        builder.Property(plan => plan.Key).HasMaxLength(50).IsRequired();
        builder.Property(plan => plan.Name).HasMaxLength(100).IsRequired();
        builder.Property(plan => plan.AccountType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(plan => plan.IsBillable).IsRequired();
        builder.Property(plan => plan.CreatedAt).IsRequired();

        builder.HasIndex(plan => plan.Key).IsUnique().HasDatabaseName("ux_plans_key");
    }
}

internal sealed class PlanVersionConfiguration : IEntityTypeConfiguration<PlanVersion>
{
    public void Configure(EntityTypeBuilder<PlanVersion> builder)
    {
        builder.ToTable("plan_versions", table =>
        {
            table.HasCheckConstraint("ck_plan_versions_status", "status IN ('Draft', 'Published')");
            table.HasCheckConstraint("ck_plan_versions_version", "version >= 1");
            table.HasCheckConstraint("ck_plan_versions_published_at", "(status = 'Published') = (published_at IS NOT NULL)");
        });

        builder.HasKey(version => version.Id);
        builder.Property(version => version.Id).ValueGeneratedNever();
        builder.Property(version => version.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(version => version.CreatedAt).IsRequired();

        builder.HasOne(version => version.Plan)
            .WithMany()
            .HasForeignKey(version => version.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(version => new { version.PlanId, version.Version })
            .IsUnique()
            .HasDatabaseName("ux_plan_versions_plan_id_version");

        builder.HasMany(version => version.Capabilities)
            .WithOne()
            .HasForeignKey(capability => capability.PlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(version => version.Capabilities).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(version => version.Prices)
            .WithOne()
            .HasForeignKey(price => price.PlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(version => version.Prices).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PlanCapabilityConfiguration : IEntityTypeConfiguration<PlanCapability>
{
    public void Configure(EntityTypeBuilder<PlanCapability> builder)
    {
        builder.ToTable("plan_capabilities", table =>
        {
            // Exactly one of: boolean value, limit, unlimited. Unlimited is never an arbitrary large number.
            table.HasCheckConstraint(
                "ck_plan_capabilities_one_value",
                "(bool_value IS NOT NULL)::int + (limit_value IS NOT NULL)::int + is_unlimited::int = 1");
            table.HasCheckConstraint("ck_plan_capabilities_limit_value", "limit_value IS NULL OR limit_value >= 0");
        });

        builder.HasKey(capability => new { capability.PlanVersionId, capability.FeatureKey });
        builder.Property(capability => capability.FeatureKey).HasMaxLength(100);
        builder.Property(capability => capability.IsUnlimited).IsRequired();

        builder.HasOne(capability => capability.Definition)
            .WithMany()
            .HasForeignKey(capability => capability.FeatureKey)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PlanPriceConfiguration : IEntityTypeConfiguration<PlanPrice>
{
    public void Configure(EntityTypeBuilder<PlanPrice> builder)
    {
        builder.ToTable("plan_prices", table =>
        {
            table.HasCheckConstraint("ck_plan_prices_interval", "interval IN ('Month', 'Year')");
            table.HasCheckConstraint("ck_plan_prices_currency", "currency ~ '^[A-Z]{3}$'");
            table.HasCheckConstraint("ck_plan_prices_unit_amount_minor", "unit_amount_minor > 0");
            table.HasCheckConstraint("ck_plan_prices_external_price_id", "external_price_id IS NULL OR external_price_id <> ''");
        });

        builder.HasKey(price => price.Id);
        builder.Property(price => price.Id).ValueGeneratedNever();
        builder.Property(price => price.Interval).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(price => price.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(price => price.UnitAmountMinor).IsRequired();

        // Provider reference: nullable and separate; the catalog never depends on any provider.
        builder.Property(price => price.ExternalPriceId).HasMaxLength(255);

        builder.HasIndex(price => new { price.PlanVersionId, price.Interval, price.Currency })
            .IsUnique()
            .HasDatabaseName("ux_plan_prices_plan_version_id_interval_currency");
    }
}
