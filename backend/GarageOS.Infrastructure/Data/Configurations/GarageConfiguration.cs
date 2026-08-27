using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class GarageConfiguration : IEntityTypeConfiguration<Garage>
{
    public void Configure(EntityTypeBuilder<Garage> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Name).IsRequired();

        builder.HasOne<Account>().WithMany().HasForeignKey(g => g.AccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(g => g.DeletedBy).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(g => g.AccountId).HasDatabaseName("garages_account_idx");
        builder.HasIndex(g => g.AccountId)
            .HasDatabaseName("garages_account_active_idx")
            .HasFilter("deleted_at IS NULL");
    }
}
