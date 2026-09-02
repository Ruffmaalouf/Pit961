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
        var tamperedSignature = FlipSignatureBit(segments[2]);
        var tampered = $"{segments[0]}.{segments[1]}.{tamperedSignature}";

        var status = await CallMeWithTokenAsync(tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public void FlipSignatureBit_AlwaysProducesADifferentDecodedByteSequence()
    {
        // Regression guard for KI-18: the previous FlipLastChar implementation toggled the
        // ENCODED STRING's last character ('A'<->'B') rather than the underlying signature
        // bytes. For a 32-byte HMAC-SHA256 signature, base64url's final character carries
        // only 4 real bits (the other 2 are zero-padding), and 'A' (000000) / 'B' (000001)
        // share identical top-4 bits -- so whenever the real signature's last character was
        // already 'A' (~1/16 of runs, since BuildToken mints a fresh random signature every
        // call), the "tampered" string decoded back to the IDENTICAL bytes, making the
        // request cryptographically valid and turning this negative test into an
        // intermittent false pass. FlipSignatureBit instead flips a full byte after
        // decoding, which is decode-safe and deterministic on every run. This test iterates
        // many synthetic signatures (fixed seed, for reproducibility) since a single random
        // sample would not reliably catch a probabilistic bug.
        var random = new Random(12345);
        for (var i = 0; i < 1000; i++)
        {
            var original = new byte[32];
            random.NextBytes(original);
            var encoded = Base64UrlEncoder.Encode(original);

            var flipped = FlipSignatureBit(encoded);
            var decoded = Base64UrlEncoder.DecodeBytes(flipped);

            Assert.False(
                original.AsSpan().SequenceEqual(decoded),
                $"FlipSignatureBit produced a byte-identical signature for an input " +
                $"ending in '{encoded[^1]}' (iteration {i}) -- this is the KI-18 regression.");
        }
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

    /// <summary>
    /// Deterministically invalidates a base64url-encoded JWT signature segment by decoding
    /// it to raw bytes, flipping every bit of the FIRST byte, then re-encoding -- not by
    /// toggling a character in the encoded *string* (KI-18's root cause: see the regression
    /// test above). The first raw byte's encoding is always a full, padding-free base64
    /// group for any signature this long, so flipping it guarantees the decoded bytes differ
    /// on every run, deterministically (unlike the old FlipLastChar, which touched the final
    /// character's zero-padding bits ~1/16 of the time and produced a byte-identical,
    /// cryptographically-valid "tampered" token).
    /// </summary>
    private static string FlipSignatureBit(string signatureSegment)
    {
        var bytes = Base64UrlEncoder.DecodeBytes(signatureSegment);
        bytes[0] ^= 0xFF;
        return Base64UrlEncoder.Encode(bytes);
    }
}
