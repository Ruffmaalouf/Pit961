namespace GarageOS.Domain.Common;

/// <summary>Marker for every entity carrying a first-class garage_id column. Drives
/// AppDbContext's centralized global query filter and the tenant-isolation test
/// matrix (WP-3) — every implementer must appear in both.</summary>
public interface ITenantOwned
{
    Guid GarageId { get; }
}
