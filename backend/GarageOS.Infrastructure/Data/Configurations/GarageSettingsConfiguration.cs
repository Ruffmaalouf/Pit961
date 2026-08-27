using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class GarageSettingsConfiguration : IEntityTypeConfiguration<GarageSettings>
{
    public void Configure(EntityTypeBuilder<GarageSettings> builder)
    {
        builder.HasKey(gs => gs.GarageId);
        builder.Property(gs => gs.GarageId).ValueGeneratedNever();

        builder.Property(gs => gs.TaxRate).HasPrecision(5, 2);
        builder.Property(gs => gs.DefaultLaborRate).HasPrecision(12, 2);
        builder.Property(gs => gs.DiagnosisFeeAmount).HasPrecision(12, 2);
        builder.Property(gs => gs.DiscountLimitPercent).HasPrecision(5, 2);
        builder.Property(gs => gs.EstimateApprovalThreshold).HasPrecision(12, 2);
        builder.Property(gs => gs.ExtraSettings).HasColumnType("jsonb");

        builder.HasOne<Garage>().WithOne().HasForeignKey<GarageSettings>(gs => gs.GarageId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
