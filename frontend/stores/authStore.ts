import { create } from 'zustand';
import type { AuthUser } from '@/types/api';

/**
 * AUTH SESSION STRATEGY — settled decision, do not change without Owner sign-off.
 *
 * Access token: IN MEMORY ONLY. It lives in this Zustand store's state and
 * nowhere else. It is deliberately never written to localStorage,
 * sessionStorage, IndexedDB, a cookie, or any other persistent browser
 * storage: it is a short-lived (~15 min) bearer credential handed back in a
 * JSON body (not protected by an httpOnly cookie), so minimising its exposure
 * surface to XSS matters. Losing it on reload is expected and handled by the
 * boot-time silent refresh (see features/auth/session.ts).
 *
 * Refresh token: NOT VISIBLE TO THIS CODE AT ALL. It is the httpOnly
 * `garageos_refresh_token` cookie, owned entirely by the browser and the
 * server. JS never reads, writes or copies it — auth calls simply use
 * `credentials: 'include'` and let the browser attach it.
 *
 * This store therefore contains no persistence middleware on purpose.
 */

export type AuthStatus =
  /** Boot-time silent refresh has not resolved yet. */
  | 'bootstrapping'
  /** No valid session; protected routes redirect to /login. */
  | 'unauthenticated'
  /** Access token held in memory and user loaded. */
  | 'authenticated';

export interface AuthState {
  status: AuthStatus;
  /** In-memory only. Never persisted. */
  accessToken: string | null;
  accessTokenExpiresAt: string | null;
  user: AuthUser | null;

  setSession: (session: {
    accessToken: string;
    accessTokenExpiresAt?: string | null;
    user?: AuthUser | null;
  }) => void;
  setAccessToken: (accessToken: string, accessTokenExpiresAt?: string | null) => void;
  setUser: (user: AuthUser) => void;
  setUnauthenticated: () => void;
  clear: () => void;
}

const initialState = {
  status: 'bootstrapping' as AuthStatus,
  accessToken: null,
  accessTokenExpiresAt: null,
  user: null,
};

export const useAuthStore = create<AuthState>((set) => ({
  ...initialState,

  setSession: ({ accessToken, accessTokenExpiresAt, user }) =>
    set((prev) => {
      const nextUser = user ?? prev.user;
      return {
        accessToken,
        accessTokenExpiresAt: accessTokenExpiresAt ?? prev.accessTokenExpiresAt,
        user: nextUser,
        status: nextUser ? ('authenticated' as AuthStatus) : prev.status,
      };
    }),

  setAccessToken: (accessToken, accessTokenExpiresAt) =>
    set((prev) => ({
      accessToken,
      accessTokenExpiresAt: accessTokenExpiresAt ?? prev.accessTokenExpiresAt,
    })),

  setUser: (user) => set({ user, status: 'authenticated' }),

  setUnauthenticated: () =>
    set({ ...initialState, status: 'unauthenticated' }),

  clear: () => set({ ...initialState, status: 'unauthenticated' }),
}));

/** Non-React accessor for the API client (avoids a store <-> client import cycle). */
export const authStoreApi = {
  getAccessToken: () => useAuthStore.getState().accessToken,
  setAccessToken: (token: string, expiresAt?: string | null) =>
    useAuthStore.getState().setAccessToken(token, expiresAt),
  clear: () => useAuthStore.getState().clear(),
};

/** Test-only helper; resets the store between test cases. */
export function resetAuthStore(status: AuthStatus = 'bootstrapping') {
  useAuthStore.setState({ ...initialState, status });
}
