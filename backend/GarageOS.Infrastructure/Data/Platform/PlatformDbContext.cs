using GarageOS.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Platform;

public sealed class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform");
        modelBuilder.ApplyConfiguration(new Configurations.PlatformAdminConfiguration());
    }
}
