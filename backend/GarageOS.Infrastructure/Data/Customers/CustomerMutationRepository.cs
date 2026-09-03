using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Customers;

/// <summary>
/// P2-WP2. The single Infrastructure class permitted to mutate Customer rows -- enforced
/// by CustomerMutationBoundaryTests' source-scan. Mirrors EstimateMutationRepository's
/// AsNoTracking-on-read / fresh-tracked-re-fetch-on-write pattern exactly, for the same
/// reason: a tracked entity handed back from FindByIdAsync could otherwise have its
/// in-memory properties mutated by an unrelated caller and silently flushed by the next
/// unrelated SaveChangesAsync on the same per-request AppDbContext instance.
/// </summary>
public sealed class CustomerMutationRepository(AppDbContext db) : ICustomerMutationRepository
{
    public Task<Customer?> FindByIdAsync(Guid customerId, CancellationToken ct = default) =>
        db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == customerId, ct);

    public async Task<Customer> InsertAsync(Customer customer, CancellationToken ct = default)
    {
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    public async Task UpdateAsync(
        Guid customerId, string firstName, string? lastName, string phone,
        string? whatsapp, string? email, string? notes, bool isFleet,
        CancellationToken ct = default)
    {
        var customer = await db.Customers.SingleAsync(c => c.Id == customerId, ct);
        customer.FirstName = firstName;
        customer.LastName = lastName;
        customer.Phone = phone;
        customer.Whatsapp = whatsapp;
        customer.Email = email;
        customer.Notes = notes;
        customer.IsFleet = isFleet;
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(Guid customerId, Guid deletedBy, CancellationToken ct = default)
    {
        var customer = await db.Customers.SingleAsync(c => c.Id == customerId, ct);
        customer.DeletedAt = DateTimeOffset.UtcNow;
        customer.DeletedBy = deletedBy;
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
