using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Jobs;
using GarageOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>
/// P2-WP3. Thin controller -- business logic lives in JobManagementService (intake CRUD)
/// and JobStatusService (the state machine), mirroring CustomersController's shape. Status
/// can ONLY be changed through the dedicated status-transitions sub-resource below, never
/// through UpdateIntake -- the same "no arbitrary raw status assignment at the HTTP
/// boundary" principle VehiclesController's DELETE-for-soft-delete split already
/// establishes (P2-WP3_ARCHITECTURE.md §6.4).
/// </summary>
[ApiController]
[Route("api/v1/jobs")]
[Authorize(Policy = "GarageTenant")]
public sealed class JobsController(
    JobManagementService jobService,
    JobStatusService jobStatusService,
    FloorBoardService floorBoardService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<JobDto>> Create([FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var result = await jobService.CreateAsync(
            new CreateJobFields(
                request.CustomerId, request.VehicleId, request.PrimaryMechanicId, request.SecondaryMechanicId,
                request.MileageAtIntake, request.CustomerComplaint, request.AdvisorNotes,
                request.PromisedAt, request.CustomerWaiting, request.Source,
                request.Overnight, request.OvernightNote, request.IsWarrantyReturn, request.ParentJobId), ct);

        return result.Outcome switch
        {
            JobMutationOutcome.CustomerNotFound => NotFound(new { error = "Customer not found." }),
            JobMutationOutcome.VehicleNotFound => NotFound(new { error = "Vehicle not found." }),
            JobMutationOutcome.ParentJobNotFound => NotFound(new { error = "Parent job not found." }),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Job!.Id }, ToDto(result.Job!)),
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDto>> GetById(Guid id, CancellationToken ct)
    {
        var job = await jobService.GetByIdAsync(id, ct);
        return job is null ? NotFound() : Ok(ToDto(job));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobDto>> UpdateIntake(
        Guid id, [FromBody] UpdateJobIntakeRequest request, CancellationToken ct)
    {
        var updated = await jobService.UpdateIntakeAsync(
            id, new UpdateJobIntakeFields(
                request.PrimaryMechanicId, request.SecondaryMechanicId, request.MileageAtIntake,
                request.CustomerComplaint, request.AdvisorNotes, request.PromisedAt,
                request.CustomerWaiting, request.Overnight, request.OvernightNote), ct);

        return updated is null ? NotFound() : Ok(ToDto(updated));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<JobHistoryEntryDto>>> GetHistory(Guid id, CancellationToken ct)
    {
        var job = await jobService.GetByIdAsync(id, ct);
        if (job is null)
        {
            return NotFound();
        }

        var history = await jobService.GetHistoryAsync(id, ct);
        return Ok(history.Select(ToHistoryDto).ToList());
    }

    /// <summary>The ONLY HTTP entry point that can change Status.</summary>
    [HttpPost("{id:guid}/status-transitions")]
    public async Task<ActionResult<JobDto>> TransitionStatus(
        Guid id, [FromBody] TransitionJobStatusRequest request, CancellationToken ct)
    {
        var result = await jobStatusService.TransitionAsync(id, request.TargetStatus, request.Reason, ct);
        return result.Outcome switch
        {
            JobTransitionOutcome.NotFound => NotFound(),
            JobTransitionOutcome.Conflict => Conflict(new { error = "This job was updated by someone else. Please refresh and try again." }),
            _ => Ok(ToDto(result.Job!)),
        };
    }

    [HttpGet("floor-board")]
    public async Task<ActionResult<FloorBoardResponse>> GetFloorBoard(CancellationToken ct)
    {
        var board = await floorBoardService.GetBoardAsync(ct);
        var columns = board.Columns.Select(col => new FloorBoardColumnDto(
            col.Status, col.Cards.Select(ToCardDto).ToList())).ToList();
        return Ok(new FloorBoardResponse(columns));
    }

    private static JobDto ToDto(Job j) => new(
        j.Id, j.JobNumber, j.CustomerId, j.VehicleId, j.PrimaryMechanicId, j.SecondaryMechanicId, j.Status,
        j.MileageAtIntake, j.CustomerComplaint, j.AdvisorNotes, j.PromisedAt, j.CustomerWaiting, j.Source,
        j.Overnight, j.OvernightNote, j.IsWarrantyReturn, j.ParentJobId,
        j.CancellationReason, j.DeletionReason, j.CreatedAt, j.UpdatedAt);

    private static JobHistoryEntryDto ToHistoryDto(JobHistoryEntry h) => new(
        h.Id, h.ActorId, h.ActorName, h.ActorRole, h.EventType, h.Summary, h.Detail, h.CreatedAt);

    private static FloorBoardCardDto ToCardDto(FloorBoardCard c) => new(
        c.JobId, c.JobNumber, c.CustomerDisplayName, c.VehicleDisplay, c.PrimaryMechanicId,
        c.PrimaryMechanicName, c.CheckedInAt, c.PromisedAt, c.CustomerWaiting, c.Overnight,
        c.IsWarrantyReturn, c.StatusUpdatedAt);
}
