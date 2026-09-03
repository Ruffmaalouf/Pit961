/**
 * Client-side Job intake form validation is a UX convenience ONLY — the
 * backend is authoritative. Status/GarageId/JobNumber are never part of this
 * form's values at all (see features/jobs/CreateJobModal.tsx) so there is
 * nothing here to validate for them.
 */

export interface JobIntakeFormValues {
  customerId: string;
  vehicleId: string;
  mileageAtIntake: string;
  customerComplaint: string;
  advisorNotes: string;
  promisedAt: string;
  customerWaiting: boolean;
  overnight: boolean;
  overnightNote: string;
  isWarrantyReturn: boolean;
}

export type JobIntakeFormErrors = Partial<
  Record<'customerId' | 'vehicleId' | 'mileageAtIntake', string>
>;

export const JOB_VALIDATION_MESSAGES = {
  customerRequired: 'Select a customer first.',
  vehicleRequired: 'Select a vehicle.',
  mileageInvalid: 'Enter a valid mileage.',
} as const;

export function validateJobIntakeForm(values: JobIntakeFormValues): JobIntakeFormErrors {
  const errors: JobIntakeFormErrors = {};

  if (values.customerId.trim().length === 0) {
    errors.customerId = JOB_VALIDATION_MESSAGES.customerRequired;
  }
  if (values.vehicleId.trim().length === 0) {
    errors.vehicleId = JOB_VALIDATION_MESSAGES.vehicleRequired;
  }
  if (values.mileageAtIntake.trim().length > 0) {
    const mileage = Number(values.mileageAtIntake);
    if (!Number.isInteger(mileage) || mileage < 0) {
      errors.mileageAtIntake = JOB_VALIDATION_MESSAGES.mileageInvalid;
    }
  }

  return errors;
}

export function hasErrors(errors: JobIntakeFormErrors): boolean {
  return Object.keys(errors).length > 0;
}

export function emptyJobIntakeFormValues(customerId = '', vehicleId = ''): JobIntakeFormValues {
  return {
    customerId,
    vehicleId,
    mileageAtIntake: '',
    customerComplaint: '',
    advisorNotes: '',
    promisedAt: '',
    customerWaiting: false,
    overnight: false,
    overnightNote: '',
    isWarrantyReturn: false,
  };
}
