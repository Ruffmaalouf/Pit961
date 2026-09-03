import * as React from 'react';
import { cn } from '@/lib/utils';

export interface CheckboxProps extends React.InputHTMLAttributes<HTMLInputElement> {}

/** Native checkbox, styled minimally — this scope needs a simple boolean toggle, not Radix. */
const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      type="checkbox"
      className={cn(
        'h-[16px] w-[16px] shrink-0 cursor-pointer rounded-[4px] border border-border bg-surface-input',
        'accent-accent-primary focus-visible:outline-none focus-visible:shadow-focus-accent',
        className,
      )}
      {...props}
    />
  ),
);
Checkbox.displayName = 'Checkbox';

export { Checkbox };
