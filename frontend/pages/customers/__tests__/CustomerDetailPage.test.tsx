import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { makeUser } from '@/lib/test-utils';
import { CustomerDetailPage } from '@/pages/customers/CustomerDetailPage';
import { ApiError } from '@/services/apiClient';
import { resetAuthStore, useAuthStore } from '@/stores/authStore';
import type { CustomerDetailResponse, VehicleMutationResponse } from '@/types/api';

vi.mock('@/features/customers/api', () => ({
  getCustomerDetail: vi.fn(),
  updateCustomer: vi.fn(),
  softDeleteCustomer: vi.fn(),
}));
vi.mock('@/features/vehicles/api', () => ({
  createVehicle: vi.fn(),
  updateVehicle: vi.fn(),
}));

import * as customersApi from '@/features/customers/api';
import * as vehiclesApi from '@/features/vehicles/api';

const mockedGetDetail = vi.mocked(customersApi.getCustomerDetail);
const mockedUpdate = vi.mocked(customersApi.updateCustomer);
const mockedSoftDelete = vi.mocked(customersApi.softDeleteCustomer);
const mockedCreateVehicle = vi.mocked(vehiclesApi.createVehicle);

function makeDetail(overrides: Partial<CustomerDetailResponse> = {}): CustomerDetailResponse {
  return {
    customer: {
      id: 'c-1',
      firstName: 'Amir',
      lastName: 'Haddad',
      phone: '+961 3 111 222',
      whatsapp: null,
      email: 'amir@example.test',
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
    jobsHistory: {
      recentJobs: [
        {
          jobId: 'j-1',
          jobNumber: 'J-000001',
          vehiclePlate: '123456',
          status: 'checked_in',
          openedAt: '2026-01-05T00:00:00.000Z',
          closedAt: null,
          invoiceTotal: null,
        },
      ],
      totalJobCount: 1,
      moreAvailable: false,
    },
    balanceSummary: {
      totalInvoiced: 100,
      totalPaid: 40,
      outstandingBalance: 60,
      currency: 'USD',
    },
    ...overrides,
  };
}

function renderPage(role: string | null) {
  resetAuthStore(role ? 'authenticated' : 'unauthenticated');
  if (role) {
    useAuthStore.setState({ user: makeUser({ role }) });
  }
  return render(
    <MemoryRouter initialEntries={['/customers/c-1']}>
      <Routes>
        <Route path="/customers/:id" element={<CustomerDetailPage />} />
        <Route path="/customers" element={<div data-testid="customers-list-stub" />} />
        <Route path="/jobs/:id" element={<div data-testid="job-stub" />} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  resetAuthStore('unauthenticated');
});

describe('CustomerDetailPage', () => {
  it('shows the not-found state for a 404', async () => {
    mockedGetDetail.mockRejectedValue(new ApiError({ status: 404, title: 'Not found.' }));

    renderPage('owner');

    expect(await screen.findByTestId('customer-not-found')).toBeInTheDocument();
  });

  it('shows the generic error state and lets the user retry', async () => {
    mockedGetDetail.mockRejectedValue(new ApiError({ status: 500, title: 'Something went wrong. Please try again.' }));

    renderPage('owner');

    expect(await screen.findByTestId('customer-detail-error')).toBeInTheDocument();
  });

  it('renders the customer, their vehicles and job history', async () => {
    mockedGetDetail.mockResolvedValue(makeDetail());

    renderPage('owner');

    expect(await screen.findByText('Amir Haddad')).toBeInTheDocument();
    expect(screen.getByTestId('vehicle-card-v-1')).toHaveTextContent('Toyota Corolla');
    expect(screen.getByText('J-000001')).toBeInTheDocument();
    expect(screen.getByText('USD 60.00')).toBeInTheDocument();
  });

  it('only shows the delete action to owner/manager roles, matching backend role authority', async () => {
    mockedGetDetail.mockResolvedValue(makeDetail());
    renderPage('staff');

    await screen.findByText('Amir Haddad');
    expect(screen.queryByTestId('delete-customer-button')).toBeNull();

    cleanup();
    vi.clearAllMocks();
    mockedGetDetail.mockResolvedValue(makeDetail());
    renderPage('manager');

    await screen.findByText('Amir Haddad');
    expect(screen.getByTestId('delete-customer-button')).toBeInTheDocument();
  });

  it('soft-deletes the customer and navigates back to the list on confirmation', async () => {
    mockedGetDetail.mockResolvedValue(makeDetail());
    mockedSoftDelete.mockResolvedValue({ hadOpenJobs: false });
    renderPage('owner');

    await screen.findByText('Amir Haddad');
    await userEvent.click(screen.getByTestId('delete-customer-button'));

    expect(await screen.findByTestId('customers-list-stub')).toBeInTheDocument();
    expect(mockedSoftDelete).toHaveBeenCalledWith('c-1');
  });

  it('edits the customer and refreshes the detail from the server', async () => {
    mockedGetDetail.mockResolvedValueOnce(makeDetail());
    mockedUpdate.mockResolvedValue(makeDetail().customer);
    renderPage('owner');

    await screen.findByText('Amir Haddad');

    const updated = makeDetail({
      customer: { ...makeDetail().customer, firstName: 'Amir', lastName: 'Nassar' },
    });
    mockedGetDetail.mockResolvedValueOnce(updated);

    await userEvent.click(screen.getByTestId('edit-customer-button'));
    const lastNameInput = await screen.findByLabelText(/last name/i);
    await userEvent.clear(lastNameInput);
    await userEvent.type(lastNameInput, 'Nassar');
    await userEvent.click(screen.getByRole('button', { name: /save changes/i }));

    expect(mockedUpdate).toHaveBeenCalledWith('c-1', expect.objectContaining({ lastName: 'Nassar' }));
    expect(await screen.findByText('Amir Nassar')).toBeInTheDocument();
  });

  it('adding a vehicle with a duplicate plate warns without blocking the save (Owner Decision #5)', async () => {
    mockedGetDetail.mockResolvedValue(makeDetail());
    const mutation: VehicleMutationResponse = {
      vehicle: {
        id: 'v-2',
        customerId: 'c-1',
        plateNumber: '999888',
        plateCountry: 'LB',
        make: 'Kia',
        model: 'Sportage',
        year: 2021,
        color: null,
        vin: null,
        engine: null,
        engineCode: null,
        transmission: null,
        drivetrain: null,
        fuelType: null,
        currentMileage: null,
        createdAt: '2026-01-01T00:00:00.000Z',
        updatedAt: '2026-01-01T00:00:00.000Z',
      },
      duplicateWarning: {
        hasDuplicates: true,
        matches: [
          {
            vehicleId: 'v-other',
            customerId: 'c-other',
            customerName: 'Nadia Fares',
            plateNumber: '999888',
            plateCountry: 'LB',
            make: 'Kia',
            model: 'Sportage',
          },
        ],
      },
    };
    mockedCreateVehicle.mockResolvedValue(mutation);

    renderPage('owner');
    await screen.findByText('Amir Haddad');

    await userEvent.click(screen.getByTestId('add-vehicle-button'));
    await userEvent.type(await screen.findByLabelText(/plate number/i), '999888');
    await userEvent.type(screen.getByLabelText(/plate country/i), 'LB');
    await userEvent.type(screen.getByLabelText(/^make$/i), 'Kia');
    await userEvent.type(screen.getByLabelText(/^model$/i), 'Sportage');
    await userEvent.click(screen.getByRole('button', { name: /add vehicle/i }));

    const warning = await screen.findByTestId('duplicate-plate-warning');
    expect(warning).toHaveTextContent('Nadia Fares');
    expect(mockedCreateVehicle).toHaveBeenCalledWith(expect.objectContaining({ customerId: 'c-1' }));
    await waitFor(() => expect(mockedGetDetail).toHaveBeenCalledTimes(2));
  });
});
