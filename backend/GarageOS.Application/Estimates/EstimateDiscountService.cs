using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;

namespace GarageOS.Application.Estimates;

/// <summary>
/// WP-5 brief §4/§7. The single authoritative application-service mutation path for
/// Estimate.DiscountAmount/Total. No other code in the solution may call
/// IEstimateMutationRepository.UpdateDiscountAsync -- guarded by
/// EstimateMutationBoundaryTests.
/// </summary>
public sealed class EstimateDiscountService(
    IEstimateMutationRepository estimates,
    IBusinessRuleAuthorizer businessRuleAuthorizer,
    ICurrentTenant currentTenant)
{
    public async Task<ApplyDiscountResult> ApplyDiscountAsync(
        Guid estimateId, decimal discountPercent, CancellationToken ct = default)
    {
        if (discountPercent < 0)
        {
            return ApplyDiscountResult.Failure("Discount percent must not be negative.");
        }

        var estimate = await estimates.FindByIdAsync(estimateId, ct);
        if (estimate is null)
        {
            return ApplyDiscountResult.Failure("Estimate not found.");
        }

        // Defense-in-depth alongside the EF global query filter that already scopes
        // FindByIdAsync to the current tenant (WP-3 pattern) -- explicit re-check before
        // any write, matching TenantGuard.EnsureOwned's existing role elsewhere.
        TenantGuard.EnsureOwned(estimate.GarageId, currentTenant);

        var outcome = await businessRuleAuthorizer.AuthorizeDiscountAsync(estimate.GarageId, discountPercent, ct);
        if (!outcome.Succeeded)
        {
            // Every DiscountLimitHandler failure reason (tenant_mismatch,
            // role_not_permitted, exceeds_manager_cap) is a hard deny here -- unlike
            // EstimateApprovalService, DiscountLimitHandler has no "soft" outcome.
            return ApplyDiscountResult.Denied(outcome.FailureReason!);
        }

        var discountAmount = Math.Round(estimate.Subtotal * discountPercent / 100m, 2);
        var newTotal = estimate.Subtotal - discountAmount + estimate.TaxAmount;

        await estimates.UpdateDiscountAsync(estimateId, discountAmount, newTotal, ct);

        return ApplyDiscountResult.Ok(discountAmount, newTotal);
    }
}

public sealed record ApplyDiscountResult
{
    public bool Success { get; init; }

    /// True specifically when the DiscountLimitHandler denied the request (as opposed to
    /// an ordinary Failure such as "estimate not found") -- named IsDenied rather than
    /// Denied because a property and the static Denied(...) factory below cannot share a
    /// name in C#.
    public bool IsDenied { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? Total { get; init; }

    public static ApplyDiscountResult Ok(decimal discountAmount, decimal total) => new()
    { Success = true, DiscountAmount = discountAmount, Total = total };

    public static ApplyDiscountResult Denied(string reason) => new()
    { Success = false, IsDenied = true, ErrorMessage = reason };

    public static ApplyDiscountResult Failure(string reason) => new()
    { Success = false, IsDenied = false, ErrorMessage = reason };
}
