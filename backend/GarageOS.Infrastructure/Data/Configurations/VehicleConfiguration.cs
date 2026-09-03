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

        // P2-WP2 / DECISIONS.md #12 Decision #4. See CustomerConfiguration remarks --
        // no HasQueryFilter call here either; the centralized mechanism picks this up
        // automatically because Vehicle implements ISoftDeletable.
        builder.Property(v => v.DeletedAt);
        builder.Property(v => v.DeletedBy);

        builder.HasOne<Garage>().WithMany().HasForeignKey(v => v.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>().WithMany().HasForeignKey(v => v.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(v => v.DeletedBy).OnDelete(DeleteBehavior.SetNull);

        // P2-WP2 / DECISIONS.md #12 Decision #5: duplicate plate is a WARNING surfaced by
        // VehicleManagementService's pre-check query, never a DB-level block. This index
        // MUST stay non-unique -- it exists purely for the duplicate-check query's
        // performance. If a future change adds .IsUnique() back here, that is a Decision
        // #5 regression and must fail review on sight. (Original schema had this as a
        // UNIQUE index -- P2-WP2 corrects that per the ratified decision.)
        builder.HasIndex(v => new { v.GarageId, v.PlateNumber, v.PlateCountry })
            .IsUnique(false).HasDatabaseName("vehicles_plate_idx");
        builder.HasIndex(v => v.CustomerId).HasDatabaseName("vehicles_customer_idx");
        builder.HasIndex(v => new { v.GarageId, v.Vin }).HasDatabaseName("vehicles_vin_idx");
        builder.HasIndex(v => new { v.GarageId, v.DeletedAt }).HasDatabaseName("vehicles_garage_deleted_idx");
    }
}
