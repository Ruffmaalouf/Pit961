namespace GarageOS.Domain.Entities;

using GarageOS.Domain.Common;

public class User : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "mechanic";
    public bool IsActive { get; set; } = true;
    public string? AvatarUrl { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
