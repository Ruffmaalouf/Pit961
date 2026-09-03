import { useEffect, useId, useState, type FormEvent } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { FieldError } from '@/components/ui/field-error';
import { FormErrorBanner } from '@/components/ui/form-error-banner';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Modal, ModalContent } from '@/components/ui/modal';
import { Spinner } from '@/components/ui/spinner';
import * as vehiclesApi from '@/features/vehicles/api';
import { ApiError } from '@/services/apiClient';
import type { DuplicateVehicleMatchDto, VehicleDto } from '@/types/api';
import {
  EMPTY_VEHICLE_FORM_VALUES,
  hasErrors,
  validateVehicleForm,
  type VehicleFormErrors,
  type VehicleFormValues,
} from '@/validation/vehicleForm';

/**
 * Create/Edit Vehicle. Owner Decision #5 is binding here: a duplicate plate
 * is a WARNING surfaced from the server's response (`duplicateWarning`,
 * always sent alongside a real 201/200 — never a 409), not a client-side or
 * server-side hard block. The warning is shown and the save still succeeds.
 */
export function VehicleFormModal({
  open,
  onOpenChange,
  customerId,
  vehicle,
  onSaved,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Required to create — a Vehicle always belongs to a Customer. */
  customerId: string;
  /** Present -> edit; absent -> create. */
  vehicle?: VehicleDto;
  onSaved: (vehicle: VehicleDto) => void;
}) {
  const isEdit = Boolean(vehicle);
  const plateId = useId();
  const countryId = useId();
  const makeId = useId();
  const modelId = useId();
  const yearId = useId();
  const colorId = useId();
  const vinId = useId();
  const mileageId = useId();

  const [values, setValues] = useState<VehicleFormValues>(EMPTY_VEHICLE_FORM_VALUES);
  const [fieldErrors, setFieldErrors] = useState<VehicleFormErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [duplicateMatches, setDuplicateMatches] = useState<DuplicateVehicleMatchDto[] | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    setFieldErrors({});
    setDuplicateMatches(null);
    setValues(
      vehicle
        ? {
            plateNumber: vehicle.plateNumber,
            plateCountry: vehicle.plateCountry,
            make: vehicle.make,
            model: vehicle.model,
            year: vehicle.year != null ? String(vehicle.year) : '',
            color: vehicle.color ?? '',
            vin: vehicle.vin ?? '',
            currentMileage: vehicle.currentMileage != null ? String(vehicle.currentMileage) : '',
          }
        : EMPTY_VEHICLE_FORM_VALUES,
    );
  }, [open, vehicle]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setFormError(null);
    setDuplicateMatches(null);
    const errors = validateVehicleForm(values);
    setFieldErrors(errors);
    if (hasErrors(errors)) return;

    const shared = {
      plateNumber: values.plateNumber.trim(),
      plateCountry: values.plateCountry.trim(),
      make: values.make.trim(),
      model: values.model.trim(),
      year: values.year.trim() ? Number(values.year) : null,
      color: values.color.trim() || null,
      vin: values.vin.trim() || null,
      engine: null,
      engineCode: null,
      transmission: null,
      drivetrain: null,
      fuelType: null,
      currentMileage: values.currentMileage.trim() ? Number(values.currentMileage) : null,
    };

    setIsSubmitting(true);
    try {
      const result = isEdit
        ? await vehiclesApi.updateVehicle(vehicle!.id, shared)
        : await vehiclesApi.createVehicle({ ...shared, customerId });

      // Duplicate plate: WARN, never block (Owner Decision #5). The save has
      // already succeeded (real 201/200) by the time this branch runs.
      if (result.duplicateWarning.hasDuplicates) {
        setDuplicateMatches(result.duplicateWarning.matches);
      }
      onSaved(result.vehicle);
      if (!result.duplicateWarning.hasDuplicates) {
        onOpenChange(false);
      }
    } catch (error) {
      setFormError(error instanceof ApiError ? error.title : 'Something went wrong. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const disabledClass = isSubmitting ? 'pointer-events-none opacity-60' : '';

  return (
    <Modal open={open} onOpenChange={(next) => !isSubmitting && onOpenChange(next)}>
      <ModalContent title={isEdit ? 'Edit vehicle' : 'Add vehicle'}>
        <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting} data-testid="vehicle-form">
          {formError ? <FormErrorBanner message={formError} className="mb-4" /> : null}

          {duplicateMatches && duplicateMatches.length > 0 ? (
            <div
              role="status"
              data-testid="duplicate-plate-warning"
              className="mb-4 rounded-control border border-status-warning bg-[var(--status-warning-soft)] px-[14px] py-[10px] font-sans text-[12.5px] font-medium text-status-warning"
            >
              This plate is already on file for {duplicateMatches[0].customerName}
              {duplicateMatches.length > 1 ? ` and ${duplicateMatches.length - 1} other vehicle(s)` : ''}.
              The vehicle was saved anyway — please confirm this isn't a duplicate entry.
              <div className="mt-3">
                <Button type="button" size="sm" variant="outline" onClick={() => onOpenChange(false)}>
                  Done
                </Button>
              </div>
            </div>
          ) : null}

          <div className={`grid grid-cols-2 gap-3 ${disabledClass}`}>
            <div>
              <Label htmlFor={plateId}>Plate number</Label>
              <Input
                id={plateId}
                value={values.plateNumber}
                invalid={Boolean(fieldErrors.plateNumber)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, plateNumber: e.target.value }))}
              />
              {fieldErrors.plateNumber ? <FieldError message={fieldErrors.plateNumber} /> : null}
            </div>
            <div>
              <Label htmlFor={countryId}>Plate country</Label>
              <Input
                id={countryId}
                value={values.plateCountry}
                invalid={Boolean(fieldErrors.plateCountry)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, plateCountry: e.target.value }))}
              />
              {fieldErrors.plateCountry ? <FieldError message={fieldErrors.plateCountry} /> : null}
            </div>
          </div>

          <div className={`mt-3 grid grid-cols-2 gap-3 ${disabledClass}`}>
            <div>
              <Label htmlFor={makeId}>Make</Label>
              <Input
                id={makeId}
                value={values.make}
                invalid={Boolean(fieldErrors.make)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, make: e.target.value }))}
              />
              {fieldErrors.make ? <FieldError message={fieldErrors.make} /> : null}
            </div>
            <div>
              <Label htmlFor={modelId}>Model</Label>
              <Input
                id={modelId}
                value={values.model}
                invalid={Boolean(fieldErrors.model)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, model: e.target.value }))}
              />
              {fieldErrors.model ? <FieldError message={fieldErrors.model} /> : null}
            </div>
          </div>

          <div className={`mt-3 grid grid-cols-3 gap-3 ${disabledClass}`}>
            <div>
              <Label htmlFor={yearId}>Year</Label>
              <Input
                id={yearId}
                inputMode="numeric"
                value={values.year}
                invalid={Boolean(fieldErrors.year)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, year: e.target.value }))}
              />
              {fieldErrors.year ? <FieldError message={fieldErrors.year} /> : null}
            </div>
            <div>
              <Label htmlFor={colorId}>Color</Label>
              <Input
                id={colorId}
                value={values.color}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, color: e.target.value }))}
              />
            </div>
            <div>
              <Label htmlFor={mileageId}>Mileage</Label>
              <Input
                id={mileageId}
                inputMode="numeric"
                value={values.currentMileage}
                invalid={Boolean(fieldErrors.currentMileage)}
                disabled={isSubmitting}
                onChange={(e) => setValues((v) => ({ ...v, currentMileage: e.target.value }))}
              />
              {fieldErrors.currentMileage ? <FieldError message={fieldErrors.currentMileage} /> : null}
            </div>
          </div>

          <div className={`mt-3 ${disabledClass}`}>
            <Label htmlFor={vinId}>VIN</Label>
            <Input
              id={vinId}
              value={values.vin}
              disabled={isSubmitting}
              onChange={(e) => setValues((v) => ({ ...v, vin: e.target.value }))}
            />
          </div>

          <div className="mt-6 flex items-center justify-between gap-2">
            <Badge tone="neutral">Duplicate plates warn, never block</Badge>
            <div className="flex gap-2">
              <Button type="button" variant="outline" disabled={isSubmitting} onClick={() => onOpenChange(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? <Spinner /> : isEdit ? 'Save changes' : 'Add vehicle'}
              </Button>
            </div>
          </div>
        </form>
      </ModalContent>
    </Modal>
  );
}
