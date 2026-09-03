import { JOB_ENDPOINTS, apiClient } from '@/services/apiClient';
import type {
  CreateJobRequest,
  FloorBoardResponse,
  JobDto,
  JobHistoryEntryDto,
  TransitionJobStatusRequest,
  UpdateJobIntakeRequest,
} from '@/types/api';

/**
 * Thin transport layer for the P2-WP3 Job endpoints. Status is NEVER a field
 * in a create/update payload here — the only place it can change is
 * transitionJobStatus, matching the backend's "no arbitrary raw Job.Status
 * assignment" rule. GarageId/JobNumber are also never client-supplied.
 */

export function createJob(request: CreateJobRequest): Promise<JobDto> {
  return apiClient.post<JobDto>(JOB_ENDPOINTS.create, request, { auth: true });
}

export function getJob(id: string, signal?: AbortSignal): Promise<JobDto> {
  return apiClient.get<JobDto>(JOB_ENDPOINTS.byId(id), { auth: true, signal });
}

export function updateJobIntake(id: string, request: UpdateJobIntakeRequest): Promise<JobDto> {
  return apiClient.put<JobDto>(JOB_ENDPOINTS.updateIntake(id), request, { auth: true });
}

export function getJobHistory(id: string, signal?: AbortSignal): Promise<JobHistoryEntryDto[]> {
  return apiClient.get<JobHistoryEntryDto[]>(JOB_ENDPOINTS.history(id), { auth: true, signal });
}

/**
 * The ONLY way Status changes. A 409 here is a real, expected outcome (another
 * actor changed this job's status first) — never a bug. Callers must catch
 * ApiError({status: 409}) and re-fetch rather than assume success or crash.
 */
export function transitionJobStatus(id: string, request: TransitionJobStatusRequest): Promise<JobDto> {
  return apiClient.post<JobDto>(JOB_ENDPOINTS.transitionStatus(id), request, { auth: true });
}

export function getFloorBoard(signal?: AbortSignal): Promise<FloorBoardResponse> {
  return apiClient.get<FloorBoardResponse>(JOB_ENDPOINTS.floorBoard, { auth: true, signal });
}
