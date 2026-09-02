import { useEffect } from 'react';
import { useBrandingStore } from '@/stores/brandingStore';

/**
 * Keeps <title> in sync with the runtime product display name. The name is
 * never a build-time constant — index.html only carries a neutral placeholder.
 */
export function useBrandedDocumentTitle(): void {
  const productDisplayName = useBrandingStore(
    (state) => state.config?.productDisplayName ?? '',
  );

  useEffect(() => {
    if (typeof document === 'undefined') return;
    const name = productDisplayName.trim();
    if (name.length > 0) document.title = name;
  }, [productDisplayName]);
}
