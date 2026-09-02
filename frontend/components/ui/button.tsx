import * as React from 'react';
import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

/**
 * shadcn-style Button, themed to the approved PIT961 tokens.
 * No destructive/danger variant — not needed for WP-8.
 */
const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap font-sans font-semibold transition-colors ' +
    'focus-visible:outline-none focus-visible:ring-0 focus-visible:shadow-focus-accent ' +
    'disabled:pointer-events-none disabled:opacity-60',
  {
    variants: {
      variant: {
        primary:
          'bg-accent-primary text-accent-primary-ink hover:bg-accent-primary-hover',
        outline:
          'border border-border bg-transparent text-text-primary hover:border-text-muted-3',
        ghost: 'bg-transparent text-text-muted-1 hover:text-text-primary',
      },
      size: {
        default: 'h-9 rounded-control px-3.5 text-[13px]',
        sm: 'h-8 rounded-control px-2.5 text-[12px]',
        block: 'h-[46px] w-full rounded-button px-4 text-[14px]',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'default',
    },
  },
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, type, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button';
    return (
      <Comp
        ref={ref}
        type={asChild ? undefined : (type ?? 'button')}
        className={cn(buttonVariants({ variant, size }), className)}
        {...props}
      />
    );
  },
);
Button.displayName = 'Button';

export { Button, buttonVariants };
