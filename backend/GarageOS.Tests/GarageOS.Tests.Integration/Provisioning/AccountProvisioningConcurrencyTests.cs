namespace GarageOS.Tests.Integration.Provisioning;

using GarageOS.Application.Accounts;
using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using GarageOS.Infrastructure.Data.Provisioning;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Concurrency proof for AccountProvisioningService (WP-3B brief §8/acceptance criteria):
/// two simultaneous CreateGarageUnderAccountAsync calls for the same account must never
/// both succeed. Each concurrent attempt gets its own AppDbContext (its own Npgsql
/// connection/transaction) so the `SELECT ... FOR UPDATE` lock on the parent `accounts`
/// row genuinely serializes the two calls rather than sharing one in-process context.
/// </summary>
[Collection("Integration")]
public class AccountProvisioningConcurrencyTests(IntegrationTestFixture fixture)
{
    private async Task<Account> SeedAccountAsync()
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var account = new Account
        {
            Name = "Concurrency Test Account " + Guid.NewGuid(),
            BillingEmail = $"billing+{Guid.NewGuid():N}@example.test",
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }

    private async Task<(int Succeeded, int Rejected)> RunConcurrentAttemptsAsync(Guid accountId, int attemptCount)
    {
        var tasks = Enumerable.Range(0, attemptCount).Select(i => Task.Run(async () =>
        {
            await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
            var service = new AccountProvisioningService(db);
            try
            {
                await service.CreateGarageUnderAccountAsync(
                    accountId, new GarageProvisioningDetails(Name: $"Concurrent Garage {i}"));
                return true;
            }
            catch (AccountAlreadyHasGarageException)
            {
                return false;
            }
        }));

        var results = await Task.WhenAll(tasks);
        return (results.Count(r => r), results.Count(r => !r));
    }

    [Fact]
    public async Task TwoSimultaneousCalls_SameAccount_ExactlyOneSucceeds()
    {
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        var (succeeded, rejected) = await RunConcurrentAttemptsAsync(account.Id, attemptCount: 2);

        Assert.Equal(1, succeeded);
        Assert.Equal(1, rejected);

        await using var verifyDb = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var garageCount = await verifyDb.Garages.CountAsync(g => g.AccountId == account.Id);
        Assert.Equal(1, garageCount);
    }

    [Fact]
    public async Task TenSimultaneousCalls_SameAccount_ExactlyOneSucceeds()
    {
        await fixture.ResetDatabaseAsync();
        var account = await SeedAccountAsync();

        var (succeeded, rejected) = await RunConcurrentAttemptsAsync(account.Id, attemptCount: 10);

        Assert.Equal(1, succeeded);
        Assert.Equal(9, rejected);

        await using var verifyDb = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var garageCount = await verifyDb.Garages.CountAsync(g => g.AccountId == account.Id);
        Assert.Equal(1, garageCount);
    }
}
