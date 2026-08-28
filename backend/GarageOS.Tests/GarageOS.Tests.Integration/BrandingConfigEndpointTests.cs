using System.Net;
using System.Text.Json;

namespace GarageOS.Tests.Integration;

/// <summary>
/// WP-7 brief §4, T4 + T6. Proves the public runtime-branding surface
/// (<c>GET /api/config/branding</c>) exposes EXACTLY the four approved non-secret
/// BrandingOptions fields -- no more, no less -- and is reachable without
/// authentication, matching appsettings.Testing.json's Branding section (see
/// IntegrationTestFixture, which boots the host under the "Testing" environment).
///
/// The "no more" half is deliberately re-asserted here at the HTTP/JSON boundary, on top
/// of ConfigControllerDependencySurfaceTests' structural (reflection) guarantee that the
/// controller cannot even reach a secret-bearing options type -- belt-and-suspenders, per
/// ConfigController's own doc comment (three independent layers).
/// </summary>
[Collection("Integration")]
public class BrandingConfigEndpointTests(IntegrationTestFixture fixture)
{
    // ASP.NET Core's default System.Text.Json output uses camelCase property names.
    private static readonly string[] ExpectedPropertyNames =
        ["productDisplayName", "emailFromName", "logoUrl", "supportEmail"];

    [Fact]
    public async Task GetBranding_Returns200_WithExactlyTheFourApprovedFields_MatchingConfiguration()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/config/branding");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var actualPropertyNames = root.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(ExpectedPropertyNames.OrderBy(n => n, StringComparer.Ordinal).ToArray(), actualPropertyNames);

        Assert.Equal("Garage Management Platform (Test)", root.GetProperty("productDisplayName").GetString());
        Assert.Equal("Garage Management Platform (Test)", root.GetProperty("emailFromName").GetString());
        Assert.Equal("https://app.garageos.example/assets/logo-test.png", root.GetProperty("logoUrl").GetString());
        Assert.Equal("support@garageos.example", root.GetProperty("supportEmail").GetString());
    }

    [Fact]
    public async Task GetBranding_ReachableWithoutAuthentication_NoAuthorizationHeaderSent()
    {
        var client = fixture.CreateClient();
        Assert.Null(client.DefaultRequestHeaders.Authorization);

        var response = await client.GetAsync("/api/config/branding");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Defensive, redundant-by-design check: even though GetBranding() hand-maps a
    // closed DTO and can therefore never accidentally serialize an unlisted field, this
    // proves the SAME thing at the actual wire boundary a client would see, and would
    // fail loudly if a future edit ever widened BrandingConfigResponse or started
    // serializing BrandingOptions directly.
    [Theory]
    [InlineData("SigningKey")]
    [InlineData("Jwt")]
    [InlineData("Issuer")]
    [InlineData("Audience")]
    [InlineData("ConnectionString")]
    [InlineData("ApiKey")]
    [InlineData("Password")]
    [InlineData("Secret")]
    public async Task GetBranding_ResponseNeverContainsAnyKnownSecretOrIdentityFieldName(string forbiddenPropertyName)
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/config/branding");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        var propertyNames = document.RootElement.EnumerateObject().Select(p => p.Name);
        Assert.DoesNotContain(propertyNames, name => string.Equals(name, forbiddenPropertyName, StringComparison.OrdinalIgnoreCase));
    }
}
