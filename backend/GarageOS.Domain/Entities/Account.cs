namespace GarageOS.Domain.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public string SubscriptionStatus { get; set; } = "trial";
    public string Plan { get; set; } = "pro";
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
