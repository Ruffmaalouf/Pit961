import { VEHICLE_ENDPOINTS, apiClient } from '@/services/apiClient';
import type {
  CreateVehicleRequest,
  DuplicatePlateCheckResponse,
  UpdateVehicleRequest,
  VehicleDto,
  VehicleMutationResponse,
  VehicleSoftDeleteResponse,
} from '@/types/api';

/** Thin transport layer for the P2-WP2 Vehicle endpoints. */

export function getVehicle(id: string): Promise<VehicleDto> {
  return apiClient.get<VehicleDto>(VEHICLE_ENDPOINTS.byId(id), { auth: true });
}

/**
 * Owner Decision #5 is binding: duplicate plate is a WARNING, never a hard
 * block. HTTP status is always 201 regardless of duplicateWarning.hasDuplicates
 * — callers must never treat a duplicate as a failure.
 */
export function createVehicle(request: CreateVehicleRequest): Promise<VehicleMutationResponse> {
  return apiClient.post<VehicleMutationResponse>(VEHICLE_ENDPOINTS.create, request, { auth: true });
}

export function updateVehicle(id: string, request: UpdateVehicleRequest): Promise<VehicleMutationResponse> {
  return apiClient.put<VehicleMutationResponse>(VEHICLE_ENDPOINTS.update(id), request, { auth: true });
}

/** Owner/manager only server-side — a 403 surfaces as ApiError({status: 403}). */
export function softDeleteVehicle(id: string): Promise<VehicleSoftDeleteResponse> {
  return apiClient.del<VehicleSoftDeleteResponse>(VEHICLE_ENDPOINTS.softDelete(id), { auth: true });
}

/** Live check as the user types a plate, before submitting the form. */
export function checkDuplicatePlate(params: {
  plateNumber: string;
  plateCountry: string;
  excludeVehicleId?: string;
  signal?: AbortSignal;
}): Promise<DuplicatePlateCheckResponse> {
  const query = new URLSearchParams({
    plateNumber: params.plateNumber,
    plateCountry: params.plateCountry,
  });
  if (params.excludeVehicleId) query.set('excludeVehicleId', params.excludeVehicleId);

  return apiClient.get<DuplicatePlateCheckResponse>(
    `${VEHICLE_ENDPOINTS.checkDuplicatePlate}?${query.toString()}`,
    { auth: true, signal: params.signal },
  );
}
