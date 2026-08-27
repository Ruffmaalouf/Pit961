using System.Threading.Channels;
using GarageOS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GarageOS.Infrastructure.BackgroundJobs;

/// <summary>WP-4 brief §13, bounded per Security Reviewer's required change -- see
/// IPasswordResetRequestQueue's remarks for the full capacity/overflow rationale.</summary>
public sealed class PasswordResetRequestQueue : IPasswordResetRequestQueue
{
    private const int Capacity = 1000;
    private readonly Channel<PasswordResetQueueItem> _channel;
    private readonly ILogger<PasswordResetRequestQueue> _logger;

    public PasswordResetRequestQueue(ILogger<PasswordResetRequestQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<PasswordResetQueueItem>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public bool TryEnqueue(string email, string? requestedByIp)
    {
        var wrote = _channel.Writer.TryWrite(new PasswordResetQueueItem(email, requestedByIp));
        if (!wrote)
        {
            // Should be unreachable with DropOldest (TryWrite always succeeds by dropping
            // the oldest item instead of failing) except in the channel's Complete()/
            // shutdown race -- logged defensively, never surfaced to the caller (see
            // interface remarks: the HTTP response must never vary on this).
            _logger.LogWarning("PasswordResetRequestQueue: failed to enqueue a request (queue completing?).");
        }
        return wrote;
    }

    public IAsyncEnumerable<PasswordResetQueueItem> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
