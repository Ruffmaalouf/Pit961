namespace GarageOS.Application.Configuration;

/// <summary>
/// WP-4 brief §3. Bound from the "Jwt" configuration section with mandatory
/// ValidateOnStart() (see Program.cs) -- a missing/short SigningKey crashes the app at
/// boot in EVERY environment, never a silent default-key fallback.
///
/// SigningKey MUST be generated with a cryptographically secure random number generator
/// (e.g. `openssl rand -base64 32`, or `RandomNumberGenerator.GetBytes(32)` then
/// base64-encoded) -- never a human-chosen passphrase or a value derived from another
/// secret. Validation below can only check length (>= 32 UTF-8 bytes), which is a
/// necessary but not sufficient condition for adequate entropy; the CSPRNG requirement
/// is an operational mandate enforced by code review and the local-dev/CI/Production
/// key-provisioning runbook (see JwtOptionsValidation.cs and README.md), not something
/// this class can verify at runtime. (Security Reviewer required change, WP-4 brief review.)
///
/// Never committed to appsettings.json/appsettings.Development.json (both carry only an
/// empty placeholder, matching the existing ConnectionStrings convention). Local dev:
/// `dotnet user-secrets` or a gitignored appsettings.Local.json. CI: a GitHub Actions
/// secret env var (Jwt__SigningKey), generated once via a CSPRNG and stored as a repo
/// secret -- never hand-typed. Production: environment variable / host secret store only.
/// Test host (GarageOS.Tests.Integration): appsettings.Testing.json carries a fixed,
/// non-secret, CSPRNG-generated 32-byte test-only key -- safe to commit because it signs
/// nothing outside the test process and is never valid against any real deployment's
/// issuer/audience. (Technical Architect required change #5, WP-4 brief review.)
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Current signing key, base64 or raw UTF-8 text >= 32 bytes. See the CSPRNG
    /// mandate above.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>JWT header `kid` value for <see cref="SigningKey"/>. Not exercised by any
    /// live rotation event in Phase 1 -- see PreviousSigningKey below.</summary>
    public string KeyId { get; set; } = "1";

    /// <summary>Rotation support, documented as a runbook path but not exercised/tested in
    /// Phase 1 (no live rotation event exists yet): while set and
    /// <see cref="PreviousSigningKeyValidUntil"/> is in the future, a token signed with
    /// this key (identified by <see cref="PreviousKeyId"/> in its `kid` header) still
    /// validates, so in-flight access tokens survive a key rotation.</summary>
    public string? PreviousSigningKey { get; set; }
    public string? PreviousKeyId { get; set; }
    public DateTimeOffset? PreviousSigningKeyValidUntil { get; set; }

    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 14;
}
