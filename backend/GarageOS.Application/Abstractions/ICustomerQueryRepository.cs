using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

public sealed record CustomerSearchResult(IReadOnlyList<CustomerSearchItem> Items, int TotalCount);

public sealed record CustomerSearchItem(Customer Customer, int VehicleCount);

public sealed record CustomerJobHistoryItem(
    Guid JobId, string JobNumber, string? VehiclePlate, string Status,
    DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, decimal? InvoiceTotal);

public sealed record CustomerJobsHistoryResult(
    IReadOnlyList<CustomerJobHistoryItem> RecentJobs, int TotalJobCount, bool MoreAvailable);

public sealed record CustomerBalanceSummary(
    decimal TotalInvoiced, decimal TotalPaid, decimal OutstandingBalance, string Currency);

/// <summary>
/// P2-WP2 read-only repository. Unlike ICustomerMutationRepository, this has no
/// single-caller constraint -- reads carry no tenant-guard-bypass risk, so any
/// application service needing a Customer read projection may call it (including a
/// future Jobs-slice service composing a VehicleSummaryDto for a job detail screen).
/// </summary>
public interface ICustomerQueryRepository
{
    /// <summary>Tenant-filtered, soft-delete-excluded, AsNoTracking lookup for normal
    /// (non-historical) reads.</summary>
    Task<Customer?> FindByIdAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Bypasses the global query filter (both tenant AND soft-delete halves --
    /// see AppDbContext.ApplyTenantQueryFilters, they are ANDed into one composed filter,
    /// not two separate ones) and re-applies an explicit GarageId check by hand. For
    /// historical Job/Estimate/Invoice/Payment composition reads ONLY -- never build a
    /// general-purpose helper on top of this that omits the re-applied GarageId check,
    /// or it becomes a cross-tenant data leak. See CustomerSoftDeleteTests for the
    /// regression test proving a Garage B caller cannot use this path to read Garage A's
    /// soft-deleted customer.</summary>
    Task<Customer?> FindByIdIncludingDeletedAsync(Guid customerId, Guid garageId, CancellationToken ct = default);

    Task<CustomerSearchResult> SearchAsync(
        string? search, bool? isFleet, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Recent jobs for this customer, newest first, capped at `take`. Reads the
    /// Jobs/Invoices tables directly rather than going through a not-yet-built
    /// Jobs-slice/Invoices-slice read interface (P2-WP3/WP4/WP6 haven't landed their own
    /// HTTP layers yet) -- documented simplification, flagged for reconciliation with
    /// whichever read interface those work packages eventually own.</summary>
    Task<CustomerJobsHistoryResult> GetJobsHistoryAsync(Guid customerId, int take, CancellationToken ct = default);

    /// <summary>Sums Invoice.Total/TotalPaid across every Job belonging to this customer.
    /// Currency is read from GarageSettings.Currency (this system is single-currency per
    /// garage; there is no per-transaction currency field on Invoice/Payment).</summary>
    Task<CustomerBalanceSummary> GetBalanceSummaryAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>True if this customer has any Job whose Status is not a terminal state
    /// (closed/cancelled/invoiced/deleted) -- used to warn (never block) on soft-delete of
    /// a customer with open jobs, by direct analogy to Decision #5's warn-don't-block
    /// precedent for duplicate plates.</summary>
    Task<bool> HasOpenJobsAsync(Guid customerId, CancellationToken ct = default);
}
