using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Estimates;

/// <summary>
/// WP-5 brief §7/§8. The single Infrastructure class permitted to mutate
/// Estimate.DiscountAmount/Total/Status -- enforced by EstimateMutationBoundaryTests'
/// source-scan (GarageOS.Tests.Unit.Architecture). This is the one allow-listed file in
/// that scan.
///
/// FindByIdAsync deliberately uses AsNoTracking, and both write methods deliberately
/// re-fetch their OWN tracked instance rather than accepting the caller's AsNoTracking()
/// copy back. This closes a real EF Core sharp edge: if FindByIdAsync returned a TRACKED
/// entity and some unrelated caller mutated its properties in memory without persisting,
/// EF's ambient change tracking could silently flush that dangling mutation the next time
/// ANY SaveChangesAsync ran on the same per-request AppDbContext instance -- a bypass
/// vector no source-scan regex could ever catch, since it involves no suspicious call-site
/// text at all. AsNoTracking on the read side plus a fresh tracked re-fetch here closes it
/// structurally instead. Do not remove AsNoTracking from FindByIdAsync, and do not hold a
/// tracked Estimate instance across calls within this class.
/// </summary>
public sealed class EstimateMutationRepository(AppDbContext db) : IEstimateMutationRepository
{
    public Task<Estimate?> FindByIdAsync(Guid estimateId, CancellationToken ct = default) =>
        db.Estimates.AsNoTracking().SingleOrDefaultAsync(e => e.Id == estimateId, ct);

    public async Task UpdateDiscountAsync(
        Guid estimateId, decimal discountAmount, decimal total, CancellationToken ct = default)
    {
        var estimate = await db.Estimates.SingleAsync(e => e.Id == estimateId, ct);
        estimate.DiscountAmount = discountAmount;
        estimate.Total = total;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateApprovalRoutingStatusAsync(
        Guid estimateId, string status, CancellationToken ct = default)
    {
        var estimate = await db.Estimates.SingleAsync(e => e.Id == estimateId, ct);
        estimate.Status = status;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
