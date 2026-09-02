import { cn } from '@/lib/utils';

/**
 * Form-level error banner (e.g. a 401 from POST /login).
 *
 * The message is the server's ProblemDetails `title`, rendered verbatim as
 * text content. It is deliberately generic/non-enumerable server-side — do not
 * rephrase it client-side, and never use dangerouslySetInnerHTML here.
 */
export function FormErrorBanner({
  message,
  className,
}: {
  message: string;
  className?: string;
}) {
  return (
    <div
      role="alert"
      data-testid="form-error-banner"
      className={cn(
        'rounded-control border border-status-critical bg-[var(--status-critical-soft)]',
        'px-[14px] py-[10px] font-sans text-[12.5px] font-medium text-status-critical',
        className,
      )}
    >
      {message}
    </div>
  );
}
