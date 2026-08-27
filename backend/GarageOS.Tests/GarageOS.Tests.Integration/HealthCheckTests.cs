using System.Net;

namespace GarageOS.Tests.Integration;

/// <summary>WP-2 harness-proof test: the app actually starts under
/// <see cref="IntegrationTestFixture"/> (real WebApplicationFactory + real Postgres
/// connection) and serves a 200 from /health.</summary>
[Collection("Integration")]
public class HealthCheckTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
