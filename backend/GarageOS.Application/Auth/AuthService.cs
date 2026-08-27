namespace GarageOS.Application.Auth;

using GarageOS.Application.Abstractions;
using GarageOS.Application.Common;
using GarageOS.Application.Configuration;
using GarageOS.Domain.Entities;

/// <summary>
/// WP-4 brief §12. Orchestrates the garage-tenant authentication foundation entirely
/// through Application-layer abstractions -- no EF/ASP.NET Core dependency here, matching
/// the codebase's existing framework-free-Application pattern (ICurrentTenant,
/// TenantGuard). AuthController (Api layer) is a thin adapter mapping these results onto
/// HTTP status codes/cookies; ProcessForgotPasswordRequestAsync is called ONLY by the
/// background consumer (WP-4 brief §13), never directly from the controller.
/// </summary>
public sealed class AuthService(
    IUserAuthLookupRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokens,
    IPasswordResetTokenRepository passwordResetTokens,
    IEmailService emailService,
    JwtOptions jwtOptions,
    PasswordResetOptions passwordResetOptions)
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MinPasswordLength = 10;
    private const int MaxPasswordLength = 128;

    // Plain option-object parameters (not IOptions<T>) -- GarageOS.Application carries no
    // Microsoft.Extensions.Options package reference, matching its existing
    // framework-free-Application pattern (ICurrentTenant, TenantGuard). Program.cs still
    // registers JwtOptions/PasswordResetOptions through the normal AddOptions<T>()
    // .BindConfiguration().ValidateOnStart() pipeline (so the mandatory boot-time
    // validation still runs) and additionally projects out the resolved .Value for direct
    // injection here.
    private readonly JwtOptions _jwt = jwtOptions;
    private readonly PasswordResetOptions _passwordReset = passwordResetOptions;

    public async Task<LoginResult> LoginAsync(string email, string password, string? requestIp, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email, ct);

        // Every failure mode below returns the identical generic LoginResult.Failure() --
        // wrong email, wrong password, inactive account, locked-out account are all
        // indistinguishable to the caller (WP-4 brief §12: "401 generic ... for every
        // failure mode ... all server-side authoritative").
        if (user is null)
        {
            return LoginResult.Failure();
        }

        if (!user.IsActive)
        {
            return LoginResult.Failure();
        }

        if (user.LockoutEndAt is { } lockoutEndAt && lockoutEndAt > DateTimeOffset.UtcNow)
        {
            return LoginResult.Failure();
        }

        var verifyOutcome = passwordHasher.Verify(user.PasswordHash, password);
        if (verifyOutcome == PasswordVerifyOutcome.Failed)
        {
            var failedAttempts = user.FailedLoginAttempts + 1;
            var newLockoutEndAt = failedAttempts >= MaxFailedLoginAttempts
                ? DateTimeOffset.UtcNow.Add(LockoutDuration)
                : (DateTimeOffset?)null;
            await users.RecordFailedLoginAsync(user.Id, failedAttempts, newLockoutEndAt, ct);
            return LoginResult.Failure();
        }

        if (verifyOutcome == PasswordVerifyOutcome.SuccessRehashNeeded)
        {
            var rehash = passwordHasher.Hash(password);
            await users.UpdatePasswordHashAsync(user.Id, rehash, ct);
        }

        await users.RecordSuccessfulLoginAsync(user.Id, ct);

        var accessToken = tokenService.IssueGarageTenantAccessToken(user);
        var (rawRefreshToken, _, refreshExpiresAt) = await InsertRefreshTokenAsync(user.Id, requestIp, userAgent: null, ct);

        return LoginResult.Ok(
            accessToken.AccessToken, accessToken.ExpiresAt,
            rawRefreshToken, refreshExpiresAt,
            new AuthenticatedUserSummary(user.Id, user.GarageId, user.GarageName, user.Email, user.Name, user.Role));
    }

    public async Task<RefreshResult> RefreshAsync(string presentedRawToken, string? requestIp, string? userAgent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(presentedRawToken))
        {
            return RefreshResult.Failure();
        }

        var presentedHash = OpaqueTokenGenerator.Hash(presentedRawToken);
        var existing = await refreshTokens.FindByTokenHashAsync(presentedHash, ct);
        if (existing is null)
        {
            return RefreshResult.Failure();
        }

        if (existing.ExpiresAt < DateTimeOffset.UtcNow)
        {
            // Natural expiry -- ExpiresAt is immutable once written, so (unlike
            // RevokedAt below) there is no race to guard against here. Not revoked, no
            // reuse signal, no family revocation.
            return RefreshResult.Failure();
        }

        // Re-hydrate from a fresh DB read -- never trust anything about the old token
        // beyond its UserId, so a mid-session role change/deactivation takes effect on
        // next refresh (WP-4 brief §12).
        var user = await users.FindByIdAsync(existing.UserId, ct);

        // Insert the replacement row FIRST -- a FK constraint requires it to exist
        // before the old row's ReplacedByTokenId can reference it -- THEN atomically
        // "claim" the old row via TryClaimForRotationAsync's single conditional UPDATE.
        // This ordering (and the atomic claim itself) closes a HIGH security finding
        // from WP-4's post-implementation review: previously, checking
        // existing.RevokedAt in application code and revoking it in a LATER, separate
        // SaveChangesAsync call left a window where two concurrent presentations of the
        // same still-valid token could both pass the "not yet revoked" check before
        // either write landed, both minting a live session with reuse-detection never
        // firing. See IRefreshTokenRepository.TryClaimForRotationAsync's remarks.
        var (newRawToken, newTokenId, newExpiresAt) = await InsertRefreshTokenAsync(existing.UserId, requestIp, userAgent, ct);
        var claimed = await refreshTokens.TryClaimForRotationAsync(existing.Id, newTokenId, ct);

        if (!claimed)
        {
            // Lost the claim -- either this token was already revoked before this
            // request even started, or a concurrent request won the rotation race
            // between our read and this claim attempt. Both are indistinguishable and
            // both are the reuse-detection signal (brief §9): the just-inserted
            // replacement must not survive as a live session, and every active session
            // for the user is revoked.
            await refreshTokens.RevokeAsync(newTokenId, replacedByTokenId: null, ct);
            await refreshTokens.RevokeAllActiveForUserAsync(existing.UserId, ct);
            return RefreshResult.Failure();
        }

        if (user is null || !user.IsActive)
        {
            // Won the claim legitimately (the old token is already revoked+replaced),
            // but the user is no longer eligible -- revoke the new token too rather
            // than leaving a live orphaned session.
            await refreshTokens.RevokeAsync(newTokenId, replacedByTokenId: null, ct);
            return RefreshResult.Failure();
        }

        var accessToken = tokenService.IssueGarageTenantAccessToken(user);
        return RefreshResult.Ok(accessToken.AccessToken, accessToken.ExpiresAt, newRawToken, newExpiresAt);
    }

    public async Task LogoutAsync(string? presentedRawToken, CancellationToken ct = default)
    {
        // Idempotent no-op with no cookie/empty token -- always succeeds (WP-4 brief §12).
        if (string.IsNullOrWhiteSpace(presentedRawToken))
        {
            return;
        }

        var hash = OpaqueTokenGenerator.Hash(presentedRawToken);
        var existing = await refreshTokens.FindByTokenHashAsync(hash, ct);
        if (existing is not null && existing.RevokedAt is null)
        {
            await refreshTokens.RevokeAsync(existing.Id, replacedByTokenId: null, ct);
        }
    }

    /// <summary>Called ONLY by the background consumer (WP-4 brief §13) -- never
    /// directly from AuthController. Every branch below is off the HTTP request/response
    /// path by construction, so nothing here can leak an enumeration signal.</summary>
    public async Task ProcessForgotPasswordRequestAsync(string email, string? requestIp, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email, ct);
        if (user is null || !user.IsActive)
        {
            return;
        }

        var (rawToken, tokenHash) = OpaqueTokenGenerator.Generate();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_passwordReset.TokenLifetimeMinutes);
        await passwordResetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            RequestedByIp = requestIp,
        }, ct);

        var resetLink = $"{_passwordReset.ResetLinkBaseUrl}?token={Uri.EscapeDataString(rawToken)}";
        await emailService.SendPasswordResetAsync(user.Email, resetLink, ct);
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(string presentedRawToken, string newPassword, CancellationToken ct = default)
    {
        if (newPassword.Length < MinPasswordLength || newPassword.Length > MaxPasswordLength)
        {
            // Uniform failure shape with the token-invalid case below -- both are 400 with
            // no further detail distinguishing "bad token" from "bad password" isn't
            // required by the brief, but keeping this check first avoids a wasted DB
            // round-trip for an obviously-invalid request.
            return ResetPasswordResult.Failure("Invalid request.");
        }

        var presentedHash = OpaqueTokenGenerator.Hash(presentedRawToken);
        var token = await passwordResetTokens.FindByTokenHashAsync(presentedHash, ct);
        if (token is null || token.UsedAt is not null || token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            // Uniform 400 regardless of which of the three -- not found / already used /
            // expired (WP-4 brief §12).
            return ResetPasswordResult.Failure("Invalid request.");
        }

        var user = await users.FindByIdAsync(token.UserId, ct);
        if (user is null)
        {
            return ResetPasswordResult.Failure("Invalid request.");
        }

        var newHash = passwordHasher.Hash(newPassword);
        await users.UpdatePasswordHashAsync(user.Id, newHash, ct);
        await passwordResetTokens.MarkUsedAsync(token.Id, ct);

        // Security-sensitive event -- does NOT auto-login (forces re-login) and revokes
        // every active session (WP-4 brief §12).
        await refreshTokens.RevokeAllActiveForUserAsync(user.Id, ct);

        return ResetPasswordResult.Ok();
    }

    public Task<UserAuthRecord?> GetMeAsync(CancellationToken ct = default) => users.FindByCurrentTenantAsync(ct);

    /// <summary>Inserts a new, unrevoked RefreshToken row and returns its raw
    /// (unhashed) token alongside its Id -- callers that are rotating an existing token
    /// (RefreshAsync) use the returned Id to atomically claim the old row via
    /// TryClaimForRotationAsync afterward; LoginAsync (no prior token to rotate) just
    /// discards the Id.</summary>
    private async Task<(string RawToken, Guid TokenId, DateTimeOffset ExpiresAt)> InsertRefreshTokenAsync(
        Guid userId, string? requestIp, string? userAgent, CancellationToken ct)
    {
        var (rawToken, tokenHash) = OpaqueTokenGenerator.Generate();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenLifetimeDays);
        var newTokenRow = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedByIp = requestIp,
            UserAgent = userAgent,
        };
        await refreshTokens.AddAsync(newTokenRow, ct);
        return (rawToken, newTokenRow.Id, expiresAt);
    }
}
