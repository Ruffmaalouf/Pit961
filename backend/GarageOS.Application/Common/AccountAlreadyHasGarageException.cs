namespace GarageOS.Application.Common;

/// <summary>Thrown by AccountProvisioningService when an account already owns a
/// non-deleted garage (WP-3B brief §3). Distinct from TenantOwnershipException: this is a
/// cardinality/existence rule violation on the account/garage relationship, not a
/// cross-tenant access violation, and involves no ICurrentTenant/tenant context at all.</summary>
public sealed class AccountAlreadyHasGarageException : Exception
{
    public Guid AccountId { get; }

    public AccountAlreadyHasGarageException(Guid accountId)
        : base($"Account '{accountId}' already owns an active garage. One garage per account is enforced for Phase 1.")
        => AccountId = accountId;
}
