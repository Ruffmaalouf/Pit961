using GarageOS.Api.Contracts;
using GarageOS.Application.Customers;
using GarageOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>
/// P2-WP2. Thin controller -- all business logic lives in CustomerManagementService
/// (Application layer); this class only maps HTTP &lt;-&gt; service calls and shapes
/// responses as Contracts DTOs. Every action requires the "GarageTenant" policy (see
/// AuthController's [Authorize(Policy = "GarageTenant")] precedent) -- there is no
/// platform-admin-facing Customer/Vehicle surface in this WP.
/// </summary>
[ApiController]
[Route("api/v1/customers")]
[Authorize(Policy = "GarageTenant")]
public sealed class CustomersController(CustomerManagementService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CustomerListResponse>> Search(
        [FromQuery] string? search, [FromQuery] bool? isFleet,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await customerService.SearchAsync(search, isFleet, page, pageSize, ct);
        var items = result.Items.Select(i => ToListItemDto(i.Customer, i.VehicleCount)).ToList();
        return Ok(new CustomerListResponse(items, result.TotalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailResponse>> GetDetail(Guid id, CancellationToken ct)
    {
        var detail = await customerService.GetDetailAsync(id, ct);
        if (detail is null)
        {
            return NotFound();
        }

        var vehicles = detail.CustomerVehicles.Select(ToVehicleSummaryDto).ToList();
        var jobsHistory = new CustomerJobsHistorySummaryDto(
            detail.JobsHistory.RecentJobs.Select(j => new CustomerJobHistoryItemDto(
                j.JobId, j.JobNumber, j.VehiclePlate, j.Status, j.OpenedAt, j.ClosedAt, j.InvoiceTotal)).ToList(),
            detail.JobsHistory.TotalJobCount, detail.JobsHistory.MoreAvailable);
        var balance = new CustomerBalanceSummaryDto(
            detail.BalanceSummary.TotalInvoiced, detail.BalanceSummary.TotalPaid,
            detail.BalanceSummary.OutstandingBalance, detail.BalanceSummary.Currency);

        return Ok(new CustomerDetailResponse(ToDto(detail.Customer), vehicles, jobsHistory, balance));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var created = await customerService.CreateAsync(
            new CreateCustomerFields(
                request.FirstName, request.LastName, request.Phone, request.Whatsapp,
                request.Email, request.Notes, request.IsFleet), ct);

        return CreatedAtAction(nameof(GetDetail), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var updated = await customerService.UpdateAsync(
            id, new UpdateCustomerFields(
                request.FirstName, request.LastName, request.Phone, request.Whatsapp,
                request.Email, request.Notes, request.IsFleet), ct);

        return updated is null ? NotFound() : Ok(ToDto(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<CustomerSoftDeleteResponse>> SoftDelete(Guid id, CancellationToken ct)
    {
        var result = await customerService.SoftDeleteAsync(id, ct);
        return result is null ? NotFound() : Ok(new CustomerSoftDeleteResponse(result.HadOpenJobs));
    }

    [HttpGet("{customerId:guid}/vehicles")]
    public async Task<ActionResult<IReadOnlyList<VehicleSummaryDto>>> ListVehicles(Guid customerId, CancellationToken ct)
    {
        var vehicles = await customerService.ListVehiclesForCustomerAsync(customerId, ct);
        return vehicles is null ? NotFound() : Ok(vehicles.Select(ToVehicleSummaryDto).ToList());
    }

    private static CustomerDto ToDto(Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.Phone, c.Whatsapp, c.Email, c.Notes, c.IsFleet, c.CreatedAt, c.UpdatedAt);

    private static CustomerListItemDto ToListItemDto(Customer c, int vehicleCount) => new(
        c.Id, c.FirstName, c.LastName, c.Phone, c.Email, c.IsFleet, vehicleCount, c.CreatedAt);

    private static VehicleSummaryDto ToVehicleSummaryDto(Vehicle v) => new(
        v.Id, v.PlateNumber, v.PlateCountry, v.Make, v.Model, v.Year, v.CurrentMileage);
}
