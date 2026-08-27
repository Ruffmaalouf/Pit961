using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class RepairTask : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public Guid JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedMechanicId { get; set; }
    public string Status { get; set; } = "pending";
    public bool Outsourced { get; set; } = false;
    public string? OutsourceSupplier { get; set; }
    public decimal? OutsourceCost { get; set; }
    public decimal? OutsourceBilled { get; set; }
    public DateTimeOffset? OutsourceSentAt { get; set; }
    public DateTimeOffset? OutsourceReturnedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int SortOrder { get; set; } = 0;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
