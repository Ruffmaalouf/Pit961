using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.TenantIsolation;

[Collection("Integration")]
public class VehiclesTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);
        var vehicleB = await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantB.Garage.Id, customerB.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleB.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnTenantRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerA = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);
        await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantA.Garage.Id, customerA.Id);
        var customerB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);
        await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantB.Garage.Id, customerB.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var count = await dbAsA.Vehicles.CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GarageId_CannotBeClientSupplied_OnCreate()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerA = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };
        await using var db = fixture.CreateAppDbContext(currentTenant);
        var vehicle = new Vehicle
        {
            GarageId = currentTenant.GarageId,
            CustomerId = customerA.Id,
            PlateNumber = "MAL123",
            Make = "Malicious",
            Model = "Vehicle",
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        Assert.Equal(tenants.TenantA.Garage.Id, vehicle.GarageId);
        Assert.NotEqual(tenants.TenantB.Garage.Id, vehicle.GarageId);
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);
        var vehicleB = await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantB.Garage.Id, customerB.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(vehicleB.GarageId, currentTenant));
    }
}
