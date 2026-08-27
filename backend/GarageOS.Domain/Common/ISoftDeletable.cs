namespace GarageOS.Domain.Common;

/// <summary>Marker for entities using soft-delete (DeletedAt) instead of hard
/// deletes. Combined with ITenantOwned, the global query filter also excludes
/// soft-deleted rows.</summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
}
