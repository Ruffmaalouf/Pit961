import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from '@/app/App';
import {
  FORBIDDEN_HARDCODED_BRAND_NAMES,
  installFetchMock,
  makeBranding,
  mockJsonResponse,
  unauthorizedProblem,
} from '@/lib/test-utils';
import { __resetApiClientState } from '@/services/apiClient';
import { resetAuthStore } from '@/stores/authStore';
import { resetBrandingStore } from '@/stores/brandingStore';

function renderApp(route = '/login') {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <App />
    </MemoryRouter>,
  );
}

beforeEach(() => {
  resetAuthStore('bootstrapping');
  resetBrandingStore();
  __resetApiClientState();
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe('App boot', () => {
  it('renders without crashing and lands on the login screen when there is no session', async () => {
    installFetchMock({
      branding: () => mockJsonResponse(200, makeBranding()),
      refresh: () => mockJsonResponse(401, unauthorizedProblem()),
    });

    renderApp('/login');

    expect(await screen.findByRole('heading', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /log in/i })).toBeInTheDocument();
  });

  it('bootstraps the session with a silent refresh before showing the login screen', async () => {
    const fetchMock = installFetchMock({
      branding: () => mockJsonResponse(200, makeBranding()),
      refresh: () => mockJsonResponse(401, unauthorizedProblem()),
    });

    renderApp('/login');
    await screen.findByRole('heading', { name: /sign in/i });

    const refreshCall = fetchMock.mock.calls.find((call) =>
      String(call[0]).includes('/api/v1/auth/refresh'),
    );
    expect(refreshCall).toBeDefined();

    const refreshInit = refreshCall?.[1] as RequestInit | undefined;
    expect(refreshInit?.method).toBe('POST');
    // The httpOnly refresh cookie only travels when credentials are included.
    expect(refreshInit?.credentials).toBe('include');
    // No refresh token is ever put in the body by this SPA.
    expect(refreshInit?.body).toBeUndefined();
  });

  it('fetches branding anonymously, without a bearer token or cookies', async () => {
    const fetchMock = installFetchMock({
      branding: () => mockJsonResponse(200, makeBranding()),
      refresh: () => mockJsonResponse(401, unauthorizedProblem()),
    });

    renderApp('/login');
    await screen.findByRole('heading', { name: /sign in/i });

    const brandingCall = fetchMock.mock.calls.find((call) =>
      String(call[0]).includes('/api/config/branding'),
    );
    expect(brandingCall).toBeDefined();

    const init = brandingCall?.[1] as RequestInit | undefined;
    const headers = (init?.headers ?? {}) as Record<string, string>;
    expect(headers.Authorization).toBeUndefined();
    expect(init?.credentials).not.toBe('include');
  });
});

describe('runtime branding on the login screen', () => {
  it('renders the productDisplayName returned by the branding API', async () => {
    const branding = makeBranding({ productDisplayName: 'Northgate Works' });

    installFetchMock({
      branding: () => mockJsonResponse(200, branding),
      refresh: () => mockJsonResponse(401, unauthorizedProblem()),
    });

    renderApp('/login');

    // Asserted against the mocked value, not a literal in this expectation.
    const nameEl = await screen.findByTestId('product-display-name');
    expect(nameEl).toHaveTextContent(branding.productDisplayName);

    // And no codename or placeholder wordmark is baked into the UI.
    const rendered = document.body.textContent ?? '';
    for (const forbidden of FORBIDDEN_HARDCODED_BRAND_NAMES) {
      expect(rendered.toLowerCase()).not.toContain(forbidden.toLowerCase());
    }
  });

  it('reflects a different branding config on a second load (config is not baked in)', async () => {
    const first = makeBranding({ productDisplayName: 'Northgate Works' });

    installFetchMock({
      branding: () => mockJsonResponse(200, first),
      refresh: () => mockJsonResponse(401, unauthorizedProblem()),
    });

    const firstRender = renderApp('/login');
    expect(await screen.findByTestId('product-display-name')).toHaveTextContent(
      first.productDisplayName,
    );
    // The brand mark letter is derived from the same runtime value.
    expect(screen.getByTestId('brand-mark-logo')).toHaveAttribute('src', first.logoUrl);
    firstRender.unmount();

    // Second pass with a completely different tenant branding payload.
    resetBrandingStore();
    resetAuthStore('bootstrapping');
    __resetApiClientState();
    vi.unstubAllGlobals();

    const second = makeBranding({
      productDisplayName: 'Delta Works Automotive',
      logoUrl: '',
    });

    installFetchMock({
      branding: () => mockJsonResponse(200, second),
      refresh: () => mockJsonResponse(401, unauthorizedProblem()),
    });

    renderApp('/login');

    const secondName = await screen.findByTestId('product-display-name');
    expect(secondName).toHaveTextContent(second.productDisplayName);
    expect(secondName).not.toHaveTextContent(first.productDisplayName);
    expect(screen.getByTestId('brand-mark')).toHaveTextContent('D');

    await waitFor(() => {
      expect(document.title).toBe(second.productDisplayName);
    });
  });

  it('degrades to a glyph-only brand mark when branding cannot be loaded', async () => {
    installFetchMock({
      branding: () => mockJsonResponse(500),
      refresh: () => mockJsonResponse(401, unauthorizedProblem()),
    });

    renderApp('/login');

    await screen.findByRole('heading', { name: /sign in/i });
    expect(screen.queryByTestId('product-display-name')).toBeNull();
    expect(screen.getByTestId('brand-mark').textContent).toBe('');
    expect(screen.queryByTestId('brand-mark-logo')).toBeNull();
  });
});
