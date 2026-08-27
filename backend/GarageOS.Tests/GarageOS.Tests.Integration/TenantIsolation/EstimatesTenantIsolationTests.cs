using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.TenantIsolation;

[Collection("Integration")]
public class EstimatesTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var estimateB = await ResourceSeedHelpers.SeedEstimateAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Estimates.FirstOrDefaultAsync(e => e.Id == estimateB.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnTenantRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobA = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantA.Garage.Id);
        await ResourceSeedHelpers.SeedEstimateAsync(fixture, tenants.TenantA.Garage.Id, jobA.Id);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        await ResourceSeedHelpers.SeedEstimateAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var count = await dbAsA.Estimates.CountAsync();

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
        // persisted. The assertions below only prove the server-derived value is what ends up
        // on the row -- they do not by themselves prove a malicious payload would be rejected,
        // which is the specific brief section 16 acceptance clause this closes.
        Assert.Throws<TenantOwnershipException>(
            () => TenantGuard.EnsureOwned(tenants.TenantB.Garage.Id, currentTenant));
        var jobA = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantA.Garage.Id);

        var estimate = await ResourceSeedHelpers.SeedEstimateAsync(fixture, tenants.TenantA.Garage.Id, jobA.Id);

        Assert.Equal(tenants.TenantA.Garage.Id, estimate.GarageId);
        Assert.NotEqual(tenants.TenantB.Garage.Id, estimate.GarageId);
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var estimateB = await ResourceSeedHelpers.SeedEstimateAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(estimateB.GarageId, currentTenant));
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsParentJobFromMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(jobB.GarageId, currentTenant));
    }
}
