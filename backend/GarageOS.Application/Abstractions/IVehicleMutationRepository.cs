using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

/// <summary>
/// P2-WP2. The single authoritative write surface for Vehicle rows. Guarded by
/// VehicleMutationBoundaryTests. No hard-delete method (DECISIONS.md #12 Decision #4).
/// </summary>
public interface IVehicleMutationRepository
{
    Task<Vehicle?> FindByIdAsync(Guid vehicleId, CancellationToken ct = default);

    Task<Vehicle> InsertAsync(Vehicle vehicle, CancellationToken ct = default);

    Task UpdateAsync(
        Guid vehicleId, string plateNumber, string plateCountry, string make, string model,
        int? year, string? color, string? vin, string? engine, string? engineCode,
        string? transmission, string? drivetrain, string? fuelType, int? currentMileage,
        CancellationToken ct = default);

    /// <summary>THE only method permitted to set Vehicle.DeletedAt/DeletedBy.</summary>
    Task SoftDeleteAsync(Guid vehicleId, Guid deletedBy, CancellationToken ct = default);
}
