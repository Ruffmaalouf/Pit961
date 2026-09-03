using GarageOS.Domain.Common;

namespace GarageOS.Domain.Entities;

public class Customer : ITenantOwned, ISoftDeletable
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

    /// <summary>P2-WP2 / DECISIONS.md #12 Decision #4: Customer has no hard-delete path,
    /// ever. Historical Job/Estimate/Invoice/Payment rows referencing this customer must
    /// remain fully resolvable after deletion, which soft delete provides structurally.
    /// Implementing ISoftDeletable is sufficient by itself for AppDbContext's centralized
    /// query filter (ApplyTenantQueryFilters) to exclude this row from default queries --
    /// no per-entity HasQueryFilter call needed (see CustomerConfiguration remarks).</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Who removed this record, for audit purposes. Nullable FK to Users,
    /// ON DELETE SET NULL (a user being removed later must never break this audit trail).
    /// Naming matches the established convention on Job.DeletedBy, not a bespoke
    /// "DeletedByUserId".</summary>
    public Guid? DeletedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
