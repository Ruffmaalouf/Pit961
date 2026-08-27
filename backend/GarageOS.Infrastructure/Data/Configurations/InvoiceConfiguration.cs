using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.InvoiceNumber).IsRequired();
        builder.Property(i => i.Status).HasMaxLength(32).IsRequired();
        builder.Property(i => i.Subtotal).HasPrecision(12, 2);
        builder.Property(i => i.Total).HasPrecision(12, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(12, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(12, 2);
        builder.Property(i => i.TotalPaid).HasPrecision(12, 2);
        builder.Property(i => i.DisplayRateSnapshot).HasPrecision(12, 4);

        builder.ToTable(t => t.HasCheckConstraint("ck_invoices_status",
            "status IN ('unpaid','partial','paid','voided','written_off')"));

        builder.HasOne<Garage>().WithMany().HasForeignKey(i => i.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Job>().WithMany().HasForeignKey(i => i.JobId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.VoidedBy).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.CreatedBy).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(i => new { i.GarageId, i.InvoiceNumber }).IsUnique().HasDatabaseName("invoices_number_idx");
        builder.HasIndex(i => i.JobId).HasDatabaseName("invoices_job_idx");
    }
}
