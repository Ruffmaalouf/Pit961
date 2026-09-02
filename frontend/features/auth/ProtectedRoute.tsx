import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { BootSplash } from '@/components/boot-splash';

/**
 * Route guards.
 *
 * These are a navigation convenience, not a security boundary: every protected
 * resource is enforced server-side by the API. A user who forces their way to
 * a protected URL simply gets 401s from the backend.
 */

export function ProtectedRoute({ children }: { children?: ReactNode }) {
  const status = useAuthStore((state) => state.status);
  const location = useLocation();

  if (status === 'bootstrapping') return <BootSplash />;

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return children ? <>{children}</> : <Outlet />;
}

/** Keeps an already-authenticated user off /login. */
export function GuestRoute({ children }: { children?: ReactNode }) {
  const status = useAuthStore((state) => state.status);

  if (status === 'bootstrapping') return <BootSplash />;

  if (status === 'authenticated') return <Navigate to="/floor" replace />;

  return children ? <>{children}</> : <Outlet />;
}
