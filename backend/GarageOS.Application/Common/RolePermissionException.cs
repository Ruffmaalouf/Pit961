namespace GarageOS.Application.Common;

/// <summary>
/// P2-WP2. Thrown when the current actor's role does not permit a plain role-gated
/// action (e.g. Customer/Vehicle soft-delete restricted to owner/manager). Distinct from
/// IBusinessRuleAuthorizer's BusinessRuleAuthorizationOutcome, which is reserved for the
/// two named Phase-1 money-threshold policies (discount cap, estimate approval threshold)
/// per DECISIONS.md #7's explicit "no generic rules engine" constraint -- a bare role
/// membership check is not a contextual/amount-based business rule and does not belong
/// behind that interface. Mapped to 403 by GlobalExceptionHandler.
/// </summary>
public sealed class RolePermissionException(string action)
    : Exception($"Current role does not permit: {action}")
{
    public string Action { get; } = action;
}
