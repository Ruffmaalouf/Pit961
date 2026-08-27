using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class GarageSequenceConfiguration : IEntityTypeConfiguration<GarageSequence>
{
    public void Configure(EntityTypeBuilder<GarageSequence> builder)
    {
        builder.HasKey(gs => gs.GarageId);
        builder.Property(gs => gs.GarageId).ValueGeneratedNever();

        builder.HasOne<Garage>().WithOne().HasForeignKey<GarageSequence>(gs => gs.GarageId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
