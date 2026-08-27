using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class Customer : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Whatsapp { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsFleet { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
