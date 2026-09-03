import * as React from 'react';
import { cn } from '@/lib/utils';

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  invalid?: boolean;
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, invalid, ...props }, ref) => (
    <textarea
      ref={ref}
      aria-invalid={invalid || undefined}
      className={cn(
        'min-h-[76px] w-full rounded-control bg-surface-input px-3 py-[9px]',
        'border font-sans text-[14px] text-text-primary placeholder:text-text-muted-3',
        'transition-[border-color,box-shadow] outline-none resize-y',
        'focus:border-accent-primary focus:shadow-focus-accent',
        invalid ? 'border-status-critical' : 'border-border',
        className,
      )}
      {...props}
    />
  ),
);
Textarea.displayName = 'Textarea';

export { Textarea };
