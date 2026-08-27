using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).ValueGeneratedNever();
        builder.Property(rt => rt.TokenHash).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<RefreshToken>().WithMany().HasForeignKey(rt => rt.ReplacedByTokenId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(rt => rt.UserId).HasDatabaseName("refresh_tokens_user_idx");
        builder.HasIndex(rt => rt.TokenHash).IsUnique().HasDatabaseName("refresh_tokens_token_hash_idx");
    }
}
