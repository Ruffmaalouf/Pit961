using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.PlateNumber).IsRequired();
        builder.Property(v => v.PlateCountry).IsRequired();
        builder.Property(v => v.Make).IsRequired();
        builder.Property(v => v.Model).IsRequired();

        builder.HasOne<Garage>().WithMany().HasForeignKey(v => v.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>().WithMany().HasForeignKey(v => v.CustomerId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(v => new { v.GarageId, v.PlateNumber, v.PlateCountry })
            .IsUnique().HasDatabaseName("vehicles_plate_idx");
        builder.HasIndex(v => v.CustomerId).HasDatabaseName("vehicles_customer_idx");
        builder.HasIndex(v => new { v.GarageId, v.Vin }).HasDatabaseName("vehicles_vin_idx");
    }
}
