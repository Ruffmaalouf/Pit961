namespace GarageOS.Application.Auth;

/// <summary>
/// Projection of a User row (plus its Garage's Name) used ONLY by the anonymous auth
/// flows and IUserAuthLookupRepository (WP-4 brief §7). Deliberately a richer shape than
/// the brief's literal `Task&lt;User?&gt;` return -- login's response contract requires
/// `user.garageName` (Technical Architect required change #2, WP-4 brief review), and
/// Garage.Name lives on a different table/aggregate than User, so a plain User entity
/// can't carry it without either an extra round-trip in AuthService (which has no direct
/// DB access -- Application layer stays framework/EF-free) or a second abstraction. A
/// single joined projection resolves this cleanly at the Infrastructure layer, where the
/// join actually happens.
/// </summary>
public sealed record UserAuthRecord(
    Guid Id,
    Guid GarageId,
    string GarageName,
    string Email,
    string PasswordHash,
    string Name,
    string Role,
    bool IsActive,
    int FailedLoginAttempts,
    DateTimeOffset? LockoutEndAt);
