using GarageOS.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Platform.Configurations;

public sealed class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
{
    public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
    {
        builder.ToTable("platform_admins", "platform");
        builder.HasKey(pa => pa.Id);
        builder.Property(pa => pa.Id).ValueGeneratedNever();
        builder.Property(pa => pa.Email).IsRequired();
        builder.Property(pa => pa.PasswordHash).IsRequired();

        builder.HasIndex(pa => pa.Email).IsUnique().HasDatabaseName("platform_admins_email_idx");
    }
}
