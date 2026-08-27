using GarageOS.Application.Common;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.TenantIsolation;

[Collection("Integration")]
public class GarageSettingsTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.GarageSettings.FirstOrDefaultAsync(gs => gs.GarageId == tenants.TenantB.Garage.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnTenantRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var count = await dbAsA.GarageSettings.CountAsync();

        // One-to-one with the garage — exactly one row for Tenant A, never Tenant B's.
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GarageId_CannotBeClientSupplied_OnCreate()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        // Prove the actual gate, not just the happy path: any real create path (WP-5+) must
        // reject an attacker-supplied/mismatched garage_id in the payload before it is ever
        // persisted. The assertions below this point only prove the server-derived value is
        // what ends up on the row -- they do not by themselves prove a malicious payload would
        // be rejected, which is the specific brief section 16 acceptance clause this closes.
        Assert.Throws<TenantOwnershipException>(
            () => TenantGuard.EnsureOwned(tenants.TenantB.Garage.Id, currentTenant));
        await using var dbAsA = fixture.CreateAppDbContext(currentTenant);
        var settings = await dbAsA.GarageSettings.SingleAsync();

        // The PK/FK is the garage's own id — there is structurally no client-suppliable
        // garage_id field distinct from the tenant's own garage, so this asserts the
        // invariant directly: the settings row's GarageId always matches ICurrentTenant.
        Assert.Equal(currentTenant.GarageId, settings.GarageId);
        Assert.NotEqual(tenants.TenantB.Garage.Id, settings.GarageId);
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);

        await using var dbAsB = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantB.Garage.Id });
        var settingsB = await dbAsB.GarageSettings.SingleAsync();

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(settingsB.GarageId, currentTenant));
    }
}
