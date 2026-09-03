import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { JobIntakePage } from '@/pages/jobs/JobIntakePage';
import type { CustomerDetailResponse, CustomerListResponse, JobDto } from '@/types/api';

vi.mock('@/features/customers/api', () => ({
  searchCustomers: vi.fn(),
  getCustomerDetail: vi.fn(),
  listVehiclesForCustomer: vi.fn(),
}));
vi.mock('@/features/jobs/api', () => ({
  createJob: vi.fn(),
}));

import * as customersApi from '@/features/customers/api';
import * as jobsApi from '@/features/jobs/api';

const mockedSearch = vi.mocked(customersApi.searchCustomers);
const mockedGetDetail = vi.mocked(customersApi.getCustomerDetail);
const mockedListVehicles = vi.mocked(customersApi.listVehiclesForCustomer);
const mockedCreateJob = vi.mocked(jobsApi.createJob);

const CUSTOMER = {
  id: 'c-1',
  firstName: 'Amir',
  lastName: 'Haddad',
  phone: '+961 3 111 222',
  email: null,
  isFleet: false,
  vehicleCount: 1,
  createdAt: '2026-01-01T00:00:00.000Z',
};

const VEHICLE = {
  id: 'v-1',
  plateNumber: '123456',
  plateCountry: 'LB',
  make: 'Toyota',
  model: 'Corolla',
  year: 2018,
  currentMileage: 42000,
};

function renderAt(route: string) {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <Routes>
        <Route path="/jobs/new" element={<JobIntakePage />} />
        <Route path="/jobs/:id" element={<div data-testid="job-detail-stub" />} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe('JobIntakePage', () => {
  it('creates a job from a searched customer/vehicle without ever sending status, garageId or jobNumber', async () => {
    mockedSearch.mockResolvedValue({
      items: [CUSTOMER],
      totalCount: 1,
      page: 1,
      pageSize: 8,
    } as CustomerListResponse);
    mockedListVehicles.mockResolvedValue([VEHICLE]);
    const createdJob: JobDto = {
      id: 'j-1',
      jobNumber: 'J-000001',
      customerId: 'c-1',
      vehicleId: 'v-1',
      primaryMechanicId: null,
      secondaryMechanicId: null,
      status: 'checked_in',
      mileageAtIntake: 42500,
      customerComplaint: 'Squeaky brakes',
      advisorNotes: null,
      promisedAt: null,
      customerWaiting: false,
      source: 'walk_in',
      overnight: false,
      overnightNote: null,
      isWarrantyReturn: false,
      parentJobId: null,
      cancellationReason: null,
      deletionReason: null,
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    };
    mockedCreateJob.mockResolvedValue(createdJob);

    renderAt('/jobs/new');

    await userEvent.type(screen.getByTestId('job-intake-customer-search'), 'Amir');
    await userEvent.click(await screen.findByTestId('job-intake-customer-option-c-1'));

    const vehicleSelect = await screen.findByTestId('job-intake-vehicle-select');
    await userEvent.selectOptions(vehicleSelect, 'v-1');

    await userEvent.type(screen.getByLabelText(/mileage at intake/i), '42500');
    await userEvent.type(screen.getByLabelText(/customer complaint/i), 'Squeaky brakes');

    await userEvent.click(screen.getByTestId('create-job-submit'));

    await waitFor(() => expect(mockedCreateJob).toHaveBeenCalled());
    const payload = mockedCreateJob.mock.calls[0][0] as unknown as Record<string, unknown>;
    expect(payload).toEqual(
      expect.objectContaining({ customerId: 'c-1', vehicleId: 'v-1', mileageAtIntake: 42500, customerComplaint: 'Squeaky brakes' }),
    );
    // The server, not this form, is the sole source of status/garageId/jobNumber.
    expect(payload).not.toHaveProperty('status');
    expect(payload).not.toHaveProperty('garageId');
    expect(payload).not.toHaveProperty('jobNumber');

    // The server-generated JobNumber is what Job Detail shows next — this page
    // never invents or displays one itself before the response arrives.
    expect(await screen.findByTestId('job-detail-stub')).toBeInTheDocument();
  });

  it('blocks submission with a field error when no customer or vehicle is selected', async () => {
    mockedSearch.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 8 } as CustomerListResponse);

    renderAt('/jobs/new');

    await userEvent.click(screen.getByTestId('create-job-submit'));

    expect(await screen.findByText(/select a customer first/i)).toBeInTheDocument();
    expect(mockedCreateJob).not.toHaveBeenCalled();
  });

  it('pre-selects the customer and vehicle from a Customer Detail deep link', async () => {
    const detail: CustomerDetailResponse = {
      customer: {
        id: 'c-1',
        firstName: 'Amir',
        lastName: 'Haddad',
        phone: '+961 3 111 222',
        whatsapp: null,
        email: null,
        notes: null,
        isFleet: false,
        createdAt: '2026-01-01T00:00:00.000Z',
        updatedAt: '2026-01-01T00:00:00.000Z',
      },
      vehicles: [VEHICLE],
      jobsHistory: { recentJobs: [], totalJobCount: 0, moreAvailable: false },
      balanceSummary: { totalInvoiced: 0, totalPaid: 0, outstandingBalance: 0, currency: 'USD' },
    };
    mockedGetDetail.mockResolvedValue(detail);

    renderAt('/jobs/new?customerId=c-1&vehicleId=v-1');

    expect(await screen.findByTestId('selected-customer')).toHaveTextContent('Amir Haddad');
    expect(await screen.findByTestId('job-intake-vehicle-select')).toHaveValue('v-1');
  });
});
