using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Auth;
using GarageOS.Application.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GarageOS.Infrastructure.Security;

/// <summary>
/// WP-4 brief §3/§4/§6. The one authoritative JWT-issuance path -- both the real login/
/// refresh path and TestJwtTokenFactory (test-only, §17) call through this same
/// production class, constructed with the real (or test-host) JwtOptions. HS256, single
/// configured Issuer/Audience for both token shapes (Decision #6); mutual exclusion
/// between garage-tenant and platform-admin tokens is entirely a claims-presence
/// difference, enforced downstream by GarageTenantRequirement/PlatformAdminRequirement.
/// </summary>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public IssuedAccessToken IssueGarageTenantAccessToken(UserAuthRecord user)
    {
        // Deliberately small claim set: sub, garage_id, role, jti + standard claims.
        // Email/name/avatar are excluded on purpose -- /me does a live DB read for those
        // (WP-4 brief §6).
        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("garage_id", user.GarageId.ToString()),
            new Claim("role", user.Role),
            new Claim("jti", Guid.NewGuid().ToString()),
        };
        return Issue(claims);
    }

    public IssuedAccessToken IssuePlatformAdminAccessToken(Guid platformAdminId)
    {
        // Deliberately absent: garage_id, role. This is the mechanism (not an oversight)
        // that makes HttpContextCurrentTenant throw for a platform-admin token, realizing
        // Decision #7's "fails every garage-tenant boundary check by construction"
        // (WP-4 brief §6). Test-only construction in Phase 1 -- see brief §0/§17; no live
        // route ever calls this.
        var claims = new[]
        {
            new Claim("sub", platformAdminId.ToString()),
            new Claim("platform_admin", "true"),
            new Claim("jti", Guid.NewGuid().ToString()),
        };
        return Issue(claims);
    }

    private IssuedAccessToken Issue(Claim[] claims)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey))
        {
            KeyId = _options.KeyId,
        };
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedAccessToken(accessToken, expiresAt);
    }
}
