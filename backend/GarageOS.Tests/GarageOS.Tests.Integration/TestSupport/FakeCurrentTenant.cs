using GarageOS.Application.Abstractions;

namespace GarageOS.Tests.Integration.TestSupport;

/// <summary>Trivial mutable ICurrentTenant for direct AppDbContext construction in
/// tenant-isolation tests (WP-3 brief §9/§14) — no HTTP involved. This is what proves
/// the global query filter works, per resource.</summary>
public sealed class FakeCurrentTenant : ICurrentTenant
{
    public Guid GarageId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "owner";
}
