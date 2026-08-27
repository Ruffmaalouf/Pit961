using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.Auth;

[Collection("Integration")]
public class RefreshTokenTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewAccessToken()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var refreshCookie = CookieTestHelpers.ExtractCookieValue(loginResponse, "garageos_refresh_token")!;

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);
        var refreshResponse = await client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var body = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));

        // Rotation issued a NEW refresh cookie, distinct from the login one.
        var newCookie = CookieTestHelpers.ExtractCookieValue(refreshResponse, "garageos_refresh_token");
        Assert.NotNull(newCookie);
        Assert.NotEqual(refreshCookie, newCookie);
    }

    [Fact]
    public async Task Refresh_ReusedAlreadyRotatedToken_RevokesAllActiveSessionsForUser()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var firstRefreshCookie = CookieTestHelpers.ExtractCookieValue(loginResponse, "garageos_refresh_token")!;

        // Legitimate rotation: R1 -> R2.
        using var firstRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        firstRefreshRequest.Headers.Add("Cookie", firstRefreshCookie);
        var firstRefreshResponse = await client.SendAsync(firstRefreshRequest);
        var secondCookie = CookieTestHelpers.ExtractCookieValue(firstRefreshResponse, "garageos_refresh_token")!;
        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);

        // Reuse of the now-revoked R1 -- this is the theft signal (WP-4 brief §9).
        using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        reuseRequest.Headers.Add("Cookie", firstRefreshCookie);
        var reuseResponse = await client.SendAsync(reuseRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // Reuse detection must have revoked EVERY active session for the user, including
        // R2, which was legitimately issued and had not itself been presented twice.
        using var secondRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        secondRefreshRequest.Headers.Add("Cookie", secondCookie);
        var secondRefreshResponse = await client.SendAsync(secondRefreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshResponse.StatusCode);

        // Direct DB proof, not just HTTP-observed behavior: every RefreshToken row for
        // this user is revoked.
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var stillActiveCount = await db.RefreshTokens.CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
        Assert.Equal(0, stillActiveCount);
    }

    [Fact]
    public async Task Refresh_NoCookieNoBody_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Regression test for a HIGH finding from WP-4's post-implementation security
    /// review: two concurrent presentations of the SAME still-valid refresh token used
    /// to both pass the "not yet revoked" check before either write landed, both minting
    /// a live session with reuse-detection never firing (see
    /// IRefreshTokenRepository.TryClaimForRotationAsync's remarks and
    /// AuthService.RefreshAsync for the fix -- an atomic conditional UPDATE ... WHERE
    /// RevokedAt IS NULL is now the single race-safe gate). Fires two /refresh requests
    /// truly concurrently (Task.WhenAll, no await between dispatch) and asserts exactly
    /// ONE wins.
    /// </summary>
    [Fact]
    public async Task Refresh_ConcurrentPresentationOfSameToken_ExactlyOneWinsAndReuseDetectionStillFires()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var refreshCookie = CookieTestHelpers.ExtractCookieValue(loginResponse, "garageos_refresh_token")!;

        Task<HttpResponseMessage> SendRefresh()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
            request.Headers.Add("Cookie", refreshCookie);
            return client.SendAsync(request);
        }

        // No await between dispatch -- both requests are in flight against the database
        // at the same time, genuinely racing for the same token row.
        var firstTask = SendRefresh();
        var secondTask = SendRefresh();
        var responses = await Task.WhenAll(firstTask, secondTask);

        var statusCodes = responses.Select(r => r.StatusCode).OrderBy(s => s).ToList();
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Unauthorized], statusCodes); // OrderBy is ascending by the enum's underlying int value: OK=200 < Unauthorized=401

        // Reuse detection must still have fired for the loser: the ORIGINAL token's
        // session, and every other active session for the user, end up revoked --
        // never two simultaneously-live sessions born from one presented token.
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var activeTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ToListAsync();

        // Exactly the winner's newly-issued token may remain active; the loser's
        // just-inserted replacement and the original presented token must both be
        // revoked (see AuthService.RefreshAsync's claim-failure branch).
        Assert.True(activeTokens.Count <= 1,
            $"Expected at most 1 active refresh token after a concurrent-reuse race, found {activeTokens.Count}.");
    }
}
