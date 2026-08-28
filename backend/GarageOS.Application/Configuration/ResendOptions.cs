namespace GarageOS.Application.Configuration;

/// <summary>
/// WP-6 brief. Bound from the "Resend" configuration section with mandatory
/// ValidateOnStart() (see Program.cs) -- a missing ApiKey crashes the app at boot in
/// EVERY environment, never a silent empty-key fallback. Mirrors JwtOptions.cs's
/// secrets-handling pattern exactly (not BrandingOptions.cs's no-validation pattern --
/// ApiKey is a real secret, unlike any BrandingOptions field).
///
/// Never committed to appsettings.json/appsettings.Development.json (both carry only an
/// empty placeholder, matching the existing ConnectionStrings/Jwt convention). Local dev:
/// `dotnet user-secrets` or a gitignored appsettings.Local.json. CI/Production:
/// environment variable (`Resend__ApiKey`) / host secret store only -- never hand-typed
/// into a committed file. Test host (GarageOS.Tests.Integration): appsettings.Testing.json
/// carries a fixed, obviously-fake, non-secret test-only placeholder key -- safe to commit
/// because ResendEmailService is never actually constructed or called in the Testing host
/// (IntegrationTestFixture.ConfigureWebHost removes IEmailService's registration and
/// substitutes CapturingEmailService before the host finishes building), so this value
/// only needs to satisfy ValidateOnStart(), never to authenticate against a real API.
///
/// ResendEmailService is the ONLY class in the codebase permitted to reference the Resend
/// API/SDK directly (Decision #8, IEmailService.cs governance comment) -- enforced by
/// scripts/ci/check-no-resend-outside-service.sh, a blocking CI check, not a one-off
/// manual grep.
/// </summary>
public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>Resend API key. A real secret -- see the class remarks above for the full
    /// per-environment provisioning story. Never logged, never emitted by any runtime
    /// config endpoint (contrast with BrandingOptions, which IS deliberately exposed via
    /// GET /api/config/branding -- ResendOptions must never be).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The verified sending address used as the email portion of the "From"
    /// header (paired with BrandingOptions.EmailFromName for the display-name portion).
    /// Not secret, but not yet a decided production value -- left an empty placeholder in
    /// appsettings.json/appsettings.Development.json, non-fatal if blank (a blank
    /// FromAddress is a delivery-configuration issue, not a security hole, so it is not
    /// ValidateOnStart()-guarded the way ApiKey is).</summary>
    public string FromAddress { get; set; } = string.Empty;
}
