using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class EstimateItem : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public Guid EstimateId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; } = 0;
    public decimal UnitPrice { get; set; } = 0;
    public string ApprovalStatus { get; set; } = "pending";
    public int SortOrder { get; set; } = 0;
}
