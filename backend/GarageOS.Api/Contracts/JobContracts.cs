namespace GarageOS.Api.Contracts;

// P2-WP3. Requests never contain GarageId, Id, or Status -- Id is route-supplied on
// update/transition, Create generates both server-side, GarageId is always
// currentTenant.GarageId, and Status can only ever change via the dedicated
// TransitionStatus sub-resource (never a field on Create/UpdateIntake).

public sealed record CreateJobRequest(
    Guid CustomerId, Guid VehicleId, Guid? PrimaryMechanicId, Guid? SecondaryMechanicId,
    int? MileageAtIntake, string? CustomerComplaint, string? AdvisorNotes,
    DateTimeOffset? PromisedAt, bool CustomerWaiting, string Source,
    bool Overnight, string? OvernightNote, bool IsWarrantyReturn, Guid? ParentJobId);

public sealed record UpdateJobIntakeRequest(
    Guid? PrimaryMechanicId, Guid? SecondaryMechanicId, int? MileageAtIntake,
    string? CustomerComplaint, string? AdvisorNotes, DateTimeOffset? PromisedAt,
    bool CustomerWaiting, bool Overnight, string? OvernightNote);

public sealed record TransitionJobStatusRequest(string TargetStatus, string? Reason);

public sealed record JobDto(
    Guid Id, string JobNumber, Guid CustomerId, Guid VehicleId,
    Guid? PrimaryMechanicId, Guid? SecondaryMechanicId, string Status,
    int? MileageAtIntake, string? CustomerComplaint, string? AdvisorNotes,
    DateTimeOffset? PromisedAt, bool CustomerWaiting, string Source,
    bool Overnight, string? OvernightNote, bool IsWarrantyReturn, Guid? ParentJobId,
    string? CancellationReason, string? DeletionReason,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record JobHistoryEntryDto(
    Guid Id, Guid? ActorId, string ActorName, string ActorRole,
    string EventType, string Summary, string? Detail, DateTimeOffset CreatedAt);

public sealed record FloorBoardCardDto(
    Guid JobId, string JobNumber, string CustomerDisplayName, string VehicleDisplay,
    Guid? PrimaryMechanicId, string? PrimaryMechanicName, DateTimeOffset CheckedInAt,
    DateTimeOffset? PromisedAt, bool CustomerWaiting, bool Overnight, bool IsWarrantyReturn,
    DateTimeOffset StatusUpdatedAt);

public sealed record FloorBoardColumnDto(string Status, IReadOnlyList<FloorBoardCardDto> Cards);

public sealed record FloorBoardResponse(IReadOnlyList<FloorBoardColumnDto> Columns);
