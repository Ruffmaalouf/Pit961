/**
 * Client-side Vehicle form validation is a UX convenience ONLY — the backend
 * is authoritative. Duplicate plate is explicitly NOT validated here as an
 * error — Owner Decision #5 makes it a warning surfaced from the server's
 * response, never a client-side (or server-side) hard block.
 */

export interface VehicleFormValues {
  plateNumber: string;
  plateCountry: string;
  make: string;
  model: string;
  year: string;
  color: string;
  vin: string;
  currentMileage: string;
}

export type VehicleFormErrors = Partial<
  Record<'plateNumber' | 'plateCountry' | 'make' | 'model' | 'year' | 'currentMileage', string>
>;

export const VEHICLE_VALIDATION_MESSAGES = {
  plateNumberRequired: 'Plate number is required.',
  plateCountryRequired: 'Plate country is required.',
  makeRequired: 'Make is required.',
  modelRequired: 'Model is required.',
  yearInvalid: 'Enter a valid year.',
  mileageInvalid: 'Enter a valid mileage.',
} as const;

const CURRENT_YEAR = new Date().getFullYear();

export function validateVehicleForm(values: VehicleFormValues): VehicleFormErrors {
  const errors: VehicleFormErrors = {};

  if (values.plateNumber.trim().length === 0) {
    errors.plateNumber = VEHICLE_VALIDATION_MESSAGES.plateNumberRequired;
  }
  if (values.plateCountry.trim().length === 0) {
    errors.plateCountry = VEHICLE_VALIDATION_MESSAGES.plateCountryRequired;
  }
  if (values.make.trim().length === 0) {
    errors.make = VEHICLE_VALIDATION_MESSAGES.makeRequired;
  }
  if (values.model.trim().length === 0) {
    errors.model = VEHICLE_VALIDATION_MESSAGES.modelRequired;
  }

  if (values.year.trim().length > 0) {
    const year = Number(values.year);
    if (!Number.isInteger(year) || year < 1900 || year > CURRENT_YEAR + 1) {
      errors.year = VEHICLE_VALIDATION_MESSAGES.yearInvalid;
    }
  }

  if (values.currentMileage.trim().length > 0) {
    const mileage = Number(values.currentMileage);
    if (!Number.isInteger(mileage) || mileage < 0) {
      errors.currentMileage = VEHICLE_VALIDATION_MESSAGES.mileageInvalid;
    }
  }

  return errors;
}

export function hasErrors(errors: VehicleFormErrors): boolean {
  return Object.keys(errors).length > 0;
}

export const EMPTY_VEHICLE_FORM_VALUES: VehicleFormValues = {
  plateNumber: '',
  plateCountry: '',
  make: '',
  model: '',
  year: '',
  color: '',
  vin: '',
  currentMileage: '',
};
