using GarageOS.Application.Abstractions;
using GarageOS.Application.Auth;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Auth;

/// <summary>WP-4 brief §7 -- see IUserAuthLookupRepository's remarks for the full
/// rationale on IgnoreQueryFilters usage and the FindByCurrentTenantAsync exception.</summary>
public sealed class UserAuthLookupRepository(AppDbContext db, ICurrentTenant currentTenant)
    : IUserAuthLookupRepository
{
    // THE one sanctioned cross-garage Users read (mirrors the codebase's existing habit,
    // cf. the Platform-namespace-exclusion comment in AppDbContext.OnModelCreating) --
    // required because email is globally unique (users_email_idx) and these flows run
    // pre-authentication, before ICurrentTenant.GarageId can be evaluated at all.
    public Task<UserAuthRecord?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        Project(db.Users.IgnoreQueryFilters().Where(u => u.Email == email)).SingleOrDefaultAsync(ct);

    public Task<UserAuthRecord?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
        Project(db.Users.IgnoreQueryFilters().Where(u => u.Id == userId)).SingleOrDefaultAsync(ct);

    // Deliberately the NORMAL filtered path (no IgnoreQueryFilters) -- see class remarks
    // on IUserAuthLookupRepository. A garage_id/user_id mismatch resolves to not-found.
    public Task<UserAuthRecord?> FindByCurrentTenantAsync(CancellationToken ct = default) =>
        Project(db.Users.Where(u => u.Id == currentTenant.UserId)).SingleOrDefaultAsync(ct);

    public async Task RecordSuccessfulLoginAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == userId, ct);
        user.FailedLoginAttempts = 0;
        user.LockoutEndAt = null;
        user.LastLogin = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RecordFailedLoginAsync(Guid userId, int failedAttempts, DateTimeOffset? lockoutEndAt, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == userId, ct);
        user.FailedLoginAttempts = failedAttempts;
        user.LockoutEndAt = lockoutEndAt;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdatePasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == userId, ct);
        user.PasswordHash = newPasswordHash;
        await db.SaveChangesAsync(ct);
    }

    // Garages carries no tenant query filter (it's a tenant root, see WP-3B), so this
    // join is safe regardless of whether `source` above used IgnoreQueryFilters() or not.
    private IQueryable<UserAuthRecord> Project(IQueryable<User> source) =>
        from u in source
        join g in db.Garages on u.GarageId equals g.Id
        select new UserAuthRecord(
            u.Id, u.GarageId, g.Name, u.Email, u.PasswordHash, u.Name, u.Role,
            u.IsActive, u.FailedLoginAttempts, u.LockoutEndAt);
}
