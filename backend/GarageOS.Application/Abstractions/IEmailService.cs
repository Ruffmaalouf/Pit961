namespace GarageOS.Application.Abstractions;

/// <summary>
/// GOVERNANCE (WP-4 brief §14, Technical Architect required change #3, Decision #8):
/// this interface is architecturally owned by WP-6 (Email / Resend integration), NOT
/// WP-4. WP-4 defines it now, at this exact signature, only because forgot-password is
/// its first Phase 1 caller and WP-6 has not yet landed. If/when WP-6 lands first in a
/// given build, WP-4 depends on WP-6's copy directly instead of redefining it.
/// WP-6's ResendEmailService is the ONLY class in the codebase permitted to reference the
/// Resend SDK (Decision #8) -- until it exists, GarageOS.Infrastructure.Email.NoOpEmailService
/// is registered as an explicitly-temporary stub (logs and no-ops). No caller outside the
/// password-reset background consumer may reference IEmailService in Phase 1.
/// </summary>
public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);

    /// <summary>WP-6 brief §11A/DECISIONS.md (Resend decision): general account-related
    /// transactional email (e.g. a future team invite, trial/subscription status change --
    /// see 11_engineering_handoff.md §44/§45), NOT a Phase 1-only method. No caller exists
    /// yet in this codebase (no invite/trial-notice feature is implemented in Phase 1) --
    /// this method exists as approved infrastructure ahead of its first caller, per the
    /// Owner's WP-6 scope ("password reset + account-related transactional email
    /// infrastructure"), not as scope creep. Do NOT use this for SMS/WhatsApp (out of
    /// Phase 1 scope entirely) or for registration email verification (explicitly
    /// optional/TBD per §11A, no approved requirement exists for it yet).</summary>
    Task SendTransactionalAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
