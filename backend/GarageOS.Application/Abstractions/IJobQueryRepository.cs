using GarageOS.Domain.Entities;

namespace GarageOS.Application.Abstractions;

public sealed record FloorBoardCard(
    Guid JobId, string JobNumber, string CustomerDisplayName, string VehicleDisplay,
    Guid? PrimaryMechanicId, string? PrimaryMechanicName, DateTimeOffset CheckedInAt,
    DateTimeOffset? PromisedAt, bool CustomerWaiting, bool Overnight, bool IsWarrantyReturn,
    DateTimeOffset StatusUpdatedAt);

public sealed record FloorBoardColumn(string Status, IReadOnlyList<FloorBoardCard> Cards);

public sealed record FloorBoardResult(IReadOnlyList<FloorBoardColumn> Columns);

/// <summary>
/// P2-WP3 read-only repository. Unlike IJobMutationRepository, this has no single-caller
/// constraint -- reads carry no tenant-guard-bypass risk, same reasoning as
/// ICustomerQueryRepository's remarks.
/// </summary>
public interface IJobQueryRepository
{
    Task<Job?> FindByIdAsync(Guid jobId, CancellationToken ct = default);

    Task<IReadOnlyList<JobHistoryEntry>> GetHistoryAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>One query, grouped in memory into JobStatuses.OpenBoardOrder's fixed column
    /// order (every column present, even empty ones, for a stable UI grid). Never uses
    /// IgnoreQueryFilters() -- the Floor Board never needs another garage's jobs, so the
    /// default tenant+soft-delete filter is exactly what's wanted.</summary>
    Task<FloorBoardResult> GetFloorBoardAsync(CancellationToken ct = default);
}
