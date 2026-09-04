import { useCallback, useEffect, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select } from '@/components/ui/select';
import { Spinner } from '@/components/ui/spinner';
import * as estimatesApi from '@/features/estimates/api';
import { useAuthStore } from '@/stores/authStore';
import { ApiError } from '@/services/apiClient';
import {
  ESTIMATE_APPROVAL_METHODS,
  ESTIMATE_STATUS_LABELS,
  type EstimateDto,
  type EstimateItemRequest,
} from '@/types/api';

/** Manager discount cap (DECISIONS.md / DiscountLimitHandler). UX-only — a
 * lightweight local guard so a Manager sees the ceiling before submitting;
 * the backend's DiscountLimitHandler remains the sole authority and is
 * re-checked on every request regardless of what this constant says. */
const MANAGER_DISCOUNT_CAP_PERCENT = 15;

/** $500 owner-approval subtotal threshold (EstimateApprovalThresholdRequirement).
 * Display-only, purely to explain the "why" in copy — never used to decide
 * what the UI allows; the backend computes RequiresOwnerApproval itself. */
const OWNER_APPROVAL_THRESHOLD = 500;

function money(n: number): string {
  return `$${n.toFixed(2)}`;
}

function emptyItem(sortOrder: number): EstimateItemRequest {
  return { type: 'labor', description: '', partNumber: null, quantity: 1, unitCost: 0, unitPrice: 0, sortOrder };
}

function toItemRequests(items: EstimateItemRequest[]): EstimateItemRequest[] {
  return items.map((item, index) => ({ ...item, sortOrder: index }));
}

function estimateTone(status: string): 'neutral' | 'accent' | 'success' | 'warning' | 'critical' {
  if (status === 'pending_owner_approval') return 'warning';
  if (status === 'approved') return 'success';
  if (status === 'rejected') return 'critical';
  if (status === 'superseded') return 'neutral';
  return 'accent';
}

/**
 * P2-WP4. Job Detail's Estimate/Money workflow: create → edit line items →
 * see server-computed subtotal/total → apply a discount → submit for
 * approval → (above $500) see a distinct PENDING OWNER APPROVAL state →
 * Owner clears it → record the customer's separate decision → re-quote as a
 * new revision when needed. No endpoint here ever sends a client-computed
 * Total/DiscountAmount, or an approval-owned Status value — see
 * features/estimates/api.ts's doc comment. Design per
 * DESIGN_IMPLEMENTATION_DIFFERENCES.md §11 (P2-WP9 audit): discount is a
 * single inline control near the total (no modal); the Owner-approval gate
 * is a visually distinct amber banner + a separate action, never the same
 * control as the discount step.
 */
export function EstimateSection({ jobId }: { jobId: string }) {
  const role = useAuthStore((s) => s.user?.role);
  const isOwner = role === 'owner';

  const [revisions, setRevisions] = useState<EstimateDto[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState<
    'create' | 'items' | 'discount' | 'submit' | 'clear' | 'customer' | 'revision' | null
  >(null);

  const [draftItems, setDraftItems] = useState<EstimateItemRequest[] | null>(null);
  const [discountPercent, setDiscountPercent] = useState('');
  const [customerDecision, setCustomerDecision] = useState('approved');
  const [customerMethod, setCustomerMethod] = useState(ESTIMATE_APPROVAL_METHODS[0].value);
  const [customerName, setCustomerName] = useState('');

  const load = useCallback(() => {
    setLoadError(null);
    estimatesApi
      .listEstimatesByJob(jobId)
      .then(setRevisions)
      .catch((err) => {
        setLoadError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
      });
  }, [jobId]);

  useEffect(() => {
    setRevisions(null);
    load();
  }, [load]);

  const active = revisions?.find((r) => r.status !== 'superseded') ?? null;
  const superseded = (revisions ?? []).filter((r) => r.status === 'superseded');

  async function handleStartCreate() {
    setDraftItems([emptyItem(0)]);
    setActionError(null);
  }

  async function handleCreate() {
    if (!draftItems || busy) return;
    setActionError(null);
    setBusy('create');
    try {
      const created = await estimatesApi.createEstimate({
        jobId,
        type: 'standard',
        notes: null,
        items: toItemRequests(draftItems),
      });
      setDraftItems(null);
      setRevisions((prev) => [...(prev ?? []), created]);
    } catch (err) {
      setActionError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
    } finally {
      setBusy(null);
    }
  }

  async function handleSaveItems(estimateId: string) {
    if (!draftItems || busy) return;
    setActionError(null);
    setBusy('items');
    try {
      const updated = await estimatesApi.replaceEstimateItems(estimateId, { items: toItemRequests(draftItems) });
      setDraftItems(null);
      setRevisions((prev) => (prev ?? []).map((r) => (r.id === updated.id ? updated : r)));
    } catch (err) {
      setActionError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
    } finally {
      setBusy(null);
    }
  }

  async function handleApplyDiscount(estimateId: string) {
    const percent = Number(discountPercent);
    if (busy || !discountPercent || Number.isNaN(percent)) return;
    setActionError(null);
    setBusy('discount');
    try {
      const updated = await estimatesApi.applyDiscount(estimateId, { discountPercent: percent });
      setDiscountPercent('');
      setRevisions((prev) => (prev ?? []).map((r) => (r.id === updated.id ? updated : r)));
    } catch (err) {
      setActionError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
    } finally {
      setBusy(null);
    }
  }

  async function handleSubmit(estimateId: string) {
    if (busy) return;
    setActionError(null);
    setBusy('submit');
    try {
      const updated = await estimatesApi.submitEstimate(estimateId);
      setRevisions((prev) => (prev ?? []).map((r) => (r.id === updated.id ? updated : r)));
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setActionError('This estimate was updated by someone else. Refreshing…');
        load();
      } else {
        setActionError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
      }
    } finally {
      setBusy(null);
    }
  }

  async function handleClearOwnerApproval(estimateId: string) {
    if (busy) return;
    setActionError(null);
    setBusy('clear');
    try {
      const updated = await estimatesApi.clearOwnerApproval(estimateId);
      setRevisions((prev) => (prev ?? []).map((r) => (r.id === updated.id ? updated : r)));
    } catch (err) {
      setActionError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
    } finally {
      setBusy(null);
    }
  }

  async function handleRecordCustomerApproval(estimateId: string) {
    if (busy) return;
    setActionError(null);
    setBusy('customer');
    try {
      const updated = await estimatesApi.recordCustomerApproval(estimateId, {
        decision: customerDecision,
        approvalMethod: customerMethod,
        approvedByName: customerName.trim() || null,
      });
      setCustomerName('');
      setRevisions((prev) => (prev ?? []).map((r) => (r.id === updated.id ? updated : r)));
    } catch (err) {
      setActionError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
    } finally {
      setBusy(null);
    }
  }

  async function handleCreateRevision(estimateId: string) {
    if (busy) return;
    setActionError(null);
    setBusy('revision');
    try {
      const revision = await estimatesApi.createEstimateRevision(estimateId);
      setRevisions((prev) => (prev ?? []).map((r) => (r.id === estimateId ? { ...r, status: 'superseded' } : r)));
      setRevisions((prev) => [...(prev ?? []), revision]);
    } catch (err) {
      setActionError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
    } finally {
      setBusy(null);
    }
  }

  function updateDraftItem(index: number, patch: Partial<EstimateItemRequest>) {
    setDraftItems((prev) => (prev ? prev.map((it, i) => (i === index ? { ...it, ...patch } : it)) : prev));
  }

  function addDraftRow() {
    setDraftItems((prev) => (prev ? [...prev, emptyItem(prev.length)] : prev));
  }

  function removeDraftRow(index: number) {
    setDraftItems((prev) => (prev ? prev.filter((_, i) => i !== index) : prev));
  }

  const managerOverCap =
    role === 'manager' && discountPercent !== '' && Number(discountPercent) > MANAGER_DISCOUNT_CAP_PERCENT;

  if (loadError) {
    return (
      <div
        className="rounded-panel border border-status-critical bg-[var(--status-critical-soft)] p-[17px]"
        data-testid="estimate-section-error"
      >
        <p className="font-sans text-[12.5px] text-status-critical">{loadError}</p>
        <Button variant="outline" size="sm" className="mt-2.5" onClick={load}>
          Try again
        </Button>
      </div>
    );
  }

  if (revisions === null) {
    return (
      <div
        className="flex items-center justify-center gap-2 rounded-panel border border-border-subtle bg-surface-card p-6 text-text-muted-2"
        data-testid="estimate-section-loading"
      >
        <Spinner />
        <span className="font-sans text-[13px]">Loading estimate…</span>
      </div>
    );
  }

  return (
    <div className="rounded-panel border border-border-subtle bg-surface-card p-[17px]" data-testid="estimate-section">
      <div className="flex items-center justify-between">
        <h2 className="font-sans text-[15.5px] font-semibold text-text-primary">Estimate</h2>
        {active ? (
          <Badge tone={estimateTone(active.status)}>{ESTIMATE_STATUS_LABELS[active.status] ?? active.status}</Badge>
        ) : null}
      </div>

      {actionError ? (
        <div
          role="alert"
          data-testid="estimate-action-error"
          className="mt-2.5 rounded-control border border-status-critical bg-[var(--status-critical-soft)] px-[14px] py-[10px] font-sans text-[12.5px] font-medium text-status-critical"
        >
          {actionError}
        </div>
      ) : null}

      {!active && !draftItems ? (
        <div className="mt-3 flex flex-col items-start gap-2" data-testid="estimate-empty-state">
          <p className="font-sans text-[12.5px] text-text-muted-2">No estimate has been started for this job yet.</p>
          <Button size="sm" onClick={handleStartCreate} data-testid="start-create-estimate">
            Create Estimate
          </Button>
        </div>
      ) : null}

      {draftItems && !active ? (
        <div className="mt-3">
          <ItemsEditor items={draftItems} onChange={updateDraftItem} onAdd={addDraftRow} onRemove={removeDraftRow} />
          <div className="mt-3 flex gap-2">
            <Button size="sm" onClick={handleCreate} disabled={busy === 'create'} data-testid="submit-create-estimate">
              {busy === 'create' ? <Spinner /> : 'Save Estimate'}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setDraftItems(null)}>
              Cancel
            </Button>
          </div>
        </div>
      ) : null}

      {active ? (
        <div className="mt-3 flex flex-col gap-3.5" data-testid={`estimate-${active.id}`}>
          {active.status === 'pending_owner_approval' ? (
            <div
              role="status"
              data-testid="pending-owner-approval-banner"
              className="rounded-control border border-status-warning bg-[var(--status-warning-soft)] px-3 py-2.5"
            >
              <div className="font-mono text-[10px] font-semibold tracking-wide text-status-warning">
                PENDING OWNER APPROVAL
              </div>
              <p className="mt-1 font-sans text-[12px] text-text-primary">
                This estimate&apos;s subtotal is above ${OWNER_APPROVAL_THRESHOLD.toFixed(2)} and needs the Owner to
                clear it before it can be sent.
              </p>
              {isOwner ? (
                <Button
                  size="sm"
                  className="mt-2"
                  onClick={() => handleClearOwnerApproval(active.id)}
                  disabled={busy === 'clear'}
                  data-testid="clear-owner-approval"
                >
                  {busy === 'clear' ? <Spinner /> : 'Clear Owner Approval'}
                </Button>
              ) : (
                <p className="mt-1.5 font-sans text-[11.5px] text-text-muted-2" data-testid="owner-only-notice">
                  Only the Owner can clear this.
                </p>
              )}
            </div>
          ) : null}

          {draftItems ? (
            <div>
              <ItemsEditor
                items={draftItems}
                onChange={updateDraftItem}
                onAdd={addDraftRow}
                onRemove={removeDraftRow}
              />
              <div className="mt-3 flex gap-2">
                <Button
                  size="sm"
                  onClick={() => handleSaveItems(active.id)}
                  disabled={busy === 'items'}
                  data-testid="save-items"
                >
                  {busy === 'items' ? <Spinner /> : 'Save Items'}
                </Button>
                <Button size="sm" variant="outline" onClick={() => setDraftItems(null)}>
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <div>
              <ItemsTable items={active.items} />
              {active.status !== 'superseded' ? (
                <Button
                  size="sm"
                  variant="outline"
                  className="mt-2"
                  onClick={() => setDraftItems(active.items.map((i) => ({ ...i })))}
                  data-testid="edit-items"
                >
                  Edit Items
                </Button>
              ) : null}
            </div>
          )}

          <div className="rounded-control border border-border-subtle bg-surface-card-item p-3" data-testid="estimate-totals">
            <div className="flex justify-between font-sans text-[12.5px] text-text-muted-1">
              <span>Subtotal</span>
              <span data-testid="estimate-subtotal">{money(active.subtotal)}</span>
            </div>
            <div className="flex justify-between font-sans text-[12.5px] text-text-muted-1">
              <span>Discount</span>
              <span data-testid="estimate-discount">-{money(active.discountAmount)}</span>
            </div>
            <div className="flex justify-between font-sans text-[12.5px] text-text-muted-1">
              <span>Tax</span>
              <span>{money(active.taxAmount)}</span>
            </div>
            <div className="mt-1.5 flex justify-between border-t border-border-subtle pt-1.5 font-sans text-[14px] font-semibold text-text-primary">
              <span>Total</span>
              <span data-testid="estimate-total">{money(active.total)}</span>
            </div>

            {active.status !== 'superseded' ? (
              <div className="mt-3 flex items-end gap-2">
                <div className="flex-1">
                  <label htmlFor="discount-percent" className="font-mono text-[9.5px] tracking-wide text-text-muted-3">
                    DISCOUNT %
                  </label>
                  <Input
                    id="discount-percent"
                    type="number"
                    min={0}
                    step="0.01"
                    value={discountPercent}
                    onChange={(e) => setDiscountPercent(e.target.value)}
                    className="mt-1 h-8"
                    data-testid="discount-percent-input"
                  />
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={busy === 'discount' || managerOverCap}
                  onClick={() => handleApplyDiscount(active.id)}
                  data-testid="apply-discount"
                >
                  {busy === 'discount' ? <Spinner /> : 'Apply'}
                </Button>
              </div>
            ) : null}
            {managerOverCap ? (
              <p className="mt-1.5 font-sans text-[11px] text-status-warning" data-testid="manager-discount-cap-warning">
                Managers can apply up to {MANAGER_DISCOUNT_CAP_PERCENT}% — an Owner is needed for more.
              </p>
            ) : null}
          </div>

          {active.status === 'draft' ? (
            <Button
              size="sm"
              onClick={() => handleSubmit(active.id)}
              disabled={busy === 'submit'}
              data-testid="submit-for-approval"
            >
              {busy === 'submit' ? <Spinner /> : 'Submit for Approval'}
            </Button>
          ) : null}

          {active.status !== 'superseded' ? (
            <div className="rounded-control border border-border-subtle p-3" data-testid="customer-approval-section">
              <div className="font-sans text-[12.5px] font-semibold text-text-primary">Customer approval</div>
              <p className="mt-0.5 font-sans text-[11px] text-text-muted-2">
                Separate from Owner approval — records what the customer decided.
              </p>

              {active.approvedAt ? (
                <p className="mt-2 font-sans text-[12px] text-text-primary" data-testid="customer-approval-status">
                  Recorded: {ESTIMATE_STATUS_LABELS[active.status] ?? active.status} via {active.approvalMethod}
                  {active.approvedByName ? ` · ${active.approvedByName}` : ''}
                </p>
              ) : null}

              <div className="mt-2.5 flex flex-wrap items-end gap-2">
                <div>
                  <label className="font-mono text-[9.5px] tracking-wide text-text-muted-3">DECISION</label>
                  <Select
                    className="mt-1 h-8 w-[150px]"
                    value={customerDecision}
                    onChange={(e) => setCustomerDecision(e.target.value)}
                    data-testid="customer-decision-select"
                  >
                    <option value="approved">Approved</option>
                    <option value="partially_approved">Partially Approved</option>
                    <option value="rejected">Rejected</option>
                  </Select>
                </div>
                <div>
                  <label className="font-mono text-[9.5px] tracking-wide text-text-muted-3">CHANNEL</label>
                  <Select
                    className="mt-1 h-8 w-[130px]"
                    value={customerMethod}
                    onChange={(e) => setCustomerMethod(e.target.value)}
                    data-testid="customer-method-select"
                  >
                    {ESTIMATE_APPROVAL_METHODS.map((m) => (
                      <option key={m.value} value={m.value}>
                        {m.label}
                      </option>
                    ))}
                  </Select>
                </div>
                <div className="flex-1">
                  <label className="font-mono text-[9.5px] tracking-wide text-text-muted-3">NAME (optional)</label>
                  <Input
                    className="mt-1 h-8"
                    value={customerName}
                    onChange={(e) => setCustomerName(e.target.value)}
                    data-testid="customer-name-input"
                  />
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={busy === 'customer'}
                  onClick={() => handleRecordCustomerApproval(active.id)}
                  data-testid="record-customer-approval"
                >
                  {busy === 'customer' ? <Spinner /> : 'Record'}
                </Button>
              </div>
            </div>
          ) : null}

          {active.status !== 'superseded' ? (
            <Button
              size="sm"
              variant="ghost"
              onClick={() => handleCreateRevision(active.id)}
              disabled={busy === 'revision'}
              data-testid="create-revision"
            >
              {busy === 'revision' ? <Spinner /> : 'Create New Revision (Re-quote)'}
            </Button>
          ) : (
            <p className="font-sans text-[11.5px] text-text-muted-2" data-testid="superseded-notice">
              This revision has been superseded and can no longer be changed.
            </p>
          )}
        </div>
      ) : null}

      {superseded.length > 0 ? (
        <div className="mt-4 border-t border-border-subtle pt-3" data-testid="superseded-revisions">
          <div className="font-mono text-[9.5px] tracking-wide text-text-muted-3">PRIOR REVISIONS</div>
          <div className="mt-2 flex flex-col gap-1.5">
            {superseded.map((r) => (
              <div
                key={r.id}
                className="flex items-center justify-between rounded-control border border-border-subtle bg-surface-card-item px-2.5 py-1.5"
                data-testid={`superseded-revision-${r.id}`}
              >
                <span className="font-sans text-[12px] text-text-muted-1">Revision {r.revisionNumber}</span>
                <span className="font-mono text-[11px] text-text-muted-2">{money(r.total)}</span>
                <Badge tone="neutral">SUPERSEDED</Badge>
              </div>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}

function ItemsEditor({
  items,
  onChange,
  onAdd,
  onRemove,
}: {
  items: EstimateItemRequest[];
  onChange: (index: number, patch: Partial<EstimateItemRequest>) => void;
  onAdd: () => void;
  onRemove: (index: number) => void;
}) {
  return (
    <div data-testid="items-editor" className="flex flex-col gap-2">
      {items.map((item, index) => (
        <div key={index} className="grid grid-cols-[1fr_70px_80px_80px_32px] items-center gap-1.5">
          <Input
            placeholder="Description"
            value={item.description}
            onChange={(e) => onChange(index, { description: e.target.value })}
            className="h-8"
            data-testid={`item-description-${index}`}
          />
          <Input
            type="number"
            min={0}
            step="0.01"
            placeholder="Qty"
            value={item.quantity}
            onChange={(e) => onChange(index, { quantity: Number(e.target.value) })}
            className="h-8"
            data-testid={`item-quantity-${index}`}
          />
          <Input
            type="number"
            min={0}
            step="0.01"
            placeholder="Unit cost"
            value={item.unitCost}
            onChange={(e) => onChange(index, { unitCost: Number(e.target.value) })}
            className="h-8"
            data-testid={`item-unit-cost-${index}`}
          />
          <Input
            type="number"
            min={0}
            step="0.01"
            placeholder="Unit price"
            value={item.unitPrice}
            onChange={(e) => onChange(index, { unitPrice: Number(e.target.value) })}
            className="h-8"
            data-testid={`item-unit-price-${index}`}
          />
          <button
            type="button"
            onClick={() => onRemove(index)}
            className="h-8 rounded-control border border-border text-[12px] text-text-muted-2 hover:text-status-critical"
            aria-label="Remove line"
            data-testid={`remove-item-${index}`}
          >
            ×
          </button>
        </div>
      ))}
      <Button size="sm" variant="outline" onClick={onAdd} data-testid="add-item-row" className="self-start">
        + Add line
      </Button>
    </div>
  );
}

function ItemsTable({ items }: { items: EstimateDto['items'] }) {
  if (items.length === 0) {
    return (
      <p className="font-sans text-[12px] text-text-muted-2" data-testid="items-empty">
        No line items yet.
      </p>
    );
  }
  return (
    <div data-testid="items-table" className="flex flex-col gap-1">
      {items.map((item) => (
        <div key={item.id} className="flex justify-between font-sans text-[12.5px] text-text-primary">
          <span>
            {item.description || item.type} × {item.quantity}
          </span>
          <span className="font-mono text-text-muted-1">{money(item.quantity * item.unitPrice)}</span>
        </div>
      ))}
    </div>
  );
}
