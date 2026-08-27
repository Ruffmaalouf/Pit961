using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Tests.Integration.TestSupport;

namespace GarageOS.Tests.Integration.Auth;

[Collection("Integration")]
public class LogoutTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Logout_WithValidRefreshCookie_Returns204AndRevokesToken()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var refreshCookie = CookieTestHelpers.ExtractCookieValue(loginResponse, "garageos_refresh_token")!;

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Add("Cookie", refreshCookie);
        var logoutResponse = await client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // The now-revoked token must not work for a subsequent refresh.
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);
        var refreshResponse = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithNoCookie_IsIdempotentAndReturns204()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();

        // No prior login at all -- brief §12: logout is idempotent, always 204, even
        // with nothing to revoke.
        var response = await client.PostAsync("/api/v1/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_CalledTwiceWithSameCookie_BothReturn204()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var refreshCookie = CookieTestHelpers.ExtractCookieValue(loginResponse, "garageos_refresh_token")!;

        using var firstLogout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        firstLogout.Headers.Add("Cookie", refreshCookie);
        var firstResponse = await client.SendAsync(firstLogout);

        using var secondLogout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        secondLogout.Headers.Add("Cookie", refreshCookie);
        var secondResponse = await client.SendAsync(secondLogout);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);
    }
}
