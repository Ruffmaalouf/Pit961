using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GarageOS.Infrastructure.Email;

/// <summary>
/// WP-6 brief. The ONLY class in the codebase permitted to reference the Resend API
/// directly (Decision #8, IEmailService.cs governance comment) -- enforced by
/// scripts/ci/check-no-resend-outside-service.sh, a blocking CI check with its own
/// proven negative test, not a one-off manual grep.
///
/// Deliberately a hand-rolled HttpClient POST against Resend's documented "Send Email"
/// REST endpoint, not a third-party Resend SDK NuGet package -- no such package exists
/// anywhere in this repo's dependency graph, and adding one would mean trusting an
/// unfamiliar wrapper's own type surface/versioning for a Phase 1 scope this narrow (two
/// email shapes, one endpoint). This choice also makes the "Resend SDK usage cannot leak
/// outside ResendEmailService" acceptance criterion mechanically true: there is no SDK
/// type to leak, only this file's own literal endpoint/request/auth shape.
///
/// Registered as a typed HttpClient (see Program.cs's AddHttpClient&lt;IEmailService,
/// ResendEmailService&gt; call) -- IHttpClientFactory-managed handler lifetime/pooling,
/// not a hand-constructed HttpClient.
///
/// Error handling: on a non-2xx Resend response, this class logs metadata (recipient,
/// subject, HTTP status -- NEVER the API key, NEVER the email body/reset-link content)
/// and then lets the exception propagate. It does NOT swallow failures itself --
/// PasswordResetRequestBackgroundService (this class's only Phase 1 caller, via
/// AuthService.ProcessForgotPasswordRequestAsync) already catches and logs per-item
/// exceptions as a deliberate at-most-once tradeoff (see that file's own doc comment);
/// swallowing here too would make failures silently invisible at both layers.
/// </summary>
public sealed class ResendEmailService(
    HttpClient httpClient,
    IOptions<ResendOptions> resendOptions,
    IOptions<BrandingOptions> brandingOptions,
    ILogger<ResendEmailService> logger) : IEmailService
{
    /// <summary>Public (not internal) so Program.cs's AddHttpClient registration lambda
    /// can reference it without needing an InternalsVisibleTo cross-assembly grant --
    /// this is a base API host, not a secret, so public exposure is harmless. Keeping the
    /// literal string defined in exactly one place (here) rather than duplicated in
    /// Program.cs is what matters for the isolation guarantee, not its visibility
    /// modifier.</summary>
    public const string ResendApiBaseUrl = "https://api.resend.com/";

    private const string PasswordResetSubject = "Reset your password";

    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
        => SendAsync(toEmail, PasswordResetSubject, BuildPasswordResetHtml(resetLink), ct);

    public Task SendTransactionalAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        => SendAsync(toEmail, subject, htmlBody, ct);

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var fromName = brandingOptions.Value.EmailFromName;
        var fromAddress = resendOptions.Value.FromAddress;

        var payload = new ResendSendEmailRequest(
            From: $"{fromName} <{fromAddress}>",
            To: [toEmail],
            Subject: subject,
            Html: htmlBody);

        // Security Reviewer (WP-6 security gate) noted: the Authorization header must be
        // set per-HttpRequestMessage, never on the injected HttpClient's shared
        // DefaultRequestHeaders. Setting it on DefaultRequestHeaders was safe under
        // today's only call pattern (AddHttpClient yields a fresh client per resolution;
        // AuthService is scoped, one email per background-queue item) but would race if a
        // future caller ever held a long-lived/singleton reference to this IEmailService
        // and issued concurrent sends. Building the request explicitly closes this by
        // construction, not by convention.
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resendOptions.Value.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "ResendEmailService: request to Resend failed before a response was received " +
                "(recipient omitted from this log line; see PasswordResetRequestBackgroundService " +
                "for how this is handled by the sole Phase 1 caller).");
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "ResendEmailService: Resend API returned {StatusCode} for subject {Subject}. " +
                "Recipient and email body are intentionally omitted from this log line.",
                (int)response.StatusCode, subject);
            response.EnsureSuccessStatusCode();
        }
    }

    private static string BuildPasswordResetHtml(string resetLink)
    {
        var encodedLink = System.Net.WebUtility.HtmlEncode(resetLink);
        return $"""
            <p>We received a request to reset your password.</p>
            <p><a href="{encodedLink}">Click here to reset your password</a></p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            <p>This link will expire soon and can only be used once.</p>
            """;
    }

    /// <summary>Resend's documented "Send Email" request body shape -- deliberately a
    /// private record, not a shared/public DTO, so nothing outside this file can construct
    /// (or come to depend on) a Resend-shaped payload.</summary>
    private sealed record ResendSendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);
}
