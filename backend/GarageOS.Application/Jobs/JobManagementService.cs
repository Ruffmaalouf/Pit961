using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Domain.Common;
using GarageOS.Domain.Entities;

namespace GarageOS.Application.Jobs;

public sealed record CreateJobFields(
    Guid CustomerId, Guid VehicleId, Guid? PrimaryMechanicId, Guid? SecondaryMechanicId,
    int? MileageAtIntake, string? CustomerComplaint, string? AdvisorNotes,
    DateTimeOffset? PromisedAt, bool CustomerWaiting, string Source,
    bool Overnight, string? OvernightNote, bool IsWarrantyReturn, Guid? ParentJobId);

public sealed record UpdateJobIntakeFields(
    Guid? PrimaryMechanicId, Guid? SecondaryMechanicId, int? MileageAtIntake,
    string? CustomerComplaint, string? AdvisorNotes, DateTimeOffset? PromisedAt,
    bool CustomerWaiting, bool Overnight, string? OvernightNote);

public enum JobMutationOutcome { Ok, CustomerNotFound, VehicleNotFound, ParentJobNotFound }

public sealed class JobMutationResult
{
    public JobMutationOutcome Outcome { get; }
    public Job? Job { get; }

    private JobMutationResult(JobMutationOutcome outcome, Job? job)
    {
        Outcome = outcome;
        Job = job;
    }

    public static JobMutationResult Ok(Job job) => new(JobMutationOutcome.Ok, job);
    public static JobMutationResult CustomerNotFound() => new(JobMutationOutcome.CustomerNotFound, null);
    public static JobMutationResult VehicleNotFound() => new(JobMutationOutcome.VehicleNotFound, null);
    public static JobMutationResult ParentJobNotFound() => new(JobMutationOutcome.ParentJobNotFound, null);
}

/// <summary>
/// P2-WP3. Non-status intake CRUD -- deliberately separate from JobStatusService (mirrors
/// Estimate's split between EstimateApprovalService/EstimateDiscountService, both sharing one
/// mutation repository but each owning one guarded concern). CreateJobFields/
/// UpdateJobIntakeFields carry no Status property at all -- same pattern CreateCustomerFields
/// uses to make client-supplied GarageId impossible: there is nothing for a malicious or
/// buggy payload to override, because the DTO shape itself doesn't carry the field.
/// </summary>
public sealed class JobManagementService(
    IJobMutationRepository jobs,
    IJobQueryRepository jobsRead,
    ICustomerQueryRepository customersRead,
    IVehicleQueryRepository vehiclesRead,
    ICurrentTenant currentTenant)
{
    public async Task<JobMutationResult> CreateAsync(CreateJobFields fields, CancellationToken ct = default)
    {
        var customer = await customersRead.FindByIdAsync(fields.CustomerId, ct);
        if (customer is null)
        {
            return JobMutationResult.CustomerNotFound();
        }

        var vehicle = await vehiclesRead.FindByIdAsync(fields.VehicleId, ct);
        if (vehicle is null)
        {
            return JobMutationResult.VehicleNotFound();
        }

        // Defense-in-depth on both parents, mirroring JobsTenantIsolationTests'
        // WriteOwnershipCheck_RejectsParentCustomerFromMismatchedTenant precedent -- even
        // though customersRead/vehiclesRead already tenant-filter, and even though a
        // cross-tenant fields.CustomerId/VehicleId would already have returned null above.
        TenantGuard.EnsureOwned(customer.GarageId, currentTenant);
        TenantGuard.EnsureOwned(vehicle.GarageId, currentTenant);

        // Security-review finding (P2-WP3 gate): ParentJobId is client-suppliable
        // (CreateJobRequest.ParentJobId) and was being persisted with NO ownership check at
        // all -- unlike CustomerId/VehicleId above. A caller could supply another tenant's
        // real Job GUID and it would insert successfully (the FK has no garage scoping),
        // creating a narrow cross-tenant existence oracle and violating this WP's own stated
        // "denormalized garage_id integrity" invariant for parent references. Same
        // find-then-EnsureOwned pattern as Customer/Vehicle above; a supplied ParentJobId
        // that doesn't resolve to an owned Job is rejected outright rather than silently
        // persisted or treated as if it didn't exist.
        if (fields.ParentJobId is { } parentJobId)
        {
            var parentJob = await jobsRead.FindByIdAsync(parentJobId, ct);
            if (parentJob is null)
            {
                return JobMutationResult.ParentJobNotFound();
            }

            TenantGuard.EnsureOwned(parentJob.GarageId, currentTenant);
        }

        var job = new Job
        {
            GarageId = currentTenant.GarageId,
            CustomerId = fields.CustomerId,
            VehicleId = fields.VehicleId,
            PrimaryMechanicId = fields.PrimaryMechanicId,
            SecondaryMechanicId = fields.SecondaryMechanicId,
            CreatedBy = currentTenant.UserId,
            Status = JobStatuses.CheckedIn, // the ONLY place a literal status is ever
                                             // assigned outside JobStatusService, and only
                                             // ever this one value -- CreateJobFields has no
                                             // Status property at all for a client to override.
            MileageAtIntake = fields.MileageAtIntake,
            CustomerComplaint = fields.CustomerComplaint,
            AdvisorNotes = fields.AdvisorNotes,
            PromisedAt = fields.PromisedAt,
            CustomerWaiting = fields.CustomerWaiting,
            Source = fields.Source,
            Overnight = fields.Overnight,
            OvernightNote = fields.OvernightNote,
            IsWarrantyReturn = fields.IsWarrantyReturn,
            ParentJobId = fields.ParentJobId,
        };

        var inserted = await jobs.InsertAsync(job, ct); // allocates JobNumber internally, one transaction
        return JobMutationResult.Ok(inserted);
    }

    public async Task<Job?> UpdateIntakeAsync(Guid jobId, UpdateJobIntakeFields fields, CancellationToken ct = default)
    {
        var existing = await jobs.FindByIdAsync(jobId, ct);
        if (existing is null)
        {
            return null;
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant);

        await jobs.UpdateIntakeDetailsAsync(jobId, fields, ct);
        return await jobs.FindByIdAsync(jobId, ct);
    }

    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken ct = default) => jobsRead.FindByIdAsync(jobId, ct);

    public Task<IReadOnlyList<JobHistoryEntry>> GetHistoryAsync(Guid jobId, CancellationToken ct = default) =>
        jobsRead.GetHistoryAsync(jobId, ct);
}
