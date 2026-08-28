using System.Security.Claims;
using GarageOS.Infrastructure.Security.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace GarageOS.Tests.Unit.Authorization;

/// <summary>
/// WP-5 brief §9, mandatory boundary tests 4/5/7 (approval-threshold half). Same pure-unit
/// shape as DiscountLimitHandlerTests -- hand-built ClaimsPrincipal + context, no
/// WebApplicationFactory/database.
/// </summary>
public class EstimateApprovalThresholdHandlerTests
{
    private static readonly Guid GarageId = Guid.NewGuid();

    private static ClaimsPrincipal BuildPrincipal(Guid? garageId, string role) =>
        new(new ClaimsIdentity(BuildClaims(garageId, role), authenticationType: "Test"));

    private static IEnumerable<Claim> BuildClaims(Guid? garageId, string role)
    {
        if (garageId is not null) yield return new Claim("garage_id", garageId.Value.ToString());
        yield return new Claim("role", role);
    }

    private static async Task<AuthorizationHandlerContext> RunAsync(
        ClaimsPrincipal principal, EstimateApprovalAuthorizationResource resource)
    {
        var context = new AuthorizationHandlerContext(
            new[] { new EstimateApprovalThresholdRequirement() }, principal, resource);
        await new EstimateApprovalThresholdHandler().HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task Subtotal500_00_Succeeds_DoesNotRouteToApproval()
    {
        var principal = BuildPrincipal(GarageId, "advisor");

        var context = await RunAsync(principal, new EstimateApprovalAuthorizationResource(GarageId, 500.00m));

        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("manager")]
    [InlineData("advisor")]
    public async Task Subtotal500_01_Fails_RequiresOwnerApproval_RegardlessOfActorRole(string role)
    {
        var principal = BuildPrincipal(GarageId, role);

        var context = await RunAsync(principal, new EstimateApprovalAuthorizationResource(GarageId, 500.01m));

        Assert.False(context.HasSucceeded);
        Assert.Contains(context.FailureReasons, r => r.Message == "requires_owner_approval");
    }

    [Fact]
    public async Task TenantMismatch_Fails_TenantMismatch()
    {
        var principal = BuildPrincipal(Guid.NewGuid(), "owner");
        var otherGarageId = Guid.NewGuid();

        var context = await RunAsync(principal, new EstimateApprovalAuthorizationResource(otherGarageId, 100.00m));

        Assert.False(context.HasSucceeded);
        Assert.Contains(context.FailureReasons, r => r.Message == "tenant_mismatch");
    }

    [Fact]
    public async Task MissingGarageIdClaim_Fails_TenantMismatch()
    {
        var principal = BuildPrincipal(garageId: null, role: "owner");

        var context = await RunAsync(principal, new EstimateApprovalAuthorizationResource(GarageId, 100.00m));

        Assert.False(context.HasSucceeded);
        Assert.Contains(context.FailureReasons, r => r.Message == "tenant_mismatch");
    }
}
