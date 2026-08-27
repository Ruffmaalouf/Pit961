using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.Auth;

[Collection("Integration")]
public class ResetPasswordTests(IntegrationTestFixture fixture)
{
    private const string NewPassword = "BrandNewPassword1";

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPasswordAndRevokesAllSessions()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        // Establish an active session BEFORE the reset, so we can prove it gets revoked.
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        var refreshCookie = CookieTestHelpers.ExtractCookieValue(loginResponse, "garageos_refresh_token")!;

        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(user.Email));
        var sent = await fixture.CapturedEmails.WaitForSendAsync(TimeSpan.FromSeconds(5));
        Assert.True(sent, "Background consumer did not process the forgot-password request in time.");
        var token = ExtractTokenFromResetLink(fixture.CapturedEmails.LastResetLink!);

        var resetResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, NewPassword));
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        // Old password no longer works.
        var oldPasswordAttempt = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordAttempt.StatusCode);

        // New password works.
        var newPasswordAttempt = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email, NewPassword));
        Assert.Equal(HttpStatusCode.OK, newPasswordAttempt.StatusCode);

        // Security-sensitive event -- does NOT auto-login and revokes every active
        // session, including the one established before the reset (brief §12).
        using var preResetRefresh = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        preResetRefresh.Headers.Add("Cookie", refreshCookie);
        var preResetRefreshResponse = await client.SendAsync(preResetRefresh);
        Assert.Equal(HttpStatusCode.Unauthorized, preResetRefreshResponse.StatusCode);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var stillActiveCount = await db.RefreshTokens.CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
        Assert.Equal(0, stillActiveCount);
    }

    [Fact]
    public async Task ResetPassword_TokenAlreadyUsed_ReturnsUniform400()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var token = await RequestAndCaptureResetTokenAsync(client, user.Email);

        var firstUse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, NewPassword));
        Assert.Equal(HttpStatusCode.OK, firstUse.StatusCode);

        var secondUse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, "AnotherNewPassword1"));
        Assert.Equal(HttpStatusCode.BadRequest, secondUse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsUniform400()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var token = await RequestAndCaptureResetTokenAsync(client, user.Email);

        // Force expiry directly -- waiting out the real 45-minute lifetime is not viable
        // in a test; this exercises exactly the same ExpiresAt check ResetPasswordAsync
        // performs regardless of how the row got there.
        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            var tokenHash = GarageOS.Application.Common.OpaqueTokenGenerator.Hash(token);
            var row = await db.PasswordResetTokens.SingleAsync(t => t.TokenHash == tokenHash);
            row.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, NewPassword));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnknownToken_ReturnsIdentical400AsUsedAndExpiredTokens()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var usedToken = await RequestAndCaptureResetTokenAsync(client, user.Email);
        await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(usedToken, NewPassword));
        var usedResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(usedToken, "SomeOtherPassword1"));

        var neverIssuedResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest("this-token-was-never-issued", "SomeOtherPassword1"));

        Assert.Equal(HttpStatusCode.BadRequest, usedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, neverIssuedResponse.StatusCode);
        var usedBody = await usedResponse.Content.ReadAsStringAsync();
        var neverIssuedBody = await neverIssuedResponse.Content.ReadAsStringAsync();
        Assert.Equal(usedBody, neverIssuedBody);
    }

    [Theory]
    [InlineData("Short1")] // below 10-char minimum
    public async Task ResetPassword_PasswordTooShort_ReturnsBadRequest(string tooShortPassword)
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var token = await RequestAndCaptureResetTokenAsync(client, user.Email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, tooShortPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_PasswordTooLong_ReturnsBadRequest()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var token = await RequestAndCaptureResetTokenAsync(client, user.Email);
        var tooLongPassword = new string('a', 129); // above the 128-char maximum

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, tooLongPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Boundary-value coverage (QA finding, WP-4 post-implementation review): the two
    // "too short"/"too long" tests above only prove values OUTSIDE the 10-128 range are
    // rejected -- they don't catch an off-by-one in AuthService.ResetPasswordAsync's
    // `< MinPasswordLength`/`> MaxPasswordLength` checks (e.g. `<=`/`>=` would silently
    // reject the valid boundary values themselves). These two prove exactly-10 and
    // exactly-128 succeed.
    [Fact]
    public async Task ResetPassword_PasswordExactlyAtMinimumLength_Succeeds()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var token = await RequestAndCaptureResetTokenAsync(client, user.Email);
        var exactlyMinLengthPassword = new string('a', 10);

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, exactlyMinLengthPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_PasswordExactlyAtMaximumLength_Succeeds()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var token = await RequestAndCaptureResetTokenAsync(client, user.Email);
        var exactlyMaxLengthPassword = new string('a', 128);

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(token, exactlyMaxLengthPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> RequestAndCaptureResetTokenAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email));
        var sent = await fixture.CapturedEmails.WaitForSendAsync(TimeSpan.FromSeconds(5));
        Assert.True(sent, "Background consumer did not process the forgot-password request in time.");
        return ExtractTokenFromResetLink(fixture.CapturedEmails.LastResetLink!);
    }

    private static string ExtractTokenFromResetLink(string resetLink)
    {
        const string marker = "token=";
        var tokenIndex = resetLink.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(tokenIndex >= 0, $"Reset link did not contain a token query parameter: {resetLink}");
        return Uri.UnescapeDataString(resetLink[(tokenIndex + marker.Length)..]);
    }
}
