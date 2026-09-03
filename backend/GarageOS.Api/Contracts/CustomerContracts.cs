namespace GarageOS.Api.Contracts;

// P2-WP2. Requests never contain GarageId or Id -- Id is route-supplied on
// update/delete, Create generates server-side, GarageId is always currentTenant.GarageId.

public sealed record CreateCustomerRequest(
    string FirstName, string? LastName, string Phone, string? Whatsapp,
    string? Email, string? Notes, bool IsFleet);

public sealed record UpdateCustomerRequest(
    string FirstName, string? LastName, string Phone, string? Whatsapp,
    string? Email, string? Notes, bool IsFleet);

public sealed record CustomerDto(
    Guid Id, string FirstName, string? LastName, string Phone, string? Whatsapp,
    string? Email, string? Notes, bool IsFleet, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CustomerListItemDto(
    Guid Id, string FirstName, string? LastName, string Phone, string? Email,
    bool IsFleet, int VehicleCount, DateTimeOffset CreatedAt);

public sealed record CustomerListResponse(
    IReadOnlyList<CustomerListItemDto> Items, int TotalCount, int Page, int PageSize);

public sealed record VehicleSummaryDto(
    Guid Id, string PlateNumber, string PlateCountry, string Make, string Model,
    int? Year, int? CurrentMileage);

public sealed record CustomerJobHistoryItemDto(
    Guid JobId, string JobNumber, string? VehiclePlate, string Status,
    DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, decimal? InvoiceTotal);

public sealed record CustomerJobsHistorySummaryDto(
    IReadOnlyList<CustomerJobHistoryItemDto> RecentJobs, int TotalJobCount, bool MoreAvailable);

public sealed record CustomerBalanceSummaryDto(
    decimal TotalInvoiced, decimal TotalPaid, decimal OutstandingBalance, string Currency);

public sealed record CustomerDetailResponse(
    CustomerDto Customer, IReadOnlyList<VehicleSummaryDto> Vehicles,
    CustomerJobsHistorySummaryDto JobsHistory, CustomerBalanceSummaryDto BalanceSummary);

public sealed record CustomerSoftDeleteResponse(bool HadOpenJobs);
