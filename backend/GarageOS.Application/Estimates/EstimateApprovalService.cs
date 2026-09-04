using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;

namespace GarageOS.Application.Estimates;

/// <summary>
/// WP-5 brief §4/§7, extended P2-WP4. The single authoritative application-service mutation
/// path for Estimate.Status as part of the $500 approval-threshold routing decision, AND
/// (P2-WP4) the Owner's explicit clearing of "pending_owner_approval". No other code in the
/// solution may call IEstimateMutationRepository.UpdateApprovalRoutingStatusAsync -- guarded
/// by EstimateMutationBoundaryTests.
///
/// ClearOwnerApprovalAsync deliberately does NOT re-run
/// IBusinessRuleAuthorizer.AuthorizeEstimateApprovalThresholdAsync -- that handler is
/// role-blind by design (Owner included; see EstimateApprovalThresholdHandler's remarks),
/// so re-running it against an unchanged Subtotal would just re-derive
/// "pending_owner_approval" again and the gate could never be cleared by anyone. Clearing
/// is instead a PLAIN role-gated action (Owner Decision #2: only Owner may clear it), the
/// same RolePermissionException idiom JobStatusService.RolesFor already uses -- not a
/// second pass through the money-threshold policy.
/// </summary>
public sealed class EstimateApprovalService(
    IEstimateMutationRepository estimates,
    IBusinessRuleAuthorizer businessRuleAuthorizer,
    ICurrentTenant currentTenant)
{
    public async Task<EstimateApprovalRoutingResult> RouteStatusAsync(
        Guid estimateId, string requestedStatus, CancellationToken ct = default)
    {
        var estimate = await estimates.FindByIdAsync(estimateId, ct);
        if (estimate is null)
        {
            return EstimateApprovalRoutingResult.NotFound("Estimate not found.");
        }

        TenantGuard.EnsureOwned(estimate.GarageId, currentTenant);

        // P2-WP4, Owner Decision #3: superseded revisions are immutable -- no submit
        // attempt against a superseded row may resurrect it. A 400, not a 404: the
        // resource exists, the request is simply invalid against its current state --
        // mirrors EstimateDiscountService.ApplyDiscountAsync's identical superseded check.
        if (estimate.Status == "superseded")
        {
            return EstimateApprovalRoutingResult.Failure(
                "This estimate has been superseded by a newer revision and can no longer be changed.");
        }

        // Always reads Subtotal (pre-discount), never Total -- 06_permission_matrix.md
        // Special Rule 3.
        var outcome = await businessRuleAuthorizer.AuthorizeEstimateApprovalThresholdAsync(
            estimate.GarageId, estimate.Subtotal, ct);

        if (!outcome.Succeeded && outcome.FailureReason == "tenant_mismatch")
        {
            // Should be unreachable -- TenantGuard.EnsureOwned above already throws for
            // this case -- kept as defense-in-depth in case the two checks ever
            // desynchronize (e.g. a future refactor removes the EnsureOwned call above).
            throw new TenantOwnershipException();
        }

        // The ONLY other failure reason this handler can produce is
        // "requires_owner_approval" -- a routing signal, not a rejection (see
        // EstimateApprovalThresholdRequirement's doc comment). The request still completes
        // successfully; it just lands on a different Status.
        var finalStatus = outcome.Succeeded ? requestedStatus : "pending_owner_approval";

        try
        {
            await estimates.UpdateApprovalRoutingStatusAsync(estimateId, finalStatus, ct);
        }
        catch (EstimateConcurrencyConflictException)
        {
            return EstimateApprovalRoutingResult.Conflict();
        }

        return EstimateApprovalRoutingResult.Ok(finalStatus, requiresOwnerApproval: !outcome.Succeeded);
    }

    /// <summary>
    /// P2-WP4, Owner Decision #2. Only the Owner role may clear an Estimate out of
    /// "pending_owner_approval" -- a Manager must not be able to, regardless of how small
    /// the amount actually is (the threshold handler already put it here specifically
    /// because it was role-blind at submit time; nothing about clearing it should be more
    /// permissive). Moves the estimate to "sent" -- the status it would have landed on at
    /// submit time had the threshold not intervened.
    /// </summary>
    public async Task<EstimateApprovalRoutingResult> ClearOwnerApprovalAsync(
        Guid estimateId, CancellationToken ct = default)
    {
        var estimate = await estimates.FindByIdAsync(estimateId, ct);
        if (estimate is null)
        {
            return EstimateApprovalRoutingResult.NotFound("Estimate not found.");
        }

        TenantGuard.EnsureOwned(estimate.GarageId, currentTenant);

        if (estimate.Status == "superseded")
        {
            return EstimateApprovalRoutingResult.Failure(
                "This estimate has been superseded by a newer revision and can no longer be changed.");
        }

        if (estimate.Status != "pending_owner_approval")
        {
            return EstimateApprovalRoutingResult.Failure(
                "This estimate is not pending owner approval.");
        }

        if (currentTenant.Role != "owner")
        {
            throw new RolePermissionException("Estimate.ClearOwnerApproval");
        }

        const string clearedStatus = "sent";
        try
        {
            await estimates.UpdateApprovalRoutingStatusAsync(estimateId, clearedStatus, ct);
        }
        catch (EstimateConcurrencyConflictException)
        {
            return EstimateApprovalRoutingResult.Conflict();
        }

        return EstimateApprovalRoutingResult.Ok(clearedStatus, requiresOwnerApproval: false);
    }
}

public sealed record EstimateApprovalRoutingResult
{
    public bool Success { get; init; }
    public bool IsConflict { get; init; }

    /// True specifically when the Estimate does not exist / is not visible to this tenant
    /// (maps to 404). Every other failure -- superseded, not-currently-pending -- maps to
    /// 400: the resource exists, the request is simply invalid against its current state.
    /// Mirrors ApplyDiscountResult.IsDenied's role in EstimateDiscountService.
    public bool IsNotFound { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FinalStatus { get; init; }
    public bool RequiresOwnerApproval { get; init; }

    public static EstimateApprovalRoutingResult Ok(string finalStatus, bool requiresOwnerApproval) => new()
    { Success = true, FinalStatus = finalStatus, RequiresOwnerApproval = requiresOwnerApproval };

    public static EstimateApprovalRoutingResult Failure(string reason) => new()
    { Success = false, ErrorMessage = reason };

    public static EstimateApprovalRoutingResult NotFound(string reason) => new()
    { Success = false, IsNotFound = true, ErrorMessage = reason };

    public static EstimateApprovalRoutingResult Conflict() => new()
    {
        Success = false,
        IsConflict = true,
        ErrorMessage = "This estimate was updated by someone else. Please refresh and try again.",
    };
}
