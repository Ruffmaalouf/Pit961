using Microsoft.AspNetCore.Authorization;

namespace GarageOS.Infrastructure.Security.Authorization;

/// <summary>
/// WP-5 brief §2. Subtotal is Estimate.Subtotal (pre-discount) -- 06_permission_matrix.md
/// Special Rule 3 is explicit the $500 check reads Subtotal, never Total. Never pass Total
/// into this resource.
/// </summary>
public sealed record EstimateApprovalAuthorizationResource(Guid GarageId, decimal Subtotal);

/// <summary>
/// WP-5 brief §2 design-alternatives discussion. This requirement's Handler encodes a
/// role-blind ROUTING signal via <c>context.Fail("requires_owner_approval")</c>, not a
/// genuine access denial. That makes <c>AuthorizationResult.Succeeded == false</c> a
/// MISLEADING signal if this policy is ever consumed the ordinary way.
///
/// Do NOT attach this policy directly to a controller action via
/// <c>[Authorize(Policy = "EstimateApprovalThreshold")]</c> -- ASP.NET Core's authorization
/// middleware would translate the routing case into an HTTP 403, which is wrong (the
/// request should succeed and land the estimate on a different Status). This policy must
/// only ever be invoked explicitly via
/// <c>IBusinessRuleAuthorizer.AuthorizeEstimateApprovalThresholdAsync</c>, whose caller
/// (EstimateApprovalService) already knows to treat "requires_owner_approval" as a
/// reroute, not a rejection. Enforced by AuthorizationAttributeMisuseTests (WP-5 brief
/// §3/§9 test 22) -- no allow-listed exception, there is no file where attaching this
/// policy via a bare attribute is ever correct.
/// </summary>
public sealed class EstimateApprovalThresholdRequirement : IAuthorizationRequirement;

/// <summary>
/// WP-5 brief §2. Enforces 06_permission_matrix.md Special Rule 3: estimates at or below
/// $500.00 pre-discount subtotal do not require Owner approval; above $500.00, the
/// estimate routes to "pending_owner_approval" regardless of actor role, Owner included.
/// Deliberately role-blind past the tenant-boundary check.
/// </summary>
public sealed class EstimateApprovalThresholdHandler
    : AuthorizationHandler<EstimateApprovalThresholdRequirement, EstimateApprovalAuthorizationResource>
{
    public const decimal ApprovalThreshold = 500.00m;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EstimateApprovalThresholdRequirement requirement,
        EstimateApprovalAuthorizationResource resource)
    {
        var garageIdClaim = context.User.FindFirst("garage_id")?.Value;
        if (garageIdClaim is null
            || !Guid.TryParse(garageIdClaim, out var actorGarageId)
            || actorGarageId != resource.GarageId)
        {
            context.Fail(new AuthorizationFailureReason(this, "tenant_mismatch"));
            return Task.CompletedTask;
        }

        // Deliberately role-blind from here down -- Special Rule 3 applies "regardless of
        // which of those two roles is acting" for the <=$500 path, and the mandatory test
        // matrix requires the >$500 routing to apply "regardless of actor role" too, Owner
        // included. Succeed() here is NOT "this actor may broadly send estimates" -- that
        // eligibility question is out of WP-5 scope (brief §6).
        if (resource.Subtotal <= ApprovalThreshold)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail(new AuthorizationFailureReason(this, "requires_owner_approval"));
        return Task.CompletedTask;
    }
}
