using System.Net;
using System.Net.Http.Json;

namespace GarageOS.Tests.Integration;

/// <summary>WP-2 QA requirement: an integration test confirming the ProblemDetails
/// error shape an unhandled exception produces, exercised against the real running
/// app (not a unit test of the handler in isolation).</summary>
[Collection("Integration")]
public class ProblemDetailsTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task UnhandledException_ReturnsProblemDetailsEnvelope()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/diagnostics/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem!.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal("/api/diagnostics/throw", problem.Instance);
    }

    private sealed record ProblemDetailsPayload(string? Type, string? Title, int Status, string? Instance);
}
