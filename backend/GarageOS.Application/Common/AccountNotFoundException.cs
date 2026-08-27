namespace GarageOS.Application.Common;

/// <summary>Thrown by AccountProvisioningService when the target account does not exist
/// (WP-3B brief §3).</summary>
public sealed class AccountNotFoundException : Exception
{
    public Guid AccountId { get; }

    public AccountNotFoundException(Guid accountId)
        : base($"Account '{accountId}' does not exist.")
        => AccountId = accountId;
}
