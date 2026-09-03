using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Tests.Integration.TenantIsolation;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Tests.Integration.Customers;

/// <summary>
/// P2-WP2. HTTP-level tests for CustomersController -- proves the real route table,
/// [Authorize(Policy = "GarageTenant")] gate, and CustomerManagementService wiring work
/// end-to-end against a real PostgreSQL instance, complementing (not duplicating)
/// CustomersTenantIsolationTests' DbContext-level checks. Uses TestJwtTokenFactory's real
/// ITokenService-backed bearer tokens, same pattern as MeEndpointTests.
/// </summary>
[Collection("Integration")]
public class CustomersApiTests(IntegrationTestFixture fixture)
{
    private async Task<(string Token, Guid GarageId)> SeedAuthenticatedUserAsync(string role = "owner")
    {
        var (_, garage, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture, role: role);
        var tokenService = fixture.Services.GetRequiredService<ITokenService>();
        var token = TestJwtTokenFactory.CreateGarageTenantToken(tokenService, user, garage.Name);
        return (token, garage.Id);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task CreateThenGetDetail_RoundTrips()
    {
        await fixture.ResetDatabaseAsync();
        var (token, _) = await SeedAuthenticatedUserAsync();
        var client = fixture.CreateClient();

        var createRequest = Authorized(HttpMethod.Post, "/api/v1/customers", token);
        createRequest.Content = JsonContent.Create(new CreateCustomerRequest(
            "Jane", "Doe", "+961 70 111 222", null, "jane@example.test", "VIP customer", false));
        var createResponse = await client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(created);
        Assert.Equal("Jane", created!.FirstName);

        var detailRequest = Authorized(HttpMethod.Get, $"/api/v1/customers/{created.Id}", token);
        var detailResponse = await client.SendAsync(detailRequest);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<CustomerDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Customer.Id);
        Assert.Empty(detail.Vehicles);
        Assert.Equal(0, detail.JobsHistory.TotalJobCount);
        Assert.Equal(0m, detail.BalanceSummary.OutstandingBalance);
    }

    [Fact]
    public async Task Update_ChangesPersistedFields()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId) = await SeedAuthenticatedUserAsync();
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var client = fixture.CreateClient();

        var updateRequest = Authorized(HttpMethod.Put, $"/api/v1/customers/{customer.Id}", token);
        updateRequest.Content = JsonContent.Create(new UpdateCustomerRequest(
            "Updated", "Name", "+961 70 999 000", null, null, null, true));
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal("Updated", updated!.FirstName);
        Assert.True(updated.IsFleet);
    }

    [Fact]
    public async Task GetDetail_CrossTenantId_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();
        var (token, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var otherGarageCustomer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Get, $"/api/v1/customers/{otherGarageCustomer.Id}", token);
        var response = await client.SendAsync(request);

        // Never leaks cross-tenant existence via a distinct status from a genuine
        // not-found (GlobalExceptionHandler's TenantOwnershipException -> 404 mapping,
        // and the AppDbContext global query filter returning null before that even fires).
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_CrossTenantId_ReturnsNotFound()
    {
        await fixture.ResetDatabaseAsync();
        var (token, _) = await SeedAuthenticatedUserAsync();
        var tenants = new TwoTenantFixture();
        await tenants.SeedAsync(fixture);
        var otherGarageCustomer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, tenants.TenantB.Garage.Id);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Put, $"/api/v1/customers/{otherGarageCustomer.Id}", token);
        request.Content = JsonContent.Create(new UpdateCustomerRequest("X", null, "+961 70 000 000", null, null, null, false));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SoftDelete_AsMechanic_ReturnsForbidden()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId) = await SeedAuthenticatedUserAsync(role: "mechanic");
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var client = fixture.CreateClient();

        var request = Authorized(HttpMethod.Delete, $"/api/v1/customers/{customer.Id}", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SoftDelete_AsOwner_Succeeds_AndExcludesFromSubsequentList()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId) = await SeedAuthenticatedUserAsync(role: "owner");
        var customer = await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var client = fixture.CreateClient();

        var deleteRequest = Authorized(HttpMethod.Delete, $"/api/v1/customers/{customer.Id}", token);
        var deleteResponse = await client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deleteBody = await deleteResponse.Content.ReadFromJsonAsync<CustomerSoftDeleteResponse>();
        Assert.False(deleteBody!.HadOpenJobs);

        var getRequest = Authorized(HttpMethod.Get, $"/api/v1/customers/{customer.Id}", token);
        var getResponse = await client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var listRequest = Authorized(HttpMethod.Get, "/api/v1/customers", token);
        var listResponse = await client.SendAsync(listRequest);
        var list = await listResponse.Content.ReadFromJsonAsync<CustomerListResponse>();
        Assert.DoesNotContain(list!.Items, i => i.Id == customer.Id);
    }

    [Fact]
    public async Task Search_FiltersByIsFleet()
    {
        await fixture.ResetDatabaseAsync();
        var (token, garageId) = await SeedAuthenticatedUserAsync();
        await ResourceSeedHelpers.SeedCustomerAsync(fixture, garageId);
        var client = fixture.CreateClient();

        // Seed one fleet customer directly (ResourceSeedHelpers.SeedCustomerAsync doesn't
        // parameterize IsFleet) via the real create endpoint instead.
        var createRequest = Authorized(HttpMethod.Post, "/api/v1/customers", token);
        createRequest.Content = JsonContent.Create(new CreateCustomerRequest(
            "Fleet", "Co", "+961 70 333 444", null, null, null, true));
        await client.SendAsync(createRequest);

        var listRequest = Authorized(HttpMethod.Get, "/api/v1/customers?isFleet=true", token);
        var listResponse = await client.SendAsync(listRequest);
        var list = await listResponse.Content.ReadFromJsonAsync<CustomerListResponse>();

        Assert.All(list!.Items, i => Assert.True(i.IsFleet));
        Assert.Contains(list.Items, i => i.FirstName == "Fleet");
    }

    [Fact]
    public async Task NoBearerToken_ReturnsUnauthorized()
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
