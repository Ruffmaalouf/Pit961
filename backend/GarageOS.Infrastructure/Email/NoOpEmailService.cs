using GarageOS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GarageOS.Infrastructure.Email;

/// <summary>
/// Explicitly-temporary stub (WP-4 brief §14) registered until WP-6 supplies
/// ResendEmailService against the same IEmailService contract. Logs the would-be send at
/// Information level (useful during Phase 1 dev/test for eyeballing reset links) and
/// no-ops. Never references the Resend SDK -- ResendEmailService is the only class
/// permitted to (Decision #8), and this class is not it.
/// </summary>
public sealed class NoOpEmailService(ILogger<NoOpEmailService> logger) : IEmailService
{
    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "NoOpEmailService: would send password-reset email to {ToEmail} with link {ResetLink} " +
            "(WP-6 has not yet supplied a real IEmailService implementation).",
            toEmail, resetLink);
        return Task.CompletedTask;
    }
}
