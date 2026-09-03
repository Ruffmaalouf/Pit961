using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Domain.Common;
using GarageOS.Domain.Entities;

namespace GarageOS.Application.Jobs;

public enum JobTransitionOutcome { Ok, NotFound, Conflict }

public sealed class JobTransitionResult
{
    public JobTransitionOutcome Outcome { get; }
    public Job? Job { get; }

    private JobTransitionResult(JobTransitionOutcome outcome, Job? job)
    {
        Outcome = outcome;
        Job = job;
    }

    public static JobTransitionResult Ok(Job job) => new(JobTransitionOutcome.Ok, job);
    public static JobTransitionResult NotFound() => new(JobTransitionOutcome.NotFound, null);
    public static JobTransitionResult Conflict() => new(JobTransitionOutcome.Conflict, null);
}

/// <summary>
/// P2-WP3. The sole authoritative writer of Job.Status, mirroring
/// CustomerManagementService.SoftDeleteAsync's check ordering exactly: not-found/cross-tenant
/// (404) -> transition-validity (400) -> role (403) -> write. Transition-validity is checked
/// before role deliberately -- a garbage targetStatus value should never leak "well at least
/// your role would have been allowed to do that" information, and it lets the Floor Board
/// disable invalid drag targets client-side using the same table without a role-check
/// round-trip first (P2-WP3_ARCHITECTURE.md §3.2).
///
/// AllowedTransitions/RolesFor are the ONLY two places DECISIONS.md #12 Decision #1's state
/// machine and P2-WP3_ARCHITECTURE.md §3.5's proposed role-gating table are encoded --
/// loosening either later (e.g. an IsWarrantyReturn-conditional skip edge, once confirmed by
/// product-manager/business-analyst per §2.4/§10) is a change to these two dictionaries only.
/// </summary>
public sealed class JobStatusService(
    IJobMutationRepository jobs,
    ICurrentTenant currentTenant)
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [JobStatuses.CheckedIn] = Set(JobStatuses.EstimatePending, JobStatuses.Cancelled, JobStatuses.Deleted),
            [JobStatuses.EstimatePending] = Set(JobStatuses.AwaitingApproval, JobStatuses.Cancelled, JobStatuses.Deleted),
            [JobStatuses.AwaitingApproval] = Set(JobStatuses.Approved, JobStatuses.Cancelled, JobStatuses.Deleted),
            [JobStatuses.Approved] = Set(JobStatuses.InProgress, JobStatuses.Cancelled, JobStatuses.Deleted),
            [JobStatuses.InProgress] = Set(JobStatuses.Completed, JobStatuses.Cancelled, JobStatuses.Deleted),
            [JobStatuses.Completed] = Set(JobStatuses.Invoiced, JobStatuses.Cancelled, JobStatuses.Deleted),
            // No cancel/delete once invoiced -- a real Invoice row exists by this point;
            // financial correction after invoicing is Invoice.VoidedAt/VoidReason, a
            // different, already-provisioned mechanism this WP does not touch
            // (P2-WP3_ARCHITECTURE.md §2.3).
            [JobStatuses.Invoiced] = Set(JobStatuses.Closed),
            [JobStatuses.Closed] = Set(),
            [JobStatuses.Cancelled] = Set(JobStatuses.Deleted),
            [JobStatuses.Deleted] = Set(),
        };

    private static readonly HashSet<string> DispatchRoles = new(StringComparer.Ordinal) { "advisor", "manager", "owner" };
    private static readonly HashSet<string> FloorRoles = new(StringComparer.Ordinal) { "mechanic", "advisor", "manager", "owner" };
    private static readonly HashSet<string> BillingRoles = new(StringComparer.Ordinal) { "advisor", "accountant", "manager", "owner" };
    private static readonly HashSet<string> ManagerRoles = new(StringComparer.Ordinal) { "manager", "owner" };

    public async Task<JobTransitionResult> TransitionAsync(
        Guid jobId, string targetStatus, string? reason = null, CancellationToken ct = default)
    {
        var existing = await jobs.FindByIdAsync(jobId, ct);
        if (existing is null)
        {
            return JobTransitionResult.NotFound();
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant); // defense-in-depth

        if (!AllowedTransitions.TryGetValue(existing.Status, out var allowedTargets)
            || !allowedTargets.Contains(targetStatus))
        {
            throw new InvalidJobStatusTransitionException(existing.Status, targetStatus);
        }

        if (!RolesFor(existing.Status, targetStatus).Contains(currentTenant.Role))
        {
            throw new RolePermissionException($"Job.Transition:{existing.Status}->{targetStatus}");
        }

        try
        {
            var updated = await jobs.TransitionStatusAsync(
                jobId, existing.Status, targetStatus, currentTenant.UserId, currentTenant.Role, reason, ct);
            return JobTransitionResult.Ok(updated);
        }
        catch (JobConcurrencyConflictException)
        {
            // The row's xmin changed between FindByIdAsync's read and TransitionStatusAsync's
            // write -- a concurrent transition (e.g. two Floor Board actors) raced this one.
            // Tell the caller to re-fetch and retry rather than silently applying a stale
            // transition (P2-WP3_ARCHITECTURE.md §3.4).
            return JobTransitionResult.Conflict();
        }
    }

    private static IReadOnlySet<string> RolesFor(string from, string to) => (from, to) switch
    {
        (JobStatuses.CheckedIn, JobStatuses.EstimatePending) => DispatchRoles,
        (JobStatuses.EstimatePending, JobStatuses.AwaitingApproval) => DispatchRoles,
        (JobStatuses.AwaitingApproval, JobStatuses.Approved) => DispatchRoles,
        (JobStatuses.Approved, JobStatuses.InProgress) => FloorRoles,
        (JobStatuses.InProgress, JobStatuses.Completed) => FloorRoles,
        (JobStatuses.Completed, JobStatuses.Invoiced) => BillingRoles,
        (JobStatuses.Invoiced, JobStatuses.Closed) => BillingRoles,
        (_, JobStatuses.Cancelled) => ManagerRoles,
        (_, JobStatuses.Deleted) => ManagerRoles,
        _ => Set(), // unreachable once AllowedTransitions has already validated (from, to)
    };

    private static HashSet<string> Set(params string[] values) => new(values, StringComparer.Ordinal);
}
