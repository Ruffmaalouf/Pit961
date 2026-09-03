using GarageOS.Application.Jobs;
using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

/// <summary>
/// P2-WP3. The single authoritative write surface for Job/JobHistoryEntry rows -- mirrors
/// ICustomerMutationRepository's shape. Guarded by JobMutationBoundaryTests
/// (GarageOS.Tests.Unit.Architecture), anchored on TWO DbSet roots (Jobs AND JobHistory)
/// since TransitionStatusAsync legitimately mutates both tables in one unit of work.
/// </summary>
public interface IJobMutationRepository
{
    /// <summary>Tenant-filtered (via AppDbContext's global query filter), AsNoTracking
    /// lookup -- same bypass-protection reasoning as CustomerMutationRepository's
    /// FindByIdAsync: callers must never hold a tracked reference outside this class.</summary>
    Task<Job?> FindByIdAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Atomically increments and returns this garage's next job number via a
    /// row-locked `UPDATE ... RETURNING` against GarageSequence -- safe under real
    /// concurrency with no SELECT ... FOR UPDATE round-trip. Returns the formatted
    /// "J-{n:D6}" string.</summary>
    Task<string> AllocateNextJobNumberAsync(Guid garageId, CancellationToken ct = default);

    /// <summary>Allocates a job number and inserts the row in one explicit transaction --
    /// required because AllocateNextJobNumberAsync issues a raw SQL statement that executes
    /// immediately, outside the change tracker.</summary>
    Task<Job> InsertAsync(Job job, CancellationToken ct = default);

    /// <summary>Updates only the non-status intake fields (mechanic assignment, mileage,
    /// complaint/notes, promised time, waiting/overnight flags). Must never touch
    /// Status/Cancelled*/Deleted* columns -- enforced by JobMutationBoundaryTests and
    /// documented here exactly like IEstimateMutationRepository.UpdateApprovalRoutingStatusAsync's
    /// doc comment does for the analogous case.</summary>
    Task UpdateIntakeDetailsAsync(Guid jobId, UpdateJobIntakeFields fields, CancellationToken ct = default);

    /// <summary>THE only method permitted to write Job.Status (and, when transitioning to
    /// cancelled/deleted, the corresponding Cancelled*/Deleted* columns) plus insert the
    /// corresponding JobHistoryEntry row, atomically in one SaveChangesAsync call. Callers
    /// outside JobStatusService are a bypass-protection violation -- same convention
    /// IEstimateMutationRepository.UpdateApprovalRoutingStatusAsync already documents.
    /// Throws Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException if the row's xmin
    /// changed since it was last read (a concurrent transition raced this one).</summary>
    Task<Job> TransitionStatusAsync(
        Guid jobId, string fromStatus, string toStatus, Guid actorId, string actorRole,
        string? reason, CancellationToken ct = default);
}
