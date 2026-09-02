import { authStoreApi } from '@/stores/authStore';
import type { ProblemDetails, RefreshResponse } from '@/types/api';

/**
 * The single place every backend call goes through.
 *
 * Responsibilities:
 *  - resolves the base URL from VITE_API_BASE_URL (a URL, never a secret)
 *  - JSON encode/decode, including 204 No Content
 *  - injects `Authorization: Bearer <accessToken>` when a token is held
 *  - `credentials: 'include'` for cookie-backed auth endpoints so the browser
 *    sends/receives the httpOnly refresh cookie (JS never touches it)
 *  - refresh-once-and-retry on a 401 from an authenticated call
 *  - normalises ASP.NET Core ProblemDetails into ApiError({status, title})
 *
 * Do not add ad-hoc fetch() calls elsewhere in the app.
 */

export const API_BASE_URL: string = (
  (import.meta.env?.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5289'
).replace(/\/+$/, '');

/** Endpoints that depend on the httpOnly refresh cookie. */
export const AUTH_ENDPOINTS = {
  login: '/api/v1/auth/login',
  refresh: '/api/v1/auth/refresh',
  logout: '/api/v1/auth/logout',
  me: '/api/v1/auth/me',
} as const;

export const CONFIG_ENDPOINTS = {
  branding: '/api/config/branding',
} as const;

/** Fallback copy used only when the server gave us no ProblemDetails title. */
const GENERIC_ERROR_TITLE = 'Something went wrong. Please try again.';
const NETWORK_ERROR_TITLE = 'Could not reach the server. Check your connection and try again.';

export class ApiError extends Error {
  readonly status: number;
  /**
   * The server's ProblemDetails `title`, surfaced verbatim to the UI.
   * Auth errors are intentionally generic/non-enumerable server-side — never
   * rephrase them into "email not found" style messages.
   */
  readonly title: string;
  readonly problem: ProblemDetails | null;
  readonly isNetworkError: boolean;

  constructor(params: {
    status: number;
    title: string;
    problem?: ProblemDetails | null;
    isNetworkError?: boolean;
  }) {
    super(params.title);
    this.name = 'ApiError';
    this.status = params.status;
    this.title = params.title;
    this.problem = params.problem ?? null;
    this.isNetworkError = params.isNetworkError ?? false;
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  /** Serialised as JSON when present. */
  body?: unknown;
  /** Attach the in-memory bearer token, and enable refresh-and-retry-once. */
  auth?: boolean;
  /** Send/receive the httpOnly refresh cookie. Required for /login, /refresh, /logout. */
  withCredentials?: boolean;
  signal?: AbortSignal;
  /** Internal: prevents a refresh loop. */
  _isRetry?: boolean;
}

function buildUrl(path: string): string {
  return path.startsWith('http') ? path : `${API_BASE_URL}${path}`;
}

async function readProblemDetails(response: Response): Promise<ProblemDetails | null> {
  try {
    const text = await response.text();
    if (!text) return null;
    const parsed = JSON.parse(text) as unknown;
    if (parsed && typeof parsed === 'object') return parsed as ProblemDetails;
    return null;
  } catch {
    return null;
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem = await readProblemDetails(response);
  const title =
    typeof problem?.title === 'string' && problem.title.trim().length > 0
      ? problem.title
      : GENERIC_ERROR_TITLE;

  return new ApiError({
    status: typeof problem?.status === 'number' ? problem.status : response.status,
    title,
    problem,
  });
}

async function parseBody<T>(response: Response): Promise<T> {
  if (response.status === 204) return undefined as T;

  const text = await response.text();
  if (!text) return undefined as T;

  try {
    return JSON.parse(text) as T;
  } catch {
    throw new ApiError({
      status: response.status,
      title: GENERIC_ERROR_TITLE,
    });
  }
}

async function rawFetch(path: string, options: RequestOptions): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json' };

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (options.auth) {
    const token = authStoreApi.getAccessToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  try {
    return await fetch(buildUrl(path), {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      // The refresh token is an httpOnly cookie: the browser attaches it, we never read it.
      credentials: options.withCredentials ? 'include' : 'same-origin',
      signal: options.signal,
    });
  } catch {
    throw new ApiError({
      status: 0,
      title: NETWORK_ERROR_TITLE,
      isNetworkError: true,
    });
  }
}

/**
 * Silent refresh. Sends no body — the httpOnly cookie is the credential.
 * On success the new access token is written straight into the in-memory store.
 * Concurrent callers share one in-flight request (cheap de-dupe, not a queue).
 */
let inFlightRefresh: Promise<boolean> | null = null;

export function tryRefreshSession(): Promise<boolean> {
  if (inFlightRefresh) return inFlightRefresh;

  inFlightRefresh = (async () => {
    try {
      const response = await rawFetch(AUTH_ENDPOINTS.refresh, {
        method: 'POST',
        withCredentials: true,
      });

      if (!response.ok) return false;

      const data = await parseBody<RefreshResponse>(response);
      if (!data?.accessToken) return false;

      authStoreApi.setAccessToken(data.accessToken, data.accessTokenExpiresAt);
      return true;
    } catch {
      return false;
    } finally {
      // Cleared on the next microtask so simultaneous callers still share it.
      queueMicrotask(() => {
        inFlightRefresh = null;
      });
    }
  })();

  return inFlightRefresh;
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await rawFetch(path, options);

  if (response.status === 401 && options.auth && !options._isRetry) {
    const refreshed = await tryRefreshSession();

    if (refreshed) {
      // Retry exactly once with the new access token.
      return request<T>(path, { ...options, _isRetry: true });
    }

    // Refresh failed: drop the in-memory session. Route guards react to the
    // store change and send the user back to /login.
    authStoreApi.clear();
    throw await toApiError(response);
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  return parseBody<T>(response);
}

export const apiClient = {
  baseUrl: API_BASE_URL,
  request,
  tryRefreshSession,
  get: <T>(path: string, options: Omit<RequestOptions, 'method' | 'body'> = {}) =>
    request<T>(path, { ...options, method: 'GET' }),
  post: <T>(path: string, body?: unknown, options: Omit<RequestOptions, 'method' | 'body'> = {}) =>
    request<T>(path, { ...options, method: 'POST', body }),
};

/** Test-only: clears the shared in-flight refresh promise. */
export function __resetApiClientState() {
  inFlightRefresh = null;
}
