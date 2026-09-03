import { useEffect, useId, useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { FieldError } from '@/components/ui/field-error';
import { FormErrorBanner } from '@/components/ui/form-error-banner';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select } from '@/components/ui/select';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { CustomerFormModal } from '@/features/customers/CustomerFormModal';
import * as customersApi from '@/features/customers/api';
import * as jobsApi from '@/features/jobs/api';
import { VehicleFormModal } from '@/features/vehicles/VehicleFormModal';
import { useCrumb } from '@/hooks/useCrumb';
import { ApiError } from '@/services/apiClient';
import type { CustomerListItemDto, VehicleSummaryDto } from '@/types/api';
import {
  emptyJobIntakeFormValues,
  hasErrors,
  validateJobIntakeForm,
  type JobIntakeFormErrors,
} from '@/validation/jobForm';

/**
 * Real Create Job workflow: select-or-create Customer, select-or-create
 * Vehicle, enter intake fields, create. Composed entirely from the
 * already-built Customer/Vehicle patterns, per
 * DESIGN_IMPLEMENTATION_DIFFERENCES.md item 10 — a composition, not a new
 * visual pattern. GarageId/Status/JobNumber are never fields on this form;
 * the server generates all three.
 */
export function JobIntakePage() {
  useCrumb('CHECK IN VEHICLE');
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const complaintId = useId();
  const notesId = useId();
  const mileageId = useId();
  const promisedId = useId();
  const overnightNoteId = useId();

  const [customerSearch, setCustomerSearch] = useState('');
  const [customerResults, setCustomerResults] = useState<CustomerListItemDto[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerListItemDto | null>(null);
  const [isNewCustomerOpen, setIsNewCustomerOpen] = useState(false);

  const [vehicles, setVehicles] = useState<VehicleSummaryDto[] | null>(null);
  const [selectedVehicleId, setSelectedVehicleId] = useState('');
  const [isNewVehicleOpen, setIsNewVehicleOpen] = useState(false);

  const [values, setValues] = useState(emptyJobIntakeFormValues());
  const [fieldErrors, setFieldErrors] = useState<JobIntakeFormErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Deep-link support: Customer Detail's "New job" link supplies both ids.
  useEffect(() => {
    const customerId = searchParams.get('customerId');
    const vehicleId = searchParams.get('vehicleId');
    if (!customerId) return;

    customersApi.getCustomerDetail(customerId).then((detail) => {
      setSelectedCustomer({
        id: detail.customer.id,
        firstName: detail.customer.firstName,
        lastName: detail.customer.lastName,
        phone: detail.customer.phone,
        email: detail.customer.email,
        isFleet: detail.customer.isFleet,
        vehicleCount: detail.vehicles.length,
        createdAt: detail.customer.createdAt,
      });
      setVehicles(detail.vehicles);
      if (vehicleId) setSelectedVehicleId(vehicleId);
    });
    // Deliberately runs once on mount only — this is an initial deep-link, not a live sync.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!customerSearch.trim() || selectedCustomer) {
      setCustomerResults([]);
      return;
    }
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      customersApi
        .searchCustomers({ search: customerSearch.trim(), pageSize: 8, signal: controller.signal })
        .then((res) => setCustomerResults(res.items))
        .catch(() => {
          if (!controller.signal.aborted) setCustomerResults([]);
        });
    }, 200);
    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [customerSearch, selectedCustomer]);

  function chooseCustomer(customer: CustomerListItemDto) {
    setSelectedCustomer(customer);
    setCustomerResults([]);
    setSelectedVehicleId('');
    setVehicles(null);
    customersApi.listVehiclesForCustomer(customer.id).then(setVehicles);
  }

  function clearCustomer() {
    setSelectedCustomer(null);
    setVehicles(null);
    setSelectedVehicleId('');
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setFormError(null);
    const formValues = {
      ...values,
      customerId: selectedCustomer?.id ?? '',
      vehicleId: selectedVehicleId,
    };
    const errors = validateJobIntakeForm(formValues);
    setFieldErrors(errors);
    if (hasErrors(errors)) return;

    setIsSubmitting(true);
    try {
      const job = await jobsApi.createJob({
        customerId: formValues.customerId,
        vehicleId: formValues.vehicleId,
        primaryMechanicId: null,
        secondaryMechanicId: null,
        mileageAtIntake: values.mileageAtIntake.trim() ? Number(values.mileageAtIntake) : null,
        customerComplaint: values.customerComplaint.trim() || null,
        advisorNotes: values.advisorNotes.trim() || null,
        promisedAt: values.promisedAt ? new Date(values.promisedAt).toISOString() : null,
        customerWaiting: values.customerWaiting,
        source: 'walk_in',
        overnight: values.overnight,
        overnightNote: values.overnight ? values.overnightNote.trim() || null : null,
        isWarrantyReturn: values.isWarrantyReturn,
        parentJobId: null,
      });
      // The server-generated JobNumber (e.g. J-000001) is shown on Job Detail —
      // never invented client-side before this response arrives.
      navigate(`/jobs/${job.id}`);
    } catch (error) {
      setFormError(error instanceof ApiError ? error.title : 'Something went wrong. Please try again.');
      setIsSubmitting(false);
    }
  }

  return (
    <div className="max-w-[640px]">
      <h1 className="font-sans text-[21px] font-semibold text-text-primary">Check in vehicle</h1>
      <p className="mt-1 font-sans text-[12.5px] text-text-muted-1">
        Select the customer and vehicle, then create the job.
      </p>

      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting} className="mt-5 flex flex-col gap-4" data-testid="job-intake-form">
        {formError ? <FormErrorBanner message={formError} /> : null}

        <div className="rounded-panel border border-border-subtle bg-surface-card p-4">
          <Label>Customer</Label>
          {selectedCustomer ? (
            <div className="flex items-center justify-between rounded-control border border-border bg-surface-input px-3 py-2.5" data-testid="selected-customer">
              <span className="font-sans text-[13.5px] text-text-primary">
                {selectedCustomer.firstName} {selectedCustomer.lastName ?? ''} · {selectedCustomer.phone}
              </span>
              <button type="button" onClick={clearCustomer} className="font-sans text-[12px] text-text-muted-2 hover:text-text-primary">
                Change
              </button>
            </div>
          ) : (
            <>
              <Input
                placeholder="Search by name or phone"
                value={customerSearch}
                onChange={(e) => setCustomerSearch(e.target.value)}
                data-testid="job-intake-customer-search"
              />
              {customerResults.length > 0 ? (
                <div className="mt-2 overflow-hidden rounded-control border border-border-subtle">
                  {customerResults.map((customer) => (
                    <button
                      type="button"
                      key={customer.id}
                      onClick={() => chooseCustomer(customer)}
                      className="block w-full border-b border-border-subtle bg-surface-input px-3 py-2 text-left font-sans text-[13px] text-text-primary last:border-b-0 hover:bg-surface-card-item"
                      data-testid={`job-intake-customer-option-${customer.id}`}
                    >
                      {customer.firstName} {customer.lastName ?? ''} · {customer.phone}
                    </button>
                  ))}
                </div>
              ) : null}
              <button
                type="button"
                onClick={() => setIsNewCustomerOpen(true)}
                className="mt-2 font-sans text-[12.5px] font-semibold text-accent-primary"
              >
                + New customer
              </button>
            </>
          )}
          {fieldErrors.customerId ? <FieldError message={fieldErrors.customerId} /> : null}
        </div>

        {selectedCustomer ? (
          <div className="rounded-panel border border-border-subtle bg-surface-card p-4">
            <Label>Vehicle</Label>
            {vehicles === null ? (
              <div className="flex items-center gap-2 text-text-muted-2">
                <Spinner /> <span className="font-sans text-[12.5px]">Loading vehicles…</span>
              </div>
            ) : vehicles.length === 0 ? (
              <p className="font-sans text-[12.5px] text-text-muted-1">No vehicles on file for this customer yet.</p>
            ) : (
              <Select
                value={selectedVehicleId}
                onChange={(e) => setSelectedVehicleId(e.target.value)}
                invalid={Boolean(fieldErrors.vehicleId)}
                data-testid="job-intake-vehicle-select"
              >
                <option value="">Select a vehicle…</option>
                {vehicles.map((vehicle) => (
                  <option key={vehicle.id} value={vehicle.id}>
                    {vehicle.make} {vehicle.model} {vehicle.year ? `(${vehicle.year})` : ''} · {vehicle.plateNumber}
                  </option>
                ))}
              </Select>
            )}
            {fieldErrors.vehicleId ? <FieldError message={fieldErrors.vehicleId} /> : null}
            <button
              type="button"
              onClick={() => setIsNewVehicleOpen(true)}
              className="mt-2 font-sans text-[12.5px] font-semibold text-accent-primary"
            >
              + New vehicle
            </button>
          </div>
        ) : null}

        <div className="rounded-panel border border-border-subtle bg-surface-card p-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label htmlFor={mileageId}>Mileage at intake</Label>
              <Input
                id={mileageId}
                inputMode="numeric"
                value={values.mileageAtIntake}
                invalid={Boolean(fieldErrors.mileageAtIntake)}
                onChange={(e) => setValues((v) => ({ ...v, mileageAtIntake: e.target.value }))}
              />
              {fieldErrors.mileageAtIntake ? <FieldError message={fieldErrors.mileageAtIntake} /> : null}
            </div>
            <div>
              <Label htmlFor={promisedId}>Promised by</Label>
              <Input
                id={promisedId}
                type="datetime-local"
                value={values.promisedAt}
                onChange={(e) => setValues((v) => ({ ...v, promisedAt: e.target.value }))}
              />
            </div>
          </div>

          <div className="mt-3">
            <Label htmlFor={complaintId}>Customer complaint</Label>
            <Textarea
              id={complaintId}
              value={values.customerComplaint}
              onChange={(e) => setValues((v) => ({ ...v, customerComplaint: e.target.value }))}
            />
          </div>

          <div className="mt-3">
            <Label htmlFor={notesId}>Advisor notes</Label>
            <Textarea
              id={notesId}
              value={values.advisorNotes}
              onChange={(e) => setValues((v) => ({ ...v, advisorNotes: e.target.value }))}
            />
          </div>

          <div className="mt-3 flex flex-wrap gap-4">
            <label className="flex items-center gap-2 font-sans text-[13px] text-text-primary">
              <Checkbox
                checked={values.customerWaiting}
                onChange={(e) => setValues((v) => ({ ...v, customerWaiting: e.target.checked }))}
              />
              Customer waiting
            </label>
            <label className="flex items-center gap-2 font-sans text-[13px] text-text-primary">
              <Checkbox
                checked={values.overnight}
                onChange={(e) => setValues((v) => ({ ...v, overnight: e.target.checked }))}
              />
              Overnight
            </label>
            <label className="flex items-center gap-2 font-sans text-[13px] text-text-primary">
              <Checkbox
                checked={values.isWarrantyReturn}
                onChange={(e) => setValues((v) => ({ ...v, isWarrantyReturn: e.target.checked }))}
              />
              Warranty return
            </label>
          </div>

          {values.overnight ? (
            <div className="mt-3">
              <Label htmlFor={overnightNoteId}>Overnight note</Label>
              <Input
                id={overnightNoteId}
                value={values.overnightNote}
                onChange={(e) => setValues((v) => ({ ...v, overnightNote: e.target.value }))}
              />
            </div>
          ) : null}
        </div>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => navigate(-1)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting} data-testid="create-job-submit">
            {isSubmitting ? <Spinner /> : 'Create job'}
          </Button>
        </div>
      </form>

      <CustomerFormModal
        open={isNewCustomerOpen}
        onOpenChange={setIsNewCustomerOpen}
        onSaved={(customer) => {
          setSelectedCustomer({
            id: customer.id,
            firstName: customer.firstName,
            lastName: customer.lastName,
            phone: customer.phone,
            email: customer.email,
            isFleet: customer.isFleet,
            vehicleCount: 0,
            createdAt: customer.createdAt,
          });
          setVehicles([]);
        }}
      />
      {selectedCustomer ? (
        <VehicleFormModal
          open={isNewVehicleOpen}
          onOpenChange={setIsNewVehicleOpen}
          customerId={selectedCustomer.id}
          onSaved={(vehicle) => {
            setVehicles((prev) => [...(prev ?? []), vehicle]);
            setSelectedVehicleId(vehicle.id);
          }}
        />
      ) : null}
    </div>
  );
}
