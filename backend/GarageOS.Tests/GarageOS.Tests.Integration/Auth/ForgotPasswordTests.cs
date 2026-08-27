using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Tests.Integration.TestSupport;

namespace GarageOS.Tests.Integration.Auth;

/// <summary>
/// WP-4 brief §13 anti-enumeration proof. The HTTP request path
/// (AuthController.ForgotPassword) must return an IDENTICAL 202 regardless of whether the
/// target email exists, and must do ZERO user-existence-dependent work on that path --
/// all of that happens later, off-request, in PasswordResetRequestBackgroundService. The
/// only way to observe whether the background consumer actually ran is
/// IntegrationTestFixture.CapturedEmails (replaces NoOpEmailService for this host, see
/// IntegrationTestFixture.ConfigureWebHost).
/// </summary>
[Collection("Integration")]
public class ForgotPasswordTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ForgotPassword_ExistingActiveUser_Returns202AndEventuallySendsResetLink()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture);
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(user.Email));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var sent = await fixture.CapturedEmails.WaitForSendAsync(TimeSpan.FromSeconds(5));
        Assert.True(sent, "Background consumer did not process the queued forgot-password request in time.");
        Assert.Equal(user.Email, fixture.CapturedEmails.LastToEmail);
        Assert.Contains("token=", fixture.CapturedEmails.LastResetLink);
    }

    [Fact]
    public async Task ForgotPassword_UnknownButWellFormedEmail_ReturnsIdentical202AndSendsNothing()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var client = fixture.CreateClient();
        var unknownEmail = $"nobody+{Guid.NewGuid():N}@example.test";

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(unknownEmail));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Give the background consumer a fair chance to (not) act -- it enqueues, looks
        // the email up, finds nothing, and returns without ever calling IEmailService.
        var sent = await fixture.CapturedEmails.WaitForSendAsync(TimeSpan.FromMilliseconds(500));
        Assert.False(sent);
        Assert.Null(fixture.CapturedEmails.LastResetLink);
    }

    [Fact]
    public async Task ForgotPassword_MalformedEmail_ReturnsIdentical202AsWellFormedEmail()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var client = fixture.CreateClient();

        // Malformed -- fails AuthController's format regex before the queue is ever
        // touched, but must still be indistinguishable from every other case (brief §13:
        // "the HTTP request path does ZERO user-existence-dependent work").
        var malformedResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest("not-an-email"));
        var wellFormedResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest($"nobody+{Guid.NewGuid():N}@example.test"));

        Assert.Equal(HttpStatusCode.Accepted, malformedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, wellFormedResponse.StatusCode);
        var malformedBody = await malformedResponse.Content.ReadAsStringAsync();
        var wellFormedBody = await wellFormedResponse.Content.ReadAsStringAsync();
        Assert.Equal(malformedBody, wellFormedBody);
    }

    [Fact]
    public async Task ForgotPassword_InactiveUser_Returns202ButSendsNothing()
    {
        await fixture.ResetDatabaseAsync();
        fixture.CapturedEmails.Reset();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture, isActive: false);
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(user.Email));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var sent = await fixture.CapturedEmails.WaitForSendAsync(TimeSpan.FromMilliseconds(500));
        Assert.False(sent);
    }
}
