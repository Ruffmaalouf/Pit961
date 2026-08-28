using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace GarageOS.Tests.Integration;

/// <summary>
/// WP-7 brief §4, T2. Proves ProductDisplayName/EmailFromName propagate to the running
/// app purely from CONFIGURATION -- no recompile, no redeploy of a new binary -- by
/// booting a SECOND host from the exact same already-built <see cref="Program"/> entry
/// point (via <see cref="IntegrationTestFixture.WithWebHostBuilder"/>) with only its
/// configuration overridden, and showing the response changes while every other
/// structural aspect (connection strings, email double, environment) stays identical to
/// the shared fixture's own default host.
///
/// This is the closest an in-process test can get to "the same shipped binary, a
/// different config file/env var" -- <see cref="Program"/> is compiled exactly once for
/// the whole test run; only <see cref="IConfiguration"/> input differs between the two
/// hosts used here.
/// </summary>
[Collection("Integration")]
public class BrandingConfigPropagationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ProductDisplayNameAndEmailFromName_ChangeAcrossTwoHosts_FromConfigurationAlone_SameCompiledBinary()
    {
        // Host A: the shared fixture's own default Testing-environment configuration.
        using var defaultClient = fixture.CreateClient();
        var defaultResponse = await defaultClient.GetAsync("/api/config/branding");
        Assert.Equal(HttpStatusCode.OK, defaultResponse.StatusCode);
        using var defaultDoc = JsonDocument.Parse(await defaultResponse.Content.ReadAsStringAsync());
        Assert.Equal("Garage Management Platform (Test)", defaultDoc.RootElement.GetProperty("productDisplayName").GetString());
        Assert.Equal("Garage Management Platform (Test)", defaultDoc.RootElement.GetProperty("emailFromName").GetString());

        // Host B: same Program, same DB/email test wiring (inherited from the fixture's
        // own ConfigureWebHost), but with Branding:ProductDisplayName/EmailFromName
        // overridden purely via configuration -- appended AFTER the fixture's own
        // configuration sources, so it wins for exactly these two keys.
        await using var overriddenFactory = fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Branding:ProductDisplayName"] = "Acme Test Override Motors",
                    ["Branding:EmailFromName"] = "Acme Test Override Motors Support",
                });
            });
        });

        using var overriddenClient = overriddenFactory.CreateClient();
        var overriddenResponse = await overriddenClient.GetAsync("/api/config/branding");
        Assert.Equal(HttpStatusCode.OK, overriddenResponse.StatusCode);
        using var overriddenDoc = JsonDocument.Parse(await overriddenResponse.Content.ReadAsStringAsync());
        Assert.Equal("Acme Test Override Motors", overriddenDoc.RootElement.GetProperty("productDisplayName").GetString());
        Assert.Equal("Acme Test Override Motors Support", overriddenDoc.RootElement.GetProperty("emailFromName").GetString());

        // LogoUrl/SupportEmail were left unconfigured on Host B and still bind from the
        // same underlying appsettings.Testing.json the base fixture uses -- proving the
        // override above is scoped to exactly the two keys supplied, not a wholesale
        // config replacement.
        Assert.Equal("https://app.garageos.example/assets/logo-test.png", overriddenDoc.RootElement.GetProperty("logoUrl").GetString());
        Assert.Equal("support@garageos.example", overriddenDoc.RootElement.GetProperty("supportEmail").GetString());
    }
}
