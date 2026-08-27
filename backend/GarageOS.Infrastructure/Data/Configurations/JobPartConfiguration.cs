using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class JobPartConfiguration : IEntityTypeConfiguration<JobPart>
{
    public void Configure(EntityTypeBuilder<JobPart> builder)
    {
        builder.HasKey(jp => jp.Id);
        builder.Property(jp => jp.Id).ValueGeneratedNever();
        builder.Property(jp => jp.Name).IsRequired();
        builder.Property(jp => jp.SuppliedBy).IsRequired();
        builder.Property(jp => jp.Status).HasMaxLength(32).IsRequired();
        builder.Property(jp => jp.Quantity).HasPrecision(12, 3);
        builder.Property(jp => jp.UnitCost).HasPrecision(12, 2);
        builder.Property(jp => jp.UnitPrice).HasPrecision(12, 2);

        builder.ToTable(t => t.HasCheckConstraint("ck_job_parts_status",
            "status IN ('needed','searching','ordered','arrived','installed','returned'," +
            "'issue_wrong_part','issue_damaged')"));

        // supplier_id intentionally has no FK — see WP-3 brief §2 (V1.1 suppliers table gap).
        builder.HasOne<Garage>().WithMany().HasForeignKey(jp => jp.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Job>().WithMany().HasForeignKey(jp => jp.JobId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(jp => jp.GarageId).HasDatabaseName("job_parts_garage_idx");
        builder.HasIndex(jp => jp.JobId).HasDatabaseName("job_parts_job_idx");
    }
}
