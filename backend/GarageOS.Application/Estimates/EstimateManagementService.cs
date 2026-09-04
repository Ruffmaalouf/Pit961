using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Domain.Entities;

namespace GarageOS.Application.Estimates;

public sealed record EstimateItemFields(
    string Type, string Description, string? PartNumber, decimal Quantity, decimal UnitCost,
    decimal UnitPrice, int SortOrder);

public sealed record CreateEstimateFields(Guid JobId, string Type, string? Notes, IReadOnlyList<EstimateItemFields> Items);

public enum EstimateMutationOutcome { Ok, JobNotFound, ParentEstimateNotFound, Superseded, Conflict, NotFound }

public sealed class EstimateMutationResult
{
    public EstimateMutationOutcome Outcome { get; }
    public Estimate? Estimate { get; }

    private EstimateMutationResult(EstimateMutationOutcome outcome, Estimate? estimate)
    {
        Outcome = outcome;
        Estimate = estimate;
    }

    public static EstimateMutationResult Ok(Estimate estimate) => new(EstimateMutationOutcome.Ok, estimate);
    public static EstimateMutationResult JobNotFound() => new(EstimateMutationOutcome.JobNotFound, null);
    public static EstimateMutationResult ParentEstimateNotFound() => new(EstimateMutationOutcome.ParentEstimateNotFound, null);
    public static EstimateMutationResult Superseded() => new(EstimateMutationOutcome.Superseded, null);
    public static EstimateMutationResult Conflict() => new(EstimateMutationOutcome.Conflict, null);
    public static EstimateMutationResult NotFound() => new(EstimateMutationOutcome.NotFound, null);
}

/// <summary>
/// P2-WP4. Non-financial-decision Estimate CRUD -- creation, item editing, revisioning, and
/// recording a customer's approval/rejection -- deliberately separate from
/// EstimateDiscountService (owns DiscountAmount/Total) and EstimateApprovalService (owns
/// the send/approve threshold routing and the Owner's clear action), mirroring
/// JobManagementService's split from JobStatusService. All four services share ONE
/// IEstimateMutationRepository, each owning a disjoint, explicitly-documented set of
/// guarded columns -- no two of these services ever write the same column.
///
/// CreateEstimateFields/EstimateItemFields carry no Subtotal/Total/DiscountAmount/Status
/// property at all -- the same "there is nothing for a malicious or buggy payload to
/// override, because the DTO shape itself doesn't carry the field" pattern
/// CreateJobFields/CreateCustomerFields already establish. The backend computes Subtotal
/// from the submitted items every time; nothing here ever trusts a client-supplied total.
/// </summary>
public sealed class EstimateManagementService(
    IEstimateMutationRepository estimates,
    IJobQueryRepository jobsRead,
    ICurrentTenant currentTenant)
{
    public async Task<EstimateMutationResult> CreateAsync(CreateEstimateFields fields, CancellationToken ct = default)
    {
        var job = await jobsRead.FindByIdAsync(fields.JobId, ct);
        if (job is null)
        {
            return EstimateMutationResult.JobNotFound();
        }

        // Defense-in-depth, mirroring JobManagementService.CreateAsync's precedent for its
        // own parent references.
        TenantGuard.EnsureOwned(job.GarageId, currentTenant);

        var estimate = new Estimate
        {
            GarageId = currentTenant.GarageId,
            JobId = fields.JobId,
            Type = fields.Type,
            Notes = fields.Notes,
            CreatedBy = currentTenant.UserId,
            // Status/Subtotal/DiscountAmount/Total are never set from client input here --
            // Status keeps the entity default ("draft"); Subtotal/Total are computed by
            // IEstimateMutationRepository.InsertAsync from the items below.
        };
        var items = fields.Items.Select(ToItem).ToList();

        var inserted = await estimates.InsertAsync(estimate, items, ct);
        return EstimateMutationResult.Ok(inserted);
    }

    public async Task<EstimateMutationResult> ReplaceItemsAsync(
        Guid estimateId, IReadOnlyList<EstimateItemFields> itemFields, CancellationToken ct = default)
    {
        var existing = await estimates.FindByIdAsync(estimateId, ct);
        if (existing is null)
        {
            return EstimateMutationResult.NotFound();
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant);

        if (existing.Status == "superseded")
        {
            return EstimateMutationResult.Superseded();
        }

        try
        {
            var updated = await estimates.ReplaceItemsAsync(estimateId, itemFields.Select(ToItem).ToList(), ct);
            return EstimateMutationResult.Ok(updated);
        }
        catch (Application.Common.EstimateConcurrencyConflictException)
        {
            return EstimateMutationResult.Conflict();
        }
    }

    public async Task<EstimateMutationResult> RecordCustomerApprovalAsync(
        Guid estimateId, string decision, string approvalMethod, string? approvedByName, CancellationToken ct = default)
    {
        var existing = await estimates.FindByIdAsync(estimateId, ct);
        if (existing is null)
        {
            return EstimateMutationResult.NotFound();
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant);

        if (existing.Status == "superseded")
        {
            return EstimateMutationResult.Superseded();
        }

        // Customer approval and Owner approval are separate concepts (P2-WP4 order):
        // recording a customer decision never requires the Owner role, and never touches
        // pending_owner_approval -- it is entirely independent of that gate.
        try
        {
            var updated = await estimates.RecordCustomerApprovalAsync(estimateId, decision, approvalMethod, approvedByName, ct);
            return EstimateMutationResult.Ok(updated);
        }
        catch (Application.Common.EstimateConcurrencyConflictException)
        {
            return EstimateMutationResult.Conflict();
        }
    }

    /// <summary>
    /// P2-WP4, Owner Decision #3. Creates a new revision from an existing Estimate
    /// (typically one already sent/approved and now needing a re-quote): the parent is
    /// marked superseded and immutable; the new revision starts as a fresh "draft" with
    /// both approval states reset (no ApprovalMethod/ApprovedByName/ApprovedAt carried
    /// forward, and Status is "draft" rather than "pending_owner_approval" -- owner
    /// approval restarts independently the next time this new revision is submitted). Item
    /// lines are carried forward as the new revision's starting point, re-editable via
    /// ReplaceItemsAsync before it is submitted again.
    /// </summary>
    public async Task<EstimateMutationResult> CreateRevisionAsync(Guid parentEstimateId, CancellationToken ct = default)
    {
        var parent = await estimates.FindByIdAsync(parentEstimateId, ct);
        if (parent is null)
        {
            // Also covers the cross-tenant ParentEstimateId case: FindByIdAsync is
            // tenant-scoped by AppDbContext's global query filter, so a real Estimate ID
            // belonging to a different garage returns null here, never leaking its
            // existence via a different status code than a genuine not-found (matches
            // TenantOwnershipException's own 404 mapping in GlobalExceptionHandler).
            return EstimateMutationResult.ParentEstimateNotFound();
        }

        // Defense-in-depth alongside the tenant-scoped read above.
        TenantGuard.EnsureOwned(parent.GarageId, currentTenant);

        if (parent.Status == "superseded")
        {
            return EstimateMutationResult.Superseded();
        }

        var carriedItems = (await estimates.GetItemsAsync(parentEstimateId, ct))
            .Select(i => new EstimateItem
            {
                Type = i.Type,
                Description = i.Description,
                PartNumber = i.PartNumber,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                UnitPrice = i.UnitPrice,
                ApprovalStatus = "pending",
                SortOrder = i.SortOrder,
            })
            .ToList();

        try
        {
            var revision = await estimates.CreateRevisionAsync(parentEstimateId, carriedItems, ct);
            return EstimateMutationResult.Ok(revision);
        }
        catch (Application.Common.EstimateConcurrencyConflictException)
        {
            // Someone else's concurrent CreateRevisionAsync (or a discount/approval write)
            // against the same parent already landed -- refuse rather than risk two
            // revisions both claiming the same RevisionNumber.
            return EstimateMutationResult.Conflict();
        }
    }

    public Task<Estimate?> GetByIdAsync(Guid estimateId, CancellationToken ct = default) =>
        estimates.FindByIdAsync(estimateId, ct);

    public Task<IReadOnlyList<EstimateItem>> GetItemsAsync(Guid estimateId, CancellationToken ct = default) =>
        estimates.GetItemsAsync(estimateId, ct);

    public Task<IReadOnlyList<Estimate>> ListByJobIdAsync(Guid jobId, CancellationToken ct = default) =>
        estimates.ListByJobIdAsync(jobId, ct);

    private static EstimateItem ToItem(EstimateItemFields fields) => new()
    {
        Type = fields.Type,
        Description = fields.Description,
        PartNumber = fields.PartNumber,
        Quantity = fields.Quantity,
        UnitCost = fields.UnitCost,
        UnitPrice = fields.UnitPrice,
        ApprovalStatus = "pending",
        SortOrder = fields.SortOrder,
    };
}
