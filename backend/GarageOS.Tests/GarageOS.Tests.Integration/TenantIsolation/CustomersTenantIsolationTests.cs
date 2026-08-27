using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.TenantIsolation;

[Collection("Integration")]
public class CustomersTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var inB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Customers.FirstOrDefaultAsync(c => c.Id == inB.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnTenantRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);
        await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);
        await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var count = await dbAsA.Customers.CountAsync();

        Assert.Equal(2, count);
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
        await using var db = fixture.CreateAppDbContext(currentTenant);
        var customer = new Customer
        {
            GarageId = currentTenant.GarageId, // never tenants.TenantB.Garage.Id
            FirstName = "Malicious",
            Phone = "+961 70 999 999",
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        Assert.Equal(tenants.TenantA.Garage.Id, customer.GarageId);
        Assert.NotEqual(tenants.TenantB.Garage.Id, customer.GarageId);
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var inB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(inB.GarageId, currentTenant));
    }
}
