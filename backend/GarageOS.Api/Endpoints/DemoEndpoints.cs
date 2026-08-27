using GarageOS.Application.Configuration;
using Microsoft.Extensions.Options;

namespace GarageOS.Api.Endpoints;

/// <summary>
/// WP-2 demonstration endpoint proving the options-binding pattern works end-to-end:
/// change <c>Demo:Message</c> in appsettings.json (or override it with the
/// <c>Demo__Message</c> environment variable) and observe it reflected here, not
/// hardcoded. Removed/replaced once real feature endpoints exist.
/// </summary>
public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/demo/config", (IOptions<DemoOptions> options) =>
                Results.Ok(new { message = options.Value.Message }))
            .WithName("GetDemoConfig")
            .WithTags("Demo");

        return app;
    }
}
