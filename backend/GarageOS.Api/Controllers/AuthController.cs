using System.Text.RegularExpressions;
using GarageOS.Api.Contracts;
using GarageOS.Application.Abstractions;
using GarageOS.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GarageOS.Api.Controllers;

/// <summary>
/// WP-4 brief §12. Thin controller -- all business logic lives in AuthService
/// (Application layer); this class only maps HTTP <-> AuthService calls, manages the
/// refresh-token cookie, and (for forgot-password only) talks directly to
/// IPasswordResetRequestQueue instead of AuthService, per the anti-enumeration design
/// (brief §13: the request path must do ZERO user-existence-dependent work).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService, IPasswordResetRequestQueue passwordResetQueue) : ControllerBase
{
    /// <summary>Public so integration tests can extract/forward the cookie explicitly
    /// (WebApplicationFactory's HttpClient does not persist cookies across calls the way a
    /// browser would) -- see CookieTestHelpers.</summary>
    public const string RefreshTokenCookieName = "garageos_refresh_token";
    private const string RefreshTokenCookiePath = "/api/v1/auth";
    private static readonly Regex EmailFormatRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, RemoteIp, ct);
        if (!result.Success)
        {
            return InvalidCredentials();
        }

        SetRefreshTokenCookie(result.RawRefreshToken!, result.RefreshTokenExpiresAt!.Value);

        var user = result.User!;
        return Ok(new LoginResponse(
            result.AccessToken!, result.AccessTokenExpiresAt!.Value,
            new LoginUserPayload(user.Id, user.GarageId, user.GarageName, user.Email, user.Name, user.Role)));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        // Cookie is the primary source; a body field is the documented fallback for
        // non-browser clients (WP-4 brief §12).
        var presentedToken = Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieValue)
            ? cookieValue
            : request?.RefreshToken;

        var result = await authService.RefreshAsync(presentedToken ?? string.Empty, RemoteIp, Request.Headers.UserAgent.ToString(), ct);
        if (!result.Success)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid or expired refresh token." });
        }

        SetRefreshTokenCookie(result.RawRefreshToken!, result.RefreshTokenExpiresAt!.Value);
        return Ok(new RefreshResponse(result.AccessToken!, result.AccessTokenExpiresAt!.Value));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var presentedToken = Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieValue) ? cookieValue : null;
        await authService.LogoutAsync(presentedToken, ct);
        ClearRefreshTokenCookie();
        return NoContent(); // 204 always, idempotent -- brief §12.
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-forgot-password")]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Format validation ONLY -- no DB touch, no branch on whether the email exists.
        // Even a malformed email gets the identical 202 (brief §13: "the HTTP request
        // path does ZERO user-existence-dependent work"). TryEnqueue's return value is
        // deliberately ignored -- see IPasswordResetRequestQueue's remarks.
        if (EmailFormatRegex.IsMatch(request.Email))
        {
            passwordResetQueue.TryEnqueue(request.Email, RemoteIp);
        }

        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await authService.ResetPasswordAsync(request.Token, request.NewPassword, ct);
        if (!result.Success)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = result.ErrorMessage ?? "Invalid request." });
        }

        return Ok();
    }

    [HttpGet("me")]
    [Authorize(Policy = "GarageTenant")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await authService.GetMeAsync(ct);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new MeResponse(user.Id, user.GarageId, user.GarageName, user.Email, user.Name, user.Role));
    }

    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private IActionResult InvalidCredentials() =>
        Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid email or password." });

    private void SetRefreshTokenCookie(string rawToken, DateTimeOffset expiresAt)
    {
        Response.Cookies.Append(RefreshTokenCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = RefreshTokenCookiePath,
            Expires = expiresAt,
        });
    }

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = RefreshTokenCookiePath });
}
