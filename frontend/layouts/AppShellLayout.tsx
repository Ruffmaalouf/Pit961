import { Link, Outlet, useLocation } from 'react-router-dom';
import { BrandMark } from '@/components/brand-mark';
import { NavGlyph } from '@/components/nav-glyph';
import { Button } from '@/components/ui/button';
import { logoutSession } from '@/features/auth/session';
import { NAV_ITEMS } from '@/layouts/nav-items';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/authStore';
import { useBrandingStore } from '@/stores/brandingStore';
import { useCrumbStore } from '@/stores/crumbStore';

/**
 * Authenticated GARAGE-TENANT shell: 76px icon-only nav rail + 52px header.
 *
 * WP-8 built this as a placeholder shell. Milestone 1 (P2-WP2/P2-WP3) wires
 * Floor / Jobs / Customers to real screens — WIRED_PATHS below is what makes
 * those three clickable Links instead of inert spans; the remaining five
 * (Clock/Money/Parts/Team/Reports) stay exactly as WP-8 left them, since none
 * of that is in scope yet. The search bar / role switcher / alerts bell from
 * the prototype header remain out of scope.
 *
 * This is a tenant surface. Platform-admin capabilities are a separate role
 * and domain and must never be merged into this shell.
 */
const WIRED_PATHS = new Set(['/floor', '/jobs', '/customers']);

export function AppShellLayout() {
  const location = useLocation();
  const productDisplayName = useBrandingStore(
    (state) => state.config?.productDisplayName ?? '',
  );
  const user = useAuthStore((state) => state.user);
  const pageCrumb = useCrumbStore((state) => state.crumb);
  const activeNavLabel = NAV_ITEMS.find((item) => location.pathname.startsWith(item.path))?.label;
  const crumb = pageCrumb ?? activeNavLabel?.toUpperCase() ?? 'GARAGEOS';

  return (
    <div className="flex min-h-screen w-full bg-surface-app">
      <nav
        aria-label="Primary"
        data-testid="app-sidebar"
        className="sticky top-0 z-20 flex h-screen w-[76px] flex-none flex-col items-center border-r border-border-subtle px-0 pb-3.5 pt-[13px]"
        style={{ background: 'linear-gradient(180deg,#111517,#0d1112)' }}
      >
        <div className="flex flex-col items-center gap-[7px]">
          <BrandMark size={36} radius={11} fontSize={15} />
          {productDisplayName ? (
            // Long tenant names are truncated rather than allowed to overflow
            // the 76px rail. Flagged to Design Lead: the approved prototype only
            // ever showed a short wordmark here, so there is no approved
            // treatment for long names yet.
            <span
              title={productDisplayName}
              data-testid="shell-product-display-name"
              className="max-w-[68px] truncate font-mono text-micro uppercase tracking-[0.16em] text-rail-label"
            >
              {productDisplayName}
            </span>
          ) : null}
        </div>

        <div
          className="mt-3 h-px w-[34px]"
          style={{ background: 'linear-gradient(90deg,transparent,#252c30,transparent)' }}
        />

        <ul className="mt-[13px] flex w-full flex-col items-center gap-[3px]">
          {NAV_ITEMS.map((item) => {
            const active = location.pathname.startsWith(item.path);
            const wired = WIRED_PATHS.has(item.path);
            const railClassName = cn(
              'relative flex w-[58px] flex-col items-center gap-[5px] rounded-[10px] px-0 pb-[5px] pt-[7px]',
              wired ? 'cursor-pointer' : 'cursor-default',
              active
                ? 'bg-gradient-to-b from-[#232a2e] to-[#1a2023] shadow-rail-active'
                : 'bg-transparent',
            );
            const railContent = (
              <>
                {active ? (
                  <span
                    aria-hidden
                    className="absolute -left-[9px] bottom-[9px] top-[9px] w-[3px] rounded-r-[3px] bg-accent-primary shadow-rail-blade"
                  />
                ) : null}
                <NavGlyph glyphKey={item.key} active={active} />
                <span
                  className={cn(
                    'font-mono text-micro uppercase tracking-rail',
                    active ? 'text-[#d7b184]' : 'text-rail-label',
                  )}
                >
                  {item.label}
                </span>
              </>
            );
            return (
              <li key={item.key} className="relative">
                {wired ? (
                  <Link
                    to={item.path}
                    data-testid={`nav-item-${item.key}`}
                    data-active={active ? 'true' : 'false'}
                    aria-current={active ? 'page' : undefined}
                    className={railClassName}
                  >
                    {railContent}
                  </Link>
                ) : (
                  <span
                    data-testid={`nav-item-${item.key}`}
                    data-active={active ? 'true' : 'false'}
                    aria-current={active ? 'page' : undefined}
                    className={railClassName}
                  >
                    {railContent}
                  </span>
                )}
              </li>
            );
          })}
        </ul>
      </nav>

      <div className="relative z-10 flex min-w-0 flex-1 flex-col">
        <header
          data-testid="app-header"
          className="sticky top-0 z-30 flex h-[52px] flex-none items-center gap-3.5 border-b border-border-subtle bg-surface-header px-[18px]"
        >
          <span className="font-mono text-eyebrow uppercase tracking-crumb text-[#5f696e]">
            {crumb}
          </span>

          <div className="ml-auto flex items-center gap-3">
            {user ? (
              <span className="font-mono text-[11px] text-text-muted-2">
                {user.name} · {user.garageName}
              </span>
            ) : null}
            <Button variant="outline" size="sm" onClick={() => void logoutSession()}>
              Log out
            </Button>
          </div>
        </header>

        <main className="flex-1 p-5">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
