using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.JobNumber).IsRequired();
        builder.Property(j => j.Status).HasMaxLength(32).IsRequired();
        builder.Property(j => j.Source).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("ck_jobs_status",
            "status IN ('checked_in','diagnosing','waiting_approval','waiting_parts'," +
            "'ready_to_repair','repairing','qc','ready','delivered','cancelled')"));

        builder.HasOne<Garage>().WithMany().HasForeignKey(j => j.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>().WithMany().HasForeignKey(j => j.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(j => j.VehicleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(j => j.PrimaryMechanicId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(j => j.SecondaryMechanicId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(j => j.CreatedBy).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(j => j.CancelledBy).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(j => j.DeletedBy).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Job>().WithMany().HasForeignKey(j => j.ParentJobId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(j => new { j.GarageId, j.JobNumber }).IsUnique().HasDatabaseName("jobs_number_idx");
        builder.HasIndex(j => new { j.GarageId, j.Status })
            .HasDatabaseName("jobs_garage_status_idx").HasFilter("deleted_at IS NULL");
        builder.HasIndex(j => j.VehicleId).HasDatabaseName("jobs_vehicle_idx");
        builder.HasIndex(j => j.PrimaryMechanicId).HasDatabaseName("jobs_mechanic_idx");
        builder.HasIndex(j => j.CustomerId).HasDatabaseName("jobs_customer_idx");
        builder.HasIndex(j => j.SecondaryMechanicId)
            .HasDatabaseName("jobs_secondary_mechanic_idx").HasFilter("secondary_mechanic_id IS NOT NULL");
        builder.HasIndex(j => j.ParentJobId)
            .HasDatabaseName("jobs_parent_job_idx").HasFilter("parent_job_id IS NOT NULL");
    }
}
