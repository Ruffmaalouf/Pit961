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
      {logoUrl ? (
        <img
          src={logoUrl}
          alt={productDisplayName ? `${productDisplayName} logo` : ''}
          data-testid="brand-mark-logo"
          className="h-full w-full object-contain"
        />
      ) : (
        initial
      )}
    </div>
  );
}
