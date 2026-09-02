import * as authApi from '@/features/auth/api';
import { useAuthStore } from '@/stores/authStore';
import type { LoginRequest } from '@/types/api';

/**
 * Session orchestration: the only module that mutates auth state in response
 * to network calls. Components call these, never the transport layer directly.
 */

/**
 * Boot-time bootstrap.
 *
 * The access token is in-memory only, so a reload / new tab starts with no
 * token even though the httpOnly refresh cookie may still be valid. So:
 * silently POST /refresh first; if that succeeds, load the user via /me and
 * enter the authenticated shell. Any failure means "not authenticated" and the
 * login screen is shown — this is expected, not an error to surface.
 */
export async function bootstrapSession(): Promise<void> {
  const store = useAuthStore.getState();

  try {
    const refreshed = await authApi.refresh();

    if (!refreshed) {
      store.setUnauthenticated();
      return;
    }

    const user = await authApi.fetchCurrentUser();
    store.setUser(user);
  } catch {
    // 401 (bad/expired token) and the 404 edge case both mean the same thing
    // for the UI: no usable session.
    useAuthStore.getState().setUnauthenticated();
  }
}

/** Throws ApiError on failure so the form can show the server's ProblemDetails title. */
export async function loginWithPassword(credentials: LoginRequest): Promise<void> {
  const result = await authApi.login(credentials);

  useAuthStore.getState().setSession({
    accessToken: result.accessToken,
    accessTokenExpiresAt: result.accessTokenExpiresAt,
    user: result.user,
  });
}

/**
 * Clears local session state regardless of the network call's outcome — the
 * endpoint is idempotent and always 204s, but a failure must never leave the
 * user stuck in an authenticated-looking shell.
 */
export async function logoutSession(): Promise<void> {
  try {
    await authApi.logout();
  } catch {
    // Intentionally ignored.
  } finally {
    useAuthStore.getState().clear();
  }
}
