using System.Text.RegularExpressions;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Domain.Entities;

namespace GarageOS.Application.Vehicles;

public sealed record CreateVehicleFields(
    Guid CustomerId, string PlateNumber, string PlateCountry, string Make, string Model,
    int? Year, string? Color, string? Vin, string? Engine, string? EngineCode,
    string? Transmission, string? Drivetrain, string? FuelType, int? CurrentMileage);

public sealed record UpdateVehicleFields(
    string PlateNumber, string PlateCountry, string Make, string Model,
    int? Year, string? Color, string? Vin, string? Engine, string? EngineCode,
    string? Transmission, string? Drivetrain, string? FuelType, int? CurrentMileage);

public enum VehicleMutationFailure { CustomerNotFound }

/// <summary>Vehicle is null iff Failure is set. DuplicateWarnings is always populated
/// (possibly empty) when Vehicle is non-null -- DECISIONS.md #12 Decision #5: this is
/// informational only, the write always succeeds regardless of duplicates found.</summary>
public sealed record VehicleMutationResult(
    Vehicle? Vehicle, IReadOnlyList<DuplicateVehicleMatch> DuplicateWarnings, VehicleMutationFailure? Failure)
{
    public static VehicleMutationResult Ok(Vehicle vehicle, IReadOnlyList<DuplicateVehicleMatch> warnings) =>
        new(vehicle, warnings, null);

    public static VehicleMutationResult CustomerNotFound() => new(null, [], VehicleMutationFailure.CustomerNotFound);
}

public sealed record VehicleSoftDeleteResult(bool HadOpenJobs);

/// <summary>
/// P2-WP2. Application-service mutation path for Vehicle. Never writes to Customer --
/// only reads it (via ICustomerQueryRepository) to confirm the parent exists before a
/// create/update that changes CustomerId would apply (Phase 2 scope: CustomerId is
/// create-time only, never changed by Update -- re-assigning a vehicle to a different
/// customer is out of scope, not specced by any ratified decision or the P2-WP9 design
/// audit).
/// </summary>
public sealed class VehicleManagementService(
    IVehicleMutationRepository vehicles,
    IVehicleQueryRepository vehiclesRead,
    ICustomerQueryRepository customersRead,
    ICurrentTenant currentTenant)
{
    private static readonly HashSet<string> SoftDeleteAllowedRoles = new(StringComparer.Ordinal) { "owner", "manager" };
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Normalization mechanism per technical-architect's P2-WP2 design §3: trim,
    /// collapse/remove internal whitespace, uppercase. The stored column value IS the
    /// canonical normalized form -- there is no separate raw/normalized pair of columns.
    /// Hyphen handling is deliberately left alone (not stripped) -- real-world plate
    /// formats vary and this is a documented, tunable business-analyst call, not an
    /// architectural one; the normalization mechanism itself is what's fixed.</summary>
    private static string NormalizePlateNumber(string raw) => WhitespaceRegex.Replace(raw.Trim(), "").ToUpperInvariant();

    private static string NormalizePlateCountry(string raw) => raw.Trim().ToUpperInvariant();

    public async Task<VehicleMutationResult> CreateAsync(CreateVehicleFields fields, CancellationToken ct = default)
    {
        var customer = await customersRead.FindByIdAsync(fields.CustomerId, ct);
        if (customer is null)
        {
            return VehicleMutationResult.CustomerNotFound();
        }

        var normalizedPlate = NormalizePlateNumber(fields.PlateNumber);
        var normalizedCountry = NormalizePlateCountry(fields.PlateCountry);

        // Pre-check query -- DECISIONS.md #12 Decision #5. Never throws, never blocks;
        // the write below always proceeds regardless of this result.
        var duplicates = await vehiclesRead.FindDuplicatePlateCandidatesAsync(
            normalizedPlate, normalizedCountry, excludeVehicleId: null, ct);

        var vehicle = new Vehicle
        {
            GarageId = currentTenant.GarageId,
            CustomerId = fields.CustomerId,
            PlateNumber = normalizedPlate,
            PlateCountry = normalizedCountry,
            Make = fields.Make,
            Model = fields.Model,
            Year = fields.Year,
            Color = fields.Color,
            Vin = fields.Vin,
            Engine = fields.Engine,
            EngineCode = fields.EngineCode,
            Transmission = fields.Transmission,
            Drivetrain = fields.Drivetrain,
            FuelType = fields.FuelType,
            CurrentMileage = fields.CurrentMileage,
        };

        var inserted = await vehicles.InsertAsync(vehicle, ct);
        return VehicleMutationResult.Ok(inserted, duplicates);
    }

    /// <summary>Returns null if the vehicle is not found/cross-tenant.</summary>
    public async Task<VehicleMutationResult?> UpdateAsync(Guid vehicleId, UpdateVehicleFields fields, CancellationToken ct = default)
    {
        var existing = await vehicles.FindByIdAsync(vehicleId, ct);
        if (existing is null)
        {
            return null;
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant);

        var normalizedPlate = NormalizePlateNumber(fields.PlateNumber);
        var normalizedCountry = NormalizePlateCountry(fields.PlateCountry);

        var duplicates = await vehiclesRead.FindDuplicatePlateCandidatesAsync(
            normalizedPlate, normalizedCountry, excludeVehicleId: vehicleId, ct);

        await vehicles.UpdateAsync(
            vehicleId, normalizedPlate, normalizedCountry, fields.Make, fields.Model,
            fields.Year, fields.Color, fields.Vin, fields.Engine, fields.EngineCode,
            fields.Transmission, fields.Drivetrain, fields.FuelType, fields.CurrentMileage, ct);

        var updated = await vehicles.FindByIdAsync(vehicleId, ct);
        return VehicleMutationResult.Ok(updated!, duplicates);
    }

    /// <summary>Returns null if not found/cross-tenant. Throws RolePermissionException if
    /// the current role isn't owner/manager.</summary>
    public async Task<VehicleSoftDeleteResult?> SoftDeleteAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var existing = await vehicles.FindByIdAsync(vehicleId, ct);
        if (existing is null)
        {
            return null;
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant);

        if (!SoftDeleteAllowedRoles.Contains(currentTenant.Role))
        {
            throw new RolePermissionException("Vehicle.SoftDelete");
        }

        var hadOpenJobs = await vehiclesRead.HasOpenJobsAsync(vehicleId, ct);
        await vehicles.SoftDeleteAsync(vehicleId, currentTenant.UserId, ct);
        return new VehicleSoftDeleteResult(hadOpenJobs);
    }

    public Task<Vehicle?> GetByIdAsync(Guid vehicleId, CancellationToken ct = default) =>
        vehiclesRead.FindByIdAsync(vehicleId, ct);

    public Task<IReadOnlyList<Vehicle>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        vehiclesRead.ListByCustomerAsync(customerId, ct);

    /// <summary>Live pre-submit duplicate check (API surface §4) -- the same
    /// normalization + query path CreateAsync/UpdateAsync use internally, exposed
    /// directly so the frontend can flag a likely duplicate while the user is still
    /// typing, before the form is submitted.</summary>
    public Task<IReadOnlyList<DuplicateVehicleMatch>> CheckDuplicatePlateAsync(
        string plateNumber, string plateCountry, Guid? excludeVehicleId, CancellationToken ct = default) =>
        vehiclesRead.FindDuplicatePlateCandidatesAsync(
            NormalizePlateNumber(plateNumber), NormalizePlateCountry(plateCountry), excludeVehicleId, ct);
}
