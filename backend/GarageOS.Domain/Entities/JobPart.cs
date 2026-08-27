using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class JobPart : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public Guid JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierNameSnapshot { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; } = 0;
    public decimal UnitPrice { get; set; } = 0;
    public string SuppliedBy { get; set; } = "garage";
    public string Status { get; set; } = "needed";
    public DateTimeOffset? OrderedAt { get; set; }
    public DateTimeOffset? ExpectedAt { get; set; }
    public DateTimeOffset? ArrivedAt { get; set; }
    public DateTimeOffset? InstalledAt { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string? ReturnReason { get; set; }
    public string? IssueNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
