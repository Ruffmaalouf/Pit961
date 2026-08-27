using GarageOS.Application.Abstractions;
using GarageOS.Application.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GarageOS.Infrastructure.BackgroundJobs;

/// <summary>
/// WP-4 brief §13 consumer. Runs for the process lifetime, reading off
/// IPasswordResetRequestQueue and performing ALL existence-dependent work
/// (AuthService.ProcessForgotPasswordRequestAsync) off the HTTP request/response path.
/// AuthService/AppDbContext are scoped services, so each item gets its own DI scope --
/// this class itself is registered as a singleton hosted service.
/// </summary>
public sealed class PasswordResetRequestBackgroundService(
    IPasswordResetRequestQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PasswordResetRequestBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
                await authService.ProcessForgotPasswordRequestAsync(item.Email, item.RequestedByIp, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one bad item crash the consumer loop -- at-most-once delivery
                // is an accepted tradeoff (brief §13); a failure here just means that one
                // reset email never goes out, which the user can retry.
                logger.LogError(ex, "PasswordResetRequestBackgroundService: failed to process a queued request.");
            }
        }
    }
}
