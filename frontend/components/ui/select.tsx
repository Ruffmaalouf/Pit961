import * as React from 'react';
import { cn } from '@/lib/utils';

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  invalid?: boolean;
}

/** Native <select>, styled to match Input — no Radix Select needed for this scope. */
const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, invalid, children, ...props }, ref) => (
    <select
      ref={ref}
      aria-invalid={invalid || undefined}
      className={cn(
        'h-[42px] w-full rounded-control bg-surface-input px-3 py-[9px]',
        'border font-sans text-[14px] text-text-primary',
        'transition-[border-color,box-shadow] outline-none',
        'focus:border-accent-primary focus:shadow-focus-accent',
        invalid ? 'border-status-critical' : 'border-border',
        className,
      )}
      {...props}
    >
      {children}
    </select>
  ),
);
Select.displayName = 'Select';

export { Select };
