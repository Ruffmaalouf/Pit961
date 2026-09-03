using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Application.Jobs;
using GarageOS.Domain.Common;
using GarageOS.Domain.Entities;

namespace GarageOS.Tests.Unit.Jobs;

/// <summary>
/// P2-WP3. Pure unit tests for JobStatusService's transition table + role gating + check
/// ordering, against a hand-rolled in-memory fake IJobMutationRepository (this codebase has
/// no mocking library -- CustomerManagementService/EstimateApprovalService's own equivalents
/// are instead covered end-to-end via GarageOS.Tests.Integration; this file adds the cheaper,
/// faster pure-logic layer specifically because JobStatusService's AllowedTransitions/RolesFor
/// tables are exactly the kind of pure data-driven logic a unit test suits best).
/// </summary>
public class JobStatusServiceTests
{
    private sealed class FakeCurrentTenant : ICurrentTenant
    {
        public Guid GarageId { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; } = Guid.NewGuid();
        public string Role { get; set; } = "owner";
    }

    private sealed class FakeJobMutationRepository : IJobMutationRepository
    {
        public Job? Job { get; set; }
        public bool ThrowConcurrencyConflict { get; set; }
        public int TransitionCallCount { get; private set; }

        public Task<Job?> FindByIdAsync(Guid jobId, CancellationToken ct = default) =>
            Task.FromResult(Job is not null && Job.Id == jobId ? Job : null);

        public Task<string> AllocateNextJobNumberAsync(Guid garageId, CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by JobStatusServiceTests.");

        public Task<Job> InsertAsync(Job job, CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by JobStatusServiceTests.");

        public Task UpdateIntakeDetailsAsync(Guid jobId, UpdateJobIntakeFields fields, CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by JobStatusServiceTests.");

        public Task<Job> TransitionStatusAsync(
            Guid jobId, string fromStatus, string toStatus, Guid actorId, string actorRole,
            string? reason, CancellationToken ct = default)
        {
            TransitionCallCount++;
            if (ThrowConcurrencyConflict)
            {
                throw new JobConcurrencyConflictException(jobId);
            }

            Job!.Status = toStatus;
            return Task.FromResult(Job);
        }
    }

    private static Job MakeJob(Guid garageId, string status) => new()
    {
        GarageId = garageId,
        Status = status,
        JobNumber = "J-000001",
        CustomerId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        CreatedBy = Guid.NewGuid(),
    };

    [Fact]
    public async Task TransitionAsync_NotFound_ReturnsNotFoundOutcome_AndNeverCallsRepositoryWrite()
    {
        var repo = new FakeJobMutationRepository { Job = null };
        var tenant = new FakeCurrentTenant();
        var service = new JobStatusService(repo, tenant);

        var result = await service.TransitionAsync(Guid.NewGuid(), JobStatuses.EstimatePending);

        Assert.Equal(JobTransitionOutcome.NotFound, result.Outcome);
        Assert.Equal(0, repo.TransitionCallCount);
    }

    [Fact]
    public async Task TransitionAsync_CrossTenantJob_ThrowsTenantOwnershipException()
    {
        var tenant = new FakeCurrentTenant();
        var job = MakeJob(Guid.NewGuid(), JobStatuses.CheckedIn); // different GarageId than tenant
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        await Assert.ThrowsAsync<TenantOwnershipException>(
            () => service.TransitionAsync(job.Id, JobStatuses.EstimatePending));
        Assert.Equal(0, repo.TransitionCallCount);
    }

    [Fact]
    public async Task TransitionAsync_SkipAheadTransition_ThrowsInvalidJobStatusTransitionException_BeforeRoleCheck()
    {
        var tenant = new FakeCurrentTenant { Role = "mechanic" }; // would ALSO fail the role check for this edge
        var job = MakeJob(tenant.GarageId, JobStatuses.CheckedIn);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        // checked_in -> in_progress is a skip-ahead, never a member of AllowedTransitions --
        // must surface as 400-mapped InvalidJobStatusTransitionException, not a 403
        // RolePermissionException, per §3.2's check-ordering requirement (transition-validity
        // before role).
        var ex = await Assert.ThrowsAsync<InvalidJobStatusTransitionException>(
            () => service.TransitionAsync(job.Id, JobStatuses.InProgress));

        Assert.Equal(JobStatuses.CheckedIn, ex.FromStatus);
        Assert.Equal(JobStatuses.InProgress, ex.AttemptedStatus);
        Assert.Equal(0, repo.TransitionCallCount);
    }

    [Fact]
    public async Task TransitionAsync_TerminalState_RejectsAnyOutboundTransition()
    {
        var tenant = new FakeCurrentTenant();
        var job = MakeJob(tenant.GarageId, JobStatuses.Closed);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        await Assert.ThrowsAsync<InvalidJobStatusTransitionException>(
            () => service.TransitionAsync(job.Id, JobStatuses.Cancelled));
    }

    [Theory]
    [InlineData(JobStatuses.Completed, JobStatuses.Cancelled)]
    [InlineData(JobStatuses.Invoiced, JobStatuses.Cancelled)]
    [InlineData(JobStatuses.Invoiced, JobStatuses.Deleted)]
    public async Task TransitionAsync_CancelOrDeleteOnceInvoiced_IsRejected(string from, string to)
    {
        var tenant = new FakeCurrentTenant { Role = "owner" };
        var job = MakeJob(tenant.GarageId, from);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        if (from == JobStatuses.Completed)
        {
            // completed -> cancelled IS allowed (pre-invoice); only invoiced is the cutoff.
            var result = await service.TransitionAsync(job.Id, to);
            Assert.Equal(JobTransitionOutcome.Ok, result.Outcome);
            return;
        }

        await Assert.ThrowsAsync<InvalidJobStatusTransitionException>(() => service.TransitionAsync(job.Id, to));
    }

    [Fact]
    public async Task TransitionAsync_MechanicRole_CannotDispatchFromCheckedIn()
    {
        var tenant = new FakeCurrentTenant { Role = "mechanic" };
        var job = MakeJob(tenant.GarageId, JobStatuses.CheckedIn);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        // checked_in -> estimate_pending IS a valid transition, but mechanic isn't in
        // DispatchRoles -- must be RolePermissionException (403), not silently allowed.
        await Assert.ThrowsAsync<RolePermissionException>(
            () => service.TransitionAsync(job.Id, JobStatuses.EstimatePending));
        Assert.Equal(0, repo.TransitionCallCount);
    }

    [Fact]
    public async Task TransitionAsync_MechanicRole_CanStartAnApprovedJob()
    {
        var tenant = new FakeCurrentTenant { Role = "mechanic" };
        var job = MakeJob(tenant.GarageId, JobStatuses.Approved);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        // approved -> in_progress is the one place a mechanic needs write access --
        // the actual Floor Board "I'm starting this job" action (§3.5).
        var result = await service.TransitionAsync(job.Id, JobStatuses.InProgress);

        Assert.Equal(JobTransitionOutcome.Ok, result.Outcome);
        Assert.Equal(1, repo.TransitionCallCount);
    }

    [Fact]
    public async Task TransitionAsync_MechanicRole_CannotInvoice()
    {
        var tenant = new FakeCurrentTenant { Role = "mechanic" };
        var job = MakeJob(tenant.GarageId, JobStatuses.Completed);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        // completed -> invoiced is a billing action that deliberately excludes mechanic.
        await Assert.ThrowsAsync<RolePermissionException>(
            () => service.TransitionAsync(job.Id, JobStatuses.Invoiced));
    }

    [Fact]
    public async Task TransitionAsync_AdvisorRole_CannotCancel()
    {
        var tenant = new FakeCurrentTenant { Role = "advisor" };
        var job = MakeJob(tenant.GarageId, JobStatuses.CheckedIn);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        // * -> cancelled/deleted is manager/owner only, reusing the exact
        // SoftDeleteAllowedRoles set established for Customer/Vehicle -- advisor must be
        // rejected even though advisor can dispatch checked_in -> estimate_pending.
        await Assert.ThrowsAsync<RolePermissionException>(
            () => service.TransitionAsync(job.Id, JobStatuses.Cancelled));
    }

    [Fact]
    public async Task TransitionAsync_ConcurrencyConflict_ReturnsConflictOutcome_NotAnException()
    {
        var tenant = new FakeCurrentTenant { Role = "owner" };
        var job = MakeJob(tenant.GarageId, JobStatuses.CheckedIn);
        var repo = new FakeJobMutationRepository { Job = job, ThrowConcurrencyConflict = true };
        var service = new JobStatusService(repo, tenant);

        var result = await service.TransitionAsync(job.Id, JobStatuses.EstimatePending);

        Assert.Equal(JobTransitionOutcome.Conflict, result.Outcome);
    }

    [Fact]
    public async Task TransitionAsync_CancelledToDeleted_IsAllowed_ForOwner()
    {
        var tenant = new FakeCurrentTenant { Role = "owner" };
        var job = MakeJob(tenant.GarageId, JobStatuses.Cancelled);
        var repo = new FakeJobMutationRepository { Job = job };
        var service = new JobStatusService(repo, tenant);

        // Correcting a mistaken cancellation (§2.2 notes) -- the one non-forward-chain edge.
        var result = await service.TransitionAsync(job.Id, JobStatuses.Deleted);

        Assert.Equal(JobTransitionOutcome.Ok, result.Outcome);
    }

    [Fact]
    public async Task TransitionAsync_FullForwardChain_EachStepAllowed_ForOwner()
    {
        var tenant = new FakeCurrentTenant { Role = "owner" };
        var chain = new[]
        {
            JobStatuses.CheckedIn, JobStatuses.EstimatePending, JobStatuses.AwaitingApproval,
            JobStatuses.Approved, JobStatuses.InProgress, JobStatuses.Completed,
            JobStatuses.Invoiced, JobStatuses.Closed,
        };

        for (var i = 0; i < chain.Length - 1; i++)
        {
            var job = MakeJob(tenant.GarageId, chain[i]);
            var repo = new FakeJobMutationRepository { Job = job };
            var service = new JobStatusService(repo, tenant);

            var result = await service.TransitionAsync(job.Id, chain[i + 1]);

            Assert.Equal(JobTransitionOutcome.Ok, result.Outcome);
        }
    }
}
