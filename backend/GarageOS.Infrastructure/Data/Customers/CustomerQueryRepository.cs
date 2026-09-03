using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Customers;

/// <summary>P2-WP2 read-only Customer repository. No single-caller restriction (reads
/// carry no bypass risk) -- see ICustomerQueryRepository remarks.</summary>
public sealed class CustomerQueryRepository(AppDbContext db) : ICustomerQueryRepository
{
    // Job statuses treated as "closed" for HasOpenJobsAsync -- per DECISIONS.md #12
    // Decision #1's state machine (checked_in -> ... -> closed, with cancelled/deleted as
    // terminal exception paths). "invoiced" is deliberately treated as still-open here:
    // a job isn't fully wrapped up until closed, even once invoiced.
    private static readonly string[] ClosedJobStatuses = ["closed", "cancelled", "deleted"];

    public Task<Customer?> FindByIdAsync(Guid customerId, CancellationToken ct = default) =>
        db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == customerId, ct);

    public Task<Customer?> FindByIdIncludingDeletedAsync(
        Guid customerId, Guid garageId, CancellationToken ct = default) =>
        db.Customers.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.Id == customerId && c.GarageId == garageId) // tenant check re-applied by hand -- see interface remarks
            .SingleOrDefaultAsync(ct);

    public async Task<CustomerSearchResult> SearchAsync(
        string? search, bool? isFleet, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.FirstName, $"%{term}%")
                || (c.LastName != null && EF.Functions.ILike(c.LastName, $"%{term}%"))
                || EF.Functions.ILike(c.Phone, $"%{term}%")
                || (c.Email != null && EF.Functions.ILike(c.Email, $"%{term}%")));
        }

        if (isFleet.HasValue)
        {
            query = query.Where(c => c.IsFleet == isFleet.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var pageOfCustomers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var customerIds = pageOfCustomers.Select(c => c.Id).ToList();
        var vehicleCounts = await db.Vehicles.AsNoTracking()
            .Where(v => customerIds.Contains(v.CustomerId))
            .GroupBy(v => v.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Count, ct);

        var items = pageOfCustomers
            .Select(c => new CustomerSearchItem(c, vehicleCounts.GetValueOrDefault(c.Id, 0)))
            .ToList();

        return new CustomerSearchResult(items, totalCount);
    }

    public async Task<CustomerJobsHistoryResult> GetJobsHistoryAsync(
        Guid customerId, int take, CancellationToken ct = default)
    {
        // GarageId resolved once up front so the IgnoreQueryFilters() vehicle lookup
        // below (needed because a job's vehicle may since have been soft-deleted) can
        // re-apply an explicit, simple GarageId equality check rather than a per-row
        // correlated subquery.
        var garageId = await db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId).Select(c => c.GarageId).SingleAsync(ct);

        var jobsQuery = db.Jobs.AsNoTracking()
            .Where(j => j.CustomerId == customerId)
            .OrderByDescending(j => j.CreatedAt);

        var totalJobCount = await jobsQuery.CountAsync(ct);

        var page = await jobsQuery.Take(take)
            .Select(j => new
            {
                j.Id,
                j.JobNumber,
                j.Status,
                j.VehicleId,
                j.CreatedAt,
                j.CancelledAt,
            })
            .ToListAsync(ct);

        var vehicleIds = page.Select(j => j.VehicleId).Distinct().ToList();
        var plates = await db.Vehicles.AsNoTracking().IgnoreQueryFilters()
            .Where(v => vehicleIds.Contains(v.Id) && v.GarageId == garageId) // tenant check re-applied by hand
            .ToDictionaryAsync(v => v.Id, v => v.PlateNumber, ct);

        var jobIds = page.Select(j => j.Id).ToList();
        var invoiceTotals = await db.Invoices.AsNoTracking()
            .Where(inv => jobIds.Contains(inv.JobId))
            .GroupBy(inv => inv.JobId)
            .Select(g => new { JobId = g.Key, Total = g.Sum(inv => inv.Total) })
            .ToDictionaryAsync(x => x.JobId, x => x.Total, ct);

        var recentJobs = page.Select(j => new CustomerJobHistoryItem(
            j.Id, j.JobNumber, plates.GetValueOrDefault(j.VehicleId),
            j.Status, j.CreatedAt, j.CancelledAt, invoiceTotals.GetValueOrDefault(j.Id)))
            .ToList();

        return new CustomerJobsHistoryResult(recentJobs, totalJobCount, totalJobCount > take);
    }

    public async Task<CustomerBalanceSummary> GetBalanceSummaryAsync(
        Guid customerId, CancellationToken ct = default)
    {
        var jobIds = db.Jobs.AsNoTracking().Where(j => j.CustomerId == customerId).Select(j => j.Id);

        var totals = await db.Invoices.AsNoTracking()
            .Where(inv => jobIds.Contains(inv.JobId))
            .GroupBy(_ => 1)
            .Select(g => new { TotalInvoiced = g.Sum(i => i.Total), TotalPaid = g.Sum(i => i.TotalPaid) })
            .SingleOrDefaultAsync(ct);

        var totalInvoiced = totals?.TotalInvoiced ?? 0m;
        var totalPaid = totals?.TotalPaid ?? 0m;

        // Single-currency-per-garage (GarageSettings.Currency) -- there is no
        // per-transaction currency field on Invoice/Payment in this system.
        var garageId = await db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId).Select(c => c.GarageId).SingleAsync(ct);
        var currency = await db.GarageSettings.AsNoTracking()
            .Where(s => s.GarageId == garageId).Select(s => s.Currency).SingleAsync(ct);

        return new CustomerBalanceSummary(totalInvoiced, totalPaid, totalInvoiced - totalPaid, currency);
    }

    public Task<bool> HasOpenJobsAsync(Guid customerId, CancellationToken ct = default) =>
        db.Jobs.AsNoTracking()
            .AnyAsync(j => j.CustomerId == customerId && !ClosedJobStatuses.Contains(j.Status), ct);
}
