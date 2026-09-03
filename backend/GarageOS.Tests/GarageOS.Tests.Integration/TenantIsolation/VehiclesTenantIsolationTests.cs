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

        // Prove the actual gate, not just the happy path: any real create path (WP-5+) must
        // reject an attacker-supplied/mismatched garage_id in the payload before it is ever
        // persisted. The assertions below this point only prove the server-derived value is
        // what ends up on the row -- they do not by themselves prove a malicious payload would
        // be rejected, which is the specific brief section 16 acceptance clause this closes.
        Assert.Throws<TenantOwnershipException>(
            () => TenantGuard.EnsureOwned(tenants.TenantB.Garage.Id, currentTenant));
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

    [Fact]
    public async Task WriteOwnershipCheck_RejectsParentCustomerFromMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        // Denormalized garage_id integrity (WP-3 brief section 2 closing note): creating a
        // vehicle whose parent customer belongs to Tenant B while acting as Tenant A must be
        // rejected by TenantGuard on the parent customer id.
        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(customerB.GarageId, currentTenant));
    }

    // --- P2-WP2 additions (DECISIONS.md #12 Decisions #4/#5) --------------------------

    [Fact]
    public async Task SoftDeletedVehicle_ExcludedFromDefaultQuery()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);
        var vehicle = await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantA.Garage.Id, customer.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id }))
        {
            var tracked = await db.Vehicles.SingleAsync(v => v.Id == vehicle.Id);
            tracked.DeletedAt = DateTimeOffset.UtcNow;
            tracked.DeletedBy = tenants.TenantA.Owner.Id;
            await db.SaveChangesAsync();
        }

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task SoftDeleteDoesNotCascade_FromCustomerToVehicle()
    {
        // P2-WP2 architecture design §2.4/§6.8 default assumption (pending
        // product-manager confirmation, flagged in P2-WP2_ARCHITECTURE.md): soft-deleting
        // a Customer does NOT cascade to that customer's Vehicles. Locking in the assumed
        // behavior with a real regression test so a future change can't silently flip it.
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);
        var vehicle = await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantA.Garage.Id, customer.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id }))
        {
            var trackedCustomer = await db.Customers.SingleAsync(c => c.Id == customer.Id);
            trackedCustomer.DeletedAt = DateTimeOffset.UtcNow;
            trackedCustomer.DeletedBy = tenants.TenantA.Owner.Id;
            await db.SaveChangesAsync();
        }

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var vehicleAfterCustomerDelete = await dbAsA.Vehicles.SingleOrDefaultAsync(v => v.Id == vehicle.Id);

        Assert.NotNull(vehicleAfterCustomerDelete);
        Assert.Null(vehicleAfterCustomerDelete!.DeletedAt);
    }

    [Fact]
    public async Task DuplicatePlateCheck_IsTenantScoped()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantB.Garage.Id }))
        {
            db.Vehicles.Add(new Vehicle
            {
                GarageId = tenants.TenantB.Garage.Id,
                CustomerId = customerB.Id,
                PlateNumber = "SHARED1",
                PlateCountry = "LB",
                Make = "Nissan",
                Model = "Sunny",
            });
            await db.SaveChangesAsync();
        }

        // DECISIONS.md #12 Decision #5: the duplicate-check query must filter by GarageId
        // exactly like every other Vehicle query -- Garage A must see zero matches for a
        // plate that only exists in Garage B, even though the plate index itself is
        // GarageId-inclusive.
        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var matches = await dbAsA.Vehicles
            .Where(v => v.PlateNumber == "SHARED1" && v.PlateCountry == "LB")
            .ToListAsync();

        Assert.Empty(matches);
    }

    [Fact]
    public async Task DuplicatePlateCheck_ExcludesSoftDeletedMatches()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id }))
        {
            db.Vehicles.Add(new Vehicle
            {
                GarageId = tenants.TenantA.Garage.Id,
                CustomerId = customer.Id,
                PlateNumber = "GONE001",
                PlateCountry = "LB",
                Make = "Mazda",
                Model = "3",
                DeletedAt = DateTimeOffset.UtcNow,
                DeletedBy = tenants.TenantA.Owner.Id,
            });
            await db.SaveChangesAsync();
        }

        // A soft-deleted vehicle with the same plate must not trigger a duplicate warning
        // for a new active vehicle -- the global query filter already excludes it from
        // this plain (non-IgnoreQueryFilters) query, which is exactly the mechanism
        // VehicleQueryRepository.FindDuplicatePlateCandidatesAsync relies on.
        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var matches = await dbAsA.Vehicles
            .Where(v => v.PlateNumber == "GONE001" && v.PlateCountry == "LB")
            .ToListAsync();

        Assert.Empty(matches);
    }
}
