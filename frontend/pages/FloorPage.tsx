import { Badge } from '@/components/ui/badge';
import { useAuthStore } from '@/stores/authStore';
import { useBrandingStore } from '@/stores/brandingStore';

/**
 * WP-8 placeholder landing view for the authenticated shell.
 *
 * Intentionally NOT the Floor screen — no feature UI is in scope for WP-8.
 * It only proves that a protected route rendered and that runtime branding is
 * available inside the shell.
 */
export function FloorPage() {
  const productDisplayName = useBrandingStore(
    (state) => state.config?.productDisplayName ?? '',
  );
  const user = useAuthStore((state) => state.user);

  return (
    <section
      data-testid="app-shell-landing"
      className="rounded-card border border-border-subtle bg-surface-card p-6"
    >
      <Badge tone="accent" className="mb-3">
        Signed in
      </Badge>

      {productDisplayName ? (
        <h1
          data-testid="shell-product-display-name"
          className="font-sans text-[20px] font-semibold text-text-primary"
        >
          {productDisplayName}
        </h1>
      ) : null}

      <p className="mt-2 max-w-[60ch] font-sans text-[13px] text-text-muted-1">
        Authenticated shell placeholder. Feature screens are not part of this work package.
      </p>

      {user ? (
        <dl className="mt-4 grid max-w-[420px] grid-cols-[auto_1fr] gap-x-4 gap-y-1.5 font-mono text-[11.5px]">
          <dt className="text-text-muted-3">USER</dt>
          <dd className="text-text-primary">{user.name}</dd>
          <dt className="text-text-muted-3">ROLE</dt>
          <dd className="text-text-primary">{user.role}</dd>
          <dt className="text-text-muted-3">GARAGE</dt>
          <dd className="text-text-primary">{user.garageName}</dd>
        </dl>
      ) : null}
    </section>
  );
}
