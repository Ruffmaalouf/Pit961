using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

/// <summary>
/// WP-5 brief §7/§8, extended P2-WP4. The single authoritative write surface for
/// Estimate/EstimateItem rows. Guarded by EstimateMutationBoundaryTests
/// (GarageOS.Tests.Unit.Architecture) -- a source-scan architecture test proving no other
/// production file in the solution mutates the `estimates` table via a tracked update, bulk
/// update, Attach(), or raw SQL, and that no other file holds a tracked (non-AsNoTracking)
/// reference to an Estimates query at all -- which is why estimate *creation* (InsertAsync)
/// and revision creation must live here too, even though EstimateMutationBoundaryTests'
/// class doc comment notes Estimates.Add(...) itself isn't restricted by the bypass-pattern
/// test: the tracked-reference test still forces it into this one file.
/// </summary>
public interface IEstimateMutationRepository
{
    /// <summary>
    /// Tenant-filtered, read-only (AsNoTracking) lookup -- see EstimateMutationRepository's
    /// remarks for why AsNoTracking specifically matters here (it is part of the
    /// bypass-protection design, not an incidental performance choice).
    /// </summary>
    Task<Estimate?> FindByIdAsync(Guid estimateId, CancellationToken ct = default);

    /// <summary>Tenant-filtered (global query filter), AsNoTracking, ordered by
    /// RevisionNumber ascending -- every revision of every Estimate on a Job, oldest
    /// first.</summary>
    Task<IReadOnlyList<Estimate>> ListByJobIdAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Tenant-filtered (global query filter), AsNoTracking, ordered by SortOrder.</summary>
    Task<IReadOnlyList<EstimateItem>> GetItemsAsync(Guid estimateId, CancellationToken ct = default);

    /// <summary>
    /// P2-WP4. Inserts a brand-new draft Estimate together with its items in one
    /// SaveChangesAsync call. Subtotal/Total are computed here from the supplied items
    /// (Quantity * UnitPrice, summed) -- the caller (EstimateManagementService) must never
    /// pass a client-supplied Subtotal/Total/DiscountAmount/Status; the entity it passes in
    /// carries only client-suppliable fields (JobId, Type, Notes) plus GarageId/CreatedBy
    /// already resolved server-side.
    /// </summary>
    Task<Estimate> InsertAsync(Estimate estimate, IReadOnlyList<EstimateItem> items, CancellationToken ct = default);

    /// <summary>
    /// THE only method permitted to write Estimate.DiscountAmount/Estimate.Total. Callers
    /// outside EstimateDiscountService are a bypass-protection violation. Throws
    /// GarageOS.Application.Common.EstimateConcurrencyConflictException if the row's xmin
    /// changed since it was last read.
    /// </summary>
    Task UpdateDiscountAsync(Guid estimateId, decimal discountAmount, decimal total, CancellationToken ct = default);

    /// <summary>
    /// THE only method permitted to write Estimate.Status as part of the send/approve
    /// approval-threshold routing decision, or the Owner's explicit clear of
    /// pending_owner_approval (both are the same underlying "resolve the routing decision"
    /// concern -- see EstimateApprovalService.ClearOwnerApprovalAsync's remarks). Callers
    /// outside EstimateApprovalService are a bypass-protection violation. Throws
    /// EstimateConcurrencyConflictException if the row's xmin changed since it was last
    /// read.
    /// </summary>
    Task UpdateApprovalRoutingStatusAsync(Guid estimateId, string status, CancellationToken ct = default);

    /// <summary>
    /// P2-WP4. Replaces an Estimate's item list wholesale (delete-all-and-reinsert, the
    /// simplest correct model for an MVP quoting tool) and recomputes Subtotal from the new
    /// items. DiscountAmount is reset to 0 and Total recomputed to Subtotal + TaxAmount --
    /// changing line items invalidates any previously-applied percentage discount, so it is
    /// never silently carried forward; the caller must reapply it. Callers outside
    /// EstimateManagementService are a bypass-protection violation for the same reason as
    /// the two methods above. Throws EstimateConcurrencyConflictException if the row's xmin
    /// changed since it was last read.
    /// </summary>
    Task<Estimate> ReplaceItemsAsync(Guid estimateId, IReadOnlyList<EstimateItem> items, CancellationToken ct = default);

    /// <summary>
    /// THE only method permitted to write Estimate.Status/ApprovalMethod/ApprovedByName/
    /// ApprovedAt as a customer-approval outcome (approved/partially_approved/rejected) --
    /// a distinct concern from the send/approve threshold routing above per
    /// IEstimateMutationRepository's original WP-5 doc comment ("any FUTURE, unrelated
    /// status transition... MUST use a differently-named method"). Callers outside
    /// EstimateManagementService are a bypass-protection violation. Throws
    /// EstimateConcurrencyConflictException if the row's xmin changed since it was last
    /// read.
    /// </summary>
    Task<Estimate> RecordCustomerApprovalAsync(
        Guid estimateId, string status, string approvalMethod, string? approvedByName,
        CancellationToken ct = default);

    /// <summary>
    /// P2-WP4, Owner Decision #3. Marks the parent Estimate "superseded" and inserts a new
    /// revision row (ParentEstimateId = parent.Id, RevisionNumber = parent.RevisionNumber +
    /// 1, Status = "draft", approval fields reset to null) together with its carried-forward
    /// items, all in ONE SaveChangesAsync call -- atomic, and concurrency-checked on the
    /// parent's xmin. Two concurrent revision-creation attempts against the same parent can
    /// therefore never both succeed and produce duplicate RevisionNumbers: the loser's
    /// SaveChangesAsync throws EstimateConcurrencyConflictException (its captured parent
    /// xmin no longer matches), and nothing it staged -- including the new child row -- is
    /// persisted, since a failed SaveChangesAsync commits nothing. Callers outside
    /// EstimateManagementService are a bypass-protection violation.
    /// </summary>
    Task<Estimate> CreateRevisionAsync(
        Guid parentEstimateId, IReadOnlyList<EstimateItem> carriedItems, CancellationToken ct = default);
}
