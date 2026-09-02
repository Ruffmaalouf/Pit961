import { isPlausibleEmail } from '@/lib/safe-url';

/**
 * Client-side login validation is a UX convenience ONLY. The backend is the
 * authoritative check for every rule in this product — this exists to avoid a
 * pointless round trip and to place the error next to the field, never to
 * decide whether a credential is acceptable.
 */

export interface LoginFormValues {
  email: string;
  password: string;
}

export type LoginFormErrors = Partial<Record<keyof LoginFormValues, string>>;

export const LOGIN_VALIDATION_MESSAGES = {
  emailRequired: 'Email is required.',
  emailInvalid: 'Enter a valid email address.',
  passwordRequired: 'Password is required.',
} as const;

export function validateLoginForm(values: LoginFormValues): LoginFormErrors {
  const errors: LoginFormErrors = {};

  const email = values.email.trim();
  if (email.length === 0) {
    errors.email = LOGIN_VALIDATION_MESSAGES.emailRequired;
  } else if (!isPlausibleEmail(email)) {
    errors.email = LOGIN_VALIDATION_MESSAGES.emailInvalid;
  }

  if (values.password.length === 0) {
    errors.password = LOGIN_VALIDATION_MESSAGES.passwordRequired;
  }

  return errors;
}

export function hasErrors(errors: LoginFormErrors): boolean {
  return Object.keys(errors).length > 0;
}
