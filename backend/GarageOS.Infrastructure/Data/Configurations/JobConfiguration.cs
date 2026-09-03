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

        // P2-WP3: constraint swap to DECISIONS.md #12 Decision #1's ratified vocabulary --
        // the original constraint was written against an earlier, informal status set and
        // was never updated when Decision #1 was ratified. See MigrateJobStatusVocabulary
        // for the corresponding migration (drop+add, no data backfill needed -- no
        // live/seeded row uses any value outside the intersection of the old and new
        // vocabularies).
        builder.ToTable(t => t.HasCheckConstraint("ck_jobs_status",
            "status IN ('checked_in','estimate_pending','awaiting_approval','approved'," +
            "'in_progress','completed','invoiced','closed','cancelled','deleted')"));

        // P2-WP3: Postgres's built-in xmin system column as an EF Core concurrency token --
        // the Floor Board is the first genuinely concurrent-multi-actor-on-one-row UX in
        // this codebase (e.g. a mechanic and a manager transitioning the same Job from
        // different devices at the same instant). `dotnet ef migrations add` still
        // scaffolds a spurious AddColumn<uint>("xmin", ...) for this shadow property
        // (Postgres system columns aren't special-cased by the migrations differ) -- that
        // operation is hand-removed from MigrateJobStatusVocabulary's Up/Down (see that
        // file's remarks): "xmin" already exists on every table and Postgres rejects
        // adding a column by that name outright.
        builder.Property<uint>("xmin").IsRowVersion();

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
