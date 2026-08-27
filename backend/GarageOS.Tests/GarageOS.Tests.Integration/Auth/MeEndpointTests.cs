using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Domain.Platform;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Tests.Integration.Auth;

[Collection("Integration")]
public class MeEndpointTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Me_ValidGarageTenantAccessToken_ReturnsTenantScopedUserData()
    {
        await fixture.ResetDatabaseAsync();
        var (_, garage, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture, role: "owner", garageName: "Me Endpoint Garage");
        var client = fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        var meResponse = await client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Equal(user.Id, me!.Id);
        Assert.Equal(garage.Id, me.GarageId);
        Assert.Equal("Me Endpoint Garage", me.GarageName);
        Assert.Equal(user.Email, me.Email);
        Assert.Equal("owner", me.Role);
    }

    [Fact]
    public async Task Me_PlatformAdminToken_ReturnsForbidden()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var platformAdminToken = TestJwtTokenFactory.CreatePlatformAdminToken(
            tokenService, new PlatformAdmin { Email = "admin@example.test", PasswordHash = "n/a" });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformAdminToken);
        var response = await client.SendAsync(request);

        // Platform-admin tokens carry no garage_id -- the "GarageTenant" policy gate
        // rejects with 403 (a clean policy failure), never a 500 from
        // HttpContextCurrentTenant.GarageId throwing downstream (WP-4 brief §6).
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Me_NoBearerToken_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
