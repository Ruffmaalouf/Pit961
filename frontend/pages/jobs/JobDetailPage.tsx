import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import * as customersApi from '@/features/customers/api';
import * as jobsApi from '@/features/jobs/api';
import { useCrumb } from '@/hooks/useCrumb';
import { ApiError } from '@/services/apiClient';
import {
  JOB_STATUS_LABELS,
  JOB_STATUS_UX_TRANSITIONS,
  type CustomerDetailResponse,
  type JobDto,
  type JobHistoryEntryDto,
} from '@/types/api';

const CLOSED_SET = new Set(['closed', 'cancelled', 'deleted']);

function statusTone(status: string): 'neutral' | 'accent' | 'success' | 'warning' | 'critical' {
  if (status === 'cancelled' || status === 'deleted') return 'critical';
  if (status === 'awaiting_approval' || status === 'estimate_pending') return 'warning';
  if (status === 'invoiced' || status === 'closed') return 'success';
  return 'accent';
}

export function JobDetailPage() {
  const { id } = useParams<{ id: string }>();

  const [job, setJob] = useState<JobDto | null>(null);
  const [history, setHistory] = useState<JobHistoryEntryDto[]>([]);
  const [customer, setCustomer] = useState<CustomerDetailResponse['customer'] | null>(null);
  const [vehicle, setVehicle] = useState<{
    make: string;
    model: string;
    year: number | null;
    plateNumber: string;
  } | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [conflictNotice, setConflictNotice] = useState<string | null>(null);
  const [pendingTransition, setPendingTransition] = useState<string | null>(null);

  useCrumb(job ? `${job.jobNumber} · ${vehicle ? `${vehicle.make} ${vehicle.model}`.toUpperCase() : ''}` : 'JOB');

  const load = useCallback(() => {
    if (!id) return;
    setError(null);
    setNotFound(false);
    jobsApi
      .getJob(id)
      .then((loadedJob) => {
        setJob(loadedJob);
        return Promise.all([
          jobsApi.getJobHistory(id),
          customersApi.getCustomerDetail(loadedJob.customerId),
        ]).then(([historyEntries, customerDetail]) => {
          setHistory(historyEntries);
          setCustomer(customerDetail.customer);
          const matchedVehicle = customerDetail.vehicles.find((v) => v.id === loadedJob.vehicleId);
          setVehicle(
            matchedVehicle
              ? {
                  make: matchedVehicle.make,
                  model: matchedVehicle.model,
                  year: matchedVehicle.year,
                  plateNumber: matchedVehicle.plateNumber,
                }
              : null,
          );
        });
      })
      .catch((err) => {
        if (err instanceof ApiError && err.status === 404) {
          setNotFound(true);
          return;
        }
        setError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
      });
  }, [id]);

  useEffect(() => {
    setJob(null);
    load();
  }, [load]);

  async function handleTransition(targetStatus: string) {
    if (!id || pendingTransition) return;
    setConflictNotice(null);
    setError(null);
    setPendingTransition(targetStatus);
    try {
      const updated = await jobsApi.transitionJobStatus(id, { targetStatus });
      setJob(updated);
      jobsApi.getJobHistory(id).then(setHistory);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        // Real, expected outcome — another actor changed this job first.
        // Never silently overwrite: reload the actual current server state
        // instead of assuming our attempted transition applied.
        setConflictNotice('This job was updated by someone else. Refreshing…');
        load();
      } else {
        setError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
      }
    } finally {
      setPendingTransition(null);
    }
  }

  if (notFound) {
    return (
      <div className="p-10 text-center" data-testid="job-not-found">
        <p className="font-sans text-[14px] text-text-muted-1">This job could not be found.</p>
        <Link to="/floor" className="mt-3 inline-block font-sans text-[13px] text-accent-primary">
          Back to floor
        </Link>
      </div>
    );
  }

  if (error && !job) {
    return (
      <div className="p-10 text-center" data-testid="job-detail-error">
        <p className="font-sans text-[13.5px] text-status-critical">{error}</p>
        <Button variant="outline" size="sm" className="mt-3" onClick={load}>
          Try again
        </Button>
      </div>
    );
  }

  if (!job) {
    return (
      <div className="flex items-center justify-center gap-2 p-10 text-text-muted-2" data-testid="job-detail-loading">
        <Spinner />
        <span className="font-sans text-[13px]">Loading…</span>
      </div>
    );
  }

  const allowedTransitions = JOB_STATUS_UX_TRANSITIONS[job.status] ?? [];
  const isTerminal = CLOSED_SET.has(job.status);

  return (
    <div className="grid grid-cols-[220px_1fr_280px] items-start gap-3.5" data-testid="job-detail">
      <div className="sticky top-[70px] rounded-panel border border-border-subtle bg-surface-card p-4">
        <div className="font-mono text-[9.5px] tracking-wide text-text-muted-3">STATUS</div>
        <Badge tone={statusTone(job.status)} className="mt-2">
          {JOB_STATUS_LABELS[job.status] ?? job.status}
        </Badge>

        {conflictNotice ? (
          <div role="status" data-testid="conflict-notice" className="mt-3 rounded-control border border-status-warning bg-[var(--status-warning-soft)] px-2.5 py-2 font-sans text-[11.5px] text-status-warning">
            {conflictNotice}
          </div>
        ) : null}

        {isTerminal ? (
          <p className="mt-3 font-sans text-[12px] text-text-muted-2">No further actions — this job is closed.</p>
        ) : (
          <div className="mt-3 flex flex-col gap-1.5">
            {allowedTransitions.map((target) => (
              <button
                key={target}
                type="button"
                disabled={Boolean(pendingTransition)}
                onClick={() => handleTransition(target)}
                data-testid={`transition-${target}`}
                className="flex h-9 items-center justify-center rounded-control border border-border bg-surface-input font-sans text-[12.5px] font-semibold text-text-primary hover:border-accent-primary disabled:opacity-60"
              >
                {pendingTransition === target ? <Spinner /> : `Move to ${JOB_STATUS_LABELS[target] ?? target}`}
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="flex min-w-0 flex-col gap-3.5">
        {error ? (
          <div className="rounded-control border border-status-critical bg-[var(--status-critical-soft)] px-[14px] py-[10px] font-sans text-[12.5px] font-medium text-status-critical">
            {error}
          </div>
        ) : null}

        <div className="rounded-panel border border-border-subtle bg-surface-card p-[17px]">
          <div className="flex flex-wrap items-center gap-2.5">
            <h1 className="font-sans text-[20px] font-semibold text-text-primary">
              {vehicle ? `${vehicle.make} ${vehicle.model}` : 'Vehicle'}
            </h1>
            <span className="rounded-control border border-border-subtle bg-surface-card-item px-[7px] py-[3px] font-mono text-[11px] text-text-muted-1">
              {job.jobNumber}
            </span>
          </div>
          <div className="mt-2 flex flex-wrap gap-4 font-sans text-[12.5px] text-text-muted-1">
            {vehicle ? (
              <span>
                {vehicle.year ? `${vehicle.year} · ` : ''}
                <span className="font-mono text-text-primary">{vehicle.plateNumber}</span>
              </span>
            ) : null}
            {job.mileageAtIntake != null ? <span>{job.mileageAtIntake.toLocaleString()} km</span> : null}
            {customer ? (
              <Link to={`/customers/${customer.id}`} className="text-accent-primary">
                {customer.firstName} {customer.lastName ?? ''} · {customer.phone}
              </Link>
            ) : null}
            {job.promisedAt ? <span>Promised {new Date(job.promisedAt).toLocaleString()}</span> : null}
            {job.customerWaiting ? <Badge tone="warning">Waiting</Badge> : null}
            {job.overnight ? <Badge tone="neutral">Overnight</Badge> : null}
            {job.isWarrantyReturn ? <Badge tone="accent">Warranty</Badge> : null}
          </div>
        </div>

        {job.customerComplaint || job.advisorNotes ? (
          <div className="rounded-panel border border-border-subtle bg-surface-card p-[17px]">
            <div className="grid grid-cols-2 gap-4">
              {job.customerComplaint ? (
                <div>
                  <div className="font-mono text-[9.5px] tracking-wide text-text-muted-3">COMPLAINT</div>
                  <div className="mt-1.5 border-l-2 border-accent-primary pl-2.5 font-sans text-[14px] leading-relaxed text-text-primary">
                    {job.customerComplaint}
                  </div>
                </div>
              ) : null}
              {job.advisorNotes ? (
                <div>
                  <div className="font-mono text-[9.5px] tracking-wide text-text-muted-3">ADVISOR NOTES</div>
                  <div className="mt-1.5 border-l-2 border-[#58a6c8] pl-2.5 font-sans text-[14px] leading-relaxed text-text-primary">
                    {job.advisorNotes}
                  </div>
                </div>
              ) : null}
            </div>
          </div>
        ) : null}
      </div>

      <div className="sticky top-[70px] rounded-panel border border-border-subtle bg-surface-card p-4">
        <div className="font-sans text-[13.5px] font-semibold text-text-primary">Live feed</div>
        <div className="mt-3 flex flex-col gap-3" data-testid="job-history-feed">
          {history.length === 0 ? (
            <p className="font-sans text-[12px] text-text-muted-2">No history yet.</p>
          ) : (
            history.map((entry) => (
              <div key={entry.id} className="flex gap-2.5">
                <div className="w-[62px] flex-none font-mono text-[10px] text-text-muted-3">
                  {new Date(entry.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </div>
                <div className="flex-1 font-sans text-[12.5px] leading-snug text-text-primary">
                  {entry.summary}
                  <div className="font-mono text-[10px] text-text-muted-3">{entry.actorName}</div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
