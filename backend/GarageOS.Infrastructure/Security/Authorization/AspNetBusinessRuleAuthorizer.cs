using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace GarageOS.Infrastructure.Security.Authorization;

/// <summary>
/// WP-5 brief §4. ASP.NET Core-flavored implementation of IBusinessRuleAuthorizer -- the
/// Infrastructure side of the same framework-free-Application / framework-hosting-
/// Infrastructure split ICurrentTenant/HttpContextCurrentTenant already established.
/// </summary>
public sealed class AspNetBusinessRuleAuthorizer(
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor) : IBusinessRuleAuthorizer
{
    public async Task<BusinessRuleAuthorizationOutcome> AuthorizeDiscountAsync(
        Guid resourceGarageId, decimal discountPercent, CancellationToken ct = default)
    {
        var user = httpContextAccessor.HttpContext?.User
            ?? throw new TenantContextUnavailableException();
        var result = await authorizationService.AuthorizeAsync(
            user, new DiscountAuthorizationResource(resourceGarageId, discountPercent), "DiscountLimit");
        return ToOutcome(result);
    }

    public async Task<BusinessRuleAuthorizationOutcome> AuthorizeEstimateApprovalThresholdAsync(
        Guid resourceGarageId, decimal subtotal, CancellationToken ct = default)
    {
        var user = httpContextAccessor.HttpContext?.User
            ?? throw new TenantContextUnavailableException();
        var result = await authorizationService.AuthorizeAsync(
            user, new EstimateApprovalAuthorizationResource(resourceGarageId, subtotal), "EstimateApprovalThreshold");
        return ToOutcome(result);
    }

    // IAuthorizationService.AuthorizeAsync has no CancellationToken overload -- ct is
    // accepted on the interface purely for call-site consistency with the rest of the
    // codebase's async signatures (e.g. AuthService's methods), not because it's honored.
    private static BusinessRuleAuthorizationOutcome ToOutcome(AuthorizationResult result) =>
        result.Succeeded
            ? BusinessRuleAuthorizationOutcome.Success
            : BusinessRuleAuthorizationOutcome.Denied(
                result.Failure?.FailureReasons.FirstOrDefault()?.Message ?? "denied");
}
