namespace GarageOS.Domain.Entities;

using GarageOS.Domain.Common;

public class Garage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }

    /// <summary>Soft-delete marker. Added beyond the original handoff-doc SQL because
    /// WP-3B's one-garage-per-account locking check requires it — see WP-3
    /// implementation brief §2, approved by Technical Architect.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
