import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AppRoutes } from '@/app/routes';
import { makeBranding, makeUser } from '@/lib/test-utils';
import { resetAuthStore, useAuthStore } from '@/stores/authStore';
import { resetBrandingStore, useBrandingStore } from '@/stores/brandingStore';

/**
 * Route-guard behaviour is tested against AppRoutes directly with the stores
 * pre-seeded, so no boot fetches are involved.
 */
function renderRoutes(route: string) {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <AppRoutes />
    </MemoryRouter>,
  );
}

function signIn() {
  useAuthStore.setState({
    status: 'authenticated',
    accessToken: 'test-access-token',
    accessTokenExpiresAt: '2026-09-02T12:15:00.000Z',
    user: makeUser(),
  });
}

beforeEach(() => {
  resetAuthStore('unauthenticated');
  resetBrandingStore();
  useBrandingStore.getState().setConfig(makeBranding({ productDisplayName: 'Northgate Works' }));
});

afterEach(() => {
  cleanup();
  resetAuthStore('unauthenticated');
  resetBrandingStore();
});

describe('protected routes', () => {
  it('redirects an unauthenticated user away from the authenticated shell to /login', () => {
    renderRoutes('/floor');

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /log in/i })).toBeInTheDocument();
    expect(screen.queryByTestId('app-shell-landing')).toBeNull();
    expect(screen.queryByTestId('app-sidebar')).toBeNull();
  });

  it('redirects an unauthenticated user from an unknown route to /login as well', () => {
    renderRoutes('/reports/anything');

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument();
  });

  it('shows the boot splash rather than the login screen while the session bootstrap is pending', () => {
    resetAuthStore('bootstrapping');

    renderRoutes('/floor');

    expect(screen.getByTestId('boot-splash')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: /sign in/i })).toBeNull();
  });

  it('lets an authenticated user render the protected shell', () => {
    signIn();

    renderRoutes('/floor');

    expect(screen.getByTestId('app-sidebar')).toBeInTheDocument();
    expect(screen.getByTestId('app-header')).toBeInTheDocument();
    expect(screen.getByTestId('app-shell-landing')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: /sign in/i })).toBeNull();
  });

  it('renders the runtime product display name inside the shell', () => {
    const branding = makeBranding({ productDisplayName: 'Harbour Auto Group' });
    useBrandingStore.getState().setConfig(branding);
    signIn();

    renderRoutes('/floor');

    expect(screen.getByTestId('shell-product-display-name')).toHaveTextContent(
      branding.productDisplayName,
    );
  });

  it('renders all eight nav rail items with Floor active, and no Settings item', () => {
    signIn();

    renderRoutes('/floor');

    for (const key of [
      'floor',
      'clock',
      'jobs',
      'customers',
      'money',
      'parts',
      'team',
      'reports',
    ]) {
      expect(screen.getByTestId(`nav-item-${key}`)).toBeInTheDocument();
    }

    expect(screen.getByTestId('nav-item-floor')).toHaveAttribute('data-active', 'true');
    expect(screen.getByTestId('nav-item-clock')).toHaveAttribute('data-active', 'false');
    expect(screen.queryByText(/settings/i)).toBeNull();
  });

  it('sends an already-authenticated user hitting /login into the shell', () => {
    signIn();

    renderRoutes('/login');

    expect(screen.getByTestId('app-shell-landing')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: /sign in/i })).toBeNull();
  });
});
