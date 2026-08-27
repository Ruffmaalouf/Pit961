using GarageOS.Domain.Common;
using GarageOS.Tests.Integration.TestSupport;

namespace GarageOS.Tests.Integration.TenantIsolation;

/// <summary>
/// WP-3 brief §16 coverage-completeness meta-test: proves AppDbContext's reflection-based
/// global query filter setup (ApplyTenantQueryFilters) is applied to EVERY entity that
/// implements <see cref="ITenantOwned"/> in the Domain assembly — not just the ones the
/// per-resource tenant-isolation tests happen to sample. This guards against the class of
/// bug where a new tenant-owned entity is added later (a future WP) but someone forgets to
/// wire it into the filter loop: that entity would silently leak cross-tenant data with no
/// single per-resource test catching it, since each per-resource suite only proves its own
/// resource is filtered, never that the filter loop covers the whole entity set.
/// </summary>
[Collection("Integration")]
public class QueryFilterCoverageTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void EveryITenantOwnedEntity_HasANonNullGlobalQueryFilter()
    {
        using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());

        var tenantOwnedClrTypes = typeof(ITenantOwned).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ITenantOwned).IsAssignableFrom(t))
            .ToHashSet();

        Assert.NotEmpty(tenantOwnedClrTypes);

        foreach (var clrType in tenantOwnedClrTypes)
        {
            var entityType = db.Model.FindEntityType(clrType);
            Assert.True(entityType is not null, $"{clrType.Name} implements ITenantOwned but is not part of AppDbContext's model.");
            Assert.True(entityType!.GetQueryFilter() is not null, $"{clrType.Name} implements ITenantOwned but has no global query filter applied.");
        }
    }

    [Fact]
    public void NoEntityOutsideITenantOwned_HasATenantQueryFilter()
    {
        // Guards the inverse direction: a filter should never be applied to an entity that
        // doesn't declare ITenantOwned, which would indicate the reflection loop is matching
        // on something looser than the marker interface (e.g. a "GarageId"-named property).
        using var db = fixture.CreateAppDbContext(new FakeCurrentTenant());

        var tenantOwnedClrTypes = typeof(ITenantOwned).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ITenantOwned).IsAssignableFrom(t))
            .ToHashSet();

        foreach (var entityType in db.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null || tenantOwnedClrTypes.Contains(entityType.ClrType))
            {
                continue;
            }

            Assert.True(entityType.GetQueryFilter() is null, $"{entityType.ClrType.Name} does not implement ITenantOwned but has a tenant query filter applied.");
        }
    }
}
