using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Amount).HasPrecision(12, 2);
        builder.Property(p => p.Method).HasMaxLength(32).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("ck_payments_method",
            "method IN ('cash','card','bank_transfer','cheque','other')"));

        builder.HasOne<Garage>().WithMany().HasForeignKey(p => p.GarageId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.RecordedBy).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => new { p.GarageId, p.IdempotencyKey }).IsUnique().HasDatabaseName("payments_idempotency_idx");
        builder.HasIndex(p => p.InvoiceId).HasDatabaseName("payments_invoice_idx");
    }
}
