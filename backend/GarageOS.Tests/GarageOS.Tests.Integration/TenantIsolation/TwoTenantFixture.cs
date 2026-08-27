using GarageOS.Domain.Entities;
using GarageOS.Infrastructure.Data;
using GarageOS.Tests.Integration.TestSupport;

namespace GarageOS.Tests.Integration.TenantIsolation;

/// <summary>Seeds two fully independent garages under two different accounts, directly
/// via AppDbContext (WP-3 brief §14). Inserts are never filtered by the tenant query
/// filter — only queries are — so a throwaway FakeCurrentTenant is fine here.</summary>
public sealed class TwoTenantFixture
{
    public (Account Account, Garage Garage, User Owner) TenantA { get; private set; }
    public (Account Account, Garage Garage, User Owner) TenantB { get; private set; }

    public async Task SeedAsync(IntegrationTestFixture fixture)
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());

        TenantA = await SeedOneTenantAsync(db, "Tenant A Garage");
        TenantB = await SeedOneTenantAsync(db, "Tenant B Garage");

        await db.SaveChangesAsync();
    }

    private static async Task<(Account, Garage, User)> SeedOneTenantAsync(
        AppDbContext db, string garageName)
    {
        var account = new Account
        {
            Name = garageName + " Account",
            BillingEmail = $"billing+{Guid.NewGuid():N}@example.test",
        };
        var garage = new Garage
        {
            AccountId = account.Id,
            Name = garageName,
        };
        var owner = new User
        {
            GarageId = garage.Id,
            Email = $"owner+{Guid.NewGuid():N}@example.test",
            PasswordHash = "test-only-not-a-real-hash",
            Name = "Owner",
            Role = "owner",
        };

        db.Accounts.Add(account);
        db.Garages.Add(garage);
        db.GarageSettings.Add(new GarageSettings { GarageId = garage.Id });
        db.GarageSequences.Add(new GarageSequence { GarageId = garage.Id });
        db.Users.Add(owner);

        await Task.CompletedTask;
        return (account, garage, owner);
    }
}
