using GarageOS.Application.Abstractions;
using GarageOS.Application.Auth;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Platform;

namespace GarageOS.Tests.Integration.TestSupport;

/// <summary>
/// WP-4 brief §17. Replaces TestAuthHandler (retired). Calls the REAL production
/// ITokenService (resolved from the test host's own DI container, constructed with the
/// test host's real JwtOptions from appsettings.Testing.json) -- not a hand-rolled
/// parallel token builder. A test that wants a garage-tenant or platform-admin bearer
/// token gets one that is byte-for-byte what a real login/refresh would produce, so
/// tampering/validation tests are meaningful (a spoofed claim requires forging a valid
/// signature against the real test-host signing key, not just setting a header).
/// </summary>
public static class TestJwtTokenFactory
{
    public static string CreateGarageTenantToken(ITokenService tokenService, User user, string garageName = "Test Garage")
    {
        var record = new UserAuthRecord(
            user.Id, user.GarageId, garageName, user.Email, user.PasswordHash,
            user.Name, user.Role, user.IsActive, user.FailedLoginAttempts, user.LockoutEndAt);
        return tokenService.IssueGarageTenantAccessToken(record).AccessToken;
    }

    public static string CreatePlatformAdminToken(ITokenService tokenService, PlatformAdmin admin) =>
        tokenService.IssuePlatformAdminAccessToken(admin.Id).AccessToken;
}
