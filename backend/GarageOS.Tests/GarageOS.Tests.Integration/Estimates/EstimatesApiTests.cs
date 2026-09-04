using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Domain.Entities;
using GarageOS.Tests.Integration.TenantIsolation;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Tests.Integration.Estimates;

/// <summary>
/// P2-WP4. HTTP-level tests for EstimatesController against real PostgreSQL, real
/// EstimateDiscountService/EstimateApprovalService/EstimateManagementService, and the real
/// AspNetBusinessRuleAuthorizer (DiscountLimitHandler/EstimateApprovalThresholdHandler) --
/// mirrors JobsApiTests' shape exactly. Covers the Owner's explicit boundary-test list: the
/// 15.00%/15.01% Manager discount cap, Owner unrestricted discount, the $500.00/$500.01
/// approval threshold, Owner-only clearing (Manager denied), customer approval independent
/// of owner approval, revision supersession with approval-state reset, cross-tenant
/// rejection (both direct access and ParentEstimateId), and same-row concurrency.
/// </summary>
[Collection("Integration")]
public class EstimatesApiTests(IntegrationTestFixture fixture)
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

    private static EstimateItemRequest Item(decimal quantity, decimal unitPrice, string description = "Item") =>
        new("part", description, null, quantity, 0m, unitPrice, 0);

    private async Task<EstimateDto> CreateEstimateViaApiAsync(
        HttpClient client, string token, Guid jobId, params EstimateItemRequest[] items)
    {
        var request = Authorized(HttpMethod.Post, "/api/v1/estimates", token);
        request.Content = JsonContent.Create(new CreateEstimateRequest(jobId, "standard", null, items));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.NotNull(dto);
        return dto!;
    }

    [Fact]
    public async Task Create_ComputesSubtotalServerSide_FromItems_NeverFromClient()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync();
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();

        var estimate = await CreateEstimateViaApiAsync(
            client, token, job.Id, Item(2, 100m, "Brake pads"), Item(1, 300m, "Labor"));

        Assert.Equal(500.00m, estimate.Subtotal); // 2*100 + 1*300 -- computed server-side
        Assert.Equal(500.00m, estimate.Total);
        Assert.Equal(0m, estimate.DiscountAmount);
        Assert.Equal("draft", estimate.Status);
        Assert.Equal(1, estimate.RevisionNumber);
        Assert.Null(estimate.ParentEstimateId);
    }

    [Fact]
    public async Task Create_CrossTenantJobId_IsRejected_BeforeAnyRowPersisted()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageIdA, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Post, "/api/v1/estimates", token);
        request.Content = JsonContent.Create(new CreateEstimateRequest(jobB.Id, "standard", null, [Item(1, 100m)]));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageIdA });
        Assert.Equal(0, await db.Estimates.CountAsync());
    }

    [Fact]
    public async Task GetById_CrossTenantEstimate_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();
        var (token, _, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var estimateB = await ResourceSeedHelpers.SeedEstimateAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);
        var client = fixture.CreateClient();

        var response = await client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/estimates/{estimateB.Id}", token));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Discount: 15.00%/15.01% Manager cap boundary, Owner unrestricted ------------

    [Fact]
    public async Task Discount_ManagerAtExactly15Percent_IsAllowed()
    {
        await fixture.ResetDatabaseAsync();
        var (ownerToken, garageId, _) = await SeedAuthenticatedUserAsync();
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, ownerToken, job.Id, Item(1, 1000m));

        var manager = await ResourceSeedHelpers.SeedUserAsync(fixture, garageId, "manager");
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var managerToken = TestJwtTokenFactory.CreateGarageTenantToken(tokenService, manager, "Test Garage");

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/discount", managerToken);
        request.Content = JsonContent.Create(new ApplyDiscountRequest(15.00m));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal(150.00m, updated!.DiscountAmount);
    }

    [Fact]
    public async Task Discount_ManagerAt15_01Percent_IsDenied_NoWriteOccurs()
    {
        await fixture.ResetDatabaseAsync();
        var (ownerToken, garageId, _) = await SeedAuthenticatedUserAsync();
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, ownerToken, job.Id, Item(1, 1000m));

        var manager = await ResourceSeedHelpers.SeedUserAsync(fixture, garageId, "manager");
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var managerToken = TestJwtTokenFactory.CreateGarageTenantToken(tokenService, manager, "Test Garage");

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/discount", managerToken);
        request.Content = JsonContent.Create(new ApplyDiscountRequest(15.01m));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await db.Estimates.SingleAsync(e => e.Id == estimate.Id);
        Assert.Equal(0m, stored.DiscountAmount);
    }

    [Fact]
    public async Task Discount_OwnerAbove15Percent_IsAllowed()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 1000m));

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/discount", token);
        request.Content = JsonContent.Create(new ApplyDiscountRequest(40m));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal(400.00m, updated!.DiscountAmount);
    }

    // ---- Locked states: P2-WP4 QA gate finding B1 / Owner Decision #3 ----------------
    // A decided-or-in-flight estimate is never silently edited in place -- only "draft" may
    // have its items/discount changed or be (re)submitted; a customer decision may only be
    // recorded against "sent". Anything else routes through CreateRevisionAsync instead.

    [Fact]
    public async Task Discount_AlreadySentEstimate_IsRejected_NoWriteOccurs()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 200m)); // below threshold
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token)); // -> "sent"

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/discount", token);
        request.Content = JsonContent.Create(new ApplyDiscountRequest(10m));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await db.Estimates.SingleAsync(e => e.Id == estimate.Id);
        Assert.Equal(0m, stored.DiscountAmount);
        Assert.Equal("sent", stored.Status);
    }

    [Fact]
    public async Task ReplaceItems_AlreadySentEstimate_IsRejected_ItemsUnchanged()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 200m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token)); // -> "sent"

        var request = Authorized(HttpMethod.Put, $"/api/v1/estimates/{estimate.Id}/items", token);
        request.Content = JsonContent.Create(new ReplaceEstimateItemsRequest([Item(5, 999m, "Sneaked-in item")]));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var items = await db.EstimateItems.Where(i => i.EstimateId == estimate.Id).ToListAsync();
        Assert.Single(items);
        Assert.Equal(200m, items[0].UnitPrice);
    }

    [Fact]
    public async Task Submit_AlreadySentEstimate_IsRejected_StatusUnchanged()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 200m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token)); // -> "sent"

        var response = await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await db.Estimates.SingleAsync(e => e.Id == estimate.Id);
        Assert.Equal("sent", stored.Status);
    }

    [Fact]
    public async Task CustomerApproval_AlreadyDecidedEstimate_IsRejected_DoesNotOverwritePriorDecision()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 200m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token)); // -> "sent"

        var firstApproval = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        firstApproval.Content = JsonContent.Create(new RecordCustomerApprovalRequest("approved", "in_person", "Jane"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(firstApproval)).StatusCode);

        // A second, different decision against the same already-decided row must never
        // silently overwrite the first -- no revision, no audit trail.
        var secondApproval = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        secondApproval.Content = JsonContent.Create(new RecordCustomerApprovalRequest("rejected", "phone", "Someone else"));
        var response = await client.SendAsync(secondApproval);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await db.Estimates.SingleAsync(e => e.Id == estimate.Id);
        Assert.Equal("approved", stored.Status);
        Assert.Equal("in_person", stored.ApprovalMethod);
        Assert.Equal("Jane", stored.ApprovedByName);
    }

    [Fact]
    public async Task CustomerApproval_NotYetSentEstimate_IsRejected()
    {
        // Recording a customer decision before the estimate has ever been sent (still
        // "draft") is meaningless -- the customer cannot have decided on something they
        // were never shown.
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 200m)); // still draft

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        request.Content = JsonContent.Create(new RecordCustomerApprovalRequest("approved", "in_person", "Jane"));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await db.Estimates.SingleAsync(e => e.Id == estimate.Id);
        Assert.Equal("draft", stored.Status);
    }

    // ---- Customer decision allow-list: P2-WP4 QA gate finding B2 ---------------------
    // `RecordCustomerApprovalRequest.Decision` is client input, never an authoritative
    // Status value -- it must be rejected outright unless it is exactly one of the three
    // real customer-decision outcomes. This is the single most direct approval-threshold
    // bypass surface in the API: without this allow-list, any authenticated tenant role
    // could set Status to any of the 7 DB-check-constraint-permitted values, including
    // "pending_owner_approval"-adjacent values engineered to dodge the $500 threshold.

    [Theory]
    [InlineData("sent")]
    [InlineData("pending_owner_approval")]
    [InlineData("draft")]
    [InlineData("superseded")]
    [InlineData("not_a_real_status")]
    public async Task CustomerApproval_DecisionOutsideAllowList_IsRejected_StatusUnchanged(string invalidDecision)
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 900m)); // above $500 threshold
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token)); // -> pending_owner_approval

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        request.Content = JsonContent.Create(new RecordCustomerApprovalRequest(invalidDecision, "in_person", "Jane"));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await db.Estimates.SingleAsync(e => e.Id == estimate.Id);
        // Still pending_owner_approval -- an invalid decision must never bypass the $500
        // owner-approval gate by routing Status somewhere else entirely.
        Assert.Equal("pending_owner_approval", stored.Status);
    }

    [Theory]
    [InlineData("approved")]
    [InlineData("partially_approved")]
    [InlineData("rejected")]
    public async Task CustomerApproval_DecisionInAllowList_IsAccepted(string validDecision)
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 200m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token));

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        request.Content = JsonContent.Create(new RecordCustomerApprovalRequest(validDecision, "in_person", "Jane"));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal(validDecision, updated!.Status);
    }

    // ---- $500.00 / $500.01 approval threshold, Owner Decision #2 ---------------------

    [Fact]
    public async Task Submit_AtExactly500_RoutesToSent_NoOwnerApprovalNeeded()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync();
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 500m));

        var response = await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal("sent", updated!.Status);
    }

    [Fact]
    public async Task Submit_Above500_RoutesToPendingOwnerApproval_RegardlessOfActorRole()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner"); // Owner included per the handler's role-blind design
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 500.01m));

        var response = await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // reroute, not a rejection
        var updated = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal("pending_owner_approval", updated!.Status);
    }

    [Fact]
    public async Task ClearOwnerApproval_OwnerRole_Succeeds_MovesToSent()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 900m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token));

        var response = await client.SendAsync(
            Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/clear-owner-approval", token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal("sent", updated!.Status);
    }

    [Fact]
    public async Task ClearOwnerApproval_ManagerRole_IsForbidden_StatusUnchanged()
    {
        // Owner Decision #2: only Owner may clear pending_owner_approval.
        await fixture.ResetDatabaseAsync();
        var (ownerToken, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, ownerToken, job.Id, Item(1, 900m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", ownerToken));

        var manager = await ResourceSeedHelpers.SeedUserAsync(fixture, garageId, "manager");
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var managerToken = TestJwtTokenFactory.CreateGarageTenantToken(tokenService, manager, "Test Garage");

        var response = await client.SendAsync(
            Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/clear-owner-approval", managerToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await db.Estimates.SingleAsync(e => e.Id == estimate.Id);
        Assert.Equal("pending_owner_approval", stored.Status);
    }

    // ---- Customer approval is independent of Owner approval ---------------------------

    [Fact]
    public async Task CustomerApproval_RecordedWithoutTouchingOwnerApprovalState()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "manager");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 200m)); // below threshold
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token));

        var request = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        request.Content = JsonContent.Create(new RecordCustomerApprovalRequest("approved", "in_person", "Jane Customer"));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal("approved", updated!.Status);
        Assert.Equal("in_person", updated.ApprovalMethod);
        Assert.Equal("Jane Customer", updated.ApprovedByName);
        Assert.NotNull(updated.ApprovedAt);
    }

    // ---- Revisioning: supersession, independent approval-state reset -----------------

    [Fact]
    public async Task CreateRevision_SupersedesParent_AndResetsApprovalState()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 300m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token));
        var approveRequest = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        approveRequest.Content = JsonContent.Create(new RecordCustomerApprovalRequest("approved", "in_person", "Jane"));
        await client.SendAsync(approveRequest);

        var response = await client.SendAsync(
            Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/revisions", token));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var revision = await response.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal(estimate.Id, revision!.ParentEstimateId);
        Assert.Equal(2, revision.RevisionNumber);
        Assert.Equal("draft", revision.Status); // restarts independently
        Assert.Null(revision.ApprovalMethod);
        Assert.Null(revision.ApprovedByName);
        Assert.Null(revision.ApprovedAt);
        Assert.Single(revision.Items); // items carried forward

        var parentResponse = await client.SendAsync(Authorized(HttpMethod.Get, $"/api/v1/estimates/{estimate.Id}", token));
        var parent = await parentResponse.Content.ReadFromJsonAsync<EstimateDto>();
        Assert.Equal("superseded", parent!.Status);
    }

    [Fact]
    public async Task SupersededEstimate_RejectsDiscountSubmitAndCustomerApproval()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 300m));
        await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/revisions", token));

        var discountRequest = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/discount", token);
        discountRequest.Content = JsonContent.Create(new ApplyDiscountRequest(5m));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(discountRequest)).StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.SendAsync(Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/submit", token))).StatusCode);

        var approvalRequest = Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimate.Id}/customer-approval", token);
        approvalRequest.Content = JsonContent.Create(new RecordCustomerApprovalRequest("approved", "in_person", "Jane"));
        Assert.Equal(HttpStatusCode.Conflict, (await client.SendAsync(approvalRequest)).StatusCode);
    }

    [Fact]
    public async Task CreateRevision_CrossTenantParentEstimateId_IsRejected_NoRevisionPersisted()
    {
        await fixture.ResetDatabaseAsync();
        var (token, _, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var jobB = await ResourceSeedHelpers.SeedJobAsync(fixture, tenants.TenantB.Garage.Id);
        var estimateB = await ResourceSeedHelpers.SeedEstimateAsync(fixture, tenants.TenantB.Garage.Id, jobB.Id);
        var client = fixture.CreateClient();

        var response = await client.SendAsync(
            Authorized(HttpMethod.Post, $"/api/v1/estimates/{estimateB.Id}/revisions", token));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var db = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = tenants.TenantB.Garage.Id });
        Assert.Equal(1, await db.Estimates.CountAsync(e => e.JobId == jobB.Id)); // still just the one seeded row
    }

    // ---- Concurrency: real Postgres, real xmin, real compare-and-swap ----------------

    [Fact]
    public async Task ConcurrentDiscount_TwoWritersRaceSameRow_SecondCommitThrowsConflict_FirstWriteNotLost()
    {
        // P2-WP4 QA gate finding M1: the prior version of this test never actually raced --
        // it applied one discount, then merely re-read the committed result. This version
        // forces a genuine two-DbContext race against real PostgreSQL, exercising the real
        // EF Core `xmin` optimistic-concurrency token (Estimate.IsRowVersion()) directly,
        // the same mechanism EstimateMutationRepository.UpdateDiscountAsync relies on.
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 1000m));

        // Two independent DbContexts each load their OWN tracked copy of the SAME row
        // BEFORE either writer commits -- mirrors two concurrent HTTP requests each running
        // EstimateDiscountService.ApplyDiscountAsync, whose own
        // IEstimateMutationRepository.UpdateDiscountAsync does an identical tracked
        // SingleAsync fetch internally. Both trackedA and trackedB capture the SAME
        // pre-race `xmin`.
        await using var dbA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        await using var dbB = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var trackedA = await dbA.Estimates.SingleAsync(e => e.Id == estimate.Id);
        var trackedB = await dbB.Estimates.SingleAsync(e => e.Id == estimate.Id);

        // Writer A commits first -- a real 10% discount, bumping the row's xmin in Postgres.
        trackedA.DiscountAmount = 100.00m;
        trackedA.Total = 900.00m;
        await dbA.SaveChangesAsync();

        // Writer B still holds the PRE-A xmin captured above (its own SingleAsync ran
        // before A's SaveChangesAsync committed). Its write must be rejected outright, not
        // silently overwrite or merge with A's already-committed discount -- the real
        // Postgres/EF optimistic-concurrency mechanism, not a simulated failure.
        trackedB.DiscountAmount = 200.00m;
        trackedB.Total = 800.00m;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());

        await using var verifyDb = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var stored = await verifyDb.Estimates.SingleAsync(e => e.Id == estimate.Id);
        Assert.Equal(100.00m, stored.DiscountAmount); // A's write survived; B's was rejected, never lost or merged
    }

    [Fact]
    public async Task ConcurrentCreateRevision_OnlyOneWins_NoDuplicateRevisionNumbers()
    {
        // P2-WP4: same-revision concurrency -- two actors racing to re-quote the same
        // Estimate must never both succeed and produce two revisions both claiming
        // RevisionNumber 2.
        await fixture.ResetDatabaseAsync();
        var (token, garageId, _) = await SeedAuthenticatedUserAsync(role: "owner");
        var job = await ResourceSeedHelpers.SeedJobAsync(fixture, garageId);
        var client = fixture.CreateClient();
        var estimate = await CreateEstimateViaApiAsync(client, token, job.Id, Item(1, 300m));

        // Two independent EstimateMutationRepository instances, each against its own
        // DbContext (mirrors two concurrent requests), both racing CreateRevisionAsync
        // against the SAME parent, started from the same pre-race parent state.
        await using var dbA = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        await using var dbB = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var repoA = new GarageOS.Infrastructure.Data.Estimates.EstimateMutationRepository(dbA);
        var repoB = new GarageOS.Infrastructure.Data.Estimates.EstimateMutationRepository(dbB);
        // Two distinct EstimateItem instances (each gets its own client-generated Id) --
        // sharing one instance/list between both calls would produce a spurious
        // pk_estimate_items collision that has nothing to do with the actual race being
        // proven here (the parent's xmin-guarded UPDATE).
        List<EstimateItem> NewItems() => new() { new() { Type = "part", Description = "Item", Quantity = 1, UnitPrice = 300m } };

        var resultA = await repoA.CreateRevisionAsync(estimate.Id, NewItems());

        await Assert.ThrowsAsync<GarageOS.Application.Common.EstimateConcurrencyConflictException>(
            () => repoB.CreateRevisionAsync(estimate.Id, NewItems()));

        await using var verifyDb = fixture.CreateAppDbContext(new FakeCurrentTenant { GarageId = garageId });
        var revisions = await verifyDb.Estimates.Where(e => e.JobId == job.Id).ToListAsync();
        Assert.Equal(2, revisions.Count); // original (now superseded) + exactly one new revision
        Assert.Single(revisions, e => e.RevisionNumber == 2);
        Assert.Equal(resultA.Id, revisions.Single(e => e.RevisionNumber == 2).Id);
        Assert.Equal("superseded", revisions.Single(e => e.Id == estimate.Id).Status);
    }

    [Fact]
    public async Task NoBearerToken_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/estimates", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
