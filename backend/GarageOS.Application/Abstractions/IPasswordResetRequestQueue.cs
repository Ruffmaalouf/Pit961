namespace GarageOS.Application.Abstractions;

public sealed record PasswordResetQueueItem(string Email, string? RequestedByIp);

/// <summary>
/// WP-4 brief §13 anti-enumeration mechanism. The HTTP request path
/// (AuthController.ForgotPassword) does ZERO user-existence-dependent work -- it enqueues
/// here and returns 202 immediately, regardless of TryEnqueue's result (see remarks on
/// that method). ALL existence-dependent work (DB lookup, token generation, email send)
/// happens later, off the request/response path, in the background consumer.
///
/// Bounded with a defined backpressure policy (Security Reviewer required change #3,
/// WP-4 brief review -- the brief flagged the queue's non-durability but left capacity/
/// overflow behavior unspecified): capacity 1000, drop-oldest on overflow. Rationale: a
/// dropped item is a dropped background password-reset email, never a dropped or
/// delayed HTTP response -- the response is already sent (202) before drop/no-drop is
/// even decided by the underlying bounded channel. Drop-oldest (not drop-newest/block)
/// because a stale queued request is less actionable than a fresh one under sustained
/// overflow, and blocking would violate the "returns 202 immediately" contract.
/// </summary>
public interface IPasswordResetRequestQueue
{
    /// <summary>Never throws and never blocks the caller waiting for capacity. Returns
    /// false only to allow the implementation to log an internal warning metric --
    /// callers MUST NOT branch on this return value in any way that could vary the HTTP
    /// response (that would reintroduce exactly the enumeration signal §13 removes).</summary>
    bool TryEnqueue(string email, string? requestedByIp);

    IAsyncEnumerable<PasswordResetQueueItem> ReadAllAsync(CancellationToken ct);
}
