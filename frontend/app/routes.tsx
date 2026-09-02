import { Navigate, Route, Routes } from 'react-router-dom';
import { GuestRoute, ProtectedRoute } from '@/features/auth/ProtectedRoute';
import { AppShellLayout } from '@/layouts/AppShellLayout';
import { AuthLayout } from '@/layouts/AuthLayout';
import { FloorPage } from '@/pages/FloorPage';
import { LoginPage } from '@/pages/LoginPage';

/**
 * Route table. Kept as a plain <Routes> tree (not createBrowserRouter) so the
 * whole app can be mounted inside a MemoryRouter in tests without a second
 * router configuration to keep in sync.
 *
 * Two clearly separated surfaces: the unauthenticated auth layout and the
 * authenticated garage-tenant shell. Platform-admin routes are not part of
 * this tree and must get their own surface.
 */
export function AppRoutes() {
  return (
    <Routes>
      <Route element={<GuestRoute />}>
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShellLayout />}>
          <Route path="/floor" element={<FloorPage />} />
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/floor" replace />} />
      <Route path="*" element={<Navigate to="/floor" replace />} />
    </Routes>
  );
}
