namespace GarageOS.Application.Configuration;

/// <summary>
/// WP-2 demonstration options class. Exists solely to prove the strongly-typed
/// options / configuration-binding pattern (<see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>)
/// works end-to-end for later work packages to follow (e.g. JwtOptions in WP-4,
/// BrandingOptions in WP-7). This class intentionally carries no secret or
/// brand-specific values — see DECISIONS.md #6 and 13_phase1_execution_plan.md WP-2.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>A non-secret, non-brand value read from configuration, used to prove
    /// that changing appsettings.json (or an environment-variable override) is reflected
    /// at runtime rather than hardcoded.</summary>
    public string Message { get; set; } = string.Empty;
}

// WP-9 negative-gate proof (temporary, never merged to main): deliberately
// reintroduces a Resend API host reference outside ResendEmailService.cs to
// prove the CI grep gate actually blocks it. Removed immediately after the
// CI failure is confirmed. https://api.resend.com/emails
