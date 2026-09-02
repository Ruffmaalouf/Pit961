import { AUTH_ENDPOINTS, apiClient, tryRefreshSession } from '@/services/apiClient';
import type { AuthUser, LoginRequest, LoginResponse } from '@/types/api';

/**
 * Thin transport layer for the auth endpoints. All cookie-backed calls pass
 * `withCredentials` so the browser sends/receives the httpOnly
 * `garageos_refresh_token` cookie. No JS here ever reads that cookie.
 */

export function login(credentials: LoginRequest): Promise<LoginResponse> {
  return apiClient.post<LoginResponse>(AUTH_ENDPOINTS.login, credentials, {
    withCredentials: true,
  });
}

/** 200 -> new access token in the store; 401 -> false. Never throws. */
export function refresh(): Promise<boolean> {
  return tryRefreshSession();
}

/** Always 204, and safe to call when we are not sure a session exists. */
export function logout(): Promise<void> {
  return apiClient.post<void>(AUTH_ENDPOINTS.logout, undefined, {
    withCredentials: true,
  });
}

export function fetchCurrentUser(): Promise<AuthUser> {
  return apiClient.get<AuthUser>(AUTH_ENDPOINTS.me, { auth: true });
}
