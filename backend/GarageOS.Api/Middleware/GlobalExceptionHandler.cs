using GarageOS.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Middleware;

/// <summary>
/// Converts any unhandled exception into a consistent RFC 7807 ProblemDetails
/// response instead of leaking a framework error page or a raw stack trace.
/// Registered via <c>AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in Program.cs (WP-2 acceptance criteria:
/// "an unhandled exception returns a consistent ProblemDetails envelope").
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // WP-4 defense-in-depth mapping (brief §6): a TenantContextUnavailableException
        // reaching this handler means a request got past authentication with a claims
        // shape [Authorize(Policy = "GarageTenant")] should already have rejected (e.g. a
        // future endpoint that forgets the policy attribute). Map it to a clean 403
        // rather than the generic 500 below -- HttpContextCurrentTenant.cs itself is left
        // unchanged (WP-4 brief §16 point 1); this is purely an Api-layer safety net.
        if (exception is TenantContextUnavailableException)
        {
            logger.LogWarning(exception, "TenantContextUnavailableException reached the global handler for {Method} {Path} -- an endpoint is missing its [Authorize(Policy = \"GarageTenant\")] attribute.",
                httpContext.Request.Method, httpContext.Request.Path);

            var forbiddenDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Instance = httpContext.Request.Path,
            };
            httpContext.Response.StatusCode = forbiddenDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(
                forbiddenDetails, options: null, contentType: "application/problem+json", cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Instance = httpContext.Request.Path,
        };

        // Never leak exception details/stack traces outside Development — this is a
        // security-review requirement (no verbose error messages in non-dev environments).
        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.Message;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }
}
