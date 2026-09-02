/**
 * Shown only while the boot-time silent refresh is in flight. Deliberately
 * near-empty (just the app background) so a fast refresh does not produce a
 * visible loading flash, and so no brand name is guessed before the runtime
 * branding config has loaded.
 */
export function BootSplash() {
  return <div data-testid="boot-splash" className="min-h-screen w-full bg-surface-app" aria-hidden />;
}
