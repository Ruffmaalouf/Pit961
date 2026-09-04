using GarageOS.Application.Abstractions;
using GarageOS.Application.Estimates;
using GarageOS.Domain.Entities;

namespace GarageOS.Tests.Unit.Estimates;

/// <summary>
/// WP-5 brief §9, application-service level tests 11/12. Same fake-based style as
/// EstimateDiscountServiceTests.
/// </summary>
public class EstimateApprovalServiceTests
{
    private sealed class FakeCurrentTenant : ICurrentTenant
    {
        public Guid GarageId { get; init; }
        public Guid UserId { get; init; } = Guid.NewGuid();
        public string Role { get; init; } = "owner";
    }

    private sealed class FakeBusinessRuleAuthorizer : IBusinessRuleAuthorizer
    {
        public BusinessRuleAuthorizationOutcome ApprovalOutcome { get; set; } = BusinessRuleAuthorizationOutcome.Success;

        // QA-review required fix: mirrors EstimateDiscountServiceTests' fake -- tracks
        // whether the authorizer was actually invoked, so the tenant-mismatch ordering
        // test below genuinely proves TenantGuard.EnsureOwned runs BEFORE the authorizer
        // is ever called, rather than merely asserting the exception type and the
        // repository write not happening (which would still pass even if the ordering
        // were reversed, as long as the authorizer itself didn't throw).
        public bool ApprovalAuthorizeCalled { get; private set; }

        public Task<BusinessRuleAuthorizationOutcome> AuthorizeDiscountAsync(
            Guid resourceGarageId, decimal discountPercent, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");

        public Task<BusinessRuleAuthorizationOutcome> AuthorizeEstimateApprovalThresholdAsync(
            Guid resourceGarageId, decimal subtotal, CancellationToken ct = default)
        {
            ApprovalAuthorizeCalled = true;
            return Task.FromResult(ApprovalOutcome);
        }
    }

    private sealed class FakeEstimateMutationRepository : IEstimateMutationRepository
    {
        public Estimate? EstimateToReturn { get; set; }
        public string? LastStatus { get; private set; }
        public bool UpdateStatusCalled { get; private set; }
        public bool ThrowConflictOnUpdateStatus { get; set; }

        public Task<Estimate?> FindByIdAsync(Guid estimateId, CancellationToken ct = default) =>
            Task.FromResult(EstimateToReturn);

        public Task<IReadOnlyList<Estimate>> ListByJobIdAsync(Guid jobId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");

        public Task<IReadOnlyList<EstimateItem>> GetItemsAsync(Guid estimateId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");

        public Task<Estimate> InsertAsync(Estimate estimate, IReadOnlyList<EstimateItem> items, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");

        public Task UpdateDiscountAsync(Guid estimateId, decimal discountAmount, decimal total, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");

        public Task UpdateApprovalRoutingStatusAsync(Guid estimateId, string status, CancellationToken ct = default)
        {
            if (ThrowConflictOnUpdateStatus)
            {
                throw new GarageOS.Application.Common.EstimateConcurrencyConflictException(estimateId);
            }
            UpdateStatusCalled = true;
            LastStatus = status;
            return Task.CompletedTask;
        }

        public Task<Estimate> ReplaceItemsAsync(Guid estimateId, IReadOnlyList<EstimateItem> items, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");

        public Task<Estimate> RecordCustomerApprovalAsync(
            Guid estimateId, string status, string approvalMethod, string? approvedByName, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");

        public Task<Estimate> CreateRevisionAsync(
            Guid parentEstimateId, IReadOnlyList<EstimateItem> carriedItems, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateApprovalService.");
    }

    private static Estimate MakeEstimate(Guid garageId, decimal subtotal, string status = "draft") => new()
    {
        GarageId = garageId,
        Subtotal = subtotal,
        Status = status,
    };

    [Fact]
    public async Task SubtotalAtOrBelow500_PersistsRequestedStatus_DoesNotRequireApproval()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 500.00m);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer { ApprovalOutcome = BusinessRuleAuthorizationOutcome.Success };
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        var result = await sut.RouteStatusAsync(estimate.Id, "sent");

        Assert.True(result.Success);
        Assert.Equal("sent", result.FinalStatus);
        Assert.False(result.RequiresOwnerApproval);
        Assert.Equal("sent", repository.LastStatus);
    }

    [Fact]
    public async Task SubtotalAbove500_PersistsPendingOwnerApproval_RegardlessOfRequestedStatus_AndStillSucceeds()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 500.01m);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer
        {
            ApprovalOutcome = BusinessRuleAuthorizationOutcome.Denied("requires_owner_approval"),
        };
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        var result = await sut.RouteStatusAsync(estimate.Id, "sent");

        Assert.True(result.Success); // reroute, not a rejection
        Assert.Equal("pending_owner_approval", result.FinalStatus);
        Assert.True(result.RequiresOwnerApproval);
        Assert.Equal("pending_owner_approval", repository.LastStatus);
    }

    [Fact]
    public async Task MismatchedTenant_ThrowsTenantOwnershipException_BeforeAuthorizerIsCalled()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 100m);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = Guid.NewGuid() };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        await Assert.ThrowsAsync<GarageOS.Application.Common.TenantOwnershipException>(
            () => sut.RouteStatusAsync(estimate.Id, "sent"));

        Assert.False(authorizer.ApprovalAuthorizeCalled);
        Assert.False(repository.UpdateStatusCalled);
    }

    [Fact]
    public async Task SupersededEstimate_RejectsSubmit_NoWriteOccurs()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 100m, status: "superseded");
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        var result = await sut.RouteStatusAsync(estimate.Id, "sent");

        Assert.False(result.Success);
        Assert.False(authorizer.ApprovalAuthorizeCalled);
        Assert.False(repository.UpdateStatusCalled);
    }

    [Fact]
    public async Task ConcurrentWriteRacesSubmit_ReturnsConflict_NotAnException()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 100m);
        var repository = new FakeEstimateMutationRepository
        {
            EstimateToReturn = estimate,
            ThrowConflictOnUpdateStatus = true,
        };
        var authorizer = new FakeBusinessRuleAuthorizer { ApprovalOutcome = BusinessRuleAuthorizationOutcome.Success };
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        var result = await sut.RouteStatusAsync(estimate.Id, "sent");

        Assert.False(result.Success);
        Assert.True(result.IsConflict);
    }

    // ---- ClearOwnerApprovalAsync (Owner Decision #2) ----------------------------------

    [Fact]
    public async Task ClearOwnerApproval_OwnerRole_PendingOwnerApproval_MovesToSent()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 900m, status: "pending_owner_approval");
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId, Role = "owner" };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        var result = await sut.ClearOwnerApprovalAsync(estimate.Id);

        Assert.True(result.Success);
        Assert.Equal("sent", result.FinalStatus);
        Assert.False(result.RequiresOwnerApproval);
        Assert.Equal("sent", repository.LastStatus);
        // The threshold policy is deliberately NOT re-invoked -- it is role-blind and
        // would just re-derive pending_owner_approval again (see the service's remarks).
        Assert.False(authorizer.ApprovalAuthorizeCalled);
    }

    [Fact]
    public async Task ClearOwnerApproval_ManagerRole_ThrowsRolePermissionException_NoWriteOccurs()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 900m, status: "pending_owner_approval");
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId, Role = "manager" };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        await Assert.ThrowsAsync<GarageOS.Application.Common.RolePermissionException>(
            () => sut.ClearOwnerApprovalAsync(estimate.Id));

        Assert.False(repository.UpdateStatusCalled);
    }

    [Fact]
    public async Task ClearOwnerApproval_NotCurrentlyPending_FailsWithoutRoleCheckMattering()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 100m, status: "sent"); // already cleared/never routed there
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId, Role = "owner" };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        var result = await sut.ClearOwnerApprovalAsync(estimate.Id);

        Assert.False(result.Success);
        Assert.False(repository.UpdateStatusCalled);
    }

    [Fact]
    public async Task ClearOwnerApproval_SupersededEstimate_Rejected()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 900m, status: "superseded");
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId, Role = "owner" };
        var sut = new EstimateApprovalService(repository, authorizer, currentTenant);

        var result = await sut.ClearOwnerApprovalAsync(estimate.Id);

        Assert.False(result.Success);
        Assert.False(repository.UpdateStatusCalled);
    }
}
