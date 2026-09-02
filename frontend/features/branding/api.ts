import { CONFIG_ENDPOINTS, apiClient } from '@/services/apiClient';
import { useBrandingStore } from '@/stores/brandingStore';
import type { BrandingConfig } from '@/types/api';

/**
 * GET /api/config/branding is [AllowAnonymous] and not cookie-backed, so it
 * needs neither a bearer token nor `credentials: 'include'`.
 */
export function fetchBranding(): Promise<BrandingConfig> {
  return apiClient.get<BrandingConfig>(CONFIG_ENDPOINTS.branding);
}

/**
 * Boot-time load. A failure is non-fatal: the UI degrades to a glyph-only
 * brand mark rather than falling back to any hardcoded product name.
 */
export async function bootstrapBranding(): Promise<void> {
  try {
    const config = await fetchBranding();
    useBrandingStore.getState().setConfig(config);
  } catch {
    useBrandingStore.getState().setError();
  }
}
