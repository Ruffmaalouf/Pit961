import * as React from 'react';
import * as DialogPrimitive from '@radix-ui/react-dialog';
import { cn } from '@/lib/utils';

/**
 * Panel/modal, styled to match the prototype's "Record payment" pattern
 * (header + field stack + submit) per DESIGN_IMPLEMENTATION_DIFFERENCES.md
 * item 9 — used for Customer/Vehicle/Job create-edit forms. Radix Dialog
 * gives focus trapping, ESC-to-close and aria wiring for free; only the
 * visual chrome is custom here.
 */

const Modal = DialogPrimitive.Root;
const ModalTrigger = DialogPrimitive.Trigger;

function ModalContent({
  className,
  children,
  title,
  description,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content> & {
  title: string;
  description?: string;
}) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className="fixed inset-0 z-40 bg-black/60 data-[state=open]:animate-in data-[state=open]:fade-in-0 data-[state=closed]:animate-out data-[state=closed]:fade-out-0" />
      <DialogPrimitive.Content
        className={cn(
          'fixed left-1/2 top-1/2 z-50 w-full max-w-[520px] -translate-x-1/2 -translate-y-1/2',
          'max-h-[88vh] overflow-y-auto rounded-panel border border-border-subtle bg-surface-card',
          'p-6 shadow-[0_20px_60px_rgba(0,0,0,0.5)] focus:outline-none',
          className,
        )}
        {...props}
      >
        <DialogPrimitive.Title className="font-sans text-[17px] font-semibold text-text-primary">
          {title}
        </DialogPrimitive.Title>
        {description ? (
          <DialogPrimitive.Description className="mt-1 font-sans text-[12.5px] text-text-muted-1">
            {description}
          </DialogPrimitive.Description>
        ) : null}
        <div className="mt-5">{children}</div>
        <DialogPrimitive.Close
          aria-label="Close"
          className="absolute right-4 top-4 rounded-control p-1 text-text-muted-2 hover:text-text-primary focus-visible:outline-none focus-visible:shadow-focus-accent"
        >
          ✕
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  );
}

export { Modal, ModalTrigger, ModalContent };
