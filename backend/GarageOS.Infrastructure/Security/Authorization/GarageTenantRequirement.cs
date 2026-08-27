using Microsoft.AspNetCore.Authorization;

namespace GarageOS.Infrastructure.Security.Authorization;

/// <summary>
/// WP-4 brief §6. Registered as the "GarageTenant" policy -- succeeds iff the principal
/// has a garage_id claim. Public (not internal), per Technical Architect required change
/// #4 (WP-4 brief review): WP-5 builds its own business-rule authorization handlers on
/// top of this same framework and needs to reference/compose this requirement, e.g. in a
/// combined policy or a handler that also checks GarageTenantRequirement succeeded.
///
/// Why an explicit policy rather than bare [Authorize]: a platform-admin token is still
/// validly authenticated (passes bare [Authorize]) but carries no garage_id -- without
/// this policy gating every tenant-scoped endpoint, such a token would fall through to
/// application code and crash into HttpContextCurrentTenant.GarageId throwing, surfacing
/// as a generic 500 instead of a clean 403.
/// </summary>
public sealed class GarageTenantRequirement : IAuthorizationRequirement;

public sealed class GarageTenantHandler : AuthorizationHandler<GarageTenantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, GarageTenantRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == "garage_id"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
