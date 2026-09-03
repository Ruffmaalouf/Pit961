import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import * as jobsApi from '@/features/jobs/api';
import { useCrumb } from '@/hooks/useCrumb';
import { ApiError } from '@/services/apiClient';
import { JOB_STATUS_LABELS, type FloorBoardColumnDto } from '@/types/api';

/**
 * Real P2-WP3 Floor Board — GET /api/v1/jobs/floor-board, real data only.
 *
 * Design Lead ruling (DESIGN_IMPLEMENTATION_DIFFERENCES.md #15): prototype.html's
 * "Floor control" screen is a bay/lift ops-dashboard with zero backing data in
 * the real backend (no bay/lift model exists in P2-WP2/P2-WP3). This renders
 * instead as a status kanban — one column per backend status the floor-board
 * endpoint actually returns, in the order the API returns them — which is a
 * compatible extension of the approved dark/amber visual system, not a
 * redesign, and never fabricates bay/lift positions that don't exist.
 *
 * `useCrumb('FLOOR CONTROL')` preserves the WP-8 e2e assertion that the header
 * crumb reads "FLOOR CONTROL" on this screen.
 */
export function FloorPage() {
  useCrumb('FLOOR CONTROL');

  const [columns, setColumns] = useState<FloorBoardColumnDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    setError(null);
    jobsApi
      .getFloorBoard()
      .then((response) => setColumns(response.columns))
      .catch((err) => {
        setError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
        setColumns(null);
      });
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const totalJobs = columns?.reduce((sum, column) => sum + column.cards.length, 0) ?? 0;

  return (
    <div data-testid="floor-page" className="flex flex-col gap-4">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-sans text-[21px] font-semibold tracking-tight text-text-primary">Floor</h1>
          <div className="mt-1 font-mono text-[11.5px] tracking-wide text-text-muted-3">
            {columns ? `${totalJobs} OPEN JOB${totalJobs === 1 ? '' : 'S'} ON THE FLOOR` : 'LOADING…'}
          </div>
        </div>
        <Button asChild data-testid="check-in-vehicle-button">
          <Link to="/jobs/new">Check in vehicle</Link>
        </Button>
      </div>

      {columns === null && !error ? (
        <div className="flex items-center justify-center gap-2 p-10 text-text-muted-2" data-testid="floor-loading">
          <Spinner />
          <span className="font-sans text-[13px]">Loading floor…</span>
        </div>
      ) : error ? (
        <div className="p-10 text-center" data-testid="floor-error">
          <p className="font-sans text-[13.5px] text-status-critical">{error}</p>
          <Button variant="outline" size="sm" className="mt-3" onClick={load}>
            Try again
          </Button>
        </div>
      ) : totalJobs === 0 ? (
        <div className="rounded-panel border border-border-subtle bg-surface-card p-10 text-center" data-testid="floor-empty">
          <p className="font-sans text-[13.5px] text-text-muted-1">No open jobs on the floor yet.</p>
          <Button size="sm" asChild className="mt-3">
            <Link to="/jobs/new">Check in the first vehicle</Link>
          </Button>
        </div>
      ) : (
        <div className="flex gap-3.5 overflow-x-auto pb-2" data-testid="floor-board">
          {columns!.map((column) => (
            <div
              key={column.status}
              data-testid={`floor-column-${column.status}`}
              className="flex w-[280px] flex-none flex-col rounded-panel border border-border-subtle bg-surface-card"
            >
              <div className="border-b border-border-subtle px-3.5 py-2.5">
                <div className="font-sans text-[13px] font-semibold text-text-primary">
                  {JOB_STATUS_LABELS[column.status] ?? column.status}
                </div>
                <div className="mt-0.5 font-mono text-[10px] tracking-wide text-text-muted-3">
                  {column.status} · {column.cards.length}
                </div>
              </div>

              <div className="flex flex-1 flex-col gap-2 p-2.5">
                {column.cards.length === 0 ? (
                  <div className="p-3 text-center font-sans text-[11.5px] text-text-muted-3">Empty</div>
                ) : (
                  column.cards.map((card) => (
                    <Link
                      key={card.jobId}
                      to={`/jobs/${card.jobId}`}
                      data-testid={`floor-card-${card.jobId}`}
                      className="rounded-control border border-border-subtle bg-surface-card-item p-2.5 hover:border-accent-primary"
                    >
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-mono text-[11px] font-semibold text-text-primary">
                          {card.jobNumber}
                        </span>
                        {card.customerWaiting ? <Badge tone="warning">Waiting</Badge> : null}
                      </div>
                      <div className="mt-1 font-sans text-[12px] text-text-primary">{card.vehicleDisplay}</div>
                      <div className="mt-0.5 font-sans text-[11px] text-text-muted-2">
                        {card.customerDisplayName}
                      </div>
                      {card.primaryMechanicName ? (
                        <div className="mt-1.5 font-mono text-[10px] text-text-muted-3">
                          {card.primaryMechanicName}
                        </div>
                      ) : null}
                    </Link>
                  ))
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
