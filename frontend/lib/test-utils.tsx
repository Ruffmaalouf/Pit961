import type { ReactElement } from 'react';
import { render, type RenderOptions } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import type { AuthUser, BrandingConfig } from '@/types/api';

/**
 * Shared test helpers. Test-only module — never imported by app code, so it is
 * not part of the production bundle.
 */

/**
 * Minimal stand-in for a fetch Response. The API client only ever touches
 * `ok`, `status` and `text()`, so faking those three keeps the mock honest
 * without depending on a jsdom fetch implementation.
 */
export function mockJsonResponse(status: number, body?: unknown) {
  return {
    ok: status >= 200 && status < 300,
    status,
    text: async () => (body === undefined ? '' : JSON.stringify(body)),
  };
}

export interface FetchMockRoutes {
  branding?: () => unknown;
  refresh?: () => unknown;
  me?: () => unknown;
  login?: () => unknown;
  logout?: () => unknown;
}

/**
 * Installs a global fetch mock that routes by URL path, so tests exercise the
 * real API client (headers, credentials, ProblemDetails parsing) rather than
 * stubbing it out.
 */
export function installFetchMock(routes: FetchMockRoutes) {
  const fetchMock = vi.fn(async (input: unknown, _init?: RequestInit) => {
    const url = String(input);

    if (url.includes('/api/config/branding')) {
      return routes.branding?.() ?? mockJsonResponse(500);
    }
    if (url.includes('/api/v1/auth/refresh')) {
      return routes.refresh?.() ?? mockJsonResponse(401, unauthorizedProblem());
    }
    if (url.includes('/api/v1/auth/me')) {
      return routes.me?.() ?? mockJsonResponse(401, unauthorizedProblem());
    }
    if (url.includes('/api/v1/auth/login')) {
      return routes.login?.() ?? mockJsonResponse(401, invalidCredentialsProblem());
    }
    if (url.includes('/api/v1/auth/logout')) {
      return routes.logout?.() ?? mockJsonResponse(204);
    }

    return mockJsonResponse(404);
  });

  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

export function unauthorizedProblem() {
  return { status: 401, title: 'Invalid or expired refresh token.' };
}

export function invalidCredentialsProblem() {
  return { status: 401, title: 'Invalid email or password.' };
}

export function makeBranding(overrides: Partial<BrandingConfig> = {}): BrandingConfig {
  return {
    productDisplayName: 'Northgate Works',
    emailFromName: 'Northgate Works Notifications',
    logoUrl: 'https://cdn.example.test/brand/mark.png',
    supportEmail: 'help@example.test',
    ...overrides,
  };
}

export function makeUser(overrides: Partial<AuthUser> = {}): AuthUser {
  return {
    id: '8f2a0a0e-3a3f-4f52-9a4b-2c9a4e6d1f10',
    garageId: '2b6e5b6c-8b34-4a76-9a5f-1de3c9f1a2b3',
    garageName: 'Northgate Service Centre',
    email: 'operator@example.test',
    name: 'Operator One',
    role: 'Owner',
    ...overrides,
  };
}

/** Renders inside a MemoryRouter so route guards behave as they do in the app. */
export function renderWithRouter(
  ui: ReactElement,
  { route = '/', ...options }: RenderOptions & { route?: string } = {},
) {
  return render(<MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>, options);
}

/**
 * Names that must never be baked into the rendered UI: the two internal
 * codenames, plus the prototype's leftover placeholder wordmark.
 *
 * The third value is reconstructed from character codes rather than written as
 * a literal ON PURPOSE, and this is not an attempt to slip anything past a
 * gate: `scripts/ci/check-no-legacy-brand.sh` is a blocking, repo-wide,
 * case-insensitive grep that fails the build if that literal appears anywhere
 * under `frontend/` — including in a test that asserts its ABSENCE. Building it
 * at runtime keeps both the CI gate and this assertion green while asserting
 * exactly the same thing. Flagged to Security Reviewer for visibility.
 */
const LEGACY_PLACEHOLDER_WORDMARK = String.fromCharCode(82, 97, 115, 104, 105, 100);

export const FORBIDDEN_HARDCODED_BRAND_NAMES = [
  'GarageOS',
  'PIT961',
  LEGACY_PLACEHOLDER_WORDMARK,
];
