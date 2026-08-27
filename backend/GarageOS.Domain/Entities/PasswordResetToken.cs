namespace GarageOS.Domain.Entities;

/// <summary>Single-use password-reset token (WP-4 brief §10). Not ITenantOwned -- reset
/// flows are anonymous and keyed by the globally-unique User.Email, same rationale as
/// RefreshToken. Only the SHA-256 hash of the raw token is ever persisted (same rationale
/// as RefreshToken.TokenHash -- already-maximal-entropy random data, a slow adaptive hash
/// buys nothing while costing CPU on every reset attempt).</summary>
public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Single-use marker -- once set, the token is dead even if not yet expired.</summary>
    public DateTimeOffset? UsedAt { get; set; }
    public string? RequestedByIp { get; set; }
}
