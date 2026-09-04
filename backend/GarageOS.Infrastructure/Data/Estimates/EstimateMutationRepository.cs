using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Estimates;

/// <summary>
/// WP-5 brief §7/§8, extended P2-WP4. The single Infrastructure class permitted to mutate
/// Estimate/EstimateItem rows -- enforced by EstimateMutationBoundaryTests' source-scan
/// (GarageOS.Tests.Unit.Architecture). This is the one allow-listed file in that scan.
///
/// FindByIdAsync deliberately uses AsNoTracking, and every write method deliberately
/// re-fetches its OWN tracked instance rather than accepting the caller's AsNoTracking()
/// copy back. This closes a real EF Core sharp edge: if FindByIdAsync returned a TRACKED
/// entity and some unrelated caller mutated its properties in memory without persisting,
/// EF's ambient change tracking could silently flush that dangling mutation the next time
/// ANY SaveChangesAsync ran on the same per-request AppDbContext instance -- a bypass
/// vector no source-scan regex could ever catch, since it involves no suspicious call-site
/// text at all. AsNoTracking on the read side plus a fresh tracked re-fetch here closes it
/// structurally instead. Do not remove AsNoTracking from FindByIdAsync/ListByJobIdAsync/
/// GetItemsAsync, and do not hold a tracked Estimate/EstimateItem instance across calls
/// within this class.
/// </summary>
public sealed class EstimateMutationRepository(AppDbContext db) : IEstimateMutationRepository
{
    public Task<Estimate?> FindByIdAsync(Guid estimateId, CancellationToken ct = default) =>
        db.Estimates.AsNoTracking().SingleOrDefaultAsync(e => e.Id == estimateId, ct);

    public async Task<IReadOnlyList<Estimate>> ListByJobIdAsync(Guid jobId, CancellationToken ct = default) =>
        await db.Estimates.AsNoTracking()
            .Where(e => e.JobId == jobId)
            .OrderBy(e => e.RevisionNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EstimateItem>> GetItemsAsync(Guid estimateId, CancellationToken ct = default) =>
        await db.EstimateItems.AsNoTracking()
            .Where(i => i.EstimateId == estimateId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);

    public async Task<Estimate> InsertAsync(
        Estimate estimate, IReadOnlyList<EstimateItem> items, CancellationToken ct = default)
    {
        estimate.Subtotal = ComputeSubtotal(items);
        estimate.Total = estimate.Subtotal + estimate.TaxAmount;

        db.Estimates.Add(estimate);
        foreach (var item in items)
        {
            item.EstimateId = estimate.Id;
            item.GarageId = estimate.GarageId;
            db.EstimateItems.Add(item);
        }

        await db.SaveChangesAsync(ct);
        return estimate;
    }

    public async Task UpdateDiscountAsync(
        Guid estimateId, decimal discountAmount, decimal total, CancellationToken ct = default)
    {
        var estimate = await db.Estimates.SingleAsync(e => e.Id == estimateId, ct);
        estimate.DiscountAmount = discountAmount;
        estimate.Total = total;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveOrThrowConflictAsync(estimateId, ct);
    }

    public async Task UpdateApprovalRoutingStatusAsync(
        Guid estimateId, string status, CancellationToken ct = default)
    {
        var estimate = await db.Estimates.SingleAsync(e => e.Id == estimateId, ct);
        estimate.Status = status;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveOrThrowConflictAsync(estimateId, ct);
    }

    public async Task<Estimate> ReplaceItemsAsync(
        Guid estimateId, IReadOnlyList<EstimateItem> items, CancellationToken ct = default)
    {
        var estimate = await db.Estimates.SingleAsync(e => e.Id == estimateId, ct);

        var existingItems = await db.EstimateItems
            .Where(i => i.EstimateId == estimateId)
            .ToListAsync(ct);
        db.EstimateItems.RemoveRange(existingItems);

        foreach (var item in items)
        {
            item.EstimateId = estimateId;
            item.GarageId = estimate.GarageId;
            db.EstimateItems.Add(item);
        }

        estimate.Subtotal = ComputeSubtotal(items);
        estimate.DiscountAmount = 0; // a changed item list invalidates any prior discount
        estimate.Total = estimate.Subtotal + estimate.TaxAmount;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveOrThrowConflictAsync(estimateId, ct);
        return estimate;
    }

    public async Task<Estimate> RecordCustomerApprovalAsync(
        Guid estimateId, string status, string approvalMethod, string? approvedByName,
        CancellationToken ct = default)
    {
        var estimate = await db.Estimates.SingleAsync(e => e.Id == estimateId, ct);
        estimate.Status = status;
        estimate.ApprovalMethod = approvalMethod;
        estimate.ApprovedByName = approvedByName;
        estimate.ApprovedAt = DateTimeOffset.UtcNow;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveOrThrowConflictAsync(estimateId, ct);
        return estimate;
    }

    public async Task<Estimate> CreateRevisionAsync(
        Guid parentEstimateId, IReadOnlyList<EstimateItem> carriedItems, CancellationToken ct = default)
    {
        var parent = await db.Estimates.SingleAsync(e => e.Id == parentEstimateId, ct);

        // Repository-level defense-in-depth, mirroring JobMutationRepository.
        // TransitionStatusAsync's explicit fromStatus compare-and-swap: EstimateManagementService.
        // CreateRevisionAsync already checks this before calling here, but that check and
        // this fetch are two separate reads -- a second caller whose own read landed AFTER
        // a first caller's commit already flipped this row to "superseded" would otherwise
        // sail straight through xmin's optimistic check (its own fresh SingleAsync above
        // captures the CURRENT, already-superseded row's xmin, so the later UPDATE's
        // xmin-guarded WHERE clause would still match). xmin alone only catches two writers
        // racing from the SAME pre-commit snapshot; it does not catch a writer that reads
        // fresh, but stale-relative-to-business-state, data. This explicit check closes
        // that gap deterministically, not just when the two calls happen to overlap in
        // real time.
        if (parent.Status == "superseded")
        {
            throw new EstimateConcurrencyConflictException(parentEstimateId);
        }

        var revision = new Estimate
        {
            GarageId = parent.GarageId,
            JobId = parent.JobId,
            Type = parent.Type,
            ParentEstimateId = parent.Id,
            RevisionNumber = parent.RevisionNumber + 1,
            Status = "draft",
            TaxAmount = parent.TaxAmount,
            Notes = parent.Notes,
            CreatedBy = parent.CreatedBy,
        };
        revision.Subtotal = ComputeSubtotal(carriedItems);
        revision.Total = revision.Subtotal + revision.TaxAmount;

        parent.Status = "superseded";
        parent.UpdatedAt = DateTimeOffset.UtcNow;

        db.Estimates.Add(revision);
        foreach (var item in carriedItems)
        {
            item.EstimateId = revision.Id;
            item.GarageId = revision.GarageId;
            db.EstimateItems.Add(item);
        }

        // One SaveChangesAsync -- the parent's supersede-write and the new revision's
        // insert commit atomically together, or neither does. The parent update is the
        // concurrency-checked half (its captured xmin from the SingleAsync above), so a
        // losing concurrent CreateRevisionAsync call against the same parent throws here
        // before either the parent's Status flip or the new revision row is persisted.
        await SaveOrThrowConflictAsync(parentEstimateId, ct);
        return revision;
    }

    private static decimal ComputeSubtotal(IReadOnlyList<EstimateItem> items) =>
        items.Sum(i => i.Quantity * i.UnitPrice);

    /// <summary>
    /// Wraps SaveChangesAsync's DbUpdateConcurrencyException (thrown when a row's xmin,
    /// captured by one of this class's SingleAsync re-fetches above, no longer matches at
    /// UPDATE time because a concurrent write already changed it) into
    /// EstimateConcurrencyConflictException -- keeps the EF Core exception type from
    /// leaking out of GarageOS.Infrastructure, mirroring JobMutationRepository's equivalent
    /// wrapping for Job.
    /// </summary>
    private async Task SaveOrThrowConflictAsync(Guid estimateId, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new EstimateConcurrencyConflictException(estimateId);
        }
    }
}
