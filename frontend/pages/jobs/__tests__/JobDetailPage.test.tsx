import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { JobDetailPage } from '@/pages/jobs/JobDetailPage';
import { ApiError } from '@/services/apiClient';
import type { CustomerDetailResponse, JobDto, JobHistoryEntryDto } from '@/types/api';

vi.mock('@/features/customers/api', () => ({
  getCustomerDetail: vi.fn(),
}));
vi.mock('@/features/jobs/api', () => ({
  getJob: vi.fn(),
  getJobHistory: vi.fn(),
  transitionJobStatus: vi.fn(),
}));

import * as customersApi from '@/features/customers/api';
import * as jobsApi from '@/features/jobs/api';

const mockedGetJob = vi.mocked(jobsApi.getJob);
const mockedGetHistory = vi.mocked(jobsApi.getJobHistory);
const mockedTransition = vi.mocked(jobsApi.transitionJobStatus);
const mockedGetCustomerDetail = vi.mocked(customersApi.getCustomerDetail);

function makeJob(overrides: Partial<JobDto> = {}): JobDto {
  return {
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
    ...overrides,
  };
}

const CUSTOMER_DETAIL: CustomerDetailResponse = {
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
  vehicles: [
    {
      id: 'v-1',
      plateNumber: '123456',
      plateCountry: 'LB',
      make: 'Toyota',
      model: 'Corolla',
      year: 2018,
      currentMileage: 42000,
    },
  ],
  jobsHistory: { recentJobs: [], totalJobCount: 0, moreAvailable: false },
  balanceSummary: { totalInvoiced: 0, totalPaid: 0, outstandingBalance: 0, currency: 'USD' },
};

function renderJob(id = 'j-1') {
  return render(
    <MemoryRouter initialEntries={[`/jobs/${id}`]}>
      <Routes>
        <Route path="/jobs/:id" element={<JobDetailPage />} />
        <Route path="/floor" element={<div data-testid="floor-stub" />} />
      </Routes>
    </MemoryRouter>,
  );
}

const NO_HISTORY: JobHistoryEntryDto[] = [];

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe('JobDetailPage', () => {
  it('shows the not-found state for a 404', async () => {
    mockedGetJob.mockRejectedValue(new ApiError({ status: 404, title: 'Not found.' }));

    renderJob();

    expect(await screen.findByTestId('job-not-found')).toBeInTheDocument();
  });

  it('shows the real server-generated job number, status and vehicle/customer identity', async () => {
    mockedGetJob.mockResolvedValue(makeJob());
    mockedGetHistory.mockResolvedValue(NO_HISTORY);
    mockedGetCustomerDetail.mockResolvedValue(CUSTOMER_DETAIL);

    renderJob();

    expect(await screen.findByText('J-000001')).toBeInTheDocument();
    expect(screen.getByText('Checked In')).toBeInTheDocument();
    expect(screen.getByText(/Toyota Corolla/)).toBeInTheDocument();
    expect(screen.getByText(/Amir Haddad/)).toBeInTheDocument();
  });

  it('offers only the allowed UX transitions for the current status', async () => {
    mockedGetJob.mockResolvedValue(makeJob({ status: 'checked_in' }));
    mockedGetHistory.mockResolvedValue(NO_HISTORY);
    mockedGetCustomerDetail.mockResolvedValue(CUSTOMER_DETAIL);

    renderJob();

    await screen.findByText('J-000001');
    expect(screen.getByTestId('transition-estimate_pending')).toBeInTheDocument();
    expect(screen.getByTestId('transition-cancelled')).toBeInTheDocument();
    expect(screen.getByTestId('transition-deleted')).toBeInTheDocument();
    // approved/in_progress/etc are not legal from checked_in.
    expect(screen.queryByTestId('transition-approved')).toBeNull();
    expect(screen.queryByTestId('transition-in_progress')).toBeNull();
  });

  it('shows no further actions for a terminal status', async () => {
    mockedGetJob.mockResolvedValue(makeJob({ status: 'closed' }));
    mockedGetHistory.mockResolvedValue(NO_HISTORY);
    mockedGetCustomerDetail.mockResolvedValue(CUSTOMER_DETAIL);

    renderJob();

    await screen.findByText('J-000001');
    expect(screen.getByText(/no further actions/i)).toBeInTheDocument();
    expect(screen.queryByTestId(/^transition-/)).toBeNull();
  });

  it('performs an allowed transition and reflects the new status from the server response', async () => {
    mockedGetJob.mockResolvedValue(makeJob({ status: 'checked_in' }));
    mockedGetHistory.mockResolvedValue(NO_HISTORY);
    mockedGetCustomerDetail.mockResolvedValue(CUSTOMER_DETAIL);
    mockedTransition.mockResolvedValue(makeJob({ status: 'estimate_pending' }));

    renderJob();

    await screen.findByText('J-000001');
    await userEvent.click(screen.getByTestId('transition-estimate_pending'));

    expect(mockedTransition).toHaveBeenCalledWith('j-1', { targetStatus: 'estimate_pending' });
    expect(await screen.findByText('Estimate Pending')).toBeInTheDocument();
  });

  it('shows the real conflict response on a 409 and reloads rather than assuming success', async () => {
    mockedGetJob
      .mockResolvedValueOnce(makeJob({ status: 'checked_in' }))
      .mockResolvedValueOnce(makeJob({ status: 'cancelled' }));
    mockedGetHistory.mockResolvedValue(NO_HISTORY);
    mockedGetCustomerDetail.mockResolvedValue(CUSTOMER_DETAIL);
    mockedTransition.mockRejectedValue(new ApiError({ status: 409, title: 'Conflict' }));

    renderJob();

    await screen.findByText('J-000001');
    await userEvent.click(screen.getByTestId('transition-estimate_pending'));

    const notice = await screen.findByTestId('conflict-notice');
    expect(notice).toHaveTextContent(/updated by someone else/i);

    // Never silently overwrites with the failed target — reloads real server state.
    await waitFor(() => expect(mockedGetJob).toHaveBeenCalledTimes(2));
    expect(await screen.findByText('Cancelled')).toBeInTheDocument();
  });
});
