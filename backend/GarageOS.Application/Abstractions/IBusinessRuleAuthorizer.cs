namespace GarageOS.Application.Abstractions;

/// <summary>
/// WP-5 brief §4. Framework-free bridge from Application-layer services to the ASP.NET
/// Core IAuthorizationRequirement/Handler framework that Infrastructure hosts (mirrors the
/// ICurrentTenant / HttpContextCurrentTenant split: Application depends only on this
/// interface; Infrastructure supplies the ASP.NET Core-flavored implementation). Exactly
/// two methods, one per named Phase-1 policy -- not a generic
/// AuthorizeAsync(policyName, resource) catch-all, per DECISIONS.md #7's "no generic rules
/// engine" constraint.
/// </summary>
public interface IBusinessRuleAuthorizer
{
    Task<BusinessRuleAuthorizationOutcome> AuthorizeDiscountAsync(
        Guid resourceGarageId, decimal discountPercent, CancellationToken ct = default);

    Task<BusinessRuleAuthorizationOutcome> AuthorizeEstimateApprovalThresholdAsync(
        Guid resourceGarageId, decimal subtotal, CancellationToken ct = default);
}

public sealed record BusinessRuleAuthorizationOutcome(bool Succeeded, string? FailureReason)
{
    public static readonly BusinessRuleAuthorizationOutcome Success = new(true, null);
    public static BusinessRuleAuthorizationOutcome Denied(string reason) => new(false, reason);
}
