using GarageOS.Domain.Entities;
using GarageOS.Infrastructure.Data;

namespace GarageOS.Tests.Integration.TestSupport;

/// <summary>Shared seed helpers for the WP-3 tenant-isolation test suite (brief §14).
/// Each method opens its own short-lived AppDbContext (writes are never filtered — only
/// queries are — so a throwaway FakeCurrentTenant is fine for inserts) and returns the
/// persisted entity, keeping the 12 per-resource test files small.</summary>
public static class ResourceSeedHelpers
{
    public static async Task<Customer> SeedCustomerAsync(IntegrationTestFixture fixture, Guid garageId)
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var customer = new Customer { GarageId = garageId, FirstName = "Seed", LastName = "Customer", Phone = "+961 70 000 000" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    public static async Task<Vehicle> SeedVehicleAsync(IntegrationTestFixture fixture, Guid garageId, Guid customerId)
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var vehicle = new Vehicle
        {
            GarageId = garageId,
            CustomerId = customerId,
            PlateNumber = $"T{Guid.NewGuid():N}"[..8].ToUpperInvariant(), // P2-WP2: kept normalized (uppercase) -- real writes always normalize plates before storing (VehicleManagementService), so seeded fixtures should reflect realistic stored data too.
            Make = "Seed",
            Model = "Vehicle",
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    public static async Task<User> SeedUserAsync(IntegrationTestFixture fixture, Guid garageId, string role = "mechanic")
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var user = new User
        {
            GarageId = garageId,
            Email = $"seed+{Guid.NewGuid():N}@example.test",
            PasswordHash = "test-only-not-a-real-hash",
            Name = "Seed User",
            Role = role,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>Seeds a full customer + vehicle + user + job chain for one garage.</summary>
    public static async Task<Job> SeedJobAsync(IntegrationTestFixture fixture, Guid garageId)
    {
        var customer = await SeedCustomerAsync(fixture, garageId);
        var vehicle = await SeedVehicleAsync(fixture, garageId, customer.Id);
        var user = await SeedUserAsync(fixture, garageId, "owner");

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var job = new Job
        {
            GarageId = garageId,
            JobNumber = $"J-{Guid.NewGuid():N}"[..10],
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            CreatedBy = user.Id,
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    public static async Task<Estimate> SeedEstimateAsync(IntegrationTestFixture fixture, Guid garageId, Guid jobId)
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var estimate = new Estimate { GarageId = garageId, JobId = jobId };
        db.Estimates.Add(estimate);
        await db.SaveChangesAsync();
        return estimate;
    }

    public static async Task<Invoice> SeedInvoiceAsync(IntegrationTestFixture fixture, Guid garageId, Guid jobId)
    {
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var invoice = new Invoice
        {
            GarageId = garageId,
            JobId = jobId,
            InvoiceNumber = $"INV-{Guid.NewGuid():N}"[..10],
            Status = "unpaid",
            Subtotal = 100m,
            Total = 100m,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }
}
