import { Navigate, Route, Routes } from 'react-router-dom';
import { GuestRoute, ProtectedRoute } from '@/features/auth/ProtectedRoute';
import { AppShellLayout } from '@/layouts/AppShellLayout';
import { AuthLayout } from '@/layouts/AuthLayout';
import { CustomerDetailPage } from '@/pages/customers/CustomerDetailPage';
import { CustomersListPage } from '@/pages/customers/CustomersListPage';
import { FloorPage } from '@/pages/FloorPage';
import { JobDetailPage } from '@/pages/jobs/JobDetailPage';
import { JobIntakePage } from '@/pages/jobs/JobIntakePage';
import { LoginPage } from '@/pages/LoginPage';

/**
 * Route table. Kept as a plain <Routes> tree (not createBrowserRouter) so the
 * whole app can be mounted inside a MemoryRouter in tests without a second
 * router configuration to keep in sync.
 *
 * Two clearly separated surfaces: the unauthenticated auth layout and the
 * authenticated garage-tenant shell. Platform-admin routes are not part of
 * this tree and must get their own surface.
 *
 * "/jobs" (bare) redirects to "/floor": there is no "list all jobs" backend
 * endpoint (P2-WP3 exposes floor-board + per-job reads, not a jobs index), so
 * there is nothing real to render there. "/jobs/new" and "/jobs/:id" are real
 * screens; only the bare index path is a redirect.
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
          <Route path="/customers" element={<CustomersListPage />} />
          <Route path="/customers/:id" element={<CustomerDetailPage />} />
          <Route path="/jobs/new" element={<JobIntakePage />} />
          <Route path="/jobs/:id" element={<JobDetailPage />} />
          <Route path="/jobs" element={<Navigate to="/floor" replace />} />
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/floor" replace />} />
      <Route path="*" element={<Navigate to="/floor" replace />} />
    </Routes>
  );
}
