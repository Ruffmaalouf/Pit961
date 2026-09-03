import { CUSTOMER_ENDPOINTS, apiClient } from '@/services/apiClient';
import type {
  CreateCustomerRequest,
  CustomerDetailResponse,
  CustomerDto,
  CustomerListResponse,
  CustomerSoftDeleteResponse,
  UpdateCustomerRequest,
  VehicleSummaryDto,
} from '@/types/api';

/**
 * Thin transport layer for the P2-WP2 Customer endpoints. Every call needs
 * `auth: true` — Customers/Vehicles are entirely GarageTenant-policy-gated,
 * there is no anonymous surface here at all (unlike auth/branding).
 */

export function searchCustomers(params: {
  search?: string;
  isFleet?: boolean;
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
}): Promise<CustomerListResponse> {
  const query = new URLSearchParams();
  if (params.search) query.set('search', params.search);
  if (params.isFleet !== undefined) query.set('isFleet', String(params.isFleet));
  query.set('page', String(params.page ?? 1));
  query.set('pageSize', String(params.pageSize ?? 25));

  return apiClient.get<CustomerListResponse>(`${CUSTOMER_ENDPOINTS.search}?${query.toString()}`, {
    auth: true,
    signal: params.signal,
  });
}

export function getCustomerDetail(id: string, signal?: AbortSignal): Promise<CustomerDetailResponse> {
  return apiClient.get<CustomerDetailResponse>(CUSTOMER_ENDPOINTS.detail(id), { auth: true, signal });
}

export function createCustomer(request: CreateCustomerRequest): Promise<CustomerDto> {
  return apiClient.post<CustomerDto>(CUSTOMER_ENDPOINTS.create, request, { auth: true });
}

export function updateCustomer(id: string, request: UpdateCustomerRequest): Promise<CustomerDto> {
  return apiClient.put<CustomerDto>(CUSTOMER_ENDPOINTS.update(id), request, { auth: true });
}

/** Owner/manager only server-side — a 403 surfaces as ApiError({status: 403}). */
export function softDeleteCustomer(id: string): Promise<CustomerSoftDeleteResponse> {
  return apiClient.del<CustomerSoftDeleteResponse>(CUSTOMER_ENDPOINTS.softDelete(id), { auth: true });
}

export function listVehiclesForCustomer(customerId: string): Promise<VehicleSummaryDto[]> {
  return apiClient.get<VehicleSummaryDto[]>(CUSTOMER_ENDPOINTS.vehicles(customerId), { auth: true });
}
