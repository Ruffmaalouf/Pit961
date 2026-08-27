using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class GarageSettings : ITenantOwned
{
    public Guid GarageId { get; set; }
    public string Currency { get; set; } = "USD";
    public string Timezone { get; set; } = "Asia/Beirut";
    public decimal TaxRate { get; set; }
    public string TaxLabel { get; set; } = string.Empty;
    public decimal DefaultLaborRate { get; set; } = 20m;
    public string InvoicePrefix { get; set; } = "INV-";
    public string WorkingHoursOpen { get; set; } = "08:00";
    public string WorkingHoursClose { get; set; } = "18:00";
    public string DiagnosisFeePolicy { get; set; } = "waived_if_repaired";
    public decimal DiagnosisFeeAmount { get; set; } = 50m;
    public int WarrantyPeriodDays { get; set; } = 30;
    public int WarrantyMileageKm { get; set; } = 1000;
    public bool AllowDeliveryWithBalance { get; set; } = true;
    public string? DisplayCurrency { get; set; }

    /// <summary>Forward-reserved policy-configuration capacity (WP-3 brief §0/§7,
    /// approved by Technical Architect). Phase 1 authorization handlers (WP-5) decide
    /// whether to actually read these per-garage overrides or keep the code-level
    /// defaults (DECISIONS.md #7); no Phase 1 UI/endpoint may expose these for
    /// editing (Technical Architect condition).</summary>
    public decimal DiscountLimitPercent { get; set; } = 15.00m;
    public decimal EstimateApprovalThreshold { get; set; } = 500.00m;

    public string? ExtraSettings { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
