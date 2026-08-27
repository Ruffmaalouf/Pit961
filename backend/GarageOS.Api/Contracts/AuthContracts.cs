namespace GarageOS.Api.Contracts;

// WP-4 brief §12 endpoint contracts. Api-layer HTTP DTOs, deliberately separate from
// GarageOS.Application.Auth's result records -- those carry the RawRefreshToken (which
// must go into a cookie, never JSON), these carry only what actually goes on the wire.

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginUserPayload(Guid Id, Guid GarageId, string GarageName, string Email, string Name, string Role);

public sealed record LoginResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, LoginUserPayload User);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record RefreshResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record MeResponse(Guid Id, Guid GarageId, string GarageName, string Email, string Name, string Role);
