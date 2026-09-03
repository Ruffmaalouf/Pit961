import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useParams } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { renderWithRouter } from '@/lib/test-utils';
import { CustomersListPage } from '@/pages/customers/CustomersListPage';
import { ApiError } from '@/services/apiClient';
import type { CustomerDto, CustomerListItemDto } from '@/types/api';

vi.mock('@/features/customers/api', () => ({
  searchCustomers: vi.fn(),
  createCustomer: vi.fn(),
}));

import * as customersApi from '@/features/customers/api';

const mockedSearch = vi.mocked(customersApi.searchCustomers);
const mockedCreate = vi.mocked(customersApi.createCustomer);

function makeItem(overrides: Partial<CustomerListItemDto> = {}): CustomerListItemDto {
  return {
    id: 'c-1',
    firstName: 'Amir',
    lastName: 'Haddad',
    phone: '+961 3 111 222',
    email: null,
    isFleet: false,
    vehicleCount: 2,
    createdAt: '2026-01-01T00:00:00.000Z',
    ...overrides,
  };
}

function DetailStub() {
  const { id } = useParams<{ id: string }>();
  return <div data-testid="detail-stub">{id}</div>;
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe('CustomersListPage', () => {
  it('shows the loading state before the search resolves', async () => {
    mockedSearch.mockReturnValue(new Promise(() => {}));

    renderWithRouter(<CustomersListPage />, { route: '/customers' });

    expect(screen.getByTestId('customers-loading')).toBeInTheDocument();
  });

  it('shows the error state and lets the user retry when the search fails', async () => {
    mockedSearch.mockRejectedValue(new ApiError({ status: 500, title: 'Something went wrong. Please try again.' }));

    renderWithRouter(<CustomersListPage />, { route: '/customers' });

    expect(await screen.findByTestId('customers-error')).toBeInTheDocument();
  });

  it('shows the empty state when there are no customers', async () => {
    mockedSearch.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 25 });

    renderWithRouter(<CustomersListPage />, { route: '/customers' });

    expect(await screen.findByTestId('customers-empty')).toBeInTheDocument();
  });

  it('renders the populated list and re-searches as the user types', async () => {
    mockedSearch.mockResolvedValue({
      items: [makeItem()],
      totalCount: 1,
      page: 1,
      pageSize: 25,
    });

    renderWithRouter(<CustomersListPage />, { route: '/customers' });

    expect(await screen.findByTestId('customer-row-c-1')).toHaveTextContent('Amir Haddad');
    expect(screen.getByText('2 vehicles')).toBeInTheDocument();

    mockedSearch.mockResolvedValue({
      items: [makeItem({ id: 'c-2', firstName: 'Nadia', lastName: 'Fares' })],
      totalCount: 1,
      page: 1,
      pageSize: 25,
    });

    await userEvent.type(screen.getByTestId('customer-search-input'), 'Nadia');

    await waitFor(() =>
      expect(mockedSearch).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'Nadia' })),
    );
    expect(await screen.findByTestId('customer-row-c-2')).toHaveTextContent('Nadia Fares');
  });

  it('creates a customer and navigates to their detail page on success', async () => {
    mockedSearch.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 25 });
    const created: CustomerDto = {
      id: 'c-new',
      firstName: 'Sami',
      lastName: null,
      phone: '+961 3 999 000',
      whatsapp: null,
      email: null,
      notes: null,
      isFleet: false,
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    };
    mockedCreate.mockResolvedValue(created);

    render(
      <MemoryRouter initialEntries={['/customers']}>
        <Routes>
          <Route path="/customers" element={<CustomersListPage />} />
          <Route path="/customers/:id" element={<DetailStub />} />
        </Routes>
      </MemoryRouter>,
    );

    await screen.findByTestId('customers-empty');
    await userEvent.click(screen.getByTestId('new-customer-button'));
    await userEvent.type(screen.getByLabelText(/first name/i), 'Sami');
    await userEvent.type(screen.getByLabelText(/^phone$/i), '+961 3 999 000');
    await userEvent.click(screen.getByRole('button', { name: /create customer/i }));

    expect(await screen.findByTestId('detail-stub')).toHaveTextContent('c-new');
  });
});
