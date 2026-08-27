using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class Vehicle : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public Guid CustomerId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string PlateCountry { get; set; } = "LB";
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Color { get; set; }
    public string? Vin { get; set; }
    public string? Engine { get; set; }
    public string? EngineCode { get; set; }
    public string? Transmission { get; set; }
    public string? Drivetrain { get; set; }
    public string? FuelType { get; set; }
    public int? CurrentMileage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
