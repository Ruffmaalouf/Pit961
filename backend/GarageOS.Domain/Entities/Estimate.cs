using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class Estimate : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public Guid JobId { get; set; }
    public string Type { get; set; } = "standard";
    public Guid? ParentEstimateId { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public string Status { get; set; } = "draft";
    public string? ApprovalMethod { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public decimal Subtotal { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal Total { get; set; } = 0;
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
