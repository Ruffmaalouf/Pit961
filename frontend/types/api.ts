/**
 * Wire types for the PIT961 backend.
 *
 * Casing matches ASP.NET Core's default camelCase JSON serialisation, so these
 * shapes are transcribed 1:1 from the backend contract — do not "tidy" them.
 */

/** GET /api/v1/auth/me, and the `user` object inside the login response. */
export interface AuthUser {
  id: string;
  garageId: string;
  garageName: string;
  email: string;
  name: string;
  role: string;
}

/** POST /api/v1/auth/login request body (C# `LoginRequest(string Email, string Password)`). */
export interface LoginRequest {
  email: string;
  password: string;
}

/** POST /api/v1/auth/login 200 body. Sets the httpOnly refresh cookie as a side effect. */
export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: AuthUser;
}

/** POST /api/v1/auth/refresh 200 body. Rotates the httpOnly refresh cookie as a side effect. */
export interface RefreshResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}

/** GET /api/config/branding — anonymous, fetched once at app boot. */
export interface BrandingConfig {
  productDisplayName: string;
  emailFromName: string;
  logoUrl: string;
  supportEmail: string;
}

/**
 * ASP.NET Core ProblemDetails. Only `status` and `title` are relied on —
 * everything else is optional and must never be assumed present.
 */
export interface ProblemDetails {
  status?: number;
  title?: string;
  type?: string;
  detail?: string;
  instance?: string;
  traceId?: string;
  [key: string]: unknown;
}
