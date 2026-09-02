import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginForm } from '@/features/auth/LoginForm';
import { makeUser } from '@/lib/test-utils';
import { ApiError } from '@/services/apiClient';
import { resetAuthStore, useAuthStore } from '@/stores/authStore';

// The transport layer is mocked; the real session orchestration, validation,
// store updates and error rendering all still run.
vi.mock('@/features/auth/api', () => ({
  login: vi.fn(),
  refresh: vi.fn(),
  logout: vi.fn(),
  fetchCurrentUser: vi.fn(),
}));

import * as authApi from '@/features/auth/api';

const mockedLogin = vi.mocked(authApi.login);

beforeEach(() => {
  resetAuthStore('unauthenticated');
});

afterEach(() => {
  cleanup();
  resetAuthStore('unauthenticated');
});

describe('LoginForm', () => {
  it('renders the email input, password input and submit button', () => {
    render(<LoginForm />);

    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /log in/i })).toBeInTheDocument();
  });

  it('does not offer remember-me, SSO, sign-up or forgot-password affordances', () => {
    render(<LoginForm />);

    expect(screen.queryByText(/remember me/i)).toBeNull();
    expect(screen.queryByText(/forgot/i)).toBeNull();
    expect(screen.queryByText(/sign up/i)).toBeNull();
    expect(screen.queryByText(/single sign-on|sso/i)).toBeNull();
  });

  it('establishes authenticated state in the auth store on a successful login', async () => {
    const user = makeUser();
    mockedLogin.mockResolvedValue({
      accessToken: 'test-access-token',
      accessTokenExpiresAt: '2026-09-02T12:15:00.000Z',
      user,
    });

    render(<LoginForm />);

    await userEvent.type(screen.getByLabelText(/email/i), 'operator@example.test');
    await userEvent.type(screen.getByLabelText(/password/i), 'CorrectHorse1!');
    await userEvent.click(screen.getByRole('button', { name: /log in/i }));

    await waitFor(() => {
      expect(useAuthStore.getState().status).toBe('authenticated');
    });

    const state = useAuthStore.getState();
    expect(state.user).toEqual(user);
    expect(state.accessToken).toBe('test-access-token');
    expect(mockedLogin).toHaveBeenCalledWith({
      email: 'operator@example.test',
      password: 'CorrectHorse1!',
    });
  });

  it('never writes the access token to persistent browser storage', async () => {
    mockedLogin.mockResolvedValue({
      accessToken: 'test-access-token',
      accessTokenExpiresAt: '2026-09-02T12:15:00.000Z',
      user: makeUser(),
    });

    render(<LoginForm />);
    await userEvent.type(screen.getByLabelText(/email/i), 'operator@example.test');
    await userEvent.type(screen.getByLabelText(/password/i), 'CorrectHorse1!');
    await userEvent.click(screen.getByRole('button', { name: /log in/i }));

    await waitFor(() => {
      expect(useAuthStore.getState().accessToken).toBe('test-access-token');
    });

    expect(window.localStorage.length).toBe(0);
    expect(window.sessionStorage.length).toBe(0);
    expect(document.cookie).not.toContain('test-access-token');
  });

  it('shows the server ProblemDetails title verbatim in the error banner on a 401', async () => {
    const serverTitle = 'Invalid email or password.';
    mockedLogin.mockRejectedValue(
      new ApiError({ status: 401, title: serverTitle, problem: { status: 401, title: serverTitle } }),
    );

    render(<LoginForm />);

    await userEvent.type(screen.getByLabelText(/email/i), 'operator@example.test');
    await userEvent.type(screen.getByLabelText(/password/i), 'WrongPassword1!');
    await userEvent.click(screen.getByRole('button', { name: /log in/i }));

    const banner = await screen.findByTestId('form-error-banner');
    expect(banner).toHaveTextContent(serverTitle);
    expect(banner).toHaveAttribute('role', 'alert');

    // The generic server message is shown as-is, never rephrased into an
    // account-enumerating variant.
    expect(banner.textContent).not.toMatch(/not found|no such user|unknown email/i);

    expect(useAuthStore.getState().status).toBe('unauthenticated');
    expect(useAuthStore.getState().accessToken).toBeNull();

    // The form becomes interactive again so the user can retry.
    expect(screen.getByRole('button', { name: /log in/i })).toBeEnabled();
  });

  it('blocks submission and shows field errors when the form is empty', async () => {
    render(<LoginForm />);

    await userEvent.click(screen.getByRole('button', { name: /log in/i }));

    expect(await screen.findByText(/email is required/i)).toBeInTheDocument();
    expect(screen.getByText(/password is required/i)).toBeInTheDocument();
    expect(mockedLogin).not.toHaveBeenCalled();
  });

  it('shows a spinner and disables the controls while the request is in flight', async () => {
    let resolveLogin: (() => void) | undefined;
    mockedLogin.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveLogin = () =>
            resolve({
              accessToken: 'test-access-token',
              accessTokenExpiresAt: '2026-09-02T12:15:00.000Z',
              user: makeUser(),
            });
        }),
    );

    render(<LoginForm />);
    await userEvent.type(screen.getByLabelText(/email/i), 'operator@example.test');
    await userEvent.type(screen.getByLabelText(/password/i), 'CorrectHorse1!');
    await userEvent.click(screen.getByRole('button', { name: /log in/i }));

    expect(await screen.findByTestId('spinner')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /log in/i })).toBeDisabled();
    expect(screen.getByLabelText(/email/i)).toBeDisabled();
    expect(screen.getByLabelText(/password/i)).toBeDisabled();

    resolveLogin?.();
    await waitFor(() => {
      expect(useAuthStore.getState().status).toBe('authenticated');
    });
  });
});
