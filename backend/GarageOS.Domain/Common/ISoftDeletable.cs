namespace GarageOS.Domain.Common;

/// <summary>Marker for entities using soft-delete (DeletedAt) instead of hard
/// deletes. Combined with ITenantOwned, the global query filter also excludes
/// soft-deleted rows.</summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
}

// WP-9 negative-gate proof (temporary, never merged to main): deliberately
// reintroduces the placeholder brand name "Rashid" to prove the CI grep gate
// actually blocks it. Removed immediately after the CI failure is confirmed.
