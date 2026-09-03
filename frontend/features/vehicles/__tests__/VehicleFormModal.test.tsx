import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { VehicleFormModal } from '@/features/vehicles/VehicleFormModal';
import { ApiError } from '@/services/apiClient';
import type { VehicleDto, VehicleMutationResponse } from '@/types/api';

vi.mock('@/features/vehicles/api', () => ({
  createVehicle: vi.fn(),
  updateVehicle: vi.fn(),
}));

import * as vehiclesApi from '@/features/vehicles/api';

const mockedCreate = vi.mocked(vehiclesApi.createVehicle);
const mockedUpdate = vi.mocked(vehiclesApi.updateVehicle);

function makeVehicle(overrides: Partial<VehicleDto> = {}): VehicleDto {
  return {
    id: 'v-1',
    customerId: 'c-1',
    plateNumber: '123456',
    plateCountry: 'LB',
    make: 'Toyota',
    model: 'Corolla',
    year: 2018,
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
    ...overrides,
  };
}

function withNoDuplicate(vehicle: VehicleDto): VehicleMutationResponse {
  return { vehicle, duplicateWarning: { hasDuplicates: false, matches: [] } };
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe('VehicleFormModal', () => {
  it('blocks submission and shows field errors when required fields are empty', async () => {
    render(<VehicleFormModal open onOpenChange={vi.fn()} customerId="c-1" onSaved={vi.fn()} />);

    await userEvent.click(screen.getByRole('button', { name: /add vehicle/i }));

    expect(await screen.findByText(/plate number is required/i)).toBeInTheDocument();
    expect(screen.getByText(/plate country is required/i)).toBeInTheDocument();
    expect(screen.getByText(/^make is required\.$/i)).toBeInTheDocument();
    expect(screen.getByText(/model is required/i)).toBeInTheDocument();
    expect(mockedCreate).not.toHaveBeenCalled();
  });

  it('adds a vehicle scoped to the given customer and closes on success (no duplicate)', async () => {
    const vehicle = makeVehicle();
    mockedCreate.mockResolvedValue(withNoDuplicate(vehicle));
    const onSaved = vi.fn();
    const onOpenChange = vi.fn();

    render(<VehicleFormModal open onOpenChange={onOpenChange} customerId="c-1" onSaved={onSaved} />);

    await userEvent.type(screen.getByLabelText(/plate number/i), '123456');
    await userEvent.type(screen.getByLabelText(/plate country/i), 'LB');
    await userEvent.type(screen.getByLabelText(/^make$/i), 'Toyota');
    await userEvent.type(screen.getByLabelText(/^model$/i), 'Corolla');
    await userEvent.click(screen.getByRole('button', { name: /add vehicle/i }));

    await waitFor(() => expect(onSaved).toHaveBeenCalledWith(vehicle));
    expect(mockedCreate).toHaveBeenCalledWith(
      expect.objectContaining({ customerId: 'c-1', plateNumber: '123456', make: 'Toyota', model: 'Corolla' }),
    );
    expect(onOpenChange).toHaveBeenCalledWith(false);
    expect(screen.queryByTestId('duplicate-plate-warning')).toBeNull();
  });

  it('shows the duplicate-plate warning per Owner Decision #5 and keeps the save (does not block)', async () => {
    const vehicle = makeVehicle();
    mockedCreate.mockResolvedValue({
      vehicle,
      duplicateWarning: {
        hasDuplicates: true,
        matches: [
          {
            vehicleId: 'v-existing',
            customerId: 'c-other',
            customerName: 'Nadia Fares',
            plateNumber: '123456',
            plateCountry: 'LB',
            make: 'Toyota',
            model: 'Corolla',
          },
        ],
      },
    });
    const onSaved = vi.fn();
    const onOpenChange = vi.fn();

    render(<VehicleFormModal open onOpenChange={onOpenChange} customerId="c-1" onSaved={onSaved} />);

    await userEvent.type(screen.getByLabelText(/plate number/i), '123456');
    await userEvent.type(screen.getByLabelText(/plate country/i), 'LB');
    await userEvent.type(screen.getByLabelText(/^make$/i), 'Toyota');
    await userEvent.type(screen.getByLabelText(/^model$/i), 'Corolla');
    await userEvent.click(screen.getByRole('button', { name: /add vehicle/i }));

    // The save already succeeded — onSaved fires and the record persists —
    // the modal just stays open with a warning instead of auto-closing.
    await waitFor(() => expect(onSaved).toHaveBeenCalledWith(vehicle));
    const warning = await screen.findByTestId('duplicate-plate-warning');
    expect(warning).toHaveTextContent('Nadia Fares');
    expect(onOpenChange).not.toHaveBeenCalledWith(false);

    await userEvent.click(screen.getByRole('button', { name: /done/i }));
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it('pre-fills the form and calls updateVehicle when editing an existing vehicle', async () => {
    const existing = makeVehicle({ make: 'Honda', model: 'Civic', year: 2020 });
    mockedUpdate.mockResolvedValue(withNoDuplicate(existing));
    const onSaved = vi.fn();

    render(<VehicleFormModal open onOpenChange={vi.fn()} customerId="c-1" vehicle={existing} onSaved={onSaved} />);

    expect(screen.getByLabelText(/^make$/i)).toHaveValue('Honda');
    expect(screen.getByLabelText(/^model$/i)).toHaveValue('Civic');

    await userEvent.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() =>
      expect(mockedUpdate).toHaveBeenCalledWith('v-1', expect.objectContaining({ make: 'Honda', model: 'Civic' })),
    );
    expect(onSaved).toHaveBeenCalledWith(existing);
  });

  it('shows the server ProblemDetails title in the error banner when the save fails', async () => {
    mockedCreate.mockRejectedValue(new ApiError({ status: 400, title: 'Vehicle year is out of range.' }));

    render(<VehicleFormModal open onOpenChange={vi.fn()} customerId="c-1" onSaved={vi.fn()} />);

    await userEvent.type(screen.getByLabelText(/plate number/i), '123456');
    await userEvent.type(screen.getByLabelText(/plate country/i), 'LB');
    await userEvent.type(screen.getByLabelText(/^make$/i), 'Toyota');
    await userEvent.type(screen.getByLabelText(/^model$/i), 'Corolla');
    await userEvent.click(screen.getByRole('button', { name: /add vehicle/i }));

    const banner = await screen.findByTestId('form-error-banner');
    expect(banner).toHaveTextContent('Vehicle year is out of range.');
  });
});
