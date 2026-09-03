/**
 * Wire types for the PIT961 backend.
 *
 * Casing matches ASP.NET Core's default camelCase JSON serialisation, so these
 * shapes are transcribed 1:1 from the backend contract — do not "tidy" them.
 */

/** GET /api/v1/auth/me, and the `user` object inside the login response. */
export interface AuthUser {
  id: string;
  garageId: string;
  garageName: string;
  email: string;
  name: string;
  role: string;
}

/** POST /api/v1/auth/login request body (C# `LoginRequest(string Email, string Password)`). */
export interface LoginRequest {
  email: string;
  password: string;
}

/** POST /api/v1/auth/login 200 body. Sets the httpOnly refresh cookie as a side effect. */
export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: AuthUser;
}

/** POST /api/v1/auth/refresh 200 body. Rotates the httpOnly refresh cookie as a side effect. */
export interface RefreshResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}

/** GET /api/config/branding — anonymous, fetched once at app boot. */
export interface BrandingConfig {
  productDisplayName: string;
  emailFromName: string;
  logoUrl: string;
  supportEmail: string;
}

/**
 * ASP.NET Core ProblemDetails. Only `status` and `title` are relied on —
 * everything else is optional and must never be assumed present.
 */
export interface ProblemDetails {
  status?: number;
  title?: string;
  type?: string;
  detail?: string;
  instance?: string;
  traceId?: string;
  [key: string]: unknown;
}

// ---------------------------------------------------------------------------
// P2-WP2/P2-WP3 (Milestone 1) — Customer, Vehicle, Job wire types.
// Transcribed 1:1 from GarageOS.Api/Contracts/{Customer,Vehicle,Job}Contracts.cs.
// ---------------------------------------------------------------------------

export interface CreateCustomerRequest {
  firstName: string;
  lastName?: string | null;
  phone: string;
  whatsapp?: string | null;
  email?: string | null;
  notes?: string | null;
  isFleet: boolean;
}

export type UpdateCustomerRequest = CreateCustomerRequest;

export interface CustomerDto {
  id: string;
  firstName: string;
  lastName: string | null;
  phone: string;
  whatsapp: string | null;
  email: string | null;
  notes: string | null;
  isFleet: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerListItemDto {
  id: string;
  firstName: string;
  lastName: string | null;
  phone: string;
  email: string | null;
  isFleet: boolean;
  vehicleCount: number;
  createdAt: string;
}

export interface CustomerListResponse {
  items: CustomerListItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface VehicleSummaryDto {
  id: string;
  plateNumber: string;
  plateCountry: string;
  make: string;
  model: string;
  year: number | null;
  currentMileage: number | null;
}

export interface CustomerJobHistoryItemDto {
  jobId: string;
  jobNumber: string;
  vehiclePlate: string | null;
  status: string;
  openedAt: string;
  closedAt: string | null;
  invoiceTotal: number | null;
}

export interface CustomerJobsHistorySummaryDto {
  recentJobs: CustomerJobHistoryItemDto[];
  totalJobCount: number;
  moreAvailable: boolean;
}

export interface CustomerBalanceSummaryDto {
  totalInvoiced: number;
  totalPaid: number;
  outstandingBalance: number;
  currency: string;
}

export interface CustomerDetailResponse {
  customer: CustomerDto;
  vehicles: VehicleSummaryDto[];
  jobsHistory: CustomerJobsHistorySummaryDto;
  balanceSummary: CustomerBalanceSummaryDto;
}

export interface CustomerSoftDeleteResponse {
  hadOpenJobs: boolean;
}

export interface CreateVehicleRequest {
  customerId: string;
  plateNumber: string;
  plateCountry: string;
  make: string;
  model: string;
  year?: number | null;
  color?: string | null;
  vin?: string | null;
  engine?: string | null;
  engineCode?: string | null;
  transmission?: string | null;
  drivetrain?: string | null;
  fuelType?: string | null;
  currentMileage?: number | null;
}

export type UpdateVehicleRequest = Omit<CreateVehicleRequest, 'customerId'>;

export interface VehicleDto {
  id: string;
  customerId: string;
  plateNumber: string;
  plateCountry: string;
  make: string;
  model: string;
  year: number | null;
  color: string | null;
  vin: string | null;
  engine: string | null;
  engineCode: string | null;
  transmission: string | null;
  drivetrain: string | null;
  fuelType: string | null;
  currentMileage: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface DuplicateVehicleMatchDto {
  vehicleId: string;
  customerId: string;
  customerName: string;
  plateNumber: string;
  plateCountry: string;
  make: string;
  model: string;
}

export interface DuplicateWarningDto {
  hasDuplicates: boolean;
  matches: DuplicateVehicleMatchDto[];
}

/** HTTP status is always 201/200 regardless of hasDuplicates — never a 409. */
export interface VehicleMutationResponse {
  vehicle: VehicleDto;
  duplicateWarning: DuplicateWarningDto;
}

export interface VehicleSoftDeleteResponse {
  hadOpenJobs: boolean;
}

export interface DuplicatePlateCheckResponse {
  duplicateWarning: DuplicateWarningDto;
}

export interface CreateJobRequest {
  customerId: string;
  vehicleId: string;
  primaryMechanicId?: string | null;
  secondaryMechanicId?: string | null;
  mileageAtIntake?: number | null;
  customerComplaint?: string | null;
  advisorNotes?: string | null;
  promisedAt?: string | null;
  customerWaiting: boolean;
  source: string;
  overnight: boolean;
  overnightNote?: string | null;
  isWarrantyReturn: boolean;
  parentJobId?: string | null;
}

export interface UpdateJobIntakeRequest {
  primaryMechanicId?: string | null;
  secondaryMechanicId?: string | null;
  mileageAtIntake?: number | null;
  customerComplaint?: string | null;
  advisorNotes?: string | null;
  promisedAt?: string | null;
  customerWaiting: boolean;
  overnight: boolean;
  overnightNote?: string | null;
}

export interface TransitionJobStatusRequest {
  targetStatus: string;
  reason?: string | null;
}

export interface JobDto {
  id: string;
  jobNumber: string;
  customerId: string;
  vehicleId: string;
  primaryMechanicId: string | null;
  secondaryMechanicId: string | null;
  status: string;
  mileageAtIntake: number | null;
  customerComplaint: string | null;
  advisorNotes: string | null;
  promisedAt: string | null;
  customerWaiting: boolean;
  source: string;
  overnight: boolean;
  overnightNote: string | null;
  isWarrantyReturn: boolean;
  parentJobId: string | null;
  cancellationReason: string | null;
  deletionReason: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface JobHistoryEntryDto {
  id: string;
  actorId: string | null;
  actorName: string;
  actorRole: string;
  eventType: string;
  summary: string;
  detail: string | null;
  createdAt: string;
}

export interface FloorBoardCardDto {
  jobId: string;
  jobNumber: string;
  customerDisplayName: string;
  vehicleDisplay: string;
  primaryMechanicId: string | null;
  primaryMechanicName: string | null;
  checkedInAt: string;
  promisedAt: string | null;
  customerWaiting: boolean;
  overnight: boolean;
  isWarrantyReturn: boolean;
  statusUpdatedAt: string;
}

export interface FloorBoardColumnDto {
  status: string;
  cards: FloorBoardCardDto[];
}

export interface FloorBoardResponse {
  columns: FloorBoardColumnDto[];
}

/**
 * Job.Status vocabulary (DECISIONS.md #12 Decision #1). Frontend never
 * decides transition legality — this is display-only (labels, ordering
 * fallback). The backend's AllowedTransitions/RolesFor tables remain the
 * sole authority; the UI only shows/hides using what the API returns.
 */
export const JOB_STATUS_LABELS: Record<string, string> = {
  checked_in: 'Checked In',
  estimate_pending: 'Estimate Pending',
  awaiting_approval: 'Awaiting Approval',
  approved: 'Approved',
  in_progress: 'In Progress',
  completed: 'Completed',
  invoiced: 'Invoiced',
  closed: 'Closed',
  cancelled: 'Cancelled',
  deleted: 'Deleted',
};

/**
 * UX-ONLY mirror of JobStatusService.AllowedTransitions (GarageOS.Application/
 * Jobs/JobStatusService.cs). This exists purely to decide which status-action
 * buttons Job Detail offers — it is NOT authoritative. Every transition still
 * goes through POST /status-transitions, and the backend independently
 * re-validates fromStatus/role and can reject with 400 (invalid transition)
 * or 403 (role) regardless of what this table shows. If this table ever
 * drifts from the backend's real table, the worst case is a button that 400s
 * or 403s — never a transition that skips backend validation.
 */
export const JOB_STATUS_UX_TRANSITIONS: Record<string, string[]> = {
  checked_in: ['estimate_pending', 'cancelled', 'deleted'],
  estimate_pending: ['awaiting_approval', 'cancelled', 'deleted'],
  awaiting_approval: ['approved', 'cancelled', 'deleted'],
  approved: ['in_progress', 'cancelled', 'deleted'],
  in_progress: ['completed', 'cancelled', 'deleted'],
  completed: ['invoiced', 'cancelled', 'deleted'],
  invoiced: ['closed'],
  closed: [],
  cancelled: ['deleted'],
  deleted: [],
};
