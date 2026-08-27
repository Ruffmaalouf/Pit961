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
