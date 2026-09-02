import { cn } from '@/lib/utils';

/** Field-level validation message (client-side convenience only). */
export function FieldError({ id, message, className }: { id?: string; message: string; className?: string }) {
  return (
    <p
      id={id}
      className={cn('mt-1.5 font-sans text-[11.5px] font-medium text-status-critical', className)}
    >
      {message}
    </p>
  );
}
