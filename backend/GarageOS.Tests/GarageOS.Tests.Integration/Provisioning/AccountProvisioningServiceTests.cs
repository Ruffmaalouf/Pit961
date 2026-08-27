namespace GarageOS.Tests.Integration.Provisioning;

using GarageOS.Application.Accounts;
using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using GarageOS.Infrastructure.Data.Provisioning;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>
/// Behavioral tests for AccountProvisioningService (WP-3B brief §8). All run against real
/// PostgreSQL — no EF InMemory/SQLite substitute — because the rejection/success behavior
/// is fundamentally dependent on real Postgres behavior (the partial unique index,
/// `FOR UPDATE` row locking). Only the bypass-protection source scan
/// (GarageInsertBoundaryTests) is a true unit test.
/// </summary>
[Collection("Integration")]
public class AccountProvisioningServiceTests(IntegrationTestFixture fixture)
{
    private async Task<Account> SeedAccountAsync()
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var account = new Account
        {
            Name = "Test Account " + Guid.NewGuid(),
            BillingEmail = $"billing+{Guid.NewGuid():N}@example.test",
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }

    [Fact]
    public async Task CreateGarageUnderAccountAsync_FirstGarageForAccount_Succeeds()
    {
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var service = new AccountProvisioningService(db);

        var garage = await service.CreateGarageUnderAccountAsync(
            account.Id, new GarageProvisioningDetails(Name: "First Garage"));

        Assert.Equal(account.Id, garage.AccountId);
        Assert.Equal("First Garage", garage.Name);
    }

    [Fact]
    public async Task CreateGarageUnderAccountAsync_SecondCallSameAccount_ThrowsAccountAlreadyHasGarageException()
    {
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        await using (var db1 = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            await new AccountProvisioningService(db1).CreateGarageUnderAccountAsync(
                account.Id, new GarageProvisioningDetails(Name: "Garage One"));
        }

        await using var db2 = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var service2 = new AccountProvisioningService(db2);

        await Assert.ThrowsAsync<AccountAlreadyHasGarageException>(() =>
            service2.CreateGarageUnderAccountAsync(account.Id, new GarageProvisioningDetails(Name: "Garage Two")));
    }

    [Fact]
    public async Task CreateGarageUnderAccountAsync_DifferentAccount_Succeeds()
    {
        await fixture.ResetDatabaseAsync();
        var accountA = await SeedAccountAsync();
        var accountB = await SeedAccountAsync();

        await using (var dbA = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            await new AccountProvisioningService(dbA).CreateGarageUnderAccountAsync(
                accountA.Id, new GarageProvisioningDetails(Name: "A's Garage"));
        }

        await using var dbB = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var garageB = await new AccountProvisioningService(dbB).CreateGarageUnderAccountAsync(
            accountB.Id, new GarageProvisioningDetails(Name: "B's Garage"));

        Assert.Equal(accountB.Id, garageB.AccountId);
    }

    [Fact]
    public async Task CreateGarageUnderAccountAsync_UnknownAccountId_ThrowsAccountNotFoundException()
    {
        await fixture.ResetDatabaseAsync();

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var service = new AccountProvisioningService(db);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            service.CreateGarageUnderAccountAsync(Guid.NewGuid(), new GarageProvisioningDetails(Name: "Nobody's Garage")));
    }

    [Fact]
    public async Task CreateGarageUnderAccountAsync_AfterSoftDeletingExistingGarage_AllowsNewGarageForSameAccount()
    {
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        Garage firstGarage;
        await using (var db1 = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            firstGarage = await new AccountProvisioningService(db1).CreateGarageUnderAccountAsync(
                account.Id, new GarageProvisioningDetails(Name: "Old Garage"));
        }

        await using (var dbDelete = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            var toDelete = await dbDelete.Garages.SingleAsync(g => g.Id == firstGarage.Id);
            toDelete.DeletedAt = DateTimeOffset.UtcNow;
            await dbDelete.SaveChangesAsync();
        }

        await using var db2 = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var newGarage = await new AccountProvisioningService(db2).CreateGarageUnderAccountAsync(
            account.Id, new GarageProvisioningDetails(Name: "New Garage"));

        Assert.NotEqual(firstGarage.Id, newGarage.Id);
        Assert.Equal(account.Id, newGarage.AccountId);
    }

    [Fact]
    public async Task CreateGarageUnderAccountAsync_CreatesGarageSettingsAndGarageSequenceAtomically()
    {
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var garage = await new AccountProvisioningService(db).CreateGarageUnderAccountAsync(
            account.Id, new GarageProvisioningDetails(Name: "Complete Garage"));

        await using var verifyDb = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garage.Id });
        var settings = await verifyDb.GarageSettings.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.GarageId == garage.Id);
        var sequence = await verifyDb.GarageSequences.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.GarageId == garage.Id);

        Assert.NotNull(settings);
        Assert.NotNull(sequence);
    }

    [Fact]
    public async Task CreateGarageUnderAccountAsync_RejectedAttempt_DoesNotPersistPartialRows()
    {
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        await using (var db1 = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            await new AccountProvisioningService(db1).CreateGarageUnderAccountAsync(
                account.Id, new GarageProvisioningDetails(Name: "Existing Garage"));
        }

        await using var db2 = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var service2 = new AccountProvisioningService(db2);
        await Assert.ThrowsAsync<AccountAlreadyHasGarageException>(() =>
            service2.CreateGarageUnderAccountAsync(account.Id, new GarageProvisioningDetails(Name: "Rejected Garage")));

        await using var verifyDb = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var garageCount = await verifyDb.Garages.CountAsync(g => g.AccountId == account.Id);
        Assert.Equal(1, garageCount);
    }

    [Fact]
    public async Task UniqueIndex_DirectDoubleInsertBypassingService_SecondInsertViolatesConstraint()
    {
        // Deliberately bypasses the service via a raw db.Garages.Add(...) (test-only,
        // exempt from GarageInsertBoundaryTests' scan since this file lives under
        // GarageOS.Tests.*) to prove the DB constraint itself is what ultimately
        // enforces the invariant, not merely the in-process application check.
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        await using (var db1 = fixture.CreateAppDbContext(new FakeCurrentTenant()))
        {
            db1.Garages.Add(new Garage { AccountId = account.Id, Name = "Direct Insert One" });
            await db1.SaveChangesAsync();
        }

        await using var db2 = fixture.CreateAppDbContext(new FakeCurrentTenant());
        db2.Garages.Add(new Garage { AccountId = account.Id, Name = "Direct Insert Two" });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("garages_account_active_idx", pg.ConstraintName);
    }
}
