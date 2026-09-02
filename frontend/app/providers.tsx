import type { ReactNode } from 'react';
import { BrowserRouter } from 'react-router-dom';

/**
 * Single place to compose app-wide providers.
 *
 * State lives in Zustand stores, which need no provider — so today this is
 * just the router. Tests substitute a MemoryRouter instead of using this.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return <BrowserRouter>{children}</BrowserRouter>;
}
