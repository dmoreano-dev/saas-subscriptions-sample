using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Identity;

internal sealed class PasswordCredentialConfiguration : IEntityTypeConfiguration<PasswordCredential>
{
    public void Configure(EntityTypeBuilder<PasswordCredential> builder)
    {
        // The algorithm list grows with each supported verifier; the encoded hash is bounded and never empty.
        builder.ToTable("password_credentials", table =>
        {
            table.HasCheckConstraint("ck_password_credentials_algorithm", "algorithm IN ('argon2id')");
            table.HasCheckConstraint("ck_password_credentials_encoded_hash", "encoded_hash <> ''");
        });

        // One credential per user: the user id is the key.
        builder.HasKey(credential => credential.UserId);
        builder.Property(credential => credential.UserId).ValueGeneratedNever();
        builder.Property(credential => credential.Algorithm).HasMaxLength(32).IsRequired();
        builder.Property(credential => credential.EncodedHash).HasMaxLength(255).IsRequired();
        builder.Property(credential => credential.CreatedAt).IsRequired();
        builder.Property(credential => credential.UpdatedAt).IsRequired();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<PasswordCredential>(credential => credential.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<uint>("xmin").IsRowVersion();
    }
}
