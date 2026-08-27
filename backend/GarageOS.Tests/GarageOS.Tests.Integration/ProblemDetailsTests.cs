using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    [Fact]
    public async Task UnhandledException_OutsideDevelopment_DoesNotIncludeExceptionExtensionField()
    {
        // KI-4: GlobalExceptionHandler only adds the "exception" extension field in
        // Development (verified correct by code review and by Security Reviewer during
        // WP-2's security gate) -- this asserts the field is genuinely ABSENT under
        // "Testing" (the environment WebApplicationFactory sets here, and the environment
        // this whole test suite actually runs under), not just that the fields the happy
        // path checks are present.
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/diagnostics/throw");
        var rawBody = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(rawBody);
        Assert.False(document.RootElement.TryGetProperty("exception", out _),
            "ProblemDetails response must not include the 'exception' extension field outside Development.");
    }

    private sealed record ProblemDetailsPayload(string? Type, string? Title, int Status, string? Instance);
}
