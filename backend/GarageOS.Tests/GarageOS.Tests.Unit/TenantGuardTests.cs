using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;

namespace GarageOS.Tests.Unit;

/// <summary>
/// WP-3 unit coverage for <see cref="TenantGuard.EnsureOwned"/> — the explicit
/// write-ownership half of the dual tenant-enforcement pattern (the other half being
/// the EF Core global query filters exercised by the integration-test tenant-isolation
/// matrix). This complements those integration tests, which only exercised the
/// "mismatched tenant -> throws" path, by also locking in the "matching tenant ->
/// does not throw" happy path in isolation, without needing a database.
/// </summary>
public class TenantGuardTests
{
    private sealed class FakeCurrentTenant : ICurrentTenant
    {
        public Guid GarageId { get; init; }
        public Guid UserId { get; init; } = Guid.NewGuid();
        public string Role { get; init; } = "owner";
    }

    [Fact]
    public void EnsureOwned_MatchingTenant_DoesNotThrow()
    {
        var garageId = Guid.NewGuid();
        var currentTenant = new FakeCurrentTenant { GarageId = garageId };

        var exception = Record.Exception(() => TenantGuard.EnsureOwned(garageId, currentTenant));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureOwned_MismatchedTenant_ThrowsTenantOwnershipException()
    {
        var currentTenant = new FakeCurrentTenant { GarageId = Guid.NewGuid() };
        var otherGarageId = Guid.NewGuid();

        Assert.Throws<TenantOwnershipException>(
            () => TenantGuard.EnsureOwned(otherGarageId, currentTenant));
    }
}
