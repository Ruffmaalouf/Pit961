import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CustomerFormModal } from '@/features/customers/CustomerFormModal';
import { ApiError } from '@/services/apiClient';
import type { CustomerDto } from '@/types/api';

vi.mock('@/features/customers/api', () => ({
  createCustomer: vi.fn(),
  updateCustomer: vi.fn(),
}));

import * as customersApi from '@/features/customers/api';

const mockedCreate = vi.mocked(customersApi.createCustomer);
const mockedUpdate = vi.mocked(customersApi.updateCustomer);

function makeCustomer(overrides: Partial<CustomerDto> = {}): CustomerDto {
  return {
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
    ...overrides,
  };
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe('CustomerFormModal', () => {
  it('blocks submission and shows field errors when required fields are empty', async () => {
    render(<CustomerFormModal open onOpenChange={vi.fn()} onSaved={vi.fn()} />);

    await userEvent.click(screen.getByRole('button', { name: /create customer/i }));

    expect(await screen.findByText(/first name is required/i)).toBeInTheDocument();
    expect(screen.getByText(/phone number is required/i)).toBeInTheDocument();
    expect(mockedCreate).not.toHaveBeenCalled();
  });

  it('creates a customer with the entered fields and closes on success', async () => {
    const saved = makeCustomer();
    mockedCreate.mockResolvedValue(saved);
    const onSaved = vi.fn();
    const onOpenChange = vi.fn();

    render(<CustomerFormModal open onOpenChange={onOpenChange} onSaved={onSaved} />);

    await userEvent.type(screen.getByLabelText(/first name/i), 'Amir');
    await userEvent.type(screen.getByLabelText(/^phone$/i), '+961 3 111 222');
    await userEvent.click(screen.getByRole('button', { name: /create customer/i }));

    await waitFor(() => expect(onSaved).toHaveBeenCalledWith(saved));
    expect(mockedCreate).toHaveBeenCalledWith(
      expect.objectContaining({ firstName: 'Amir', phone: '+961 3 111 222', isFleet: false }),
    );
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it('pre-fills the form and calls updateCustomer when editing an existing customer', async () => {
    const existing = makeCustomer({ firstName: 'Rana', lastName: 'Khoury', isFleet: true });
    mockedUpdate.mockResolvedValue(existing);
    const onSaved = vi.fn();

    render(<CustomerFormModal open onOpenChange={vi.fn()} customer={existing} onSaved={onSaved} />);

    expect(screen.getByLabelText(/first name/i)).toHaveValue('Rana');
    expect(screen.getByRole('checkbox', { name: /fleet account/i })).toBeChecked();

    await userEvent.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => expect(mockedUpdate).toHaveBeenCalledWith('c-1', expect.objectContaining({ firstName: 'Rana' })));
    expect(onSaved).toHaveBeenCalledWith(existing);
  });

  it('shows the server ProblemDetails title in the error banner when the save fails', async () => {
    mockedCreate.mockRejectedValue(new ApiError({ status: 422, title: 'A customer with this phone already exists.' }));

    render(<CustomerFormModal open onOpenChange={vi.fn()} onSaved={vi.fn()} />);

    await userEvent.type(screen.getByLabelText(/first name/i), 'Amir');
    await userEvent.type(screen.getByLabelText(/^phone$/i), '+961 3 111 222');
    await userEvent.click(screen.getByRole('button', { name: /create customer/i }));

    const banner = await screen.findByTestId('form-error-banner');
    expect(banner).toHaveTextContent('A customer with this phone already exists.');
  });
});
