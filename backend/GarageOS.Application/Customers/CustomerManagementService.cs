using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Domain.Entities;

namespace GarageOS.Application.Customers;

public sealed record CreateCustomerFields(
    string FirstName, string? LastName, string Phone, string? Whatsapp,
    string? Email, string? Notes, bool IsFleet);

public sealed record UpdateCustomerFields(
    string FirstName, string? LastName, string Phone, string? Whatsapp,
    string? Email, string? Notes, bool IsFleet);

// CustomerVehicles (not "Vehicles") deliberately -- VehicleMutationBoundaryTests'
// source scan matches any `.Vehicles.` member access as a potential AppDbContext.Vehicles
// DbSet reference; a property literally named Vehicles here would be a false-positive
// match at every call site that does `detail.Vehicles.Select(...)`, indistinguishable by
// regex from an actual tracked-reference bypass. Renaming this one property is simpler
// and more robust than teaching the scanner to disambiguate arbitrary receiver types.
public sealed record CustomerDetail(
    Customer Customer, IReadOnlyList<Vehicle> CustomerVehicles,
    CustomerJobsHistoryResult JobsHistory, CustomerBalanceSummary BalanceSummary);

/// <summary>HadOpenJobs is a warning surfaced to the caller, never a rejection -- by
/// direct analogy to Decision #5's warn-don't-block precedent for duplicate plates
/// (see technical-architect's P2-WP2 design §2.4). The delete always succeeds regardless.</summary>
public sealed record CustomerSoftDeleteResult(bool HadOpenJobs);

/// <summary>
/// P2-WP2. Application-service mutation path for Customer, mirroring
/// EstimateApprovalService's shape: fetch -> TenantGuard.EnsureOwned (defense-in-depth,
/// the global query filter should already make cross-tenant fetches return null) ->
/// authorize -> mutate via the single ICustomerMutationRepository. GarageId on create is
/// always currentTenant.GarageId -- CreateCustomerFields has no GarageId property at all,
/// so there is nothing for a client payload to override.
/// </summary>
public sealed class CustomerManagementService(
    ICustomerMutationRepository customers,
    ICustomerQueryRepository customersRead,
    IVehicleQueryRepository vehiclesRead,
    ICurrentTenant currentTenant)
{
    private const int JobsHistoryPageSize = 10;

    // Plain role-membership check, not an IBusinessRuleAuthorizer policy -- see
    // RolePermissionException's doc comment for why. "owner"/"manager" matches the exact
    // role strings already used by DiscountLimitRequirement's ManagerCapPercent gate.
    private static readonly HashSet<string> SoftDeleteAllowedRoles = new(StringComparer.Ordinal) { "owner", "manager" };

    public async Task<Customer> CreateAsync(CreateCustomerFields fields, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            GarageId = currentTenant.GarageId,
            FirstName = fields.FirstName,
            LastName = fields.LastName,
            Phone = fields.Phone,
            Whatsapp = fields.Whatsapp,
            Email = fields.Email,
            Notes = fields.Notes,
            IsFleet = fields.IsFleet,
        };
        return await customers.InsertAsync(customer, ct);
    }

    public async Task<Customer?> UpdateAsync(Guid customerId, UpdateCustomerFields fields, CancellationToken ct = default)
    {
        var existing = await customers.FindByIdAsync(customerId, ct);
        if (existing is null)
        {
            return null; // not found or cross-tenant -- the global query filter already excludes it
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant);

        await customers.UpdateAsync(
            customerId, fields.FirstName, fields.LastName, fields.Phone,
            fields.Whatsapp, fields.Email, fields.Notes, fields.IsFleet, ct);

        return await customers.FindByIdAsync(customerId, ct);
    }

    /// <summary>Returns null if not found/cross-tenant. Throws RolePermissionException if
    /// the current role isn't owner/manager.</summary>
    public async Task<CustomerSoftDeleteResult?> SoftDeleteAsync(Guid customerId, CancellationToken ct = default)
    {
        var existing = await customers.FindByIdAsync(customerId, ct);
        if (existing is null)
        {
            return null;
        }

        TenantGuard.EnsureOwned(existing.GarageId, currentTenant);

        if (!SoftDeleteAllowedRoles.Contains(currentTenant.Role))
        {
            throw new RolePermissionException("Customer.SoftDelete");
        }

        var hadOpenJobs = await customersRead.HasOpenJobsAsync(customerId, ct);
        await customers.SoftDeleteAsync(customerId, currentTenant.UserId, ct);
        return new CustomerSoftDeleteResult(hadOpenJobs);
    }

    public Task<CustomerSearchResult> SearchAsync(
        string? search, bool? isFleet, int page, int pageSize, CancellationToken ct = default) =>
        customersRead.SearchAsync(search, isFleet, page, pageSize, ct);

    /// <summary>Returns null if the customer is not found/cross-tenant, otherwise the
    /// customer's vehicles (possibly empty). Used by CustomersController's nested
    /// GET /customers/{customerId}/vehicles route without paying for the jobs-history +
    /// balance-summary composition GetDetailAsync also does.</summary>
    public async Task<IReadOnlyList<Vehicle>?> ListVehiclesForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await customersRead.FindByIdAsync(customerId, ct);
        return customer is null ? null : await vehiclesRead.ListByCustomerAsync(customerId, ct);
    }

    public async Task<CustomerDetail?> GetDetailAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await customersRead.FindByIdAsync(customerId, ct);
        if (customer is null)
        {
            return null;
        }

        var vehicles = await vehiclesRead.ListByCustomerAsync(customerId, ct);
        var jobsHistory = await customersRead.GetJobsHistoryAsync(customerId, JobsHistoryPageSize, ct);
        var balance = await customersRead.GetBalanceSummaryAsync(customerId, ct);

        return new CustomerDetail(customer, vehicles, jobsHistory, balance);
    }
}
