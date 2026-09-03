namespace GarageOS.Api.Contracts;

public sealed record CreateVehicleRequest(
    Guid CustomerId, string PlateNumber, string PlateCountry, string Make, string Model,
    int? Year, string? Color, string? Vin, string? Engine, string? EngineCode,
    string? Transmission, string? Drivetrain, string? FuelType, int? CurrentMileage);

public sealed record UpdateVehicleRequest(
    string PlateNumber, string PlateCountry, string Make, string Model,
    int? Year, string? Color, string? Vin, string? Engine, string? EngineCode,
    string? Transmission, string? Drivetrain, string? FuelType, int? CurrentMileage);

public sealed record VehicleDto(
    Guid Id, Guid CustomerId, string PlateNumber, string PlateCountry, string Make, string Model,
    int? Year, string? Color, string? Vin, string? Engine, string? EngineCode,
    string? Transmission, string? Drivetrain, string? FuelType, int? CurrentMileage,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record DuplicateVehicleMatchDto(
    Guid VehicleId, Guid CustomerId, string CustomerName,
    string PlateNumber, string PlateCountry, string Make, string Model);

public sealed record DuplicateWarningDto(bool HasDuplicates, IReadOnlyList<DuplicateVehicleMatchDto> Matches);

// HTTP status is always 201 (Create) / 200 (Update) regardless of HasDuplicates -- this
// is never a 409. DuplicateWarningDto with HasDuplicates: false is the normal case.
public sealed record VehicleMutationResponse(VehicleDto Vehicle, DuplicateWarningDto DuplicateWarning);

public sealed record VehicleSoftDeleteResponse(bool HadOpenJobs);

public sealed record DuplicatePlateCheckResponse(DuplicateWarningDto DuplicateWarning);
