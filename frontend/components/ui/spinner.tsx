import { cn } from '@/lib/utils';

/**
 * Pure-CSS rotating spinner. Inherits the button's text colour via
 * `currentColor` so it reads correctly on the orange primary button.
 */
export function Spinner({ className }: { className?: string }) {
  return (
    <span
      role="status"
      aria-label="Loading"
      data-testid="spinner"
      className={cn(
        'inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent',
        className,
      )}
    />
  );
}
