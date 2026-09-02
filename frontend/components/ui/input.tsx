import * as React from 'react';
import { cn } from '@/lib/utils';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  /** Renders the critical-state border from the design spec. */
  invalid?: boolean;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, invalid, type = 'text', ...props }, ref) => (
    <input
      ref={ref}
      type={type}
      aria-invalid={invalid || undefined}
      className={cn(
        'h-[42px] w-full rounded-control bg-surface-input px-3 py-[9px]',
        'border font-sans text-[14px] text-text-primary placeholder:text-text-muted-3',
        'transition-[border-color,box-shadow] outline-none',
        'focus:border-accent-primary focus:shadow-focus-accent',
        invalid ? 'border-status-critical' : 'border-border',
        className,
      )}
      {...props}
    />
  ),
);
Input.displayName = 'Input';

export { Input };
