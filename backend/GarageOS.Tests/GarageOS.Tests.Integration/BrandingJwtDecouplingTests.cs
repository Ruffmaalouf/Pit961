using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using GarageOS.Api.Contracts;
using GarageOS.Tests.Integration.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace GarageOS.Tests.Integration;

/// <summary>
/// WP-7 brief §4, T5b -- the Owner's explicit strict-separation requirement:
/// BrandingOptions must NEVER determine or influence Jwt:Issuer, Jwt:Audience, JWT
/// signing configuration, or authentication identity semantics. A future brand change
/// must never invalidate a previously issued token.
///
/// Proves this end-to-end, not just at the options-binding level (see
/// BrandingOptionsBindingTests' BrandingOptions_BindsCleanly_WhenOnlyJwtKeysArePresent /
/// JwtOptions_BindsCleanly_WhenOnlyBrandingKeysArePresent for the binding-isolation half):
/// boots two hosts from the SAME compiled Program, identical Jwt:* configuration, but
/// DIFFERENT Branding:ProductDisplayName values, has each host issue a real access token
/// for the same seeded user via the real /api/v1/auth/login path, and asserts the
/// decoded iss/aud claims are byte-for-byte identical across both -- proving a branding
/// change cannot change what a token asserts about identity.
/// </summary>
[Collection("Integration")]
public class BrandingJwtDecouplingTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task ChangingProductDisplayNameAcrossTwoHosts_NeverChangesIssuedTokenIssuerOrAudience()
    {
        await fixture.ResetDatabaseAsync();
        var (_, _, user) = await AuthTestFixtures.SeedActiveUserAsync(fixture, garageName: "Jwt Decoupling Test Garage");

        // Host A: the shared fixture's default Testing-environment Branding config.
        var defaultClient = fixture.CreateClient();
        var defaultLoginResponse = await defaultClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        Assert.Equal(HttpStatusCode.OK, defaultLoginResponse.StatusCode);
        var defaultBody = await defaultLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(defaultBody);

        // Host B: same Jwt:* configuration (untouched), only Branding:ProductDisplayName
        // overridden -- a materially different brand string from Host A's.
        await using var overriddenFactory = fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Branding:ProductDisplayName"] = "A Completely Different Brand Name Inc.",
                    ["Branding:EmailFromName"] = "A Completely Different Brand Name Inc. Support",
                });
            });
        });
        var overriddenClient = overriddenFactory.CreateClient();
        var overriddenLoginResponse = await overriddenClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email, AuthTestFixtures.DefaultPassword));
        Assert.Equal(HttpStatusCode.OK, overriddenLoginResponse.StatusCode);
        var overriddenBody = await overriddenLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(overriddenBody);

        // Sanity: prove Host B's branding endpoint actually reflects the override, so a
        // no-op override wouldn't silently pass this test.
        var overriddenBrandingResponse = await overriddenClient.GetAsync("/api/config/branding");
        var overriddenBrandingBody = await overriddenBrandingResponse.Content.ReadAsStringAsync();
        Assert.Contains("A Completely Different Brand Name Inc.", overriddenBrandingBody);

        var handler = new JwtSecurityTokenHandler();
        var defaultToken = handler.ReadJwtToken(defaultBody!.AccessToken);
        var overriddenToken = handler.ReadJwtToken(overriddenBody!.AccessToken);

        Assert.Equal(defaultToken.Issuer, overriddenToken.Issuer);
        Assert.Equal(defaultToken.Audiences.OrderBy(a => a, StringComparer.Ordinal), overriddenToken.Audiences.OrderBy(a => a, StringComparer.Ordinal));

        // Both must still equal the fixed, non-secret test-only Jwt:Issuer/Audience from
        // appsettings.Testing.json -- not merely "equal to each other by coincidence".
        Assert.Equal("garageos-tests", defaultToken.Issuer);
        Assert.Contains("garageos-tests-audience", defaultToken.Audiences);
        Assert.Equal("garageos-tests", overriddenToken.Issuer);
        Assert.Contains("garageos-tests-audience", overriddenToken.Audiences);
    }
}
