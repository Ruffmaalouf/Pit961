using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

/// <summary>
/// WP-5 brief §7/§8. The single authoritative write surface for Estimate.DiscountAmount/
/// Total/Status. Guarded by EstimateMutationBoundaryTests (GarageOS.Tests.Unit.Architecture)
/// -- a source-scan architecture test proving no other production file in the solution
/// mutates the `estimates` table via a tracked update, bulk update, Attach(), or raw SQL.
/// </summary>
public interface IEstimateMutationRepository
{
    /// <summary>
    /// Tenant-filtered, read-only (AsNoTracking) lookup -- see EstimateMutationRepository's
    /// remarks for why AsNoTracking specifically matters here (it is part of the
    /// bypass-protection design, not an incidental performance choice).
    /// </summary>
    Task<Estimate?> FindByIdAsync(Guid estimateId, CancellationToken ct = default);

    /// <summary>
    /// THE only method permitted to write Estimate.DiscountAmount/Estimate.Total. Callers
    /// outside EstimateDiscountService are a bypass-protection violation.
    /// </summary>
    Task UpdateDiscountAsync(Guid estimateId, decimal discountAmount, decimal total, CancellationToken ct = default);

    /// <summary>
    /// THE only method permitted to write Estimate.Status as part of the send/approve
    /// approval-threshold routing decision. Callers outside EstimateApprovalService are a
    /// bypass-protection violation. Any FUTURE, unrelated status transition (e.g. a later
    /// cancel/reject/supersede flow) is explicitly out of WP-5 scope and MUST use a
    /// differently-named method if/when it's built, precisely so it never inherits this
    /// method's bypass-protected call-site guarantee by accident.
    /// </summary>
    Task UpdateApprovalRoutingStatusAsync(Guid estimateId, string status, CancellationToken ct = default);
}
