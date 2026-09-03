using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Vehicles;
using GarageOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>P2-WP2. See CustomersController's remarks -- same thin-controller shape.</summary>
[ApiController]
[Route("api/v1/vehicles")]
[Authorize(Policy = "GarageTenant")]
public sealed class VehiclesController(VehicleManagementService vehicleService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> GetById(Guid id, CancellationToken ct)
    {
        var vehicle = await vehicleService.GetByIdAsync(id, ct);
        return vehicle is null ? NotFound() : Ok(ToDto(vehicle));
    }

    [HttpPost]
    public async Task<ActionResult<VehicleMutationResponse>> Create([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        var result = await vehicleService.CreateAsync(
            new CreateVehicleFields(
                request.CustomerId, request.PlateNumber, request.PlateCountry, request.Make, request.Model,
                request.Year, request.Color, request.Vin, request.Engine, request.EngineCode,
                request.Transmission, request.Drivetrain, request.FuelType, request.CurrentMileage), ct);

        if (result.Failure == VehicleMutationFailure.CustomerNotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid customer.",
                detail: "customerId does not reference an existing customer in this garage.");
        }

        var response = new VehicleMutationResponse(ToDto(result.Vehicle!), ToWarningDto(result.DuplicateWarnings));
        return CreatedAtAction(nameof(GetById), new { id = result.Vehicle!.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleMutationResponse>> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
    {
        var result = await vehicleService.UpdateAsync(
            id, new UpdateVehicleFields(
                request.PlateNumber, request.PlateCountry, request.Make, request.Model,
                request.Year, request.Color, request.Vin, request.Engine, request.EngineCode,
                request.Transmission, request.Drivetrain, request.FuelType, request.CurrentMileage), ct);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new VehicleMutationResponse(ToDto(result.Vehicle!), ToWarningDto(result.DuplicateWarnings)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<VehicleSoftDeleteResponse>> SoftDelete(Guid id, CancellationToken ct)
    {
        var result = await vehicleService.SoftDeleteAsync(id, ct);
        return result is null ? NotFound() : Ok(new VehicleSoftDeleteResponse(result.HadOpenJobs));
    }

    [HttpGet("check-duplicate-plate")]
    public async Task<ActionResult<DuplicatePlateCheckResponse>> CheckDuplicatePlate(
        [FromQuery] string plateNumber, [FromQuery] string plateCountry,
        [FromQuery] Guid? excludeVehicleId, CancellationToken ct)
    {
        var matches = await vehicleService.CheckDuplicatePlateAsync(plateNumber, plateCountry, excludeVehicleId, ct);
        return Ok(new DuplicatePlateCheckResponse(ToWarningDto(matches)));
    }

    private static VehicleDto ToDto(Vehicle v) => new(
        v.Id, v.CustomerId, v.PlateNumber, v.PlateCountry, v.Make, v.Model, v.Year, v.Color,
        v.Vin, v.Engine, v.EngineCode, v.Transmission, v.Drivetrain, v.FuelType, v.CurrentMileage,
        v.CreatedAt, v.UpdatedAt);

    private static DuplicateWarningDto ToWarningDto(IReadOnlyList<DuplicateVehicleMatch> matches) => new(
        matches.Count > 0,
        matches.Select(m => new DuplicateVehicleMatchDto(
            m.VehicleId, m.CustomerId, m.CustomerName, m.PlateNumber, m.PlateCountry, m.Make, m.Model)).ToList());
}
