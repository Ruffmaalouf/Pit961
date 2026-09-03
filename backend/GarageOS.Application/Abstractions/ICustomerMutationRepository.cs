using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

/// <summary>
/// P2-WP2. The single authoritative write surface for Customer rows -- mirrors the
/// Estimates precedent (IEstimateMutationRepository). Guarded by
/// CustomerMutationBoundaryTests (GarageOS.Tests.Unit.Architecture) -- a source-scan
/// architecture test proving no other production file mutates the `customers` table.
/// There is no hard-delete method anywhere on this interface, ever (DECISIONS.md #12
/// Decision #4) -- SoftDeleteAsync is the only removal path.
/// </summary>
public interface ICustomerMutationRepository
{
    /// <summary>Tenant-filtered (via AppDbContext's global query filter), AsNoTracking
    /// lookup -- same bypass-protection reasoning as EstimateMutationRepository's
    /// FindByIdAsync: callers must never hold a tracked reference outside this class.
    /// Excludes soft-deleted rows (the global filter already does this).</summary>
    Task<Customer?> FindByIdAsync(Guid customerId, CancellationToken ct = default);

    Task<Customer> InsertAsync(Customer customer, CancellationToken ct = default);

    Task UpdateAsync(
        Guid customerId, string firstName, string? lastName, string phone,
        string? whatsapp, string? email, string? notes, bool isFleet,
        CancellationToken ct = default);

    /// <summary>THE only method permitted to set Customer.DeletedAt/DeletedBy. There is
    /// no restore/undelete method in this WP's scope -- if restore is ever needed later,
    /// that is a separate, explicitly out-of-scope decision.</summary>
    Task SoftDeleteAsync(Guid customerId, Guid deletedBy, CancellationToken ct = default);
}
