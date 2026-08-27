using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.FirstName).IsRequired();
        builder.Property(c => c.Phone).IsRequired();

        builder.HasOne<Garage>().WithMany().HasForeignKey(c => c.GarageId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(c => c.GarageId).HasDatabaseName("customers_garage_idx");
        builder.HasIndex(c => new { c.GarageId, c.Phone }).HasDatabaseName("customers_phone_idx");
    }
}
