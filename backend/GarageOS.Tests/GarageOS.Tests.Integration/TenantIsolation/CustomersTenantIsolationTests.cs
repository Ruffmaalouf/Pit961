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

    // --- P2-WP2: soft-delete additions (DECISIONS.md #12 Decision #4) -----------------

    [Fact]
    public async Task SoftDeletedCustomer_ExcludedFromDefaultQuery()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id }))
        {
            var tracked = await db.Customers.SingleAsync(c => c.Id == customer.Id);
            tracked.DeletedAt = DateTimeOffset.UtcNow;
            tracked.DeletedBy = tenants.TenantA.Owner.Id;
            await db.SaveChangesAsync();
        }

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        var count = await dbAsA.Customers.CountAsync();

        Assert.Null(result);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SoftDeletedCustomer_ResolvableViaIgnoreQueryFilters_WhenTenantMatches()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);
        var deletedByUserId = tenants.TenantA.Owner.Id;

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id }))
        {
            var tracked = await db.Customers.SingleAsync(c => c.Id == customer.Id);
            tracked.DeletedAt = DateTimeOffset.UtcNow;
            tracked.DeletedBy = deletedByUserId;
            await db.SaveChangesAsync();
        }

        // The historical-composition pattern every read path with a legitimate reason to
        // see a soft-deleted row must use (ICustomerQueryRepository.FindByIdIncludingDeletedAsync's
        // exact shape) -- IgnoreQueryFilters() disables BOTH the tenant and soft-delete
        // halves of the composed filter at once, so the tenant check is re-applied by hand.
        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Customers.IgnoreQueryFilters()
            .Where(c => c.Id == customer.Id && c.GarageId == tenants.TenantA.Garage.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(result);
        Assert.NotNull(result!.DeletedAt);
        Assert.Equal(deletedByUserId, result.DeletedBy);
    }

    [Fact]
    public async Task SoftDeletedCustomer_NotResolvableViaIgnoreQueryFilters_WhenCrossTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerInB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantB.Garage.Id }))
        {
            var tracked = await db.Customers.SingleAsync(c => c.Id == customerInB.Id);
            tracked.DeletedAt = DateTimeOffset.UtcNow;
            tracked.DeletedBy = tenants.TenantB.Owner.Id;
            await db.SaveChangesAsync();
        }

        // This is the specific regression the historical-composition pattern is worried
        // about (ICustomerQueryRepository.FindByIdIncludingDeletedAsync's remarks): a
        // Garage A caller must NOT be able to use the IgnoreQueryFilters() path to read
        // Garage B's soft-deleted customer, even though that path bypasses the normal
        // filter -- the hand-re-applied GarageId check must still hold.
        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var result = await dbAsA.Customers.IgnoreQueryFilters()
            .Where(c => c.Id == customerInB.Id && c.GarageId == tenants.TenantA.Garage.Id)
            .SingleOrDefaultAsync();

        Assert.Null(result);
    }

    // --- P2-WP2 QA-remediation: directly exercise
    // ICustomerQueryRepository.FindByIdIncludingDeletedAsync itself (not just the inline
    // IgnoreQueryFilters() pattern above) -- closes the QA gap that the method was
    // implemented and documented but never covered by a test that actually calls it. ---

    [Fact]
    public async Task FindByIdIncludingDeletedAsync_ReturnsSoftDeletedCustomer_WhenTenantMatches()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantA.Garage.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id }))
        {
            var tracked = await db.Customers.SingleAsync(c => c.Id == customer.Id);
            tracked.DeletedAt = DateTimeOffset.UtcNow;
            tracked.DeletedBy = tenants.TenantA.Owner.Id;
            await db.SaveChangesAsync();
        }

        await using var dbAsA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var repo = new GarageOS.Infrastructure.Data.Customers.CustomerQueryRepository(dbAsA);

        var result = await repo.FindByIdIncludingDeletedAsync(customer.Id, tenants.TenantA.Garage.Id);

        Assert.NotNull(result);
        Assert.NotNull(result!.DeletedAt);
    }

    [Fact]
    public async Task FindByIdIncludingDeletedAsync_ReturnsNull_WhenGarageIdArgumentIsCrossTenant()
    {
        await fixture.ResetDatabaseAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerInB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);

        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantB.Garage.Id }))
        {
            var tracked = await db.Customers.SingleAsync(c => c.Id == customerInB.Id);
            tracked.DeletedAt = DateTimeOffset.UtcNow;
            tracked.DeletedBy = tenants.TenantB.Owner.Id;
            await db.SaveChangesAsync();
        }

        // The method's own re-applied GarageId check (not the ambient FakeCurrentTenant,
        // since IgnoreQueryFilters() disables that filter) must reject a mismatched
        // garageId argument -- proves the actual production method, called the way a real
        // caller would call it, cannot be used to read another tenant's deleted row.
        await using var db2 = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantA.Garage.Id });
        var repo = new GarageOS.Infrastructure.Data.Customers.CustomerQueryRepository(db2);

        var result = await repo.FindByIdIncludingDeletedAsync(customerInB.Id, tenants.TenantA.Garage.Id);

        Assert.Null(result);
    }
}
