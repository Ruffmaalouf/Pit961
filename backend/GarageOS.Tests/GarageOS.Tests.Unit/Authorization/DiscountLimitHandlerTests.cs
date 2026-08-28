using System.Security.Claims;
using GarageOS.Infrastructure.Security.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace GarageOS.Tests.Unit.Authorization;

/// <summary>
/// WP-5 brief §9, mandatory boundary tests 1/2/3/6/7 (discount half). Pure unit -- hand-
/// builds a ClaimsPrincipal + AuthorizationHandlerContext, invokes the handler directly via
/// its public IAuthorizationHandler.HandleAsync(context) entrypoint. No WebApplicationFactory
/// or database needed, matching TenantGuardTests.cs's precedent for framework-free unit
/// coverage of authorization-adjacent logic.
/// </summary>
public class DiscountLimitHandlerTests
{
    private static readonly Guid GarageId = Guid.NewGuid();

    private static ClaimsPrincipal BuildPrincipal(Guid? garageId, string? role) =>
        new(new ClaimsIdentity(BuildClaims(garageId, role), authenticationType: "Test"));

    private static IEnumerable<Claim> BuildClaims(Guid? garageId, string? role)
    {
        if (garageId is not null) yield return new Claim("garage_id", garageId.Value.ToString());
        if (role is not null) yield return new Claim("role", role);
    }

    private static async Task<AuthorizationHandlerContext> RunAsync(
        ClaimsPrincipal principal, DiscountAuthorizationResource resource)
    {
        var context = new AuthorizationHandlerContext(
            new[] { new DiscountLimitRequirement() }, principal, resource);
        await new DiscountLimitHandler().HandleAsync(context);
        return context;
    }

    [Fact]
    public async Task Manager_Discount15_00Percent_Succeeds()
    {
        var principal = BuildPrincipal(GarageId, "manager");

        var context = await RunAsync(principal, new DiscountAuthorizationResource(GarageId, 15.00m));

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Manager_Discount15_01Percent_Fails_ExceedsManagerCap()
    {
        var principal = BuildPrincipal(GarageId, "manager");

        var context = await RunAsync(principal, new DiscountAuthorizationResource(GarageId, 15.01m));

        Assert.False(context.HasSucceeded);
        Assert.Contains(context.FailureReasons, r => r.Message == "exceeds_manager_cap");
    }

    [Fact]
    public async Task Owner_DiscountAbove15Percent_Succeeds()
    {
        var principal = BuildPrincipal(GarageId, "owner");

        var context = await RunAsync(principal, new DiscountAuthorizationResource(GarageId, 40.00m));

        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData("advisor")]
    [InlineData("mechanic")]
    [InlineData("accountant")]
    public async Task UnauthorizedRole_AnyDiscount_Fails_RoleNotPermitted(string role)
    {
        var principal = BuildPrincipal(GarageId, role);

        var context = await RunAsync(principal, new DiscountAuthorizationResource(GarageId, 5.00m));

        Assert.False(context.HasSucceeded);
        Assert.Contains(context.FailureReasons, r => r.Message == "role_not_permitted");
    }

    [Fact]
    public async Task TenantMismatch_Fails_TenantMismatch()
    {
        var principal = BuildPrincipal(Guid.NewGuid(), "manager");
        var otherGarageId = Guid.NewGuid();

        var context = await RunAsync(principal, new DiscountAuthorizationResource(otherGarageId, 5.00m));

        Assert.False(context.HasSucceeded);
        Assert.Contains(context.FailureReasons, r => r.Message == "tenant_mismatch");
    }

    [Fact]
    public async Task MissingGarageIdClaim_Fails_TenantMismatch()
    {
        var principal = BuildPrincipal(garageId: null, role: "manager");

        var context = await RunAsync(principal, new DiscountAuthorizationResource(GarageId, 5.00m));

        Assert.False(context.HasSucceeded);
        Assert.Contains(context.FailureReasons, r => r.Message == "tenant_mismatch");
    }
}
