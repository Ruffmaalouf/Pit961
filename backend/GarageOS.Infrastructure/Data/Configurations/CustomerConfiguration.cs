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

        // P2-WP2 / DECISIONS.md #12 Decision #4 (soft delete only). Do NOT add a
        // HasQueryFilter call here -- AppDbContext.ApplyTenantQueryFilters already
        // composes GarageId + DeletedAt into a single filter for every ITenantOwned +
        // ISoftDeletable entity by reflecting over the marker interfaces. A second
        // HasQueryFilter call on this entity would silently overwrite that one.
        builder.Property(c => c.DeletedAt);
        builder.Property(c => c.DeletedBy);

        builder.HasOne<Garage>().WithMany().HasForeignKey(c => c.GarageId).OnDelete(DeleteBehavior.NoAction);
        // DeletedBy -> Users, SetNull: a user being later removed must never break this
        // soft-delete audit trail. Never Cascade/Restrict here -- this FK is audit
        // metadata, not a structural dependency of the Customer row.
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.DeletedBy).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.GarageId).HasDatabaseName("customers_garage_idx");
        builder.HasIndex(c => new { c.GarageId, c.Phone }).HasDatabaseName("customers_phone_idx");
        // Supports the soft-delete-aware default list/search filter staying index-backed
        // now that every default query also filters on DeletedAt.
        builder.HasIndex(c => new { c.GarageId, c.DeletedAt }).HasDatabaseName("customers_garage_deleted_idx");
    }
}
