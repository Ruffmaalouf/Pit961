import { Outlet } from 'react-router-dom';

/**
 * Unauthenticated surface: full-viewport app background with a single centred
 * card. This is the garage-tenant sign-in surface — platform-admin UI is a
 * separate surface and must never share a screen with it.
 */
export function AuthLayout() {
  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-surface-app px-4 py-10">
      <Outlet />
    </div>
  );
}
