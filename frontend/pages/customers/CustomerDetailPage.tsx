import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { CustomerFormModal } from '@/features/customers/CustomerFormModal';
import * as customersApi from '@/features/customers/api';
import { VehicleFormModal } from '@/features/vehicles/VehicleFormModal';
import { useCrumb } from '@/hooks/useCrumb';
import { ApiError } from '@/services/apiClient';
import { useAuthStore } from '@/stores/authStore';
import { JOB_STATUS_LABELS, type CustomerDetailResponse, type VehicleDto } from '@/types/api';

/** Owner/manager only — mirrors CustomerManagementService.SoftDeleteAllowedRoles server-side. */
const SOFT_DELETE_ROLES = new Set(['owner', 'manager']);

export function CustomerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const role = useAuthStore((state) => state.user?.role);

  const [detail, setDetail] = useState<CustomerDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isAddVehicleOpen, setIsAddVehicleOpen] = useState(false);
  const [editingVehicle, setEditingVehicle] = useState<VehicleDto | undefined>(undefined);
  const [isDeleting, setIsDeleting] = useState(false);

  useCrumb(detail ? `${detail.customer.firstName} ${detail.customer.lastName ?? ''}`.trim().toUpperCase() : 'CUSTOMER');

  const load = useCallback(() => {
    if (!id) return;
    setError(null);
    setNotFound(false);
    customersApi
      .getCustomerDetail(id)
      .then(setDetail)
      .catch((err) => {
        if (err instanceof ApiError && err.status === 404) {
          setNotFound(true);
          return;
        }
        setError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
      });
  }, [id]);

  useEffect(() => {
    setDetail(null);
    load();
  }, [load]);

  async function handleSoftDelete() {
    if (!id) return;
    setIsDeleting(true);
    try {
      await customersApi.softDeleteCustomer(id);
      navigate('/customers');
    } catch (err) {
      setError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
      setIsDeleting(false);
    }
  }

  if (notFound) {
    return (
      <div className="p-10 text-center" data-testid="customer-not-found">
        <p className="font-sans text-[14px] text-text-muted-1">This customer could not be found.</p>
        <Link to="/customers" className="mt-3 inline-block font-sans text-[13px] text-accent-primary">
          Back to customers
        </Link>
      </div>
    );
  }

  if (error && !detail) {
    return (
      <div className="p-10 text-center" data-testid="customer-detail-error">
        <p className="font-sans text-[13.5px] text-status-critical">{error}</p>
        <Button variant="outline" size="sm" className="mt-3" onClick={load}>
          Try again
        </Button>
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="flex items-center justify-center gap-2 p-10 text-text-muted-2" data-testid="customer-detail-loading">
        <Spinner />
        <span className="font-sans text-[13px]">Loading…</span>
      </div>
    );
  }

  const { customer, vehicles, jobsHistory, balanceSummary } = detail;
  const canDelete = role ? SOFT_DELETE_ROLES.has(role) : false;

  return (
    <div className="flex flex-col gap-4">
      {error ? (
        <div className="rounded-control border border-status-critical bg-[var(--status-critical-soft)] px-[14px] py-[10px] font-sans text-[12.5px] font-medium text-status-critical">
          {error}
        </div>
      ) : null}

      <div className="rounded-panel border border-border-subtle bg-surface-card p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="flex h-[52px] w-[52px] flex-none items-center justify-center rounded-full border border-border-subtle bg-surface-card-item font-sans text-[16px] font-semibold text-text-primary">
              {(customer.firstName[0] ?? '') + (customer.lastName?.[0] ?? '')}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="font-sans text-[19px] font-semibold text-text-primary">
                  {customer.firstName} {customer.lastName ?? ''}
                </h1>
                {customer.isFleet ? <Badge tone="accent">Fleet</Badge> : null}
              </div>
              <div className="mt-1 font-sans text-[12.5px] text-text-muted-1">
                {customer.phone}
                {customer.email ? ` · ${customer.email}` : ''}
                {customer.whatsapp ? ` · WhatsApp ${customer.whatsapp}` : ''}
              </div>
              {customer.notes ? (
                <div className="mt-2 max-w-[52ch] font-sans text-[12.5px] text-text-muted-2">{customer.notes}</div>
              ) : null}
            </div>
          </div>
          <div className="flex flex-none gap-2">
            <Button variant="outline" size="sm" onClick={() => setIsEditOpen(true)} data-testid="edit-customer-button">
              Edit
            </Button>
            {canDelete ? (
              <Button
                variant="outline"
                size="sm"
                disabled={isDeleting}
                onClick={handleSoftDelete}
                data-testid="delete-customer-button"
              >
                {isDeleting ? <Spinner /> : 'Delete'}
              </Button>
            ) : null}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-1 overflow-hidden rounded-panel border border-border-subtle bg-border-subtle">
        <div className="bg-surface-card p-3.5">
          <div className="font-mono text-[9.5px] tracking-wide text-text-muted-3">TOTAL INVOICED</div>
          <div className="mt-1.5 font-mono text-[16px] font-semibold text-text-primary">
            {balanceSummary.currency} {balanceSummary.totalInvoiced.toFixed(2)}
          </div>
        </div>
        <div className="bg-surface-card p-3.5">
          <div className="font-mono text-[9.5px] tracking-wide text-text-muted-3">TOTAL PAID</div>
          <div className="mt-1.5 font-mono text-[16px] font-semibold text-status-success">
            {balanceSummary.currency} {balanceSummary.totalPaid.toFixed(2)}
          </div>
        </div>
        <div className="bg-surface-card p-3.5">
          <div className="font-mono text-[9.5px] tracking-wide text-text-muted-3">OUTSTANDING</div>
          <div className="mt-1.5 font-mono text-[16px] font-semibold text-status-warning">
            {balanceSummary.currency} {balanceSummary.outstandingBalance.toFixed(2)}
          </div>
        </div>
      </div>

      <div className="rounded-panel border border-border-subtle bg-surface-card">
        <div className="flex items-center justify-between border-b border-border-subtle px-[17px] py-[13px]">
          <div className="font-sans text-[14px] font-semibold text-text-primary">Vehicles</div>
          <Button
            size="sm"
            variant="outline"
            onClick={() => {
              setEditingVehicle(undefined);
              setIsAddVehicleOpen(true);
            }}
            data-testid="add-vehicle-button"
          >
            Add vehicle
          </Button>
        </div>
        {vehicles.length === 0 ? (
          <div className="p-8 text-center font-sans text-[13px] text-text-muted-1" data-testid="vehicles-empty">
            No vehicles on file yet.
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-3 p-4">
            {vehicles.map((vehicle) => (
              <div
                key={vehicle.id}
                data-testid={`vehicle-card-${vehicle.id}`}
                className="rounded-control border border-border-subtle bg-surface-card-item p-3.5"
              >
                <div className="flex items-center justify-between">
                  <div className="font-sans text-[13.5px] font-semibold text-text-primary">
                    {vehicle.make} {vehicle.model} {vehicle.year ? `· ${vehicle.year}` : ''}
                  </div>
                  <Link
                    to={`/jobs/new?customerId=${customer.id}&vehicleId=${vehicle.id}`}
                    className="font-sans text-[12px] font-semibold text-accent-primary"
                    data-testid={`new-job-for-vehicle-${vehicle.id}`}
                  >
                    New job
                  </Link>
                </div>
                <div className="mt-1.5 font-mono text-[11px] text-text-muted-2">
                  {vehicle.plateNumber} · {vehicle.plateCountry}
                  {vehicle.currentMileage != null ? ` · ${vehicle.currentMileage.toLocaleString()} km` : ''}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="rounded-panel border border-border-subtle bg-surface-card">
        <div className="border-b border-border-subtle px-[17px] py-[13px]">
          <div className="font-sans text-[14px] font-semibold text-text-primary">Job history</div>
        </div>
        {jobsHistory.recentJobs.length === 0 ? (
          <div className="p-8 text-center font-sans text-[13px] text-text-muted-1" data-testid="jobs-history-empty">
            No jobs yet for this customer.
          </div>
        ) : (
          jobsHistory.recentJobs.map((job) => (
            <Link
              key={job.jobId}
              to={`/jobs/${job.jobId}`}
              className="flex items-center gap-3.5 border-b border-border-subtle px-[17px] py-[12px] last:border-b-0 hover:bg-surface-card-item"
            >
              <div className="w-[110px] flex-none font-mono text-[11px] text-text-muted-2">{job.jobNumber}</div>
              <div className="flex-1 font-sans text-[12.5px] text-text-primary">{job.vehiclePlate ?? '—'}</div>
              <div className="flex-none font-mono text-[10.5px] uppercase tracking-wide text-text-muted-2">
                {JOB_STATUS_LABELS[job.status] ?? job.status}
              </div>
            </Link>
          ))
        )}
      </div>

      <CustomerFormModal
        open={isEditOpen}
        onOpenChange={setIsEditOpen}
        customer={customer}
        onSaved={() => load()}
      />
      <VehicleFormModal
        open={isAddVehicleOpen}
        onOpenChange={setIsAddVehicleOpen}
        customerId={customer.id}
        vehicle={editingVehicle}
        onSaved={() => load()}
      />
    </div>
  );
}
