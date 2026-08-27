using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Email).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Name).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(32).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("ck_users_role",
            "role IN ('owner','manager','advisor','mechanic','accountant')"));

        builder.HasOne<Garage>().WithMany().HasForeignKey(u => u.GarageId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("users_email_idx");
        builder.HasIndex(u => u.GarageId).HasDatabaseName("users_garage_idx");
    }
}
