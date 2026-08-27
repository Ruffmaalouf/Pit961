using GarageOS.Domain.Entities;
using GarageOS.Infrastructure.Security;

namespace GarageOS.Tests.Integration.TestSupport;

/// <summary>
/// WP-4 test-only seed helper. Deliberately does NOT go through
/// IAccountProvisioningService (WP-3B) -- per WP-4 brief §14: "WP-4's own tests must NOT
/// depend on AccountProvisioningService running -- create User fixture rows directly."
/// Mirrors TwoTenantFixture's existing direct-insert pattern (WP-3, already reviewed/
/// accepted). Uses the REAL PasswordHasherService (parameterless, no DI needed) so login
/// tests exercise the actual PBKDF2 verify path, not a fake hash.
/// </summary>
public static class AuthTestFixtures
{
    public const string DefaultPassword = "CorrectHorseBattery1";

    public static async Task<(Account Account, Garage Garage, User User)> SeedActiveUserAsync(
        IntegrationTestFixture fixture,
        string? password = null,
        string role = "owner",
        bool isActive = true,
        string? garageName = null)
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.Hash(password ?? DefaultPassword);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());
        var account = new Account
        {
            Name = "Auth Test Account " + Guid.NewGuid(),
            BillingEmail = $"billing+{Guid.NewGuid():N}@example.test",
        };
        var garage = new Garage
        {
            AccountId = account.Id,
            Name = garageName ?? "Auth Test Garage",
        };
        var user = new User
        {
            GarageId = garage.Id,
            Email = $"authtest+{Guid.NewGuid():N}@example.test",
            PasswordHash = hash,
            Name = "Auth Test User",
            Role = role,
            IsActive = isActive,
        };

        db.Accounts.Add(account);
        db.Garages.Add(garage);
        db.GarageSettings.Add(new GarageSettings { GarageId = garage.Id });
        db.GarageSequences.Add(new GarageSequence { GarageId = garage.Id });
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (account, garage, user);
    }
}
