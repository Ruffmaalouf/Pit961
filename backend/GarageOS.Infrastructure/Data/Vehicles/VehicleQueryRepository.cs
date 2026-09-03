using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Vehicles;

/// <summary>P2-WP2 read-only Vehicle repository. No single-caller restriction.</summary>
public sealed class VehicleQueryRepository(AppDbContext db) : IVehicleQueryRepository
{
    public Task<Vehicle?> FindByIdAsync(Guid vehicleId, CancellationToken ct = default) =>
        db.Vehicles.AsNoTracking().SingleOrDefaultAsync(v => v.Id == vehicleId, ct);

    public Task<Vehicle?> FindByIdIncludingDeletedAsync(
        Guid vehicleId, Guid garageId, CancellationToken ct = default) =>
        db.Vehicles.AsNoTracking().IgnoreQueryFilters()
            .Where(v => v.Id == vehicleId && v.GarageId == garageId) // tenant check re-applied by hand
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Vehicle>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await db.Vehicles.AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DuplicateVehicleMatch>> FindDuplicatePlateCandidatesAsync(
        string normalizedPlateNumber, string normalizedPlateCountry, Guid? excludeVehicleId,
        CancellationToken ct = default)
    {
        // Tenant-filtered and soft-delete-excluded automatically (Vehicle implements
        // ITenantOwned + ISoftDeletable) -- DECISIONS.md #12 Decision #5 requires this
        // check never cross tenants and never fire on a soft-deleted match.
        var query = db.Vehicles.AsNoTracking()
            .Where(v => v.PlateNumber == normalizedPlateNumber && v.PlateCountry == normalizedPlateCountry);

        if (excludeVehicleId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVehicleId.Value);
        }

        var matches = await query.ToListAsync(ct);
        if (matches.Count == 0)
        {
            return [];
        }

        var customerIds = matches.Select(v => v.CustomerId).Distinct().ToList();
        var customerNames = await db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => (c.FirstName + " " + c.LastName).Trim(), ct);

        return matches.Select(v => new DuplicateVehicleMatch(
            v.Id, v.CustomerId, customerNames.GetValueOrDefault(v.CustomerId, "Unknown"),
            v.PlateNumber, v.PlateCountry, v.Make, v.Model)).ToList();
    }

    // Kept in sync with CustomerQueryRepository.ClosedJobStatuses -- both derive from the
    // same DECISIONS.md #12 Decision #1 state machine.
    private static readonly string[] ClosedJobStatuses = ["closed", "cancelled", "deleted"];

    public Task<bool> HasOpenJobsAsync(Guid vehicleId, CancellationToken ct = default) =>
        db.Jobs.AsNoTracking()
            .AnyAsync(j => j.VehicleId == vehicleId && !ClosedJobStatuses.Contains(j.Status), ct);
}
