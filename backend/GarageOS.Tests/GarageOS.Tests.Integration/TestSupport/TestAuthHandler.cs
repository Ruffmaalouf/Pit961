using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GarageOS.Tests.Integration.TestSupport;

/// <summary>Temporary WP-3 scaffold (brief §9) proving the ICurrentTenant → HttpContext.User
/// claims wiring end-to-end, registered only under Environment.IsEnvironment("Testing").
/// WP-4 is expected to retire/replace this with real signed test JWTs once JWT issuance
/// exists — this must never become a permanent parallel auth mechanism alongside WP-4's
/// real one.</summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-GarageId", out var garageIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("garage_id", garageIdHeader.ToString()),
            new("sub", Request.Headers.TryGetValue("X-Test-UserId", out var userId)
                ? userId.ToString()
                : Guid.NewGuid().ToString()),
            new("role", Request.Headers.TryGetValue("X-Test-Role", out var role)
                ? role.ToString()
                : "owner"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
