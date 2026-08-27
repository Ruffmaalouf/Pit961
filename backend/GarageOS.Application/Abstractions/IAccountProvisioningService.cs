namespace GarageOS.Application.Abstractions;

using GarageOS.Application.Accounts;
using GarageOS.Domain.Entities;

/// <summary>
/// The one authoritative path for inserting a row into `garages` (WP-3B brief §1/§2).
/// Enforces the Phase 1 one-active-garage-per-account rule under a single DB transaction,
/// backed by a partial unique index on garages(account_id) WHERE deleted_at IS NULL (see
/// GarageConfiguration/migration MakeGaragesAccountActiveIndexUnique). Also creates the
/// new garage's GarageSettings and GarageSequence rows atomically, so no caller can ever
/// produce an incomplete garage.
///
/// Ships zero HTTP routes in Phase 1 (WP-3B brief §0) — called only from
/// DevelopmentSeeder today; the future Phase 6 signup/billing flow is the only other
/// planned caller.
/// </summary>
public interface IAccountProvisioningService
{
    /// <summary>
    /// Throws <see cref="Common.AccountNotFoundException"/> if <paramref name="accountId"/>
    /// does not exist, or <see cref="Common.AccountAlreadyHasGarageException"/> if the
    /// account already owns a non-deleted garage (including under concurrent-call races —
    /// the DB-level partial unique index is the final backstop, not just the in-process check).
    /// </summary>
    Task<Garage> CreateGarageUnderAccountAsync(
        Guid accountId,
        GarageProvisioningDetails details,
        CancellationToken cancellationToken = default);
}
