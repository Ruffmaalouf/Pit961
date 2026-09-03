using GarageOS.Application.Abstractions;

namespace GarageOS.Application.Jobs;

/// <summary>
/// P2-WP3. Thin pass-through over IJobQueryRepository.GetFloorBoardAsync -- the fixed-column
/// grouping/ordering logic lives in the Infrastructure repository (one query, grouped in
/// memory into JobStatuses.OpenBoardOrder), not here; this service exists purely so
/// JobsController depends on an Application-layer service rather than an
/// Infrastructure-layer repository directly, matching every other controller in this
/// codebase.
/// </summary>
public sealed class FloorBoardService(IJobQueryRepository jobsRead)
{
    public Task<FloorBoardResult> GetBoardAsync(CancellationToken ct = default) =>
        jobsRead.GetFloorBoardAsync(ct);
}
