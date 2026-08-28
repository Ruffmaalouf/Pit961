using GarageOS.Api.Contracts;
using GarageOS.Application.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GarageOS.Api.Controllers;

/// <summary>
/// WP-7 brief §4. Exposes ONLY the four non-secret BrandingOptions fields, for a
/// future frontend (WP-8) to consume without a rebuild whenever branding
/// configuration changes. Deliberately an endpoint now, not a build-time constant --
/// a build-time constant would require a frontend rebuild/redeploy on every branding
/// change, which would violate the "config change alone, no recompile" acceptance
/// criterion for the frontend surface once it exists.
///
/// Structural guarantee against ever leaking a secret through this surface (not "we'll
/// remember not to" -- three independent layers, see WP-7 brief §4):
///  1. GetBranding() hand-builds BrandingConfigResponse field-by-field; it never
///     serializes BrandingOptions directly, so a fifth property added to
///     BrandingOptions later does not change this endpoint's output unless a human
///     explicitly edits both BrandingConfigResponse and this mapping.
///  2. This controller's ONLY dependency is IOptions&lt;BrandingOptions&gt; -- no
///     IConfiguration, no IOptions&lt;JwtOptions&gt;, no connection-string options. It is
///     structurally incapable of reaching Jwt:SigningKey or ConnectionStrings:* because
///     it holds no object graph containing them (enforced by
///     ConfigControllerDependencySurfaceTests.cs).
///  3. BrandingOptions itself is a closed, WP-7-owned class boundary (see its own doc
///     comment) -- any genuinely secret field must live in a different options class.
///
/// [AllowAnonymous]: deliberately reachable pre-login and outside both the
/// platform-admin and garage-tenant authenticated domains -- a future login page needs
/// the product name/logo before authentication exists, and this endpoint carries no
/// tenant/garage context of any kind.
/// </summary>
[ApiController]
[Route("api/config")]
[AllowAnonymous]
public sealed class ConfigController(IOptions<BrandingOptions> brandingOptions) : ControllerBase
{
    private readonly BrandingOptions _branding = brandingOptions.Value;

    [HttpGet("branding")]
    [ProducesResponseType(typeof(BrandingConfigResponse), StatusCodes.Status200OK)]
    public ActionResult<BrandingConfigResponse> GetBranding()
    {
        var response = new BrandingConfigResponse(
            ProductDisplayName: _branding.ProductDisplayName,
            EmailFromName: _branding.EmailFromName,
            LogoUrl: _branding.LogoUrl,
            SupportEmail: _branding.SupportEmail);

        return Ok(response);
    }
}
