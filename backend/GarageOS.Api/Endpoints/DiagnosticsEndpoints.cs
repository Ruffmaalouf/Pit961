namespace GarageOS.Api.Endpoints;

/// <summary>
/// Test-only diagnostic endpoints. <see cref="MapDiagnosticsEndpoints"/> is only
/// ever called for the Development and Testing environments (see Program.cs) —
/// this surface never exists in Production. Exists so WP-2's integration test
/// suite can exercise <c>GlobalExceptionHandler</c>'s ProblemDetails shape
/// against a real unhandled exception thrown by the running app, without
/// shipping a permanent "throw an error" endpoint to customers.
/// </summary>
public static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/diagnostics/throw", () =>
            {
                throw new InvalidOperationException("WP-2 diagnostic endpoint: deliberate test exception.");
#pragma warning disable CS0162 // unreachable code by design
                return Results.Ok();
#pragma warning restore CS0162
            })
            .WithName("DiagnosticsThrow")
            .WithTags("Diagnostics")
            .ExcludeFromDescription();

        return app;
    }
}
