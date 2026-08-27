using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Name).IsRequired();
        builder.Property(a => a.BillingEmail).IsRequired();
        builder.Property(a => a.SubscriptionStatus).HasMaxLength(32).IsRequired();
        builder.Property(a => a.Plan).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("ck_accounts_subscription_status",
            "subscription_status IN ('trial','active','past_due','suspended','cancelled','expired')"));
    }
}
