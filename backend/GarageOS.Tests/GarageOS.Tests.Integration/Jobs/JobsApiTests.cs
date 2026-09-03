using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Domain.Common;
using GarageOS.Tests.Integration.TenantIsolation;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Tests.Integration.Jobs;

/// <summary>
/// P2-WP3. HTTP-level tests for JobsController -- closes the genuinely new coverage gaps
/// P2-WP3_ARCHITECTURE.md §8 identifies (none satisfied by the pre-existing DbContext-level
/// JobsTenantIsolationTests/JobHistoryTenantIsolationTests files, since none of this HTTP
/// surface existed before this WP): real cross-tenant 404s through the actual controller,
/// real create-path cross-tenant parent rejection, FloorBoardService isolation, and
/// JobStatusService write-side isolation (including the zero-history-rows-on-rejection
/// property). Mirrors CustomersApiTests' shape exactly.
/// </summary>
[Collection("Integration")]
public class JobsApiTests(IntegrationTestFixture fixture)
{
    private async Task<(string Token, Guid GarageId, Guid UserId)> SeedAuthenticatedUserAsync(string role = "owner")
    {
        var (_, garage, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture, role: role);
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var token = TestJwtTokenFactory.CreateGarageTenantToken(tokenService, user, garage.Name);
        return (token, garage.Id, user.Id);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<JobDto> CreateJobViaApiAsync(HttpClient client, string token, Guid customerId, Guid vehicleId)
    {
        var request = Authorized(HttpMethod.Post, "/api/v1/jobs", token);
        request.Content = JsonContent.Create(new CreateJobRequest(
            customerId, vehicleId, null, null, null, "Noisy brakes", null,
            null, false, "walk_in", false, null, false, null));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<JobDto>();
        Assert.NotNull(dto);
        return dto!;
    }

    [Fact]
    public async Task Create_AllocatesSequentialJobNumber_AndDefaultsToCheckedIn()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync();
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var vehicle = await ResourceSeedHelpers.SeedVehicleAsync(fixture, garageId, customer.Id);
        var client = fixture.CreateClient();

        var job = await CreateJobViaApiAsync(client, token, customer.Id, vehicle.Id);

        Assert.Equal(JobStatuses.CheckedIn, job.Status);
        Assert.StartsWith("J-", job.JobNumber);
    }

    [Fact]
    public async Task Create_CrossTenantCustomerId_IsRejected_BeforeAnyRowPersisted()
    {
        // §8 gap 2: the existing JobsTenantIsolationTests explicitly only prove the
        // DbContext-level plumbing, not that a malicious payload through the real create
        // path is rejected. This is the real version.
        await fixture.ResetDatabaseAsync();
        var (token, garageIdA, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerB = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);
        var vehicleB = await ResourceSeedHelpers.SeedVehicleAsync(fixture, tenants.TenantB.Garage.Id, customerB.Id);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Post, "/api/v1/jobs", token);
        request.Content = JsonContent.Create(new CreateJobRequest(
            customerB.Id, vehicleB.Id, null, null, null, null, null,
            null, false, "walk_in", false, null, false, null));
        var response = await client.SendAsync(request);

        // TenantGuard.EnsureOwned throws TenantOwnershipException -> 404 (never leaks
        // cross-tenant existence via a distinct status), matching GlobalExceptionHandler's
        // established policy.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageIdA });
        Assert.Equal(0, await db.Jobs.CountAsync());
    }

    [Fact]
    public async Task TransitionStatus_CrossTenantJob_ReturnsNotFound_NeverA403()
    {
        // §8 gap 1: real HTTP-level cross-tenant rejection through the actual controller,
        // not just TenantGuard.EnsureOwned called directly.
        await fixture.ResetDatabaseAsync();
        var (token, _, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Post, $"/api/v1/jobs/{jobB.Id}/status-transitions", token);
        request.Content = JsonContent.Create(new TransitionJobStatusRequest(JobStatuses.EstimatePending, null));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TransitionStatus_CrossTenantJob_WritesZeroHistoryRows()
    {
        // §8 gap 4b: the rejection must happen before the atomic write, not as a
        // compensating rollback after -- prove zero JobHistoryEntry rows result.
        await fixture.ResetDatabaseAsync();
        var (token, _, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Post, $"/api/v1/jobs/{jobB.Id}/status-transitions", token);
        request.Content = JsonContent.Create(new TransitionJobStatusRequest(JobStatuses.EstimatePending, null));
        await client.SendAsync(request);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantB.Garage.Id });
        Assert.Equal(0, await db.JobHistory.CountAsync(h => h.JobId == jobB.Id));
    }

    [Fact]
    public async Task TransitionStatus_ValidTransition_UpdatesStatus_AndWritesOneHistoryRow()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var vehicle = await ResourceSeedHelpers.SeedVehicleAsync(fixture, garageId, customer.Id);
        var client = fixture.CreateClient();
        var job = await CreateJobViaApiAsync(client, token, customer.Id, vehicle.Id);

        var request = Authorized(HttpMethod.Post, $"/api/v1/jobs/{job.Id}/status-transitions", token);
        request.Content = JsonContent.Create(new TransitionJobStatusRequest(JobStatuses.EstimatePending, "Front desk triage"));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<JobDto>();
        Assert.Equal(JobStatuses.EstimatePending, updated!.Status);

        var historyRequest = Authorized(HttpMethod.Get, $"/api/v1/jobs/{job.Id}/history", token);
        var historyResponse = await client.SendAsync(historyRequest);
        var history = await historyResponse.Content.ReadFromJsonAsync<List<JobHistoryEntryDto>>();
        Assert.Single(history!);
        Assert.Equal("status_transition", history![0].EventType);
    }

    [Fact]
    public async Task TransitionStatus_InvalidSkipAhead_ReturnsBadRequest()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var vehicle = await ResourceSeedHelpers.SeedVehicleAsync(fixture, garageId, customer.Id);
        var client = fixture.CreateClient();
        var job = await CreateJobViaApiAsync(client, token, customer.Id, vehicle.Id);

        var request = Authorized(HttpMethod.Post, $"/api/v1/jobs/{job.Id}/status-transitions", token);
        request.Content = JsonContent.Create(new TransitionJobStatusRequest(JobStatuses.InProgress, null)); // skips estimate/approval
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TransitionStatus_MechanicDispatching_ReturnsForbidden()
    {
        await fixture.ResetDatabaseAsync();
        var (ownerToken, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var vehicle = await ResourceSeedHelpers.SeedVehicleAsync(fixture, garageId, customer.Id);
        var client = fixture.CreateClient();
        var job = await CreateJobViaApiAsync(client, ownerToken, customer.Id, vehicle.Id);

        // A mechanic in the SAME garage (not SeedAuthenticatedUserAsync, which always seeds
        // a fresh garage -- that would conflate "cross-tenant" with "wrong role").
        var mechanic = await ResourceSeedHelpers.SeedUserAsync(fixture, garageId, "mechanic");
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var mechanicToken = TestJwtTokenFactory.CreateGarageTenantToken(tokenService, mechanic, "Test Garage");

        var request = Authorized(HttpMethod.Post, $"/api/v1/jobs/{job.Id}/status-transitions", mechanicToken);
        request.Content = JsonContent.Create(new TransitionJobStatusRequest(JobStatuses.EstimatePending, null));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FloorBoard_OnlyIncludesOwnTenantJobs_AndExcludesClosedStates()
    {
        // §8 gap 3: FloorBoardService isolation -- proves the Customer/Vehicle/User joins
        // GetFloorBoardAsync performs don't accidentally widen the result across tenants,
        // and that closed/cancelled/deleted jobs never appear on the board.
        await fixture.ResetDatabaseAsync();
        var (token, garageIdA, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);

        var customerA = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageIdA);
        var vehicleA = await ResourceSeedHelpers.SeedVehicleAsync(fixture, garageIdA, customerA.Id);
        var client = fixture.CreateClient();
        var openJob = await CreateJobViaApiAsync(client, token, customerA.Id, vehicleA.Id);

        // A same-garage job pushed to a closed-set status must NOT appear on the board.
        var closedJobSeed = await ResourceSeedHelpers.SeedJobAsync(fixture, garageIdA);
        await using (var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageIdA }))
        {
            var tracked = await db.Jobs.SingleAsync(j => j.Id == closedJobSeed.Id);
            tracked.Status = JobStatuses.Cancelled;
            await db.SaveChangesAsync();
        }

        // Another garage's open job -- must never appear on Garage A's board.
        await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);

        var request = Authorized(HttpMethod.Get, "/api/v1/jobs/floor-board", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var board = await response.Content.ReadFromJsonAsync<FloorBoardResponse>();
        Assert.NotNull(board);

        var allCardIds = board!.Columns.SelectMany(c => c.Cards).Select(c => c.JobId).ToList();
        Assert.Contains(openJob.Id, allCardIds);
        Assert.DoesNotContain(closedJobSeed.Id, allCardIds);

        // Fixed column order, every column present (even empty).
        Assert.Equal(JobStatuses.OpenBoardOrder, board.Columns.Select(c => c.Status).ToList());

        var checkedInColumn = board.Columns.Single(c => c.Status == JobStatuses.CheckedIn);
        var card = checkedInColumn.Cards.Single(c => c.JobId == openJob.Id);
        Assert.Contains(customerA.FirstName, card.CustomerDisplayName);
        Assert.Contains(vehicleA.PlateNumber, card.VehicleDisplay);
    }

    [Fact]
    public async Task Create_CrossTenantParentJobId_IsRejected_BeforeAnyRowPersisted()
    {
        // Security-review finding (P2-WP3 gate): ParentJobId was accepted with no ownership
        // check at all. Real regression test through the actual create path.
        await fixture.ResetDatabaseAsync();
        var (token, garageIdA, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var customerA = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageIdA);
        var vehicleA = await ResourceSeedHelpers.SeedVehicleAsync(fixture, garageIdA, customerA.Id);
        var parentJobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Post, "/api/v1/jobs", token);
        request.Content = JsonContent.Create(new CreateJobRequest(
            customerA.Id, vehicleA.Id, null, null, null, null, null,
            null, false, "walk_in", false, null, false, parentJobB.Id));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageIdA });
        Assert.Equal(0, await db.Jobs.CountAsync());
    }

    [Fact]
    public async Task TransitionStatusAsync_StaleFromStatus_ThrowsConcurrencyConflict_NotSilentOverwrite()
    {
        // QA/Security-review finding (P2-WP3 gate, both independently caught it): a bare
        // xmin token doesn't catch the race between JobStatusService's earlier validation
        // read and JobMutationRepository.TransitionStatusAsync's own fresh re-read. This
        // exercises the real repository (real Postgres, real xmin, real compare-and-swap)
        // directly -- not through the fake repository JobStatusServiceTests uses -- proving
        // the fix: a transition validated against a `fromStatus` the row no longer has must
        // be rejected, never silently applied on top of whatever the row actually holds now.
        await fixture.ResetDatabaseAsync();
        var (token, garageId, userId) = await SeedAuthenticatedUserAsync(role: "owner");
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var vehicle = await ResourceSeedHelpers.SeedVehicleAsync(fixture, garageId, customer.Id);
        var client = fixture.CreateClient();
        var job = await CreateJobViaApiAsync(client, token, customer.Id, vehicle.Id);

        // Actor A's transition genuinely commits checked_in -> estimate_pending first.
        var firstRequest = Authorized(HttpMethod.Post, $"/api/v1/jobs/{job.Id}/status-transitions", token);
        firstRequest.Content = JsonContent.Create(new TransitionJobStatusRequest(JobStatuses.EstimatePending, null));
        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Actor B's transition was validated against the STALE pre-commit status
        // (checked_in) -- simulated directly against a real JobMutationRepository backed by
        // a FakeCurrentTenant-scoped AppDbContext (not fixture.Services -- that resolves
        // HttpContextCurrentTenant, which throws outside a real HTTP request; same pattern
        // CustomersTenantIsolationTests' QA-remediation tests already use for
        // CustomerQueryRepository), since forcing two real HTTP requests into that exact
        // interleaving isn't reliably reproducible. Real Postgres, real row, real
        // compare-and-swap: this must throw JobConcurrencyConflictException, never silently
        // apply on top of the now-current estimate_pending status.
        await using var dbForStaleWrite = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var mutationRepo = new GarageOS.Infrastructure.Data.Jobs.JobMutationRepository(dbForStaleWrite);
        await Assert.ThrowsAsync<JobConcurrencyConflictException>(() => mutationRepo.TransitionStatusAsync(
            job.Id, fromStatus: JobStatuses.CheckedIn, toStatus: JobStatuses.Cancelled,
            actorId: userId, actorRole: "owner", reason: null));

        // The job must still be estimate_pending (A's real commit), never cancelled.
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var current = await db.Jobs.SingleAsync(j => j.Id == job.Id);
        Assert.Equal(JobStatuses.EstimatePending, current.Status);

        // And exactly one history row exists -- A's, not a second one from the rejected B.
        Assert.Equal(1, await db.JobHistory.CountAsync(h => h.JobId == job.Id));
    }

    [Fact]
    public async Task NoBearerToken_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/jobs/floor-board");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
