using GarageOS.Application.Abstractions;
using GarageOS.Application.Accounts;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data.Seed;

/// <summary>Idempotent Development-only seed data (WP-3 brief §11; updated WP-3B).
/// Invoked from Program.cs only under app.Environment.IsDevelopment() — never
/// Production, never Testing (integration tests seed their own fixtures per-test, see
/// TwoTenantFixture). The Account is persisted directly (Accounts is a tenant root
/// with no service of its own in Phase 1); the Garage -- and its
/// GarageSettings/GarageSequence rows -- is created exclusively through
/// IAccountProvisioningService, the one authoritative path for inserting into the
/// garages table (WP-3B brief §9). This also proves GarageInsertBoundaryTests'
/// bypass-protection scan reflects reality: the seeder itself no longer does a
/// direct Garages.Add.</summary>
public static class DevelopmentSeeder
{
    public static async Task SeedAsync(AppDbContext db, IAccountProvisioningService provisioning)
    {
        if (await db.Accounts.AnyAsync(a => a.Id == SeedIds.PerformanceAutoGarageAccount))
        {
            return;
        }

        var account = new Account
        {
            Id = SeedIds.PerformanceAutoGarageAccount,
            Name = "Performance Auto Garage",
            BillingEmail = "billing@performanceautogarage.example",
            SubscriptionStatus = "trial",
            Plan = "pro",
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        // Ordering note: CreateGarageUnderAccountAsync does a real DB round-trip
        // (SELECT ... FOR UPDATE against accounts), not a change-tracker-aware lookup,
        // so the Account above must already be persisted before this call.
        var garage = await provisioning.CreateGarageUnderAccountAsync(
            account.Id,
            new GarageProvisioningDetails(
                Name: "Performance Auto Garage",
                Phone: "+961 1 234 567",
                Address: "Beirut, Lebanon",
                Id: SeedIds.PerformanceAutoGarage));

        var users = new[]
        {
            new User { Id = SeedIds.UserRalph, GarageId = garage.Id, Email = "ralph@performanceautogarage.example", PasswordHash = "seed-only-not-a-real-hash", Name = "Ralph", Role = "owner" },
            new User { Id = SeedIds.UserSarahKhalil, GarageId = garage.Id, Email = "sarah.khalil@performanceautogarage.example", PasswordHash = "seed-only-not-a-real-hash", Name = "Sarah Khalil", Role = "advisor" },
            new User { Id = SeedIds.UserAhmedHassan, GarageId = garage.Id, Email = "ahmed.hassan@performanceautogarage.example", PasswordHash = "seed-only-not-a-real-hash", Name = "Ahmed Hassan", Role = "mechanic" },
            new User { Id = SeedIds.UserHassanAli, GarageId = garage.Id, Email = "hassan.ali@performanceautogarage.example", PasswordHash = "seed-only-not-a-real-hash", Name = "Hassan Ali", Role = "mechanic" },
            new User { Id = SeedIds.UserMaya, GarageId = garage.Id, Email = "maya@performanceautogarage.example", PasswordHash = "seed-only-not-a-real-hash", Name = "Maya", Role = "accountant" },
        };
        db.Users.AddRange(users);

        var customers = new[]
        {
            new Customer { Id = SeedIds.CustomerJohnSmith, GarageId = garage.Id, FirstName = "John", LastName = "Smith", Phone = "+961 70 111 222" },
            new Customer { Id = SeedIds.CustomerNourKhalil, GarageId = garage.Id, FirstName = "Nour", LastName = "Khalil", Phone = "+961 70 222 333" },
            new Customer { Id = SeedIds.CustomerWalidFares, GarageId = garage.Id, FirstName = "Walid", LastName = "Fares", Phone = "+961 70 333 444" },
            new Customer { Id = SeedIds.CustomerRaniaSaade, GarageId = garage.Id, FirstName = "Rania", LastName = "Saade", Phone = "+961 70 444 555" },
            new Customer { Id = SeedIds.CustomerKarimAbouZeid, GarageId = garage.Id, FirstName = "Karim", LastName = "Abou Zeid", Phone = "+961 70 555 666" },
            new Customer { Id = SeedIds.CustomerElieNassar, GarageId = garage.Id, FirstName = "Elie", LastName = "Nassar", Phone = "+961 70 666 777" },
        };
        db.Customers.AddRange(customers);

        var vehicles = new[]
        {
            new Vehicle { Id = SeedIds.VehicleBmw328i, GarageId = garage.Id, CustomerId = SeedIds.CustomerJohnSmith, PlateNumber = "A12345", Make = "BMW", Model = "328i", Year = 2011, CurrentMileage = 91850 },
            new Vehicle { Id = SeedIds.VehicleMercedesC300, GarageId = garage.Id, CustomerId = SeedIds.CustomerNourKhalil, PlateNumber = "B23456", Make = "Mercedes-Benz", Model = "C300", Year = 2014 },
            new Vehicle { Id = SeedIds.VehicleBmwX5, GarageId = garage.Id, CustomerId = SeedIds.CustomerWalidFares, PlateNumber = "C34567", Make = "BMW", Model = "X5", Year = 2009 },
            new Vehicle { Id = SeedIds.VehicleGolfGti, GarageId = garage.Id, CustomerId = SeedIds.CustomerRaniaSaade, PlateNumber = "D45678", Make = "Volkswagen", Model = "Golf GTI", Year = 2017 },
            new Vehicle { Id = SeedIds.VehicleAudiA4, GarageId = garage.Id, CustomerId = SeedIds.CustomerKarimAbouZeid, PlateNumber = "E56789", Make = "Audi", Model = "A4", Year = 2016 },
            new Vehicle { Id = SeedIds.VehicleWranglerRubicon, GarageId = garage.Id, CustomerId = SeedIds.CustomerElieNassar, PlateNumber = "F67890", Make = "Jeep", Model = "Wrangler Rubicon", Year = 2019 },
        };
        db.Vehicles.AddRange(vehicles);

        await db.SaveChangesAsync();
    }
}
