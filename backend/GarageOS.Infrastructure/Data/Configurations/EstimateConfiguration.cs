using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    public void Configure(EntityTypeBuilder<Estimate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Subtotal).HasPrecision(12, 2);
        builder.Property(e => e.TaxAmount).HasPrecision(12, 2);
        builder.Property(e => e.DiscountAmount).HasPrecision(12, 2);
        builder.Property(e => e.Total).HasPrecision(12, 2);

        builder.ToTable(t => t.HasCheckConstraint("ck_estimates_status",
            "status IN ('draft','sent','pending_owner_approval','approved','partially_approved','rejected','superseded')"));

        builder.HasOne<Garage>().WithMany().HasForeignKey(e => e.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Job>().WithMany().HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Estimate>().WithMany().HasForeignKey(e => e.ParentEstimateId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => e.GarageId).HasDatabaseName("estimates_garage_idx");
        builder.HasIndex(e => e.JobId).HasDatabaseName("estimates_job_idx");
        builder.HasIndex(e => e.ParentEstimateId)
            .HasDatabaseName("estimates_parent_idx").HasFilter("parent_estimate_id IS NOT NULL");
    }
}
