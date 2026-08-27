namespace GarageOS.Application.Abstractions;

using GarageOS.Domain.Entities;

/// <summary>
/// WP-4 brief §10. Explicit contract for password-reset-token persistence (Technical
/// Architect required change #1, WP-4 brief review -- named alongside
/// IRefreshTokenRepository for the same reason). Not ITenantOwned; single-use, enforced
/// by MarkUsedAsync plus a UsedAt-null check at consumption time in AuthService.
/// </summary>
public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task MarkUsedAsync(Guid tokenId, CancellationToken ct = default);
}
