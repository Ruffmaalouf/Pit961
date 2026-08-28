using System.Net;
using GarageOS.Application.Configuration;
using GarageOS.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GarageOS.Tests.Unit.Email;

/// <summary>
/// WP-6 brief. Unit-level proof of ResendEmailService's request shape, branding
/// sourcing, error propagation, and log-content hygiene -- no real network call, no DI
/// container. A fake HttpMessageHandler captures the outgoing HttpRequestMessage so
/// these tests can assert on it directly, matching this project's existing convention of
/// preferring real, direct exercise over mocking frameworks where practical.
/// </summary>
public class ResendEmailServiceTests
{
    private static (ResendEmailService Service, CapturingHandler Handler) CreateService(
        HttpStatusCode responseStatusCode = HttpStatusCode.OK,
        string emailFromName = "Test Garage Co",
        string resendApiKey = "re_test_key",
        string fromAddress = "no-reply@example.test")
    {
        var handler = new CapturingHandler(responseStatusCode);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(ResendEmailService.ResendApiBaseUrl) };

        var resendOptions = Options.Create(new ResendOptions { ApiKey = resendApiKey, FromAddress = fromAddress });
        var brandingOptions = Options.Create(new BrandingOptions { EmailFromName = emailFromName });

        var service = new ResendEmailService(httpClient, resendOptions, brandingOptions, NullLogger<ResendEmailService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task SendPasswordResetAsync_SendsCorrectRecipientAndSubjectAndHtmlBody()
    {
        var (service, handler) = CreateService();

        await service.SendPasswordResetAsync("user@example.test", "https://app.example.test/reset?token=abc123");

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("\"user@example.test\"", handler.LastRequestBody);
        Assert.Contains("Reset your password", handler.LastRequestBody);
        Assert.Contains("https://app.example.test/reset?token=abc123", handler.LastRequestBody);
        Assert.Equal(new Uri(ResendEmailService.ResendApiBaseUrl + "emails"), handler.LastRequestUri);
        Assert.Equal("Bearer", handler.LastAuthorizationScheme);
        Assert.Equal("re_test_key", handler.LastAuthorizationParameter);
    }

    [Fact]
    public async Task SendPasswordResetAsync_UsesBrandingOptions_EmailFromName()
    {
        var (service, handler) = CreateService(emailFromName: "A Distinctive Test Brand Name");

        await service.SendPasswordResetAsync("user@example.test", "https://app.example.test/reset?token=abc123");

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("A Distinctive Test Brand Name", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendTransactionalAsync_SendsExactSubjectAndHtmlBodyPassedByCaller()
    {
        var (service, handler) = CreateService();

        await service.SendTransactionalAsync(
            "user@example.test", "A Caller-Supplied Subject", "<p>A caller-supplied body.</p>");

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("A Caller-Supplied Subject", handler.LastRequestBody);
        Assert.Contains("A caller-supplied body.", handler.LastRequestBody);
        // Proves this method does NOT apply the password-reset template.
        Assert.DoesNotContain("Reset your password", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendPasswordResetAsync_OnResendApiFailure_ThrowsAndDoesNotSwallow()
    {
        var (service, _) = CreateService(responseStatusCode: HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SendPasswordResetAsync("user@example.test", "https://app.example.test/reset?token=abc123"));
    }

    [Fact]
    public async Task SendPasswordResetAsync_OnSuccess_NeverLogsApiKeyOrResetLinkAtAnyLevel()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(ResendEmailService.ResendApiBaseUrl) };
        var resendOptions = Options.Create(new ResendOptions { ApiKey = "re_super_secret_key_value", FromAddress = "no-reply@example.test" });
        var brandingOptions = Options.Create(new BrandingOptions { EmailFromName = "Test Garage Co" });
        var capturingLogger = new CapturingLogger();

        var service = new ResendEmailService(httpClient, resendOptions, brandingOptions, capturingLogger);
        var resetLink = "https://app.example.test/reset?token=super-secret-single-use-token";

        await service.SendPasswordResetAsync("user@example.test", resetLink);

        Assert.DoesNotContain(capturingLogger.LoggedMessages, m => m.Contains("re_super_secret_key_value"));
        Assert.DoesNotContain(capturingLogger.LoggedMessages, m => m.Contains(resetLink));
        Assert.DoesNotContain(capturingLogger.LoggedMessages, m => m.Contains("super-secret-single-use-token"));
    }

    [Fact]
    public async Task SendPasswordResetAsync_OnFailure_LogsStatusMetadata_ButNeverLogsApiKeyOrResetLink()
    {
        var handler = new CapturingHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(ResendEmailService.ResendApiBaseUrl) };
        var resendOptions = Options.Create(new ResendOptions { ApiKey = "re_super_secret_key_value", FromAddress = "no-reply@example.test" });
        var brandingOptions = Options.Create(new BrandingOptions { EmailFromName = "Test Garage Co" });
        var capturingLogger = new CapturingLogger();

        var service = new ResendEmailService(httpClient, resendOptions, brandingOptions, capturingLogger);
        var resetLink = "https://app.example.test/reset?token=super-secret-single-use-token";

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SendPasswordResetAsync("user@example.test", resetLink));

        // The failure path DOES log (status metadata, to aid debugging) -- but even then,
        // never the secret key or the reset link/token value.
        Assert.NotEmpty(capturingLogger.LoggedMessages);
        Assert.DoesNotContain(capturingLogger.LoggedMessages, m => m.Contains("re_super_secret_key_value"));
        Assert.DoesNotContain(capturingLogger.LoggedMessages, m => m.Contains(resetLink));
        Assert.DoesNotContain(capturingLogger.LoggedMessages, m => m.Contains("super-secret-single-use-token"));
    }

    /// <summary>Captures the outgoing request (URI, Authorization header, JSON body) and
    /// returns a canned status code -- no real network call.</summary>
    private sealed class CapturingHandler(HttpStatusCode responseStatusCode) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }
        public string? LastAuthorizationScheme { get; private set; }
        public string? LastAuthorizationParameter { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastAuthorizationScheme = request.Headers.Authorization?.Scheme;
            LastAuthorizationParameter = request.Headers.Authorization?.Parameter;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(responseStatusCode)
            {
                Content = new StringContent("{}"),
            };
        }
    }

    /// <summary>Minimal ILogger&lt;T&gt; test double capturing every formatted log
    /// message string, across all log levels -- used to prove no log line ever contains
    /// a secret/token value, not just to assert a specific expected message.</summary>
    private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger<ResendEmailService>
    {
        public List<string> LoggedMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(formatter(state, exception));
        }
    }
}
