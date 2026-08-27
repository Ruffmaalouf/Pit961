using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Tests.Integration.Auth;

/// <summary>
/// WP-4 brief §15. Proves the auth-login sliding-window rate limiter actually rejects
/// with 429 once its configured permit limit is exceeded -- the production-sized limits
/// (5/min etc., Program.cs) are NOT exercised against the shared IntegrationTestFixture
/// (see its ConfigureWebHost remarks: one TestServer == one rate-limiter partition store
/// shared by the entire "Integration" collection's functional tests, which run with
/// generous appsettings.Testing.json overrides instead). This test builds a fully
/// independent WebApplicationFactory&lt;Program&gt; (deliberately NOT composed on top of
/// the shared fixture via WithWebHostBuilder -- composing two separate ConfigureWebHost/
/// ConfigureAppConfiguration registrations left the override's precedence ambiguous in
/// practice) so it can dial the limit down to something a single test can trigger
/// deterministically, without disturbing every other test's budget. Points at the same
/// physical database as the shared fixture (fixture.ConnectionString) purely so the host
/// boots cleanly -- this test never touches a real user row.
/// </summary>
[Collection("Integration")]
public class RateLimitingTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Login_ExceedsConfiguredPermitLimit_ReturnsTooManyRequestsWithRetryAfter()
    {
        await fixture.ResetDatabaseAsync();

        using var isolatedFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:GarageOsDb"] = fixture.ConnectionString,
                    ["ConnectionStrings:PlatformDb"] = fixture.ConnectionString,
                    ["RateLimiting:AuthLogin:PermitLimit"] = "2",
                    ["RateLimiting:AuthLogin:WindowSeconds"] = "60",
                }));
        });
        var client = isolatedFactory.CreateClient();

        // Credentials don't need to be valid -- the limiter runs ahead of routing to the
        // controller, so even two failed attempts consume the budget.
        var request = new LoginRequest("nobody@example.test", "WhateverPassword1");
        var first = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        var second = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        var third = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.True(third.Headers.RetryAfter is not null || third.Headers.Contains("Retry-After"));
        Assert.Equal("application/problem+json", third.Content.Headers.ContentType?.MediaType);
    }
}
