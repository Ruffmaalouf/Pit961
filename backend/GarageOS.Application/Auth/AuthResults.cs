namespace GarageOS.Application.Auth;

public sealed record AuthenticatedUserSummary(
    Guid Id, Guid GarageId, string GarageName, string Email, string Name, string Role);

public sealed record LoginResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public DateTimeOffset? AccessTokenExpiresAt { get; init; }
    public string? RawRefreshToken { get; init; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
    public AuthenticatedUserSummary? User { get; init; }

    public static LoginResult Failure() => new() { Success = false };

    public static LoginResult Ok(
        string accessToken, DateTimeOffset accessTokenExpiresAt,
        string rawRefreshToken, DateTimeOffset refreshTokenExpiresAt,
        AuthenticatedUserSummary user) => new()
    {
        Success = true,
        AccessToken = accessToken,
        AccessTokenExpiresAt = accessTokenExpiresAt,
        RawRefreshToken = rawRefreshToken,
        RefreshTokenExpiresAt = refreshTokenExpiresAt,
        User = user,
    };
}

public sealed record RefreshResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public DateTimeOffset? AccessTokenExpiresAt { get; init; }
    public string? RawRefreshToken { get; init; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }

    public static RefreshResult Failure() => new() { Success = false };

    public static RefreshResult Ok(
        string accessToken, DateTimeOffset accessTokenExpiresAt,
        string rawRefreshToken, DateTimeOffset refreshTokenExpiresAt) => new()
    {
        Success = true,
        AccessToken = accessToken,
        AccessTokenExpiresAt = accessTokenExpiresAt,
        RawRefreshToken = rawRefreshToken,
        RefreshTokenExpiresAt = refreshTokenExpiresAt,
    };
}

public sealed record ResetPasswordResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static ResetPasswordResult Ok() => new() { Success = true };
    public static ResetPasswordResult Failure(string message) => new() { Success = false, ErrorMessage = message };
}
