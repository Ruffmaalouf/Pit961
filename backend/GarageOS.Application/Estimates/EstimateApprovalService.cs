using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;

namespace GarageOS.Application.Estimates;

/// <summary>
/// WP-5 brief §4/§7. The single authoritative application-service mutation path for
/// Estimate.Status as part of the $500 approval-threshold routing decision. No other code
/// in the solution may call IEstimateMutationRepository.UpdateApprovalRoutingStatusAsync --
/// guarded by EstimateMutationBoundaryTests.
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
            return EstimateApprovalRoutingResult.Failure("Estimate not found.");
        }

        TenantGuard.EnsureOwned(estimate.GarageId, currentTenant);

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
        await estimates.UpdateApprovalRoutingStatusAsync(estimateId, finalStatus, ct);

        return EstimateApprovalRoutingResult.Ok(finalStatus, requiresOwnerApproval: !outcome.Succeeded);
    }
}

public sealed record EstimateApprovalRoutingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FinalStatus { get; init; }
    public bool RequiresOwnerApproval { get; init; }

    public static EstimateApprovalRoutingResult Ok(string finalStatus, bool requiresOwnerApproval) => new()
    { Success = true, FinalStatus = finalStatus, RequiresOwnerApproval = requiresOwnerApproval };

    public static EstimateApprovalRoutingResult Failure(string reason) => new()
    { Success = false, ErrorMessage = reason };
}
