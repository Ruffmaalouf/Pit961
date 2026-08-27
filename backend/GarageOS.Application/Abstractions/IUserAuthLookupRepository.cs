namespace GarageOS.Application.Abstractions;

using GarageOS.Application.Auth;

/// <summary>
/// WP-4 brief §7. Used ONLY by the anonymous auth flows (login lookup, refresh
/// re-hydration, forgot-password lookup, reset-email consumer lookup) plus the two
/// login-outcome writes and the /me tenant-scoped read. The Infrastructure implementation
/// uses AppDbContext.Users.IgnoreQueryFilters() for the by-email/by-id members ONLY --
/// this is THE one sanctioned cross-garage Users read, required because email is
/// globally unique (users_email_idx) and these flows run pre-authentication, before
/// ICurrentTenant.GarageId can be evaluated at all.
///
/// FindByCurrentTenantAsync is the exception: it deliberately goes through the NORMAL
/// filtered AppDbContext.Users path (no IgnoreQueryFilters) because by the time /me runs,
/// garage_id/user_id are legitimately populated from a validated JWT -- this is a
/// deliberate extra tenant-boundary sanity check (a claims/tenant mismatch resolves to
/// not-found rather than a leaked mismatched profile), not an oversight.
///
/// Deviation from the brief's literal two-method (`FindByEmailAsync`/`FindByIdAsync`)
/// interface, flagged: the login-outcome and password-reset flows also need writes
/// (failed-attempt counter, lockout, password-hash replacement), which the brief didn't
/// give a home to. Rather than inventing a second repository for three narrow write
/// operations that share this interface's exact anonymous-bypass rationale, they're
/// added here.
/// </summary>
public interface IUserAuthLookupRepository
{
    Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Tenant-filtered read for GET /auth/me -- see class remarks.</summary>
    Task<UserAuthRecord?> FindByCurrentTenantAsync(CancellationToken ct = default);

    Task RecordSuccessfulLoginAsync(Guid userId, CancellationToken ct = default);
    Task RecordFailedLoginAsync(Guid userId, int failedAttempts, DateTimeOffset? lockoutEndAt, CancellationToken ct = default);
    Task UpdatePasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken ct = default);
}
