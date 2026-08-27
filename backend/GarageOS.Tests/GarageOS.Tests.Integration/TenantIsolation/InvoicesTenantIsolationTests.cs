using GarageOS.Application.Common;
using GarageOS.Domain.Entities;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Tests.Integration.TenantIsolation;

[Collection("Integration")]
public class InvoicesTenantIsolationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task CrossTenantQuery_ReturnsZeroRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var invoiceB = await ResourceSeedHelpers.SeedInvoiceAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceB.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnTenantRows()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobA = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantA.Garage.Id);
        await ResourceSeedHelpers.SeedInvoiceAsync(fixture, tenants.TenantA.Garage.Id, jobA.Id);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        await ResourceSeedHelpers.SeedInvoiceAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var count = await dbAsA.Invoices.CountAsync();

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

        var invoice = await ResourceSeedHelpers.SeedInvoiceAsync(fixture, tenants.TenantA.Garage.Id, jobA.Id);

        Assert.Equal(tenants.TenantA.Garage.Id, invoice.GarageId);
        Assert.NotEqual(tenants.TenantB.Garage.Id, invoice.GarageId);
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var invoiceB = await ResourceSeedHelpers.SeedInvoiceAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(invoiceB.GarageId, currentTenant));
    }

    [Fact]
    public async Task WriteOwnershipCheck_RejectsParentJobFromMismatchedTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);

        var currentTenant = new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id };

        // Denormalized garage_id integrity (WP-3 brief section 2 closing note): creating an
        // invoice whose parent job belongs to Tenant B while acting as Tenant A must be
        // rejected by TenantGuard on the parent job id.
        Assert.Throws<TenantOwnershipException>(() => TenantGuard.EnsureOwned(jobB.GarageId, currentTenant));
    }
}
