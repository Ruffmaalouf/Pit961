import { useEffect, useState } from 'react';
import { brandInitial } from '@/lib/branding';
import { safeHttpUrlOrNull } from '@/lib/safe-url';
import { cn } from '@/lib/utils';
import { useBrandingStore } from '@/stores/brandingStore';

/**
 * The rounded-square brand mark, used identically on the login screen and in
 * the authenticated shell's nav rail.
 *
 * Everything it renders comes from the runtime branding config:
 *  - the letter is derived from `productDisplayName` at render time
 *    (never a hardcoded character or wordmark)
 *  - `logoUrl` is only used as an <img src> after passing isSafeHttpUrl();
 *    anything else (javascript:, data:, protocol-relative, ...) falls back to
 *    the glyph-only mark with no image at all
 *  - a URL that IS safe but fails to actually load (404, DNS failure, CORS)
 *    also falls back to the glyph-only mark on the image's error event,
 *    rather than leaving a broken-image icon with overflowing alt text.
 *    Caught via real-device screenshot review during WP-8 verification.
 */
export function BrandMark({
  size = 44,
  radius = 13,
  fontSize = 18,
  className,
}: {
  size?: number;
  radius?: number;
  fontSize?: number;
  className?: string;
}) {
  const config = useBrandingStore((state) => state.config);
  const productDisplayName = config?.productDisplayName ?? '';
  const initial = brandInitial(productDisplayName);
  const logoUrl = safeHttpUrlOrNull(config?.logoUrl);
  const [logoFailed, setLogoFailed] = useState(false);

  // Re-arm the fallback whenever the URL itself changes (a new config load
  // deserves a fresh attempt, not a fallback state stuck from a prior logo).
  useEffect(() => {
    setLogoFailed(false);
  }, [logoUrl]);

  const showLogo = Boolean(logoUrl) && !logoFailed;

  return (
    <div
      data-testid="brand-mark"
      aria-hidden={productDisplayName ? undefined : true}
      className={cn('flex items-center justify-center overflow-hidden', className)}
      style={{
        width: size,
        height: size,
        borderRadius: radius,
        background: 'var(--brand-mark-gradient)',
        boxShadow: '0 2px 10px #e2892f40, inset 0 1px 0 #ffd3a0',
        color: 'var(--accent-primary-ink)',
        fontWeight: 600,
        fontSize,
        letterSpacing: '-0.02em',
      }}
    >
      {showLogo ? (
        <img
          src={logoUrl ?? undefined}
          alt={productDisplayName ? `${productDisplayName} logo` : ''}
          data-testid="brand-mark-logo"
          className="h-full w-full object-contain"
          onError={() => setLogoFailed(true)}
        />
      ) : (
        initial
      )}
    </div>
  );
}
