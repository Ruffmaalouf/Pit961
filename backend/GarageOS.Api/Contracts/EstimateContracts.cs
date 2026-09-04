namespace GarageOS.Api.Contracts;

/// <summary>
/// P2-WP4. Every request record here deliberately carries no Subtotal/Total/
/// DiscountAmount, and no Status/approval-outcome field except where the client's intent
/// genuinely IS the whole point of the request (ApplyDiscountRequest's DiscountPercent --
/// an intent, not the authoritative DiscountAmount the server computes from it; and
/// RecordCustomerApprovalRequest's Decision -- an intent about what the customer said, not
/// a status value the server accepts verbatim). No endpoint in EstimatesController maps a
/// client-supplied field directly onto Estimate.Total/DiscountAmount/Status.
/// </summary>
public sealed record EstimateItemRequest(
    string Type, string Description, string? PartNumber, decimal Quantity, decimal UnitCost,
    decimal UnitPrice, int SortOrder);

public sealed record CreateEstimateRequest(
    Guid JobId, string Type, string? Notes, IReadOnlyList<EstimateItemRequest> Items);

public sealed record ReplaceEstimateItemsRequest(IReadOnlyList<EstimateItemRequest> Items);

public sealed record ApplyDiscountRequest(decimal DiscountPercent);

/// <summary>"decision" is one of approved/partially_approved/rejected -- what the customer
/// actually said, recorded independently of and never conflated with Owner approval.</summary>
public sealed record RecordCustomerApprovalRequest(string Decision, string ApprovalMethod, string? ApprovedByName);

public sealed record EstimateItemDto(
    Guid Id, string Type, string Description, string? PartNumber, decimal Quantity,
    decimal UnitCost, decimal UnitPrice, string ApprovalStatus, int SortOrder);

public sealed record EstimateDto(
    Guid Id, Guid JobId, string Type, Guid? ParentEstimateId, int RevisionNumber, string Status,
    string? ApprovalMethod, string? ApprovedByName, DateTimeOffset? ApprovedAt, DateTimeOffset? SentAt,
    decimal Subtotal, decimal TaxAmount, decimal DiscountAmount, decimal Total, string? Notes,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyList<EstimateItemDto> Items);
