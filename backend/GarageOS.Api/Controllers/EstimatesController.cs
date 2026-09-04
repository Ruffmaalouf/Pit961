using GarageOS.Api.Contracts;
using GarageOS.Application.Estimates;
using GarageOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>
/// P2-WP4. Thin controller -- business logic lives in EstimateManagementService (creation/
/// items/revisioning/customer-approval CRUD), EstimateDiscountService (the sole authoritative
/// writer of DiscountAmount/Total), and EstimateApprovalService (the sole authoritative
/// writer of Status for the send/approve threshold routing and the Owner's clear action),
/// mirroring JobsController's shape. No action here ever accepts a client-supplied
/// Total/DiscountAmount, or an approval-owned Status value -- see EstimateContracts.cs's
/// class doc comment.
/// </summary>
[ApiController]
[Route("api/v1/estimates")]
[Authorize(Policy = "GarageTenant")]
public sealed class EstimatesController(
    EstimateManagementService estimateService,
    EstimateDiscountService discountService,
    EstimateApprovalService approvalService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<EstimateDto>> Create([FromBody] CreateEstimateRequest request, CancellationToken ct)
    {
        var result = await estimateService.CreateAsync(
            new CreateEstimateFields(
                request.JobId, request.Type, request.Notes,
                request.Items.Select(ToItemFields).ToList()), ct);

        if (result.Outcome != EstimateMutationOutcome.Ok)
        {
            return NotFound(new { error = "Job not found." });
        }

        var dto = await ToDtoAsync(result.Estimate!, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Estimate!.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EstimateDto>> GetById(Guid id, CancellationToken ct)
    {
        var estimate = await estimateService.GetByIdAsync(id, ct);
        return estimate is null ? NotFound() : Ok(await ToDtoAsync(estimate, ct));
    }

    [HttpGet("/api/v1/jobs/{jobId:guid}/estimates")]
    public async Task<ActionResult<IReadOnlyList<EstimateDto>>> ListByJob(Guid jobId, CancellationToken ct)
    {
        var revisions = await estimateService.ListByJobIdAsync(jobId, ct);
        var dtos = new List<EstimateDto>();
        foreach (var e in revisions)
        {
            dtos.Add(await ToDtoAsync(e, ct));
        }
        return Ok(dtos);
    }

    [HttpPut("{id:guid}/items")]
    public async Task<ActionResult<EstimateDto>> ReplaceItems(
        Guid id, [FromBody] ReplaceEstimateItemsRequest request, CancellationToken ct)
    {
        var result = await estimateService.ReplaceItemsAsync(id, request.Items.Select(ToItemFields).ToList(), ct);
        return await MapMutationResult(result);
        async Task<ActionResult<EstimateDto>> MapMutationResult(EstimateMutationResult r) => r.Outcome switch
        {
            EstimateMutationOutcome.NotFound => NotFound(),
            EstimateMutationOutcome.Superseded => Conflict(new { error = "This estimate has been superseded by a newer revision and can no longer be changed." }),
            EstimateMutationOutcome.Conflict => Conflict(new { error = "This estimate was updated by someone else. Please refresh and try again." }),
            _ => Ok(await ToDtoAsync(r.Estimate!, ct)),
        };
    }

    [HttpPost("{id:guid}/discount")]
    public async Task<ActionResult<EstimateDto>> ApplyDiscount(
        Guid id, [FromBody] ApplyDiscountRequest request, CancellationToken ct)
    {
        var result = await discountService.ApplyDiscountAsync(id, request.DiscountPercent, ct);
        if (!result.Success)
        {
            if (result.IsConflict)
            {
                return Conflict(new { error = result.ErrorMessage });
            }
            return result.IsDenied
                ? StatusCode(StatusCodes.Status403Forbidden, new { error = result.ErrorMessage })
                : BadRequest(new { error = result.ErrorMessage });
        }

        var estimate = await estimateService.GetByIdAsync(id, ct);
        return estimate is null ? NotFound() : Ok(await ToDtoAsync(estimate, ct));
    }

    /// <summary>Submit for approval -- routes to "sent" directly, or to
    /// "pending_owner_approval" if the $500 subtotal threshold requires it. Always a
    /// success response either way; see EstimateApprovalThresholdRequirement's remarks on
    /// why this is a reroute, not a rejection.</summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<EstimateDto>> Submit(Guid id, CancellationToken ct)
    {
        var result = await approvalService.RouteStatusAsync(id, "sent", ct);
        return await MapApprovalResultAsync(id, result, ct);
    }

    /// <summary>Owner-only. Clears "pending_owner_approval" -- 403 for any non-Owner role
    /// (Owner Decision #2).</summary>
    [HttpPost("{id:guid}/clear-owner-approval")]
    public async Task<ActionResult<EstimateDto>> ClearOwnerApproval(Guid id, CancellationToken ct)
    {
        var result = await approvalService.ClearOwnerApprovalAsync(id, ct);
        return await MapApprovalResultAsync(id, result, ct);
    }

    [HttpPost("{id:guid}/customer-approval")]
    public async Task<ActionResult<EstimateDto>> RecordCustomerApproval(
        Guid id, [FromBody] RecordCustomerApprovalRequest request, CancellationToken ct)
    {
        var result = await estimateService.RecordCustomerApprovalAsync(
            id, request.Decision, request.ApprovalMethod, request.ApprovedByName, ct);

        return result.Outcome switch
        {
            EstimateMutationOutcome.NotFound => NotFound(),
            EstimateMutationOutcome.Superseded => Conflict(new { error = "This estimate has been superseded by a newer revision and can no longer be changed." }),
            EstimateMutationOutcome.Conflict => Conflict(new { error = "This estimate was updated by someone else. Please refresh and try again." }),
            _ => Ok(await ToDtoAsync(result.Estimate!, ct)),
        };
    }

    /// <summary>Owner Decision #3: creates a new revision, superseding this one.</summary>
    [HttpPost("{id:guid}/revisions")]
    public async Task<ActionResult<EstimateDto>> CreateRevision(Guid id, CancellationToken ct)
    {
        var result = await estimateService.CreateRevisionAsync(id, ct);
        return result.Outcome switch
        {
            EstimateMutationOutcome.ParentEstimateNotFound => NotFound(),
            EstimateMutationOutcome.Superseded => Conflict(new { error = "This estimate has already been superseded by another revision." }),
            EstimateMutationOutcome.Conflict => Conflict(new { error = "This estimate was updated by someone else. Please refresh and try again." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Estimate!.Id }, await ToDtoAsync(result.Estimate!, ct)),
        };
    }

    private async Task<ActionResult<EstimateDto>> MapApprovalResultAsync(
        Guid estimateId, EstimateApprovalRoutingResult result, CancellationToken ct)
    {
        if (!result.Success)
        {
            if (result.IsConflict)
            {
                return Conflict(new { error = result.ErrorMessage });
            }
            return result.IsNotFound
                ? NotFound(new { error = result.ErrorMessage })
                : BadRequest(new { error = result.ErrorMessage });
        }

        var estimate = await estimateService.GetByIdAsync(estimateId, ct);
        return estimate is null ? NotFound() : Ok(await ToDtoAsync(estimate, ct));
    }

    private static EstimateItemFields ToItemFields(EstimateItemRequest r) =>
        new(r.Type, r.Description, r.PartNumber, r.Quantity, r.UnitCost, r.UnitPrice, r.SortOrder);

    private async Task<EstimateDto> ToDtoAsync(Estimate e, CancellationToken ct)
    {
        var items = await estimateService.GetItemsAsync(e.Id, ct);
        return new EstimateDto(
            e.Id, e.JobId, e.Type, e.ParentEstimateId, e.RevisionNumber, e.Status,
            e.ApprovalMethod, e.ApprovedByName, e.ApprovedAt, e.SentAt,
            e.Subtotal, e.TaxAmount, e.DiscountAmount, e.Total, e.Notes,
            e.CreatedAt, e.UpdatedAt,
            items.Select(i => new EstimateItemDto(
                i.Id, i.Type, i.Description, i.PartNumber, i.Quantity, i.UnitCost, i.UnitPrice,
                i.ApprovalStatus, i.SortOrder)).ToList());
    }
}
