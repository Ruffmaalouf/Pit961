namespace GarageOS.Application.Common;

/// <summary>
/// P2-WP4. Thrown when an Estimate row's concurrency token (Postgres xmin) changed between
/// the read and the write inside EstimateMutationRepository -- a concurrent mutation (e.g.
/// two actors applying a discount, or two actors racing to create a revision from the same
/// parent) raced this one. Mirrors JobConcurrencyConflictException's exact rationale: the
/// Application layer stays framework-free (no EF Core reference), so this wraps
/// Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException rather than letting that EF
/// type leak out of GarageOS.Infrastructure. Callers (EstimateDiscountService,
/// EstimateApprovalService, EstimateManagementService) catch this and report a 409
/// Conflict, telling the caller to reload real state rather than silently losing an update.
/// </summary>
public sealed class EstimateConcurrencyConflictException(Guid estimateId)
    : Exception($"Estimate {estimateId} was modified concurrently; the change was not applied.")
{
    public Guid EstimateId { get; } = estimateId;
}
