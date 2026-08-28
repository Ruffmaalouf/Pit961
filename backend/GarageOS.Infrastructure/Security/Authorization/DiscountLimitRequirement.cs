using Microsoft.AspNetCore.Authorization;

namespace GarageOS.Infrastructure.Security.Authorization;

/// <summary>
/// WP-5 brief §1. Resource carried into IAuthorizationService.AuthorizeAsync for the
/// "DiscountLimit" policy. DiscountPercent is the caller's PROPOSED percent for this
/// request -- Estimate has no stored discount-percent column, so this is never a value
/// read back off a persisted entity.
/// </summary>
public sealed record DiscountAuthorizationResource(Guid GarageId, decimal DiscountPercent);

/// <summary>
/// Resource-based -- like <see cref="EstimateApprovalThresholdRequirement"/>, do NOT
/// attach this policy directly via <c>[Authorize(Policy = "DiscountLimit")]</c>. A bare
/// attribute supplies no resource, so <c>context.Resource</c> would never be a
/// <see cref="DiscountAuthorizationResource"/> and the handler would never call Succeed --
/// every request would fail closed regardless of the actual discount, silently breaking
/// the feature rather than denying anything meaningful. Only invoke via
/// <c>IBusinessRuleAuthorizer.AuthorizeDiscountAsync</c>. Enforced by
/// AuthorizationAttributeMisuseTests (WP-5 brief §3/§9 test 22).
/// </summary>
public sealed class DiscountLimitRequirement : IAuthorizationRequirement;

/// <summary>
/// WP-5 brief §1. Enforces 06_permission_matrix.md's "Apply discount" row: Owner may
/// discount unrestricted (may exceed 15%); Manager may discount capped at &lt;= 15.00%
/// inclusive; every other role (Advisor/Reception/Mechanic/Accountant) is denied outright
/// regardless of the requested percent. DiscountPercent &lt; 0 is NOT sanitized here --
/// that is an ordinary input-validation concern the calling application-service rejects
/// before this check ever runs, keeping this handler a pure permission/threshold decision.
/// </summary>
public sealed class DiscountLimitHandler
    : AuthorizationHandler<DiscountLimitRequirement, DiscountAuthorizationResource>
{
    public const decimal ManagerCapPercent = 15.00m;

    // Exact-string role comparison, matching this codebase's existing convention
    // (User.Role/JWT "role" claim are always seeded/issued lowercase; PlatformAdminHandler
    // does exact HasClaim("platform_admin", "true") matching too) -- no case-normalization
    // is introduced here that doesn't already exist elsewhere.
    private static readonly HashSet<string> RolesPermittedToDiscountAtAll =
        new(StringComparer.Ordinal) { "owner", "manager" };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DiscountLimitRequirement requirement,
        DiscountAuthorizationResource resource)
    {
        var garageIdClaim = context.User.FindFirst("garage_id")?.Value;
        if (garageIdClaim is null
            || !Guid.TryParse(garageIdClaim, out var actorGarageId)
            || actorGarageId != resource.GarageId)
        {
            // Distinct, named failure reason -- the calling application-service needs to
            // tell this apart from an ordinary permission denial; both WP-5 handlers use
            // the same reason string for consistency.
            context.Fail(new AuthorizationFailureReason(this, "tenant_mismatch"));
            return Task.CompletedTask;
        }

        var role = context.User.FindFirst("role")?.Value ?? string.Empty;

        if (!RolesPermittedToDiscountAtAll.Contains(role))
        {
            // 06_permission_matrix.md: Advisor/Reception/Mechanic/Accountant = None.
            context.Fail(new AuthorizationFailureReason(this, "role_not_permitted"));
            return Task.CompletedTask;
        }

        if (role == "owner" || resource.DiscountPercent <= ManagerCapPercent)
        {
            // Owner = Admin/unrestricted (may exceed 15%). Manager <= 15.00% inclusive.
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail(new AuthorizationFailureReason(this, "exceeds_manager_cap"));
        return Task.CompletedTask;
    }
}
