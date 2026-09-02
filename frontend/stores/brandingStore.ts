import { create } from 'zustand';
import type { BrandingConfig } from '@/types/api';

/**
 * Runtime branding. Fetched once at app boot from GET /api/config/branding
 * (anonymous) so the login screen can render the brand mark and product
 * display name before the user is authenticated.
 *
 * Nothing in this codebase may hardcode a product name, wordmark letter or
 * logo asset — PIT961 has no approved customer-facing brand yet, and the whole
 * brand-identity layer must stay swappable.
 */

export type BrandingStatus = 'loading' | 'ready' | 'error';

export interface BrandingState {
  status: BrandingStatus;
  config: BrandingConfig | null;
  setConfig: (config: BrandingConfig) => void;
  setError: () => void;
}

const initialState = {
  status: 'loading' as BrandingStatus,
  config: null,
};

export const useBrandingStore = create<BrandingState>((set) => ({
  ...initialState,
  setConfig: (config) => set({ config, status: 'ready' }),
  setError: () => set({ config: null, status: 'error' }),
}));

/**
 * Product display name for rendering. Empty string when branding is
 * unavailable — the UI then degrades to a glyph-only brand mark rather than
 * inventing a name.
 */
export function selectProductDisplayName(state: BrandingState): string {
  return state.config?.productDisplayName?.trim() ?? '';
}

/** Test-only helper. */
export function resetBrandingStore() {
  useBrandingStore.setState({ ...initialState });
}
