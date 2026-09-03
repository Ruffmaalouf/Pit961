import { isPlausibleEmail } from '@/lib/safe-url';

/**
 * Client-side Customer form validation is a UX convenience ONLY — the backend
 * is authoritative for every field. This exists to place errors next to the
 * field before a round trip, never to decide acceptability. Mirrors
 * validation/loginForm.ts's shape and intent.
 */

export interface CustomerFormValues {
  firstName: string;
  lastName: string;
  phone: string;
  whatsapp: string;
  email: string;
  notes: string;
  isFleet: boolean;
}

export type CustomerFormErrors = Partial<
  Record<'firstName' | 'phone' | 'email', string>
>;

export const CUSTOMER_VALIDATION_MESSAGES = {
  firstNameRequired: 'First name is required.',
  phoneRequired: 'Phone number is required.',
  emailInvalid: 'Enter a valid email address.',
} as const;

export function validateCustomerForm(values: CustomerFormValues): CustomerFormErrors {
  const errors: CustomerFormErrors = {};

  if (values.firstName.trim().length === 0) {
    errors.firstName = CUSTOMER_VALIDATION_MESSAGES.firstNameRequired;
  }

  if (values.phone.trim().length === 0) {
    errors.phone = CUSTOMER_VALIDATION_MESSAGES.phoneRequired;
  }

  const email = values.email.trim();
  if (email.length > 0 && !isPlausibleEmail(email)) {
    errors.email = CUSTOMER_VALIDATION_MESSAGES.emailInvalid;
  }

  return errors;
}

export function hasErrors(errors: CustomerFormErrors): boolean {
  return Object.keys(errors).length > 0;
}

export const EMPTY_CUSTOMER_FORM_VALUES: CustomerFormValues = {
  firstName: '',
  lastName: '',
  phone: '',
  whatsapp: '',
  email: '',
  notes: '',
  isFleet: false,
};
