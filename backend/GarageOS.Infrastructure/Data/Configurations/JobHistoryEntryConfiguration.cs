using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class JobHistoryEntryConfiguration : IEntityTypeConfiguration<JobHistoryEntry>
{
    public void Configure(EntityTypeBuilder<JobHistoryEntry> builder)
    {
        builder.ToTable("job_history");
        builder.HasKey(jh => jh.Id);
        builder.Property(jh => jh.Id).ValueGeneratedNever();
        builder.Property(jh => jh.ActorName).IsRequired();
        builder.Property(jh => jh.ActorRole).IsRequired();
        builder.Property(jh => jh.EventType).IsRequired();
        builder.Property(jh => jh.Summary).IsRequired();
        builder.Property(jh => jh.Detail).HasColumnType("jsonb");

        builder.HasOne<Garage>().WithMany().HasForeignKey(jh => jh.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Job>().WithMany().HasForeignKey(jh => jh.JobId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(jh => jh.ActorId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(jh => new { jh.JobId, jh.CreatedAt }).HasDatabaseName("job_history_job_idx");
        builder.HasIndex(jh => new { jh.GarageId, jh.CreatedAt }).HasDatabaseName("job_history_garage_idx");
    }
}
