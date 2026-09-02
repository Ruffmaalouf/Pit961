import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mockJsonResponse } from '@/lib/test-utils';
import { ApiError, __resetApiClientState, apiClient } from '@/services/apiClient';
import { resetAuthStore, useAuthStore } from '@/stores/authStore';

beforeEach(() => {
  resetAuthStore('authenticated');
  useAuthStore.setState({ accessToken: 'stale-token' });
  __resetApiClientState();
});

afterEach(() => {
  vi.unstubAllGlobals();
  resetAuthStore('unauthenticated');
});

describe('apiClient', () => {
  it('injects the in-memory bearer token on authenticated calls', async () => {
    const fetchMock = vi.fn(async (_input: unknown, _init?: RequestInit) =>
      mockJsonResponse(200, { ok: true }),
    );
    vi.stubGlobal('fetch', fetchMock);

    await apiClient.get('/api/v1/auth/me', { auth: true });

    const headers = (fetchMock.mock.calls[0]?.[1] as RequestInit).headers as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer stale-token');
  });

  it('refreshes once and retries the original request after a 401', async () => {
    const calls: string[] = [];
    const fetchMock = vi.fn(async (input: unknown, _init?: RequestInit) => {
      const url = String(input);
      calls.push(url);

      if (url.includes('/auth/refresh')) {
        return mockJsonResponse(200, {
          accessToken: 'fresh-token',
          accessTokenExpiresAt: '2026-09-02T12:15:00.000Z',
        });
      }
      // First /me call fails, the retry (with the new token) succeeds.
      const meCalls = calls.filter((c) => c.includes('/auth/me')).length;
      return meCalls === 1
        ? mockJsonResponse(401, { status: 401, title: 'Unauthorized' })
        : mockJsonResponse(200, { id: 'user-id' });
    });
    vi.stubGlobal('fetch', fetchMock);

    const result = await apiClient.get<{ id: string }>('/api/v1/auth/me', { auth: true });

    expect(result).toEqual({ id: 'user-id' });
    expect(calls.filter((c) => c.includes('/auth/refresh'))).toHaveLength(1);
    expect(calls.filter((c) => c.includes('/auth/me'))).toHaveLength(2);
    expect(useAuthStore.getState().accessToken).toBe('fresh-token');

    const retryHeaders = (fetchMock.mock.calls[2]?.[1] as RequestInit).headers as Record<
      string,
      string
    >;
    expect(retryHeaders.Authorization).toBe('Bearer fresh-token');
  });

  it('clears the session and surfaces the ProblemDetails title when refresh also fails', async () => {
    const fetchMock = vi.fn(async (input: unknown, _init?: RequestInit) =>
      String(input).includes('/auth/refresh')
        ? mockJsonResponse(401, { status: 401, title: 'Invalid or expired refresh token.' })
        : mockJsonResponse(401, { status: 401, title: 'Unauthorized' }),
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(apiClient.get('/api/v1/auth/me', { auth: true })).rejects.toBeInstanceOf(ApiError);

    expect(useAuthStore.getState().status).toBe('unauthenticated');
    expect(useAuthStore.getState().accessToken).toBeNull();
  });

  it('does not retry more than once', async () => {
    let meCalls = 0;
    const fetchMock = vi.fn(async (input: unknown, _init?: RequestInit) => {
      const url = String(input);
      if (url.includes('/auth/refresh')) {
        return mockJsonResponse(200, {
          accessToken: 'fresh-token',
          accessTokenExpiresAt: '2026-09-02T12:15:00.000Z',
        });
      }
      meCalls += 1;
      return mockJsonResponse(401, { status: 401, title: 'Unauthorized' });
    });
    vi.stubGlobal('fetch', fetchMock);

    await expect(apiClient.get('/api/v1/auth/me', { auth: true })).rejects.toBeInstanceOf(ApiError);
    expect(meCalls).toBe(2);
  });

  it('sends cookie-backed auth calls with credentials included', async () => {
    const fetchMock = vi.fn(async (_input: unknown, _init?: RequestInit) => mockJsonResponse(204));
    vi.stubGlobal('fetch', fetchMock);

    await apiClient.post('/api/v1/auth/logout', undefined, { withCredentials: true });

    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(init.credentials).toBe('include');
    expect(init.method).toBe('POST');
  });

  it('normalises a ProblemDetails error body into ApiError({status, title})', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        mockJsonResponse(401, {
          status: 401,
          title: 'Invalid email or password.',
          traceId: '00-abc-def-01',
        }),
      ),
    );

    const error = await apiClient
      .post('/api/v1/auth/login', { email: 'a@b.test', password: 'x' }, { withCredentials: true })
      .catch((caught: unknown) => caught as ApiError);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(401);
    expect((error as ApiError).title).toBe('Invalid email or password.');
    expect((error as ApiError).problem?.traceId).toBe('00-abc-def-01');
  });

  it('falls back to a generic message when the server sends no ProblemDetails title', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => mockJsonResponse(500)));

    const error = await apiClient.get('/api/config/branding').catch((c: unknown) => c as ApiError);

    expect((error as ApiError).status).toBe(500);
    expect((error as ApiError).title).toMatch(/something went wrong/i);
  });

  it('reports a network failure as a non-HTTP ApiError', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => {
        throw new TypeError('Failed to fetch');
      }),
    );

    const error = await apiClient.get('/api/config/branding').catch((c: unknown) => c as ApiError);

    expect((error as ApiError).isNetworkError).toBe(true);
    expect((error as ApiError).status).toBe(0);
  });

  it('returns undefined for a 204 No Content response', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => mockJsonResponse(204)));

    await expect(
      apiClient.post('/api/v1/auth/logout', undefined, { withCredentials: true }),
    ).resolves.toBeUndefined();
  });
});
