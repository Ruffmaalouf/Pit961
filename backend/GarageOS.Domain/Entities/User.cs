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

    /// <summary>Account-lockout columns (WP-4 brief §11). 5 consecutive failures ->
    /// LockoutEndAt = now + 15min; reset to 0 on successful login. A locked-out login
    /// attempt returns the SAME generic invalid-credentials response as a wrong
    /// password -- revealing "account locked" would itself be an enumeration signal.</summary>
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEndAt { get; set; }
}
