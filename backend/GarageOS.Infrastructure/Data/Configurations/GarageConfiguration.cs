using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Data.Configurations;

public sealed class GarageConfiguration : IEntityTypeConfiguration<Garage>
{
    public void Configure(EntityTypeBuilder<Garage> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Name).IsRequired();

        builder.HasOne<Account>().WithMany().HasForeignKey(g => g.AccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(g => g.DeletedBy).OnDelete(DeleteBehavior.NoAction);

        // Two distinct indexes on the same column. Two required fixes, not one:
        // (1) the named HasIndex(expr, name) overload gives each call its own model-level "Name"
        //     so EF doesn't collapse them onto the same index builder (two unnamed HasIndex(expr)
        //     calls on the same property do collapse, silently dropping the first index).
        // (2) EFCore.NamingConventions recomputes the *database* name by convention unless a
        //     database name is explicitly pinned via .HasDatabaseName(...) — without it, both
        //     indexes' database names are convention-derived from the same property list and
        //     collide (EF then auto-disambiguates with a numeric suffix, silently discarding the
        //     intended garages_account_idx / garages_account_active_idx names from §9/§12).
        // This exact two-part bug shipped in the WP-3 review bundle: it produced only one index.
        builder.HasIndex(g => g.AccountId, "garages_account_idx")
            .HasDatabaseName("garages_account_idx");
        builder.HasIndex(g => g.AccountId, "garages_account_active_idx")
            .HasDatabaseName("garages_account_active_idx")
            .HasFilter("deleted_at IS NULL");
    }
}
