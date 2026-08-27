namespace GarageOS.Application.Abstractions;

using GarageOS.Application.Auth;

public sealed record IssuedAccessToken(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>
/// WP-4 brief §3/§6. The one authoritative path for issuing signed JWT access tokens --
/// both the real garage-tenant login/refresh path AND TestJwtTokenFactory (WP-4 brief
/// §17) call through this same production service, never a hand-rolled parallel token
/// builder in test code. HS256, single configured Issuer/Audience shared by both token
/// shapes (Decision #6) -- mutual exclusion between garage-tenant and platform-admin
/// tokens lives entirely in which claims are present (garage_id/role vs
/// platform_admin=="true"), enforced at the authorization-policy layer
/// (GarageTenantRequirement/PlatformAdminRequirement), not by a second audience value.
///
/// Multi-location forward-compatibility note (Technical Architect required change #6,
/// WP-4 brief review): the garage_id claim, as issued today, identifies the single
/// garage a Phase 1 user belongs to. When multi-location ownership ships, this claim's
/// meaning naturally becomes "the user's currently-active location" (still a single
/// value per token -- a session operates against one location at a time even if the
/// underlying account owns several) -- no claims-shape or token-issuance change is
/// required here; only the account/garage relationship in AccountProvisioningService
/// (WP-3B) and its authorization checks change.
/// </summary>
public interface ITokenService
{
    IssuedAccessToken IssueGarageTenantAccessToken(UserAuthRecord user);
    IssuedAccessToken IssuePlatformAdminAccessToken(Guid platformAdminId);
}
