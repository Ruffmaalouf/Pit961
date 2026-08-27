using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class RepairTaskConfiguration : IEntityTypeConfiguration<RepairTask>
{
    public void Configure(EntityTypeBuilder<RepairTask> builder)
    {
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).ValueGeneratedNever();
        builder.Property(rt => rt.Name).IsRequired();
        builder.Property(rt => rt.Status).HasMaxLength(32).IsRequired();
        builder.Property(rt => rt.OutsourceCost).HasPrecision(12, 2);
        builder.Property(rt => rt.OutsourceBilled).HasPrecision(12, 2);

        builder.ToTable(t => t.HasCheckConstraint("ck_repair_tasks_status",
            "status IN ('pending','in_progress','paused','completed','cancelled')"));

        builder.HasOne<Garage>().WithMany().HasForeignKey(rt => rt.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Job>().WithMany().HasForeignKey(rt => rt.JobId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(rt => rt.AssignedMechanicId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(rt => rt.GarageId).HasDatabaseName("repair_tasks_garage_idx");
        builder.HasIndex(rt => rt.JobId).HasDatabaseName("repair_tasks_job_idx");
        builder.HasIndex(rt => rt.AssignedMechanicId)
            .HasDatabaseName("repair_tasks_mechanic_idx").HasFilter("assigned_mechanic_id IS NOT NULL");
    }
}
