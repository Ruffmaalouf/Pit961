using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Vehicles;

/// <summary>
/// P2-WP2. The single Infrastructure class permitted to mutate Vehicle rows -- enforced
/// by VehicleMutationBoundaryTests. Same AsNoTracking-on-read / fresh-tracked-re-fetch
/// pattern as CustomerMutationRepository/EstimateMutationRepository.
/// </summary>
public sealed class VehicleMutationRepository(AppDbContext db) : IVehicleMutationRepository
{
    public Task<Vehicle?> FindByIdAsync(Guid vehicleId, CancellationToken ct = default) =>
        db.Vehicles.AsNoTracking().SingleOrDefaultAsync(v => v.Id == vehicleId, ct);

    public async Task<Vehicle> InsertAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(ct);
        return vehicle;
    }

    public async Task UpdateAsync(
        Guid vehicleId, string plateNumber, string plateCountry, string make, string model,
        int? year, string? color, string? vin, string? engine, string? engineCode,
        string? transmission, string? drivetrain, string? fuelType, int? currentMileage,
        CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles.SingleAsync(v => v.Id == vehicleId, ct);
        vehicle.PlateNumber = plateNumber;
        vehicle.PlateCountry = plateCountry;
        vehicle.Make = make;
        vehicle.Model = model;
        vehicle.Year = year;
        vehicle.Color = color;
        vehicle.Vin = vin;
        vehicle.Engine = engine;
        vehicle.EngineCode = engineCode;
        vehicle.Transmission = transmission;
        vehicle.Drivetrain = drivetrain;
        vehicle.FuelType = fuelType;
        vehicle.CurrentMileage = currentMileage;
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(Guid vehicleId, Guid deletedBy, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles.SingleAsync(v => v.Id == vehicleId, ct);
        vehicle.DeletedAt = DateTimeOffset.UtcNow;
        vehicle.DeletedBy = deletedBy;
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
