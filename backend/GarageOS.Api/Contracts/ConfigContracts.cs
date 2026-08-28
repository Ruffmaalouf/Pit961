namespace GarageOS.Api.Contracts;

// WP-7 brief. Public, non-secret branding surface for the (future) frontend.
// BrandingConfigResponse's shape IS the allow-list -- ConfigController hand-maps
// BrandingOptions into this record field-by-field, never returns BrandingOptions
// itself, so adding a field to BrandingOptions later does not automatically change
// what this endpoint exposes (a human must explicitly edit both this record and the
// controller's mapping, both visible in code review).

public sealed record BrandingConfigResponse(
    string ProductDisplayName,
    string EmailFromName,
    string LogoUrl,
    string SupportEmail);
