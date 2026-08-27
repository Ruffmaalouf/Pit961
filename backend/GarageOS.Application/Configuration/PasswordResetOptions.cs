namespace GarageOS.Application.Configuration;

/// <summary>Minimal, non-secret configuration for building the link embedded in a
/// password-reset email (WP-4 brief §12 "reset-password"). The real value only matters
/// once WP-8's frontend exists to host a /reset-password route and WP-6's real
/// IEmailService actually delivers mail -- until then, NoOpEmailService merely logs the
/// link. Deliberately its own tiny options class rather than folded into JwtOptions
/// (unrelated concern) or BrandingOptions (WP-7's, not read here per WP-4 brief §3's
/// config-ownership note).</summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public string ResetLinkBaseUrl { get; set; } = "https://app.garageos.example/reset-password";
    public int TokenLifetimeMinutes { get; set; } = 45;
}
