namespace GarageOS.Application.Abstractions;

using GarageOS.Domain.Entities;

/// <summary>
/// WP-4 brief §9. Explicit contract for refresh-token persistence (Technical Architect
/// required change #1, WP-4 brief review -- the brief described the rotation/reuse
/// behavior in prose but never named this interface's members).
/// RefreshToken is not ITenantOwned -- no tenant filter, no bypass needed; lookups are
/// exclusively by TokenHash (unique-indexed, see RefreshTokenConfiguration) or by
/// UserId (for the reuse-detection "revoke everything" fan-out).
/// </summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Sets RevokedAt = now and, when rotating (not a terminal logout),
    /// ReplacedByTokenId. Terminal revocation (logout, reuse-detection) passes
    /// replacedByTokenId: null.</summary>
    Task RevokeAsync(Guid tokenId, Guid? replacedByTokenId, CancellationToken ct = default);

    /// <summary>Atomically "claims" a token for rotation: a single conditional
    /// UPDATE ... WHERE RevokedAt IS NULL sets RevokedAt/ReplacedByTokenId and returns
    /// whether exactly one row was affected. Added post-implementation (Security
    /// Reviewer HIGH finding, WP-4 review): reading RevokedAt in application code and
    /// writing the revoke in a LATER, separate call left a window where two concurrent
    /// presentations of the same still-valid token could both pass the "not yet
    /// revoked" check before either write landed, both minting a live session with
    /// reuse-detection never firing. This method's single atomic UPDATE is the only
    /// place that decides who wins a concurrent rotation race -- callers MUST treat a
    /// false return identically to "this token was already revoked" (see
    /// AuthService.RefreshAsync), since the two cases are indistinguishable and both
    /// are the reuse-detection signal (brief §9).</summary>
    Task<bool> TryClaimForRotationAsync(Guid tokenId, Guid replacedByTokenId, CancellationToken ct = default);

    /// <summary>Reuse-detection response (WP-4 brief §9): revokes EVERY active
    /// (RevokedAt == null) token for the user, not just the presented token's lineage --
    /// "family" in Phase 1 means all of a user's sessions (no FamilyId column), an
    /// explicitly flagged simplification, not an oversight.</summary>
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
}
