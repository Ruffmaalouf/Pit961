import { buildMailtoHref } from '@/lib/safe-url';
import { cn } from '@/lib/utils';
import { useBrandingStore } from '@/stores/brandingStore';

/**
 * Renders the runtime `supportEmail` from the branding config.
 *
 * A mailto: link is only produced when the value passes the plausible-email
 * check, and the href is percent-encoded rather than concatenated. Anything
 * else renders as inert plain text (or nothing), so a hostile config value can
 * never become a clickable non-mailto target.
 */
export function SupportEmailLink({ className }: { className?: string }) {
  const supportEmail = useBrandingStore((state) => state.config?.supportEmail ?? '');
  const trimmed = supportEmail.trim();

  if (trimmed.length === 0) return null;

  const href = buildMailtoHref(trimmed);

  if (!href) {
    return (
      <span data-testid="support-email-text" className={cn('text-text-muted-3', className)}>
        {trimmed}
      </span>
    );
  }

  return (
    <a
      data-testid="support-email-link"
      href={href}
      className={cn('text-text-muted-2 underline-offset-2 hover:text-accent-primary hover:underline', className)}
    >
      {trimmed}
    </a>
  );
}
