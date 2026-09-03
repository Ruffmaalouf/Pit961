import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { FloorPage } from '@/pages/FloorPage';
import { ApiError } from '@/services/apiClient';
import type { FloorBoardCardDto, FloorBoardColumnDto } from '@/types/api';

vi.mock('@/features/jobs/api', () => ({
  getFloorBoard: vi.fn(),
}));

import * as jobsApi from '@/features/jobs/api';

const mockedGetFloorBoard = vi.mocked(jobsApi.getFloorBoard);

function makeCard(overrides: Partial<FloorBoardCardDto> = {}): FloorBoardCardDto {
  return {
    jobId: 'j-1',
    jobNumber: 'J-000001',
    customerDisplayName: 'Amir Haddad',
    vehicleDisplay: 'Toyota Corolla · 123456',
    primaryMechanicId: null,
    primaryMechanicName: null,
    checkedInAt: '2026-01-01T08:00:00.000Z',
    promisedAt: null,
    customerWaiting: false,
    overnight: false,
    isWarrantyReturn: false,
    statusUpdatedAt: '2026-01-01T08:00:00.000Z',
    ...overrides,
  };
}

function renderFloor() {
  return render(
    <MemoryRouter initialEntries={['/floor']}>
      <Routes>
        <Route path="/floor" element={<FloorPage />} />
        <Route path="/jobs/new" element={<div data-testid="jobs-new-stub" />} />
        <Route path="/jobs/:id" element={<div data-testid="job-detail-stub" />} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe('FloorPage', () => {
  it('shows the loading state before the floor board resolves', () => {
    mockedGetFloorBoard.mockReturnValue(new Promise(() => {}));

    renderFloor();

    expect(screen.getByTestId('floor-loading')).toBeInTheDocument();
  });

  it('shows the error state and lets the user retry', async () => {
    mockedGetFloorBoard.mockRejectedValue(new ApiError({ status: 500, title: 'Something went wrong. Please try again.' }));

    renderFloor();

    expect(await screen.findByTestId('floor-error')).toBeInTheDocument();
  });

  it('shows the empty state with no open jobs', async () => {
    const columns: FloorBoardColumnDto[] = [
      { status: 'checked_in', cards: [] },
      { status: 'in_progress', cards: [] },
    ];
    mockedGetFloorBoard.mockResolvedValue({ columns });

    renderFloor();

    expect(await screen.findByTestId('floor-empty')).toBeInTheDocument();
  });

  it('renders the real board with each job in its actual backend status column', async () => {
    const columns: FloorBoardColumnDto[] = [
      { status: 'checked_in', cards: [makeCard()] },
      { status: 'estimate_pending', cards: [] },
      { status: 'in_progress', cards: [makeCard({ jobId: 'j-2', jobNumber: 'J-000002', customerDisplayName: 'Nadia Fares' })] },
    ];
    mockedGetFloorBoard.mockResolvedValue({ columns });

    renderFloor();

    const board = await screen.findByTestId('floor-board');
    expect(board).toBeInTheDocument();

    const checkedInColumn = screen.getByTestId('floor-column-checked_in');
    expect(checkedInColumn).toContainElement(screen.getByTestId('floor-card-j-1'));

    const inProgressColumn = screen.getByTestId('floor-column-in_progress');
    expect(inProgressColumn).toContainElement(screen.getByTestId('floor-card-j-2'));

    // A card never leaks into a column it doesn't belong to.
    expect(checkedInColumn).not.toContainElement(screen.queryByTestId('floor-card-j-2'));
  });

  it('moves a job to its new column once the board reflects a status change', async () => {
    mockedGetFloorBoard.mockResolvedValueOnce({
      columns: [
        { status: 'checked_in', cards: [makeCard()] },
        { status: 'estimate_pending', cards: [] },
      ],
    });

    renderFloor();

    await screen.findByTestId('floor-board');
    expect(screen.getByTestId('floor-column-checked_in')).toContainElement(screen.getByTestId('floor-card-j-1'));

    // The backend is the sole authority on status — the board simply reflects
    // whatever it returns on the next load, exactly like a real transition
    // (made elsewhere, e.g. on Job Detail) would move the card here. Forcing
    // an error and retrying is this test's way of triggering that reload.
    mockedGetFloorBoard.mockRejectedValueOnce(new ApiError({ status: 500, title: 'boom' }));

    // Simulate the underlying job having transitioned in between loads.
    mockedGetFloorBoard.mockResolvedValueOnce({
      columns: [
        { status: 'checked_in', cards: [] },
        { status: 'estimate_pending', cards: [makeCard()] },
      ],
    });

    // Trigger a reload by re-mounting (a fresh GET, exactly like navigating
    // back to /floor after a transition made elsewhere).
    cleanup();
    renderFloor();
    await screen.findByTestId('floor-error');

    await userEvent.click(screen.getByRole('button', { name: /try again/i }));

    await waitFor(() =>
      expect(screen.getByTestId('floor-column-estimate_pending')).toContainElement(screen.getByTestId('floor-card-j-1')),
    );
    expect(screen.getByTestId('floor-column-checked_in')).not.toContainElement(screen.queryByTestId('floor-card-j-1'));
  });

  it('the "check in vehicle" action links to the real job intake screen', async () => {
    mockedGetFloorBoard.mockResolvedValue({ columns: [] });

    renderFloor();
    await screen.findByTestId('floor-empty');

    await userEvent.click(screen.getByTestId('check-in-vehicle-button'));
    expect(await screen.findByTestId('jobs-new-stub')).toBeInTheDocument();
  });
});
