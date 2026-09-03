namespace GarageOS.Application.Common;

/// <summary>
/// P2-WP3. Thrown by IJobMutationRepository.TransitionStatusAsync when the Job row's
/// concurrency token (Postgres xmin) changed between the read and the write -- a
/// concurrent transition (e.g. two Floor Board actors) raced this one. GarageOS.Application
/// stays framework-free (no EF Core reference, matching the rest of this layer -- see
/// Program.cs's "WP-5 brief §4" comment), so this wraps
/// Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException rather than letting that EF
/// type leak into the Application layer. JobStatusService.TransitionAsync catches this and
/// reports a 409 Conflict (JobsController maps it to Conflict()).
/// </summary>
public sealed class JobConcurrencyConflictException(Guid jobId)
    : Exception($"Job {jobId} was modified concurrently; the transition was not applied.")
{
    public Guid JobId { get; } = jobId;
}
