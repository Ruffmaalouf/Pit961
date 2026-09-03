using GarageOS.Application.Abstractions;
using GarageOS.Domain.Common;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Jobs;

/// <summary>P2-WP3 read-only Job repository. No single-caller restriction (reads carry no
/// bypass risk) -- see IJobQueryRepository remarks.</summary>
public sealed class JobQueryRepository(AppDbContext db) : IJobQueryRepository
{
    public Task<Job?> FindByIdAsync(Guid jobId, CancellationToken ct = default) =>
        db.Jobs.AsNoTracking().SingleOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task<IReadOnlyList<JobHistoryEntry>> GetHistoryAsync(Guid jobId, CancellationToken ct = default) =>
        await db.JobHistory.AsNoTracking()
            .Where(h => h.JobId == jobId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);

    public async Task<FloorBoardResult> GetFloorBoardAsync(CancellationToken ct = default)
    {
        // Single query -- the default tenant+soft-delete filter is exactly what's wanted
        // here (never IgnoreQueryFilters(), the Floor Board never needs another garage's
        // jobs). Status travels alongside each card in this one projection (rather than a
        // second query) purely so the in-memory grouping below has it to key on --
        // FloorBoardCard itself doesn't carry Status, since it's a column-membership fact,
        // not a card field, per §7's read model.
        var openStatuses = JobStatuses.OpenBoardOrder;

        var rows = await db.Jobs.AsNoTracking()
            .Where(j => openStatuses.Contains(j.Status))
            .Join(db.Customers.AsNoTracking(), j => j.CustomerId, c => c.Id, (j, c) => new { j, c })
            .Join(db.Vehicles.AsNoTracking(), x => x.j.VehicleId, v => v.Id, (x, v) => new { x.j, x.c, v })
            .GroupJoin(db.Users.AsNoTracking(), x => x.j.PrimaryMechanicId, u => (Guid?)u.Id, (x, mechanics) => new { x.j, x.c, x.v, mechanics })
            .SelectMany(x => x.mechanics.DefaultIfEmpty(), (x, mechanic) => new
            {
                x.j.Status,
                Card = new FloorBoardCard(
                    x.j.Id,
                    x.j.JobNumber,
                    x.c.LastName == null ? x.c.FirstName : x.c.FirstName + " " + x.c.LastName,
                    x.v.Year == null
                        ? x.v.Make + " " + x.v.Model + " — " + x.v.PlateNumber
                        : x.v.Year + " " + x.v.Make + " " + x.v.Model + " — " + x.v.PlateNumber,
                    x.j.PrimaryMechanicId,
                    mechanic == null ? null : mechanic.Name,
                    x.j.CreatedAt,
                    x.j.PromisedAt,
                    x.j.CustomerWaiting,
                    x.j.Overnight,
                    x.j.IsWarrantyReturn,
                    x.j.UpdatedAt),
            })
            .ToListAsync(ct);

        var columns = openStatuses
            .Select(status => new FloorBoardColumn(
                status,
                rows.Where(r => r.Status == status).Select(r => r.Card).ToList()))
            .ToList();

        return new FloorBoardResult(columns);
    }
}
