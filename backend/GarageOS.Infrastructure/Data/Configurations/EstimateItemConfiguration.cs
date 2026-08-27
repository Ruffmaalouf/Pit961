using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class EstimateItemConfiguration : IEntityTypeConfiguration<EstimateItem>
{
    public void Configure(EntityTypeBuilder<EstimateItem> builder)
    {
        builder.HasKey(ei => ei.Id);
        builder.Property(ei => ei.Id).ValueGeneratedNever();
        builder.Property(ei => ei.Type).HasMaxLength(16).IsRequired();
        builder.Property(ei => ei.Description).IsRequired();
        builder.Property(ei => ei.ApprovalStatus).HasMaxLength(16).IsRequired();
        builder.Property(ei => ei.Quantity).HasPrecision(12, 3);
        builder.Property(ei => ei.UnitCost).HasPrecision(12, 2);
        builder.Property(ei => ei.UnitPrice).HasPrecision(12, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_estimate_items_type", "type IN ('part','labor','service','misc')");
            t.HasCheckConstraint("ck_estimate_items_approval_status", "approval_status IN ('pending','approved','rejected')");
        });

        builder.HasOne<Garage>().WithMany().HasForeignKey(ei => ei.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Estimate>().WithMany().HasForeignKey(ei => ei.EstimateId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(ei => ei.GarageId).HasDatabaseName("estimate_items_garage_idx");
        builder.HasIndex(ei => ei.EstimateId).HasDatabaseName("estimate_items_estimate_idx");
    }
}
