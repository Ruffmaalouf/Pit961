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

        // P2-WP4: Postgres's built-in xmin system column as an EF Core concurrency token,
        // same pattern/rationale as JobConfiguration.cs -- Estimate now has genuinely
        // concurrent-multi-actor writes (discount application, approval routing/clearing,
        // and revision creation racing each other on the same row). `dotnet ef migrations
        // add` still scaffolds a spurious AddColumn<uint>("xmin", ...) for this shadow
        // property; that operation must be hand-removed from the generated migration's
        // Up/Down, exactly as JobConfiguration.cs's remarks describe -- "xmin" already
        // exists on every table and Postgres rejects adding a column by that name.
        builder.Property<uint>("xmin").IsRowVersion();

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
