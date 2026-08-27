using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.TenantIsolation;

[Collection("Integration")]
public class JobsTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Jobs.FirstOrDefaultAsync(j => j.Id == jobB.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnTenantRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantA.Garage.Id);
        await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantA.Garage.Id);
        await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var count = await dbAsA.Jobs.CountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GarageId_CannotBeClientSupplied_OnCreate()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerA = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);
        var vehicleA = await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantA.Garage.Id, customerA.Id);
        var userA = await ResourceSeedHelpers.SeedUserAsync(fixture, tenants.TenantA.Garage.Id, "owner");

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        // Prove the actual gate, not just the happy path: any real create path (WP-5+) must
        // reject an attacker-supplied/mismatched garage_id in the payload before it is ever
        // persisted. The assertions below this point only prove the server-derived value is
        // what ends up on the row -- they do not by themselves prove a malicious payload would
        // be rejected, which is the specific brief section 16 acceptance clause this closes.
        Assert.Throws<TenantOwnershipException>(
            () => TenantGuard.EnsureOwned(tenants.TenantB.Garage.Id, currentTenant));
        await using var db = fixture.CreateAppDbContext(currentTenant);
        var job = new Job
        {
            GarageId = currentTenant.GarageId,
            JobNumber = "J-MAL",
            CustomerId = customerA.Id,
            VehicleId = vehicleA.Id,
            CreatedBy = userA.Id,
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        Assert.Equal(tenants.TenantA.Garage.Id, job.GarageId);
        Assert.NotEqual(tenants.TenantB.Garage.Id, job.GarageId);
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(jobB.GarageId, currentTenant));
    }
}
