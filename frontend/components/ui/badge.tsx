import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

/**
 * Pill/badge. Background is the status colour at the `1f` alpha suffix and the
 * text is the same colour at full strength — the approved tint pattern.
 */
const badgeVariants = cva(
  'inline-flex items-center whitespace-nowrap rounded-pill px-[7px] py-[3px] ' +
    'font-mono text-[9.5px] font-semibold tracking-[0.09em] uppercase',
  {
    variants: {
      tone: {
        neutral: 'bg-surface-card-item text-text-muted-1',
        accent: 'bg-[var(--accent-focus-ring)] text-accent-primary',
        success: 'bg-[var(--status-success-soft)] text-status-success',
        warning: 'bg-[var(--status-warning-soft)] text-status-warning',
        critical: 'bg-[var(--status-critical-soft)] text-status-critical',
      },
    },
    defaultVariants: {
      tone: 'neutral',
    },
  },
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, tone, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ tone }), className)} {...props} />;
}

export { Badge, badgeVariants };
