import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field-error';
import { FormErrorBanner } from '@/components/ui/form-error-banner';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
import { loginWithPassword } from '@/features/auth/session';
import { ApiError } from '@/services/apiClient';
import { cn } from '@/lib/utils';
import {
  hasErrors,
  validateLoginForm,
  type LoginFormErrors,
} from '@/validation/loginForm';

/**
 * Login form.
 *
 * Client-side validation here is a UX convenience only — the backend is the
 * authoritative authentication check. The form-level banner shows the server's
 * ProblemDetails `title` verbatim (e.g. "Invalid email or password."), which is
 * intentionally generic so accounts cannot be enumerated.
 *
 * Per the approved spec there is deliberately no "remember me", no SSO, no
 * sign-up link and no "forgot password" link.
 */
export function LoginForm() {
  const emailId = useId();
  const passwordId = useId();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<LoginFormErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setFormError(null);

    const errors = validateLoginForm({ email, password });
    setFieldErrors(errors);
    if (hasErrors(errors)) return;

    setIsSubmitting(true);
    try {
      await loginWithPassword({ email: email.trim(), password });
      // Navigation is handled by the route guard reacting to auth state.
    } catch (error) {
      setFormError(
        error instanceof ApiError
          ? error.title
          : 'Something went wrong. Please try again.',
      );
      setIsSubmitting(false);
    }
  }

  const fieldsDisabledClass = isSubmitting ? 'opacity-60 pointer-events-none' : '';

  return (
    <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
      {formError ? <FormErrorBanner message={formError} className="mb-4" /> : null}

      <div className={cn('mb-[14px]', fieldsDisabledClass)}>
        <Label htmlFor={emailId}>Email</Label>
        <Input
          id={emailId}
          name="email"
          type="email"
          autoComplete="email"
          placeholder="you@garage.example"
          value={email}
          invalid={Boolean(fieldErrors.email)}
          aria-describedby={fieldErrors.email ? `${emailId}-error` : undefined}
          disabled={isSubmitting}
          onChange={(event) => setEmail(event.target.value)}
        />
        {fieldErrors.email ? (
          <FieldError id={`${emailId}-error`} message={fieldErrors.email} />
        ) : null}
      </div>

      <div className={cn('mb-5', fieldsDisabledClass)}>
        <Label htmlFor={passwordId}>Password</Label>
        <Input
          id={passwordId}
          name="password"
          type="password"
          autoComplete="current-password"
          placeholder="••••••••"
          value={password}
          invalid={Boolean(fieldErrors.password)}
          aria-describedby={fieldErrors.password ? `${passwordId}-error` : undefined}
          disabled={isSubmitting}
          onChange={(event) => setPassword(event.target.value)}
        />
        {fieldErrors.password ? (
          <FieldError id={`${passwordId}-error`} message={fieldErrors.password} />
        ) : null}
      </div>

      <Button type="submit" size="block" disabled={isSubmitting} aria-label="Log in">
        {isSubmitting ? <Spinner /> : 'Log in'}
      </Button>
    </form>
  );
}
