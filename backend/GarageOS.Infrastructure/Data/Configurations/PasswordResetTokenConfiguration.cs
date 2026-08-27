using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(t => t.UserId).HasDatabaseName("password_reset_tokens_user_idx");
        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("password_reset_tokens_token_hash_idx");
    }
}
