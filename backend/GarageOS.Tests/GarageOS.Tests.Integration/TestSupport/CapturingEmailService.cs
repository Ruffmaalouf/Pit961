using GarageOS.Application.Abstractions;

namespace GarageOS.Tests.Integration.TestSupport;

/// <summary>
/// Test-only IEmailService replacing Program.cs's NoOpEmailService registration for the
/// "Testing" environment (see IntegrationTestFixture.ConfigureWebHost). Registered as a
/// SINGLETON so the same instance is observed regardless of which DI scope sends -- the
/// forgot-password background consumer (WP-4 brief §13) creates its own child scope via
/// IServiceScopeFactory, so a Scoped registration would be invisible to a test resolving
/// IEmailService from the fixture's root provider.
/// </summary>
public sealed class CapturingEmailService : IEmailService
{
    public string? LastToEmail { get; private set; }
    public string? LastResetLink { get; private set; }

    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        LastToEmail = toEmail;
        LastResetLink = resetLink;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        LastToEmail = null;
        LastResetLink = null;
    }

    /// <summary>Polls for the background consumer to finish processing a queued
    /// forgot-password request (WP-4 brief §13's whole point is that this happens off the
    /// HTTP request/response path, asynchronously) -- there is no synchronous signal to
    /// await instead.</summary>
    public async Task<bool> WaitForSendAsync(TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (LastResetLink is null && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
        return LastResetLink is not null;
    }
}
