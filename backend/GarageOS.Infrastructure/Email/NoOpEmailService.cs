using GarageOS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GarageOS.Infrastructure.Email;

/// <summary>
/// WP-4 brief §14 originally registered this as the temporary stub until WP-6 supplied
/// ResendEmailService. WP-6 has now landed and Program.cs registers ResendEmailService by
/// default in every environment -- this class is kept, unregistered, as a live,
/// documented, second IEmailService implementation: concrete proof that "swapping in a
/// no-op/fake implementation requires zero changes to calling code" (13_phase1_execution_
/// plan.md WP-6 acceptance criterion) holds at the production-code level, not only via
/// GarageOS.Tests.Integration's test-only CapturingEmailService. Useful as a manual
/// local-dev fallback (swap the Program.cs registration back to this class) when no real
/// Resend:ApiKey is configured. Logs the would-be send at Information level and no-ops.
/// Never references the Resend SDK/API -- ResendEmailService is the only class permitted
/// to (Decision #8), and this class is not it.
/// </summary>
public sealed class NoOpEmailService(ILogger<NoOpEmailService> logger) : IEmailService
{
    public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "NoOpEmailService: would send password-reset email to {ToEmail} with link {ResetLink}.",
            toEmail, resetLink);
        return Task.CompletedTask;
    }

    public Task SendTransactionalAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation(
            "NoOpEmailService: would send transactional email to {ToEmail} with subject {Subject}.",
            toEmail, subject);
        return Task.CompletedTask;
    }
}
