using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saas.Subscription.Sample.Domain.Accounts;
using Saas.Subscription.Sample.Domain.Projects;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Projects;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id);
        builder.Property(project => project.Id).ValueGeneratedNever();
        builder.Property(project => project.Name).HasMaxLength(200).IsRequired();
        builder.Property(project => project.Description).HasMaxLength(2000);
        builder.Property(project => project.CreatedAt).IsRequired();
        builder.Property(project => project.UpdatedAt).IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(project => project.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tables that reference a project must do it with (account_id, project_id) against this key, so a row of
        // one account can never point at a project of another. It also serves lookups by account.
        builder.HasAlternateKey(project => new { project.AccountId, project.Id });

        builder.Property<uint>("xmin").IsRowVersion();
    }
}
