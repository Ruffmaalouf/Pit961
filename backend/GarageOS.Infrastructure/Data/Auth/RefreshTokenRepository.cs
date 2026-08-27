using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Auth;

/// <summary>WP-4 brief §9. RefreshToken is not ITenantOwned -- no filter, no bypass
/// needed. See IRefreshTokenRepository's remarks for the reuse-detection "revoke
/// everything for the user" rationale.</summary>
public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public async Task RevokeAsync(Guid tokenId, Guid? replacedByTokenId, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens.SingleAsync(rt => rt.Id == tokenId, ct);
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.ReplacedByTokenId = replacedByTokenId;
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var active = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in active)
        {
            token.RevokedAt = now;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryClaimForRotationAsync(Guid tokenId, Guid replacedByTokenId, CancellationToken ct = default)
    {
        // ExecuteUpdateAsync issues ONE atomic SQL UPDATE directly against the database
        // (bypassing the change tracker/SaveChangesAsync) -- the WHERE RevokedAt == null
        // guard and the SET happen as a single statement. Under concurrent presentation
        // of the same token, Postgres's row-level locking serializes the two UPDATEs:
        // whichever commits first sees RevokedAt IS NULL and wins (1 row affected); the
        // other re-evaluates the WHERE clause against the now-committed row and matches
        // zero rows. See IRefreshTokenRepository.TryClaimForRotationAsync's remarks.
        var rowsAffected = await db.RefreshTokens
            .Where(rt => rt.Id == tokenId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow)
                .SetProperty(rt => rt.ReplacedByTokenId, replacedByTokenId), ct);
        return rowsAffected == 1;
    }
}
