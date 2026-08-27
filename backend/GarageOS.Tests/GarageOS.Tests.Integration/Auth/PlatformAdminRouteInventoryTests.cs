using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Tests.Integration.Auth;

/// <summary>
/// Security Reviewer required change #1 (WP-4 brief review): an automated, code-level
/// proof that ZERO live platform-admin routes exist in Phase 1 -- not deferred to WP-9,
/// and not merely "we didn't write a PlatformAdminAuthController" asserted by inspection.
/// Platform-admin/garage-tenant separation is exercised via test-only token construction
/// only (TestJwtTokenFactory) per the Owner's explicit constraint; this test locks in
/// that no live HTTP surface for platform-admin authentication has been added, so a
/// future PR that accidentally maps one fails CI immediately.
/// </summary>
[Collection("Integration")]
public class PlatformAdminRouteInventoryTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void NoMappedRouteTargetsThePlatformAdminSurface()
    {
        var endpointDataSource = fixture.Services.GetRequiredService<EndpointDataSource>();
        var routePatterns = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .ToList();

        Assert.NotEmpty(routePatterns); // sanity: the inventory itself actually found routes

        var platformRoutes = routePatterns
            .Where(pattern => pattern.Contains("platform", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(platformRoutes.Count == 0,
            $"Found unexpected platform-admin route(s), which Phase 1 must not map: {string.Join(", ", platformRoutes)}");
    }

    [Fact]
    public void NoControllerNamedPlatformAdminAuthControllerExistsInTheApiAssembly()
    {
        // Belt-and-braces reflection check, independent of route mapping: catches the
        // controller class itself being (re-)introduced even before/without it being
        // wired into routing.
        var apiAssembly = typeof(AuthController).Assembly;
        var platformAdminAuthControllerType = apiAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "PlatformAdminAuthController");

        Assert.Null(platformAdminAuthControllerType);
    }

    [Theory]
    [InlineData("/api/v1/platform/auth/login")]
    [InlineData("/api/v1/platform/auth/refresh")]
    [InlineData("/api/platform/auth/login")]
    public async Task WouldBePlatformAdminLoginRoutes_Return404(string path)
    {
        await fixture.ResetDatabaseAsync();
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(path, new { email = "admin@example.test", password = "whatever" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
