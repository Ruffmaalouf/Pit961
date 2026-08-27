namespace GarageOS.Application.Accounts;

/// <summary>Input to <see cref="Abstractions.IAccountProvisioningService.CreateGarageUnderAccountAsync"/>
/// (WP-3B brief §2). <see cref="Id"/> is optional and exists only so
/// <c>DevelopmentSeeder</c> can preserve its existing well-known seed GUID
/// (<c>SeedIds.PerformanceAutoGarage</c>) — real callers (the future Phase 6 signup flow)
/// should leave it null and let the service assign a new Guid.</summary>
public sealed record GarageProvisioningDetails(
    string Name,
    string? Phone = null,
    string? Address = null,
    string? LogoUrl = null,
    Guid? Id = null);
