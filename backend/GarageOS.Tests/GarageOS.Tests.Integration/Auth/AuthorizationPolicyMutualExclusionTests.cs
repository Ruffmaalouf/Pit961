using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Domain.Platform;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Tests.Integration.Auth;

/// <summary>
/// WP-4 brief §6/§17: platform-admin and garage-tenant tokens must be mutually exclusive
/// under authorization -- a platform-admin token fails "GarageTenant" and a garage-tenant
/// token fails "PlatformAdminOnly". Asserted two ways per brief §17: directly via
/// IAuthorizationService.AuthorizeAsync against a ClaimsPrincipal built from the REAL
/// minted token's own claims (proving TestJwtTokenFactory/ITokenService produce the
/// expected claim shape), and via a representative live endpoint (GET /api/v1/auth/me,
/// gated by the "GarageTenant" policy) returning 403 for a platform-admin token.
/// </summary>
[Collection("Integration")]
public class AuthorizationPolicyMutualExclusionTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task GarageTenantPolicy_PlatformAdminToken_Fails()
    {
        var principal = MintPrincipal(isPlatformAdmin: true);
        var authorizationService = fixture.Services.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(principal, resource: null, policyName: "GarageTenant");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task PlatformAdminOnlyPolicy_GarageTenantToken_Fails()
    {
        var principal = MintPrincipal(isPlatformAdmin: false);
        var authorizationService = fixture.Services.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(principal, resource: null, policyName: "PlatformAdminOnly");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GarageTenantPolicy_GarageTenantToken_Succeeds()
    {
        var principal = MintPrincipal(isPlatformAdmin: false);
        var authorizationService = fixture.Services.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(principal, resource: null, policyName: "GarageTenant");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task PlatformAdminOnlyPolicy_PlatformAdminToken_Succeeds()
    {
        var principal = MintPrincipal(isPlatformAdmin: true);
        var authorizationService = fixture.Services.GetRequiredService<IAuthorizationService>();

        var result = await authorizationService.AuthorizeAsync(principal, resource: null, policyName: "PlatformAdminOnly");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task LiveGarageTenantEndpoint_PlatformAdminToken_Returns403()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var platformAdminToken = TestJwtTokenFactory.CreatePlatformAdminToken(
            tokenService, new PlatformAdmin { Email = "admin@example.test", PasswordHash = "n/a" });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformAdminToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LiveGarageTenantEndpoint_GarageTenantToken_IsNotRejectedByPolicy()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        var response = await client.SendAsync(request);

        // Never 403 -- a real garage-tenant token satisfies the "GarageTenant" policy.
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Mints a real token via the production ITokenService (resolved from the
    /// test host's own DI container, same as TestJwtTokenFactory), then decodes its own
    /// claims back into a ClaimsPrincipal -- so the AuthorizeAsync assertions above are
    /// checking the actual claim shape ITokenService produces, not a hand-authored
    /// stand-in.</summary>
    private ClaimsPrincipal MintPrincipal(bool isPlatformAdmin)
    {
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var rawToken = isPlatformAdmin
            ? TestJwtTokenFactory.CreatePlatformAdminToken(tokenService, new PlatformAdmin { Email = "admin@example.test", PasswordHash = "n/a" })
            : TestJwtTokenFactory.CreateGarageTenantToken(tokenService, new GarageOS.Domain.Entities.User
            {
                GarageId = Guid.NewGuid(),
                Email = "tenant@example.test",
                PasswordHash = "n/a",
                Name = "Tenant User",
                Role = "owner",
            });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "TestBearer");
        return new ClaimsPrincipal(identity);
    }
}
