namespace GarageOS.Domain.Common;

/// <summary>
/// P2-WP3. Shared Job.Status string constants (DECISIONS.md #12 Decision #1's ratified
/// 8-state-plus-2-exception vocabulary) so JobStatusService's transition table and any
/// read-side "is this job closed" logic reference one source of truth instead of hand-copied
/// literal lists that can drift. CustomerQueryRepository.ClosedJobStatuses (P2-WP2, a
/// different WP's owned file) is a separate hand-copied array with the same three values --
/// flagged in P2-WP3_ARCHITECTURE.md §6 as an optional follow-up to reconcile, not required
/// by this WP.
/// </summary>
public static class JobStatuses
{
    public const string CheckedIn = "checked_in";
    public const string EstimatePending = "estimate_pending";
    public const string AwaitingApproval = "awaiting_approval";
    public const string Approved = "approved";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Invoiced = "invoiced";
    public const string Closed = "closed";
    public const string Cancelled = "cancelled";
    public const string Deleted = "deleted";

    /// <summary>The three terminal/exception states a Job is considered "closed" in for
    /// read-side purposes (e.g. Floor Board exclusion, HasOpenJobsAsync-style checks).
    /// "invoiced" is deliberately NOT included -- a job isn't fully wrapped up until
    /// closed, even once invoiced (P2-WP3_ARCHITECTURE.md §7).</summary>
    public static readonly IReadOnlySet<string> ClosedSet =
        new HashSet<string>(StringComparer.Ordinal) { Closed, Cancelled, Deleted };

    /// <summary>Floor Board columns, in fixed display order (P2-WP3_ARCHITECTURE.md §7).
    /// Excludes the three ClosedSet states -- the Floor Board is a working view, not a
    /// full history.</summary>
    public static readonly IReadOnlyList<string> OpenBoardOrder =
        [CheckedIn, EstimatePending, AwaitingApproval, Approved, InProgress, Completed, Invoiced];
}
