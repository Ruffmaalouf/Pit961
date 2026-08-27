using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Auth;

/// <summary>WP-4 brief §10. Not ITenantOwned; single-use enforced by MarkUsedAsync plus
/// AuthService's UsedAt-null/ExpiresAt check at consumption time.</summary>
public sealed class PasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        db.PasswordResetTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.PasswordResetTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task MarkUsedAsync(Guid tokenId, CancellationToken ct = default)
    {
        var token = await db.PasswordResetTokens.SingleAsync(t => t.Id == tokenId, ct);
        token.UsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
