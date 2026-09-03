using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

public sealed record DuplicateVehicleMatch(
    Guid VehicleId, Guid CustomerId, string CustomerName,
    string PlateNumber, string PlateCountry, string Make, string Model);

/// <summary>P2-WP2 read-only repository for Vehicle. No single-caller constraint (see
/// ICustomerQueryRepository remarks -- reads carry no bypass risk).</summary>
public interface IVehicleQueryRepository
{
    Task<Vehicle?> FindByIdAsync(Guid vehicleId, CancellationToken ct = default);

    Task<Vehicle?> FindByIdIncludingDeletedAsync(Guid vehicleId, Guid garageId, CancellationToken ct = default);

    Task<IReadOnlyList<Vehicle>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>DECISIONS.md #12 Decision #5 support: finds active (non-soft-deleted),
    /// same-tenant vehicles whose normalized (PlateNumber, PlateCountry) match. Never
    /// throws, never blocks a write -- purely informational. `excludeVehicleId` lets an
    /// Update exclude the vehicle being updated from its own duplicate check.
    /// `normalizedPlateNumber`/`normalizedPlateCountry` must already be normalized by the
    /// caller (VehicleManagementService) before this is called -- this repository does not
    /// re-normalize.</summary>
    Task<IReadOnlyList<DuplicateVehicleMatch>> FindDuplicatePlateCandidatesAsync(
        string normalizedPlateNumber, string normalizedPlateCountry, Guid? excludeVehicleId,
        CancellationToken ct = default);

    /// <summary>Same ClosedJobStatuses semantics as ICustomerQueryRepository.HasOpenJobsAsync
    /// (DECISIONS.md #12 Decision #1's state machine) -- used to warn (never block) on
    /// soft-delete of a vehicle with open jobs.</summary>
    Task<bool> HasOpenJobsAsync(Guid vehicleId, CancellationToken ct = default);
}
