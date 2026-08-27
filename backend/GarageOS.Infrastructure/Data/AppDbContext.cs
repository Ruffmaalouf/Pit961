using System.Reflection;
using GarageOS.Application.Abstractions;
using GarageOS.Domain.Common;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Garage> Garages => Set<Garage>();
    public DbSet<GarageSettings> GarageSettings => Set<GarageSettings>();
    public DbSet<GarageSequence> GarageSequences => Set<GarageSequence>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<RepairTask> RepairTasks => Set<RepairTask>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<EstimateItem> EstimateItems => Set<EstimateItem>();
    public DbSet<JobPart> JobParts => Set<JobPart>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<JobHistoryEntry> JobHistory => Set<JobHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ApplyConfigurationsFromAssembly scans the WHOLE GarageOS.Infrastructure assembly
        // for IEntityTypeConfiguration<T> implementations, regardless of folder/namespace —
        // it would otherwise also pick up PlatformAdminConfiguration (same assembly, just a
        // different folder) and silently add PlatformAdmin to this model. That is exactly
        // what WP-3 brief §6/§15 forbid: platform_admins must be reachable ONLY through
        // PlatformDbContext. The predicate below excludes anything under the
        // GarageOS.Infrastructure.Data.Platform namespace from AppDbContext's model.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly,
            t => t.Namespace is not null && !t.Namespace.StartsWith("GarageOS.Infrastructure.Data.Platform", StringComparison.Ordinal));
        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(ITenantOwned).IsAssignableFrom(clrType))
            {
                continue;
            }

            var methodName = typeof(ISoftDeletable).IsAssignableFrom(clrType)
                ? nameof(SetTenantFilterWithSoftDelete)
                : nameof(SetTenantFilter);

            typeof(AppDbContext)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(clrType)
                .Invoke(this, new object[] { modelBuilder });
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned
        => modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.GarageId == _currentTenant.GarageId);

    private void SetTenantFilterWithSoftDelete<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned, ISoftDeletable
        => modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            e.GarageId == _currentTenant.GarageId && e.DeletedAt == null);
}
