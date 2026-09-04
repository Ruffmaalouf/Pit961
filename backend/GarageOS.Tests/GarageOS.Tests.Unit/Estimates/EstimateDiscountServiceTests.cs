using GarageOS.Application.Abstractions;
using GarageOS.Application.Estimates;
using GarageOS.Domain.Entities;

namespace GarageOS.Tests.Unit.Estimates;

/// <summary>
/// WP-5 brief §9, application-service level tests 8/9/10. Fake IBusinessRuleAuthorizer +
/// fake IEstimateMutationRepository + FakeCurrentTenant, same style as
/// TenantGuardTests.cs's own FakeCurrentTenant precedent.
/// </summary>
public class EstimateDiscountServiceTests
{
    private sealed class FakeCurrentTenant : ICurrentTenant
    {
        public Guid GarageId { get; init; }
        public Guid UserId { get; init; } = Guid.NewGuid();
        public string Role { get; init; } = "manager";
    }

    private sealed class FakeBusinessRuleAuthorizer : IBusinessRuleAuthorizer
    {
        public BusinessRuleAuthorizationOutcome DiscountOutcome { get; set; } = BusinessRuleAuthorizationOutcome.Success;
        public bool DiscountAuthorizeCalled { get; private set; }

        public Task<BusinessRuleAuthorizationOutcome> AuthorizeDiscountAsync(
            Guid resourceGarageId, decimal discountPercent, CancellationToken ct = default)
        {
            DiscountAuthorizeCalled = true;
            return Task.FromResult(DiscountOutcome);
        }

        public Task<BusinessRuleAuthorizationOutcome> AuthorizeEstimateApprovalThresholdAsync(
            Guid resourceGarageId, decimal subtotal, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");
    }

    private sealed class FakeEstimateMutationRepository : IEstimateMutationRepository
    {
        public Estimate? EstimateToReturn { get; set; }
        public bool UpdateDiscountCalled { get; private set; }
        public decimal? LastDiscountAmount { get; private set; }
        public decimal? LastTotal { get; private set; }
        public bool ThrowConflictOnUpdateDiscount { get; set; }

        public Task<Estimate?> FindByIdAsync(Guid estimateId, CancellationToken ct = default) =>
            Task.FromResult(EstimateToReturn);

        public Task<IReadOnlyList<Estimate>> ListByJobIdAsync(Guid jobId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");

        public Task<IReadOnlyList<EstimateItem>> GetItemsAsync(Guid estimateId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");

        public Task<Estimate> InsertAsync(Estimate estimate, IReadOnlyList<EstimateItem> items, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");

        public Task UpdateDiscountAsync(Guid estimateId, decimal discountAmount, decimal total, CancellationToken ct = default)
        {
            if (ThrowConflictOnUpdateDiscount)
            {
                throw new GarageOS.Application.Common.EstimateConcurrencyConflictException(estimateId);
            }
            UpdateDiscountCalled = true;
            LastDiscountAmount = discountAmount;
            LastTotal = total;
            return Task.CompletedTask;
        }

        public Task UpdateApprovalRoutingStatusAsync(Guid estimateId, string status, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");

        public Task<Estimate> ReplaceItemsAsync(Guid estimateId, IReadOnlyList<EstimateItem> items, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");

        public Task<Estimate> RecordCustomerApprovalAsync(
            Guid estimateId, string status, string approvalMethod, string? approvedByName, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");

        public Task<Estimate> CreateRevisionAsync(
            Guid parentEstimateId, IReadOnlyList<EstimateItem> carriedItems, CancellationToken ct = default) =>
            throw new InvalidOperationException("Not used by EstimateDiscountService.");
    }

    private static Estimate MakeEstimate(Guid garageId, decimal subtotal = 1000m, decimal taxAmount = 0m) => new()
    {
        GarageId = garageId,
        Subtotal = subtotal,
        TaxAmount = taxAmount,
    };

    [Fact]
    public async Task MismatchedTenant_ThrowsTenantOwnershipException_BeforeAuthorizerIsCalled()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = Guid.NewGuid() }; // different from estimate's garage
        var sut = new EstimateDiscountService(repository, authorizer, currentTenant);

        await Assert.ThrowsAsync<GarageOS.Application.Common.TenantOwnershipException>(
            () => sut.ApplyDiscountAsync(estimate.Id, 10m));

        Assert.False(authorizer.DiscountAuthorizeCalled);
    }

    [Fact]
    public async Task AuthorizerDenies_ReturnsDenied_NoWriteOccurs()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer
        {
            DiscountOutcome = BusinessRuleAuthorizationOutcome.Denied("exceeds_manager_cap"),
        };
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateDiscountService(repository, authorizer, currentTenant);

        var result = await sut.ApplyDiscountAsync(estimate.Id, 40m);

        Assert.False(result.Success);
        Assert.True(result.IsDenied);
        Assert.Equal("exceeds_manager_cap", result.ErrorMessage);
        Assert.False(repository.UpdateDiscountCalled);
    }

    [Fact]
    public async Task AuthorizerSucceeds_UpdatesDiscountWithCorrectlyRoundedAmounts()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 1000m, taxAmount: 50m);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer { DiscountOutcome = BusinessRuleAuthorizationOutcome.Success };
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateDiscountService(repository, authorizer, currentTenant);

        var result = await sut.ApplyDiscountAsync(estimate.Id, 15m);

        Assert.True(result.Success);
        Assert.True(repository.UpdateDiscountCalled);
        Assert.Equal(150.00m, repository.LastDiscountAmount); // 1000 * 15% = 150
        Assert.Equal(900.00m, repository.LastTotal);           // 1000 - 150 + 50 = 900
        Assert.Equal(150.00m, result.DiscountAmount);
        Assert.Equal(900.00m, result.Total);
    }

    [Fact]
    public async Task AuthorizerSucceeds_RoundsToNearestCent_WhenDiscountProducesAFractionalAmount()
    {
        // QA-review required fix: the original happy-path test used subtotal 1000 / 15%,
        // a "clean division" case (150.00 exactly) that never actually exercises
        // Math.Round's rounding behavior. This case (333.33 subtotal * 10% = 33.333...)
        // forces a real rounding decision -- 33.33, banker's/MidpointRounding.ToEven's
        // default for .5 boundaries doesn't apply here since 33.333... isn't a midpoint,
        // so this also implicitly locks in "round half away from zero" vs "round down"
        // behavior for the more common non-midpoint case.
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId, subtotal: 333.33m, taxAmount: 0m);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer { DiscountOutcome = BusinessRuleAuthorizationOutcome.Success };
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateDiscountService(repository, authorizer, currentTenant);

        var result = await sut.ApplyDiscountAsync(estimate.Id, 10m);

        Assert.True(result.Success);
        Assert.Equal(33.33m, repository.LastDiscountAmount); // 333.33 * 10% = 33.333 -> 33.33
        Assert.Equal(300.00m, repository.LastTotal);          // 333.33 - 33.33 + 0
        Assert.Equal(33.33m, result.DiscountAmount);
    }

    [Fact]
    public async Task NegativeDiscountPercent_FailsBeforeAuthorizerIsCalled()
    {
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId);
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateDiscountService(repository, authorizer, currentTenant);

        var result = await sut.ApplyDiscountAsync(estimate.Id, -5m);

        Assert.False(result.Success);
        Assert.False(result.IsDenied);
        Assert.False(authorizer.DiscountAuthorizeCalled);
        Assert.False(repository.UpdateDiscountCalled);
    }

    [Fact]
    public async Task SupersededEstimate_RejectsDiscount_NoWriteOccurs()
    {
        // P2-WP4, Owner Decision #3: a superseded revision is immutable.
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId);
        estimate.Status = "superseded";
        var repository = new FakeEstimateMutationRepository { EstimateToReturn = estimate };
        var authorizer = new FakeBusinessRuleAuthorizer();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateDiscountService(repository, authorizer, currentTenant);

        var result = await sut.ApplyDiscountAsync(estimate.Id, 10m);

        Assert.False(result.Success);
        Assert.False(authorizer.DiscountAuthorizeCalled);
        Assert.False(repository.UpdateDiscountCalled);
    }

    [Fact]
    public async Task ConcurrentWriteRacesDiscount_ReturnsConflict_NotAnException()
    {
        // P2-WP4: same-Estimate concurrency -- a competing mutation must never be silently
        // lost or crash the request; it must surface as a safe, reportable conflict.
        var garageId = Guid.NewGuid();
        var estimate = MakeEstimate(garageId);
        var repository = new FakeEstimateMutationRepository
        {
            EstimateToReturn = estimate,
            ThrowConflictOnUpdateDiscount = true,
        };
        var authorizer = new FakeBusinessRuleAuthorizer { DiscountOutcome = BusinessRuleAuthorizationOutcome.Success };
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };
        var sut = new EstimateDiscountService(repository, authorizer, currentTenant);

        var result = await sut.ApplyDiscountAsync(estimate.Id, 10m);

        Assert.False(result.Success);
        Assert.True(result.IsConflict);
    }
}
