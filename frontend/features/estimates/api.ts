import { ESTIMATE_ENDPOINTS, apiClient } from '@/services/apiClient';
import type {
  ApplyDiscountRequest,
  CreateEstimateRequest,
  EstimateDto,
  RecordCustomerApprovalRequest,
  ReplaceEstimateItemsRequest,
} from '@/types/api';

/**
 * Thin transport layer for the P2-WP4 Estimate endpoints — mirrors
 * features/jobs/api.ts's shape. Subtotal/Total/DiscountAmount and every
 * approval-owned Status value are NEVER fields in a request payload here:
 * the client only ever sends intent (items, a discount percent, a customer
 * decision) and the backend computes/writes the authoritative values.
 */

export function createEstimate(request: CreateEstimateRequest): Promise<EstimateDto> {
  return apiClient.post<EstimateDto>(ESTIMATE_ENDPOINTS.create, request, { auth: true });
}

export function getEstimate(id: string, signal?: AbortSignal): Promise<EstimateDto> {
  return apiClient.get<EstimateDto>(ESTIMATE_ENDPOINTS.byId(id), { auth: true, signal });
}

/** All revisions for a Job, oldest first (RevisionNumber order) — includes
 * superseded rows so the UI can render the full re-quote history. */
export function listEstimatesByJob(jobId: string, signal?: AbortSignal): Promise<EstimateDto[]> {
  return apiClient.get<EstimateDto[]>(ESTIMATE_ENDPOINTS.byJob(jobId), { auth: true, signal });
}

export function replaceEstimateItems(id: string, request: ReplaceEstimateItemsRequest): Promise<EstimateDto> {
  return apiClient.put<EstimateDto>(ESTIMATE_ENDPOINTS.items(id), request, { auth: true });
}

/**
 * The ONLY way DiscountAmount/Total change. A 403 here is a real, expected
 * outcome (a Manager over the 15% cap) — never a bug; callers must catch
 * ApiError({status: 403}) and surface it, not treat it as a crash. A 409 is
 * also expected (someone else's write landed first) — re-fetch, never retry
 * blindly on top of stale numbers.
 */
export function applyDiscount(id: string, request: ApplyDiscountRequest): Promise<EstimateDto> {
  return apiClient.post<EstimateDto>(ESTIMATE_ENDPOINTS.discount(id), request, { auth: true });
}

/** Routes to "sent" or, above the $500 subtotal threshold, to
 * "pending_owner_approval" — always a 200 either way; the UI reads the
 * returned Status to know which one happened. */
export function submitEstimate(id: string): Promise<EstimateDto> {
  return apiClient.post<EstimateDto>(ESTIMATE_ENDPOINTS.submit(id), undefined, { auth: true });
}

/** Owner-only (Owner Decision #2). A 403 here means the signed-in user is
 * not an Owner — expected, not a bug; the UI should not even show this
 * action to a non-Owner, but must still handle a 403 gracefully if role
 * changes mid-session. */
export function clearOwnerApproval(id: string): Promise<EstimateDto> {
  return apiClient.post<EstimateDto>(ESTIMATE_ENDPOINTS.clearOwnerApproval(id), undefined, { auth: true });
}

/** Entirely independent of Owner approval — recording a customer's decision
 * never requires the Owner role and never touches pending_owner_approval. */
export function recordCustomerApproval(id: string, request: RecordCustomerApprovalRequest): Promise<EstimateDto> {
  return apiClient.post<EstimateDto>(ESTIMATE_ENDPOINTS.customerApproval(id), request, { auth: true });
}

/** Owner Decision #3: creates a new revision, superseding this one. Items
 * are carried forward as the new revision's starting point. */
export function createEstimateRevision(id: string): Promise<EstimateDto> {
  return apiClient.post<EstimateDto>(ESTIMATE_ENDPOINTS.createRevision(id), undefined, { auth: true });
}
