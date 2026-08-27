using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GarageOS.Tests.Integration.Auth;

/// <summary>
/// WP-4 brief KI-7 closure: proves JWT signature/issuer/audience/expiry validation is
/// enforced at the code level by the REAL AddJwtBearer middleware -- not merely "tokens
/// can be generated" (Owner's explicit instruction). Tokens here are hand-built (rather
/// than via TestJwtTokenFactory/ITokenService) specifically so each validation property
/// can be independently violated while holding everything else constant. The signing
/// key/issuer/audience below mirror appsettings.Testing.json's Jwt section exactly --
/// the ONE real (test-host) signing key this app's AddJwtBearer is configured to accept.
/// </summary>
[Collection("Integration")]
public class JwtValidationTests(IntegrationTestFixture fixture)
{
    private const string TestSigningKey = "VK8SQKOIInbupd9az010CtRE+bgBTNplwo4xDUr3qaI=";
    private const string TestIssuer = "garageos-tests";
    private const string TestAudience = "garageos-tests-audience";
    private const string TestKeyId = "test-1";

    private static string BuildToken(
        string? issuer = TestIssuer,
        string? audience = TestAudience,
        string signingKey = TestSigningKey,
        DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var effectiveExpires = expires ?? now.AddMinutes(15);
        var claims = new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("garage_id", Guid.NewGuid().ToString()),
            new Claim("role", "owner"),
            new Claim("jti", Guid.NewGuid().ToString()),
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)) { KeyId = TestKeyId };
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            // Always well before effectiveExpires, regardless of whether the caller
            // passed a future or (deliberately, for the expiry test) past expiry --
            // JwtSecurityToken's constructor throws if Expires <= NotBefore.
            notBefore: effectiveExpires.AddMinutes(-20),
            expires: effectiveExpires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<HttpStatusCode> CallMeWithTokenAsync(string token)
    {
        var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    [Fact]
    public async Task Me_ValidToken_PassesAuthenticationAndAuthorization()
    {
        // Baseline -- proves BuildToken produces something the real middleware accepts,
        // so every tampered variant below is a meaningful negative case rather than "any
        // junk string is 401".
        await fixture.ResetDatabaseAsync();

        var status = await CallMeWithTokenAsync(BuildToken());

        // 404 (no DB row for this synthetic garage/sub) is an acceptable "authn passed,
        // authz passed, controller ran" outcome -- the point is it is NOT 401.
        Assert.Equal(HttpStatusCode.NotFound, status);
    }

    [Fact]
    public async Task Me_TamperedSignature_ReturnsUnauthorized()
    {
        var token = BuildToken();
        var segments = token.Split('.');
        var tamperedSignature = FlipLastChar(segments[2]);
        var tampered = $"{segments[0]}.{segments[1]}.{tamperedSignature}";

        var status = await CallMeWithTokenAsync(tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task Me_WrongSigningKey_ReturnsUnauthorized()
    {
        // A different 32+ byte key -- not the one this test host's AddJwtBearer is
        // configured to accept.
        var status = await CallMeWithTokenAsync(BuildToken(signingKey: "ThisIsADifferentTestOnlySigningKeyXYZ12345"));

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task Me_WrongIssuer_ReturnsUnauthorized()
    {
        var status = await CallMeWithTokenAsync(BuildToken(issuer: "not-garageos-tests"));

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task Me_WrongAudience_ReturnsUnauthorized()
    {
        var status = await CallMeWithTokenAsync(BuildToken(audience: "not-the-real-audience"));

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task Me_ExpiredToken_ReturnsUnauthorized()
    {
        var status = await CallMeWithTokenAsync(BuildToken(expires: DateTime.UtcNow.AddMinutes(-5)));

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    private static string FlipLastChar(string segment)
    {
        var chars = segment.ToCharArray();
        var lastIndex = chars.Length - 1;
        chars[lastIndex] = chars[lastIndex] == 'A' ? 'B' : 'A';
        return new string(chars);
    }
}
