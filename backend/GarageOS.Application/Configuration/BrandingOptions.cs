namespace GarageOS.Application.Configuration;

/// <summary>
/// WP-7 brief. Non-secret, customer-facing brand configuration. The final product/brand
/// name is undecided -- see DECISIONS.md #6 and 11_engineering_handoff.md §7A. This is
/// the ONLY place brand strings may live; they must never be hardcoded as string
/// literals in UI, email templates, document templates, or component code. WP-7 owns
/// this class and only this class -- do not add JWT, connection-string, or any
/// secret-bearing fields here (see JwtOptions instead). Never derived from or coupled to
/// JwtOptions/Jwt:Issuer/Jwt:Audience in either direction -- see
/// BrandingJwtDecouplingTests.cs, which proves changing these values never changes an
/// issued token's iss/aud claims.
///
/// Deliberately has no per-garage/per-tenant dimension: Phase 1 is a single global
/// brand, matching the existing Demo/Jwt/PasswordReset options-class convention (no
/// validation, no tenant scoping). A future multi-location/white-label extension would
/// sit as an ADDITIVE per-garage override layer resolved on top of these values, never a
/// replacement of this class.
///
/// No .Validate(...) clause, matching DemoOptions's precedent -- none of these four
/// fields is secret/startup-fatal-if-blank the way Jwt:SigningKey is.
/// </summary>
public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    public string ProductDisplayName { get; set; } = string.Empty;
    public string EmailFromName { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
}
