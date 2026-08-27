using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class Job : ITenantOwned, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? PrimaryMechanicId { get; set; }
    public Guid? SecondaryMechanicId { get; set; }
    public Guid CreatedBy { get; set; }
    public string Status { get; set; } = "checked_in";
    public int? MileageAtIntake { get; set; }
    public string? CustomerComplaint { get; set; }
    public string? AdvisorNotes { get; set; }
    public DateTimeOffset? PromisedAt { get; set; }
    public bool CustomerWaiting { get; set; } = false;
    public string Source { get; set; } = "walk_in";
    public bool Overnight { get; set; } = false;
    public string? OvernightNote { get; set; }
    public bool IsWarrantyReturn { get; set; } = false;
    public Guid? ParentJobId { get; set; }
    public string? CancellationReason { get; set; }
    public string? DeletionReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
