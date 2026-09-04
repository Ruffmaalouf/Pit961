using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;

namespace GarageOS.Application.Estimates;

/// <summary>
/// WP-5 brief §4/§7, extended P2-WP4. The single authoritative application-service mutation
/// path for Estimate.DiscountAmount/Total. No other code in the solution may call
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

        // P2-WP4, Owner Decision #3: an approved estimate is never silently edited in
        // place -- a superseded revision (created once a re-quote happens) is immutable.
        // Same check EstimateApprovalService.RouteStatusAsync/ClearOwnerApprovalAsync and
        // EstimateManagementService now all share.
        if (estimate.Status == "superseded")
        {
            return ApplyDiscountResult.Failure(
                "This estimate has been superseded by a newer revision and can no longer be changed.");
        }

        // P2-WP4 QA gate B1 / Owner Decision #3: a discount is only meaningful while the
        // estimate is still "draft" -- once submitted (sent/pending_owner_approval) or
        // decided by the customer (approved/partially_approved/rejected), the price the
        // recipient is looking at must not silently change under them. Mirrors
        // EstimateManagementService.ReplaceItemsAsync's identical draft-only gate; a
        // re-quote goes through CreateRevisionAsync instead.
        if (estimate.Status != "draft")
        {
            return ApplyDiscountResult.Failure(
                "This estimate has already been submitted and can no longer be discounted directly. Create a new revision to change pricing.");
        }

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

        try
        {
            await estimates.UpdateDiscountAsync(estimateId, discountAmount, newTotal, ct);
        }
        catch (EstimateConcurrencyConflictException)
        {
            // The row changed between FindByIdAsync's read and UpdateDiscountAsync's write
            // -- e.g. someone else's discount or a revision race landed first. Tell the
            // caller to re-fetch and retry rather than silently applying a stale discount
            // against numbers that may no longer be current.
            return ApplyDiscountResult.Conflict();
        }

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
    public bool IsConflict { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? Total { get; init; }

    public static ApplyDiscountResult Ok(decimal discountAmount, decimal total) => new()
    { Success = true, DiscountAmount = discountAmount, Total = total };

    public static ApplyDiscountResult Denied(string reason) => new()
    { Success = false, IsDenied = true, ErrorMessage = reason };

    public static ApplyDiscountResult Failure(string reason) => new()
    { Success = false, IsDenied = false, ErrorMessage = reason };

    public static ApplyDiscountResult Conflict() => new()
    {
        Success = false,
        IsConflict = true,
        ErrorMessage = "This estimate was updated by someone else. Please refresh and try again.",
    };
}
