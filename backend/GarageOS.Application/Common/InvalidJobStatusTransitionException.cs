namespace GarageOS.Application.Common;

/// <summary>
/// P2-WP3. Thrown when a requested Job.Status transition is not a member of
/// JobStatusService's static AllowedTransitions table for the job's current status (e.g. a
/// skip-ahead transition, or a transition attempted from a terminal state). Distinct from
/// RolePermissionException -- this is "the request shape itself is invalid for this
/// resource's current state" (400), not an authorization denial (403). Distinguishing the
/// two lets the frontend tell "you're not allowed to do this at all" apart from "this Job
/// isn't in a state where that's possible right now" (P2-WP3_ARCHITECTURE.md §3.1). Mapped
/// to 400 Bad Request by GlobalExceptionHandler.
/// </summary>
public sealed class InvalidJobStatusTransitionException(string fromStatus, string attemptedStatus)
    : Exception($"Cannot transition Job from '{fromStatus}' to '{attemptedStatus}'.")
{
    public string FromStatus { get; } = fromStatus;
    public string AttemptedStatus { get; } = attemptedStatus;
}
