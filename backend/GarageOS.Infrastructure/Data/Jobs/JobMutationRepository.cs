using System.Text.Json;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Application.Jobs;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Jobs;

/// <summary>
/// P2-WP3. The single Infrastructure class permitted to mutate Job/JobHistoryEntry rows --
/// enforced by JobMutationBoundaryTests' source-scan (anchored on both DbSet roots). Mirrors
/// CustomerMutationRepository's AsNoTracking-on-read / fresh-tracked-re-fetch-on-write
/// pattern.
/// </summary>
public sealed class JobMutationRepository(AppDbContext db) : IJobMutationRepository
{
    public Task<Job?> FindByIdAsync(Guid jobId, CancellationToken ct = default) =>
        db.Jobs.AsNoTracking().SingleOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task<string> AllocateNextJobNumberAsync(Guid garageId, CancellationToken ct = default)
    {
        // Postgres row-level locking makes this single UPDATE an atomic increment-and-fetch
        // under real concurrency -- two simultaneous intakes for the same garage serialize on
        // this row's implicit lock (P2-WP3_ARCHITECTURE.md §5.1). No per-garage Postgres
        // SEQUENCE object needed -- the GarageSequence row already is the per-garage sequence.
        // .ToListAsync() + in-memory Single(), not .SingleAsync() directly on the SqlQuery --
        // EF Core's SqlQuery composability check rejects wrapping an UPDATE ... RETURNING
        // statement in a further SELECT (the query is not a plain composable SELECT), so
        // .SingleAsync() (which composes a limiting subquery) throws
        // "non-composable SQL" at runtime. Materializing first sidesteps that entirely.
        var allocated = (await db.Database
            .SqlQuery<long>($"""
                UPDATE garage_sequences
                SET next_job_number = next_job_number + 1
                WHERE garage_id = {garageId}
                RETURNING next_job_number - 1
                """)
            .ToListAsync(ct))
            .Single();

        return $"J-{allocated:D6}";
    }

    public async Task<Job> InsertAsync(Job job, CancellationToken ct = default)
    {
        // Explicit transaction required here (unlike TransitionStatusAsync below) because
        // AllocateNextJobNumberAsync issues a raw SQL statement that executes immediately,
        // outside the change tracker -- mixing it with a change-tracked Add/SaveChanges needs
        // both wrapped in one transaction to stay atomic (P2-WP3_ARCHITECTURE.md §5.1).
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        job.JobNumber = await AllocateNextJobNumberAsync(job.GarageId, ct);
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return job;
    }

    public async Task UpdateIntakeDetailsAsync(Guid jobId, UpdateJobIntakeFields fields, CancellationToken ct = default)
    {
        var job = await db.Jobs.SingleAsync(j => j.Id == jobId, ct);
        job.PrimaryMechanicId = fields.PrimaryMechanicId;
        job.SecondaryMechanicId = fields.SecondaryMechanicId;
        job.MileageAtIntake = fields.MileageAtIntake;
        job.CustomerComplaint = fields.CustomerComplaint;
        job.AdvisorNotes = fields.AdvisorNotes;
        job.PromisedAt = fields.PromisedAt;
        job.CustomerWaiting = fields.CustomerWaiting;
        job.Overnight = fields.Overnight;
        job.OvernightNote = fields.OvernightNote;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        // Deliberately never touches Status/Cancelled*/Deleted* -- guarded by
        // JobMutationBoundaryTests and UpdateJobIntakeFields' own shape (no Status property).
        await db.SaveChangesAsync(ct);
    }

    public async Task<Job> TransitionStatusAsync(
        Guid jobId, string fromStatus, string toStatus, Guid actorId, string actorRole,
        string? reason, CancellationToken ct = default)
    {
        var job = await db.Jobs.SingleAsync(j => j.Id == jobId, ct); // tracked -- this IS the write path
        job.Status = toStatus;
        job.UpdatedAt = DateTimeOffset.UtcNow;

        if (toStatus == Domain.Common.JobStatuses.Cancelled)
        {
            job.CancellationReason = reason;
            job.CancelledAt = DateTimeOffset.UtcNow;
            job.CancelledBy = actorId;
        }
        else if (toStatus == Domain.Common.JobStatuses.Deleted)
        {
            job.DeletionReason = reason;
            job.DeletedAt = DateTimeOffset.UtcNow; // this line is what makes the row vanish
            job.DeletedBy = actorId;               // from AppDbContext's default query filter
        }

        var actorName = await db.Users.AsNoTracking()
            .Where(u => u.Id == actorId).Select(u => u.Name).SingleOrDefaultAsync(ct) ?? "Unknown";

        db.JobHistory.Add(new JobHistoryEntry
        {
            GarageId = job.GarageId,
            JobId = jobId,
            ActorId = actorId,
            ActorName = actorName,
            ActorRole = actorRole,
            EventType = "status_transition",
            Summary = $"Status changed from {fromStatus} to {toStatus}",
            Detail = JsonSerializer.Serialize(new { fromStatus, toStatus, reason }),
        });

        // ONE call -- Job update + JobHistoryEntry insert committed together as EF Core's
        // single implicit transaction for everything tracked/added since the last
        // SaveChanges on this context instance. Translates a DbUpdateConcurrencyException
        // (job's xmin changed since SingleAsync's read) into the framework-free
        // JobConcurrencyConflictException the Application layer can catch without an EF
        // Core reference (JobStatusService.TransitionAsync reports a 409 Conflict).
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new JobConcurrencyConflictException(jobId);
        }

        return job;
    }
}
