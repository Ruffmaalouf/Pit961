using Microsoft.AspNetCore.Authorization;

namespace GarageOS.Infrastructure.Security.Authorization;

/// <summary>
/// WP-4 brief §6. Registered as the "PlatformAdminOnly" policy -- succeeds iff the
/// principal has platform_admin == "true". Attached to zero controller actions in Phase 1
/// (brief §0: no live platform-admin route exists) -- exists so the mutual-exclusion
/// assertion is testable via IAuthorizationService.AuthorizeAsync directly, and so a
/// future Phase 2+ platform-admin route has a ready-made policy to attach to. Public (not
/// internal), same rationale as GarageTenantRequirement (Technical Architect required
/// change #4).
/// </summary>
public sealed class PlatformAdminRequirement : IAuthorizationRequirement;

public sealed class PlatformAdminHandler : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PlatformAdminRequirement requirement)
    {
        if (context.User.HasClaim("platform_admin", "true"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
