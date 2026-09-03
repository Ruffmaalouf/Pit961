import { useEffect, useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { FieldError } from '@/components/ui/field-error';
import { FormErrorBanner } from '@/components/ui/form-error-banner';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Modal, ModalContent } from '@/components/ui/modal';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import * as customersApi from '@/features/customers/api';
import { ApiError } from '@/services/apiClient';
import type { CustomerDto } from '@/types/api';
import {
  EMPTY_CUSTOMER_FORM_VALUES,
  hasErrors,
  validateCustomerForm,
  type CustomerFormErrors,
  type CustomerFormValues,
} from '@/validation/customerForm';

/**
 * Create/Edit Customer, styled per DESIGN_IMPLEMENTATION_DIFFERENCES.md item
 * 9's "Record payment" modal pattern (header + field stack + submit). Real
 * POST/PUT against the P2-WP2 API — no client-side-only success state.
 */
export function CustomerFormModal({
  open,
  onOpenChange,
  customer,
  onSaved,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Present -> edit; absent -> create. */
  customer?: CustomerDto;
  onSaved: (customer: CustomerDto) => void;
}) {
  const isEdit = Boolean(customer);
  const firstNameId = useId();
  const lastNameId = useId();
  const phoneId = useId();
  const whatsappId = useId();
  const emailId = useId();
  const notesId = useId();

  const [values, setValues] = useState<CustomerFormValues>(EMPTY_CUSTOMER_FORM_VALUES);
  const [fieldErrors, setFieldErrors] = useState<CustomerFormErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    setFieldErrors({});
    setValues(
      customer
        ? {
            firstName: customer.firstName,
            lastName: customer.lastName ?? '',
            phone: customer.phone,
            whatsapp: customer.whatsapp ?? '',
            email: customer.email ?? '',
            notes: customer.notes ?? '',
            isFleet: customer.isFleet,
          }
        : EMPTY_CUSTOMER_FORM_VALUES,
    );
  }, [open, customer]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setFormError(null);
    const errors = validateCustomerForm(values);
    setFieldErrors(errors);
    if (hasErrors(errors)) return;

    const request = {
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim() || null,
      phone: values.phone.trim(),
      whatsapp: values.whatsapp.trim() || null,
      email: values.email.trim() || null,
      notes: values.notes.trim() || null,
      isFleet: values.isFleet,
    };

    setIsSubmitting(true);
    try {
      const saved = isEdit
        ? await customersApi.updateCustomer(customer!.id, request)
        : await customersApi.createCustomer(request);
      onSaved(saved);
      onOpenChange(false);
    } catch (error) {
      setFormError(error instanceof ApiError ? error.title : 'Something went wrong. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const disabledClass = isSubmitting ? 'pointer-events-none opacity-60' : '';

  return (
    <Modal open={open} onOpenChange={(next) => !isSubmitting && onOpenChange(next)}>
      <ModalContent
        title={isEdit ? 'Edit customer' : 'New customer'}
        description={isEdit ? undefined : 'Create a customer record before adding a vehicle or job.'}
      >
        <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting} data-testid="customer-form">
          {formError ? <FormErrorBanner message={formError} className="mb-4" /> : null}

          <div className={`grid grid-cols-2 gap-3 ${disabledClass}`}>
            <div>
              <Label htmlFor={firstNameId}>First name</Label>
              <Input
                id={firstNameId}
                value={values.firstName}
                invalid={Boolean(fieldErrors.firstName)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, firstName: e.target.value }))}
              />
              {fieldErrors.firstName ? <FieldError message={fieldErrors.firstName} /> : null}
            </div>
            <div>
              <Label htmlFor={lastNameId}>Last name</Label>
              <Input
                id={lastNameId}
                value={values.lastName}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, lastName: e.target.value }))}
              />
            </div>
          </div>

          <div className={`mt-3 grid grid-cols-2 gap-3 ${disabledClass}`}>
            <div>
              <Label htmlFor={phoneId}>Phone</Label>
              <Input
                id={phoneId}
                type="tel"
                value={values.phone}
                invalid={Boolean(fieldErrors.phone)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, phone: e.target.value }))}
              />
              {fieldErrors.phone ? <FieldError message={fieldErrors.phone} /> : null}
            </div>
            <div>
              <Label htmlFor={whatsappId}>WhatsApp</Label>
              <Input
                id={whatsappId}
                type="tel"
                value={values.whatsapp}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, whatsapp: e.target.value }))}
              />
            </div>
          </div>

          <div className={`mt-3 ${disabledClass}`}>
            <Label htmlFor={emailId}>Email</Label>
            <Input
              id={emailId}
              type="email"
              value={values.email}
              invalid={Boolean(fieldErrors.email)}
              disabled={isSubmitting}
              onChange={(e) => setValues((v) => ({ ...v, email: e.target.value }))}
            />
            {fieldErrors.email ? <FieldError message={fieldErrors.email} /> : null}
          </div>

          <div className={`mt-3 ${disabledClass}`}>
            <Label htmlFor={notesId}>Notes</Label>
            <Textarea
              id={notesId}
              value={values.notes}
              disabled={isSubmitting}
              onChange={(e) => setValues((v) => ({ ...v, notes: e.target.value }))}
            />
          </div>

          <label className={`mt-3 flex items-center gap-2 font-sans text-[13px] text-text-primary ${disabledClass}`}>
            <Checkbox
              checked={values.isFleet}
              disabled={isSubmitting}
              onChange={(e) => setValues((v) => ({ ...v, isFleet: e.target.checked }))}
            />
            Fleet account
          </label>

          <div className="mt-6 flex justify-end gap-2">
            <Button type="button" variant="outline" disabled={isSubmitting} onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? <Spinner /> : isEdit ? 'Save changes' : 'Create customer'}
            </Button>
          </div>
        </form>
      </ModalContent>
    </Modal>
  );
}
