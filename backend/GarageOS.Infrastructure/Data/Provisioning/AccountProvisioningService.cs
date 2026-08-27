namespace GarageOS.Infrastructure.Data.Provisioning;

using GarageOS.Application.Abstractions;
using GarageOS.Application.Accounts;
using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>
/// Concrete implementation of the one authoritative garage-creation path (WP-3B brief §1/§4).
/// Lives in Infrastructure (not Application) because it must directly own AppDbContext,
/// transaction control, and Npgsql-specific exception translation — GarageOS.Application
/// does not (and must not) reference GarageOS.Infrastructure.
///
/// Concurrency design: the plan's literal suggestion of locking the `garages` row doesn't
/// work when the account has zero garages yet — Postgres `FOR UPDATE` can only lock
/// existing rows, so it cannot block a concurrent insert into an empty result set (the
/// "phantom row" gap). Instead this locks the parent `accounts` row, which always exists
/// by the time provisioning runs. Two concurrent calls for the same accountId both attempt
/// `SELECT ... FROM accounts WHERE id = @accountId FOR UPDATE` inside their own
/// transaction; the second blocks until the first commits or rolls back, serializing the
/// two calls on that one account. The read doubles as the account-existence check.
///
/// The DB-level partial unique index (garages_account_active_idx, see
/// GarageConfiguration and migration MakeGaragesAccountActiveIndexUnique) is the final
/// backstop even if the in-process lock were ever bypassed or two calls raced across
/// separate connections without the lock serializing them as expected.
/// </summary>
public sealed class AccountProvisioningService : IAccountProvisioningService
{
    private readonly AppDbContext _db;

    public AccountProvisioningService(AppDbContext db) => _db = db;

    public async Task<Garage> CreateGarageUnderAccountAsync(
        Guid accountId,
        GarageProvisioningDetails details,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var lockedAccount = await _db.Accounts
            .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {accountId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (lockedAccount is null)
        {
            throw new AccountNotFoundException(accountId);
        }

        var alreadyHasGarage = await _db.Garages
            .AnyAsync(g => g.AccountId == accountId && g.DeletedAt == null, cancellationToken);

        if (alreadyHasGarage)
        {
            throw new AccountAlreadyHasGarageException(accountId);
        }

        var garage = new Garage
        {
            Id = details.Id ?? Guid.NewGuid(),
            AccountId = accountId,
            Name = details.Name,
            Phone = details.Phone,
            Address = details.Address,
            LogoUrl = details.LogoUrl,
        };
        _db.Garages.Add(garage);
        _db.GarageSettings.Add(new GarageSettings { GarageId = garage.Id });
        _db.GarageSequences.Add(new GarageSequence { GarageId = garage.Id });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsGaragesAccountActiveUniqueViolation(ex))
        {
            // A write was attempted at this point, so roll back explicitly for
            // auditability even though tx disposal below would also roll back.
            await tx.RollbackAsync(cancellationToken);
            throw new AccountAlreadyHasGarageException(accountId);
        }

        await tx.CommitAsync(cancellationToken);
        return garage;
    }

    /// <summary>Checks the specific constraint name, not just the SQL state — a
    /// unique-violation on some other constraint must not be misreported as "account
    /// already has a garage." Any unrelated DbUpdateException propagates unchanged.</summary>
    private static bool IsGaragesAccountActiveUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && pg.ConstraintName == "garages_account_active_idx";
}
