import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { EstimateSection } from '@/features/estimates/EstimateSection';
import { ApiError } from '@/services/apiClient';
import { resetAuthStore, useAuthStore } from '@/stores/authStore';
import type { AuthUser, EstimateDto } from '@/types/api';

vi.mock('@/features/estimates/api', () => ({
  listEstimatesByJob: vi.fn(),
  createEstimate: vi.fn(),
  replaceEstimateItems: vi.fn(),
  applyDiscount: vi.fn(),
  submitEstimate: vi.fn(),
  clearOwnerApproval: vi.fn(),
  recordCustomerApproval: vi.fn(),
  createEstimateRevision: vi.fn(),
}));

import * as estimatesApi from '@/features/estimates/api';

const mockedList = vi.mocked(estimatesApi.listEstimatesByJob);
const mockedCreate = vi.mocked(estimatesApi.createEstimate);
const mockedReplaceItems = vi.mocked(estimatesApi.replaceEstimateItems);
const mockedApplyDiscount = vi.mocked(estimatesApi.applyDiscount);
const mockedSubmit = vi.mocked(estimatesApi.submitEstimate);
const mockedClearOwnerApproval = vi.mocked(estimatesApi.clearOwnerApproval);
const mockedRecordCustomerApproval = vi.mocked(estimatesApi.recordCustomerApproval);
const mockedCreateRevision = vi.mocked(estimatesApi.createEstimateRevision);

function makeUser(role: string): AuthUser {
  return { id: 'u-1', garageId: 'g-1', garageName: 'Test Garage', email: 'u@example.com', name: 'Test User', role };
}

function makeEstimate(overrides: Partial<EstimateDto> = {}): EstimateDto {
  return {
    id: 'e-1',
    jobId: 'j-1',
    type: 'standard',
    parentEstimateId: null,
    revisionNumber: 1,
    status: 'draft',
    approvalMethod: null,
    approvedByName: null,
    approvedAt: null,
    sentAt: null,
    subtotal: 300,
    taxAmount: 0,
    discountAmount: 0,
    total: 300,
    notes: null,
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    items: [
      {
        id: 'i-1',
        type: 'part',
        description: 'Brake pads',
        partNumber: null,
        quantity: 2,
        unitCost: 50,
        unitPrice: 150,
        approvalStatus: 'pending',
        sortOrder: 0,
      },
    ],
    ...overrides,
  };
}

function setRole(role: string) {
  resetAuthStore('authenticated');
  useAuthStore.getState().setUser(makeUser(role));
}

beforeEach(() => {
  setRole('manager');
});

afterEach(() => {
  cleanup();
});

describe('EstimateSection', () => {
  it('shows a loading state while fetching', () => {
    mockedList.mockReturnValue(new Promise(() => {}));
    render(<EstimateSection jobId="j-1" />);
    expect(screen.getByTestId('estimate-section-loading')).toBeInTheDocument();
  });

  it('shows an error state with retry on load failure', async () => {
    mockedList.mockRejectedValueOnce(new ApiError({ status: 500, title: 'Something went wrong. Please try again.' }));
    mockedList.mockResolvedValueOnce([]);
    render(<EstimateSection jobId="j-1" />);

    expect(await screen.findByTestId('estimate-section-error')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /try again/i }));
    expect(await screen.findByTestId('estimate-empty-state')).toBeInTheDocument();
  });

  it('shows an empty state and creates a new estimate from entered line items', async () => {
    mockedList.mockResolvedValue([]);
    mockedCreate.mockResolvedValue(makeEstimate());
    render(<EstimateSection jobId="j-1" />);

    expect(await screen.findByTestId('estimate-empty-state')).toBeInTheDocument();

    await userEvent.click(screen.getByTestId('start-create-estimate'));
    const descriptionInput = screen.getByTestId('item-description-0');
    await userEvent.type(descriptionInput, 'Brake pads');
    await userEvent.clear(screen.getByTestId('item-quantity-0'));
    await userEvent.type(screen.getByTestId('item-quantity-0'), '2');
    await userEvent.clear(screen.getByTestId('item-unit-price-0'));
    await userEvent.type(screen.getByTestId('item-unit-price-0'), '150');

    await userEvent.click(screen.getByTestId('submit-create-estimate'));

    await waitFor(() => expect(mockedCreate).toHaveBeenCalledTimes(1));
    const payload = mockedCreate.mock.calls[0][0];
    expect(payload.jobId).toBe('j-1');
    expect(payload.items).toEqual([
      expect.objectContaining({ description: 'Brake pads', quantity: 2, unitPrice: 150, sortOrder: 0 }),
    ]);
    // Subtotal/Total/DiscountAmount/Status are never client-supplied fields.
    expect(payload).not.toHaveProperty('subtotal');
    expect(payload).not.toHaveProperty('total');
    expect(payload).not.toHaveProperty('status');

    expect(await screen.findByTestId('estimate-e-1')).toBeInTheDocument();
  });

  it('edits line items on an existing estimate via replace-items', async () => {
    mockedList.mockResolvedValue([makeEstimate()]);
    mockedReplaceItems.mockResolvedValue(makeEstimate({ subtotal: 450, total: 450 }));
    render(<EstimateSection jobId="j-1" />);

    await userEvent.click(await screen.findByTestId('edit-items'));
    await userEvent.clear(screen.getByTestId('item-quantity-0'));
    await userEvent.type(screen.getByTestId('item-quantity-0'), '3');
    await userEvent.click(screen.getByTestId('save-items'));

    await waitFor(() => expect(mockedReplaceItems).toHaveBeenCalledWith('e-1', {
      items: [expect.objectContaining({ quantity: 3, sortOrder: 0 })],
    }));
    expect(await screen.findByTestId('estimate-total')).toHaveTextContent('$450.00');
  });

  it('submits a draft for approval and reflects whichever status the server routed it to', async () => {
    mockedList.mockResolvedValue([makeEstimate({ status: 'draft' })]);
    mockedSubmit.mockResolvedValue(makeEstimate({ status: 'sent' }));
    render(<EstimateSection jobId="j-1" />);

    await userEvent.click(await screen.findByTestId('submit-for-approval'));

    await waitFor(() => expect(mockedSubmit).toHaveBeenCalledWith('e-1'));
    expect(await screen.findByText('Sent')).toBeInTheDocument();
    // Once sent, submitting again is no longer offered.
    expect(screen.queryByTestId('submit-for-approval')).not.toBeInTheDocument();
  });

  it('displays server-computed subtotal/discount/tax/total exactly as returned', async () => {
    mockedList.mockResolvedValue([
      makeEstimate({ subtotal: 1000, discountAmount: 150, taxAmount: 25, total: 875 }),
    ]);
    render(<EstimateSection jobId="j-1" />);

    expect(await screen.findByTestId('estimate-subtotal')).toHaveTextContent('$1000.00');
    expect(screen.getByTestId('estimate-discount')).toHaveTextContent('-$150.00');
    expect(screen.getByTestId('estimate-total')).toHaveTextContent('$875.00');
  });

  it('warns a Manager and disables Apply above the 15% discount cap, but not an Owner', async () => {
    mockedList.mockResolvedValue([makeEstimate()]);
    setRole('manager');
    render(<EstimateSection jobId="j-1" />);

    await userEvent.type(await screen.findByTestId('discount-percent-input'), '20');
    expect(screen.getByTestId('manager-discount-cap-warning')).toBeInTheDocument();
    expect(screen.getByTestId('apply-discount')).toBeDisabled();

    cleanup();
    setRole('owner');
    mockedList.mockResolvedValue([makeEstimate()]);
    render(<EstimateSection jobId="j-1" />);
    await userEvent.type(await screen.findByTestId('discount-percent-input'), '20');
    expect(screen.queryByTestId('manager-discount-cap-warning')).not.toBeInTheDocument();
    expect(screen.getByTestId('apply-discount')).not.toBeDisabled();
  });

  it('surfaces the backend\'s discount-rejection message rather than crashing', async () => {
    mockedList.mockResolvedValue([makeEstimate()]);
    mockedApplyDiscount.mockRejectedValue(new ApiError({ status: 403, title: 'exceeds_manager_cap' }));
    render(<EstimateSection jobId="j-1" />);

    await userEvent.type(await screen.findByTestId('discount-percent-input'), '10');
    await userEvent.click(screen.getByTestId('apply-discount'));

    expect(await screen.findByTestId('estimate-action-error')).toHaveTextContent('exceeds_manager_cap');
  });

  it('shows a distinct PENDING OWNER APPROVAL banner, with the clear action only for the Owner', async () => {
    mockedList.mockResolvedValue([makeEstimate({ status: 'pending_owner_approval', subtotal: 900, total: 900 })]);
    setRole('manager');
    render(<EstimateSection jobId="j-1" />);

    expect(await screen.findByTestId('pending-owner-approval-banner')).toHaveTextContent('PENDING OWNER APPROVAL');
    expect(screen.getByTestId('owner-only-notice')).toBeInTheDocument();
    expect(screen.queryByTestId('clear-owner-approval')).not.toBeInTheDocument();
  });

  it('lets the Owner clear pending owner approval, and Manager cannot perform that action', async () => {
    mockedList.mockResolvedValue([makeEstimate({ status: 'pending_owner_approval', subtotal: 900, total: 900 })]);
    mockedClearOwnerApproval.mockResolvedValue(makeEstimate({ status: 'sent', subtotal: 900, total: 900 }));
    setRole('owner');
    render(<EstimateSection jobId="j-1" />);

    const clearButton = await screen.findByTestId('clear-owner-approval');
    await userEvent.click(clearButton);

    await waitFor(() => expect(mockedClearOwnerApproval).toHaveBeenCalledWith('e-1'));
    await waitFor(() => expect(screen.queryByTestId('pending-owner-approval-banner')).not.toBeInTheDocument());
  });

  it('records a customer approval decision independently of Owner approval', async () => {
    mockedList.mockResolvedValue([makeEstimate({ status: 'sent' })]);
    mockedRecordCustomerApproval.mockResolvedValue(
      makeEstimate({ status: 'approved', approvalMethod: 'whatsapp', approvedByName: 'Jane', approvedAt: '2026-01-02T00:00:00.000Z' }),
    );
    render(<EstimateSection jobId="j-1" />);

    const section = within(await screen.findByTestId('customer-approval-section'));
    await userEvent.selectOptions(section.getByTestId('customer-method-select'), 'whatsapp');
    await userEvent.type(section.getByTestId('customer-name-input'), 'Jane');
    await userEvent.click(section.getByTestId('record-customer-approval'));

    await waitFor(() =>
      expect(mockedRecordCustomerApproval).toHaveBeenCalledWith('e-1', {
        decision: 'approved',
        approvalMethod: 'whatsapp',
        approvedByName: 'Jane',
      }),
    );
    expect(await screen.findByTestId('customer-approval-status')).toHaveTextContent('whatsapp');
  });

  it('hides items/discount editing once the estimate is no longer draft (P2-WP4 QA gate B1)', async () => {
    mockedList.mockResolvedValue([makeEstimate({ status: 'sent' })]);
    render(<EstimateSection jobId="j-1" />);

    await screen.findByTestId(`estimate-e-1`);
    expect(screen.queryByTestId('edit-items')).not.toBeInTheDocument();
    expect(screen.queryByTestId('discount-percent-input')).not.toBeInTheDocument();
    expect(screen.queryByTestId('apply-discount')).not.toBeInTheDocument();
  });

  it('only offers the customer-decision form while the estimate is "sent", never before or after a decision', async () => {
    // Not yet sent -- pending_owner_approval -- recording a decision is not offered.
    mockedList.mockResolvedValue([makeEstimate({ status: 'pending_owner_approval', subtotal: 900, total: 900 })]);
    setRole('owner');
    render(<EstimateSection jobId="j-1" />);
    await screen.findByTestId('customer-approval-section');
    expect(screen.queryByTestId('customer-decision-select')).not.toBeInTheDocument();
    expect(screen.queryByTestId('record-customer-approval')).not.toBeInTheDocument();
    cleanup();

    // Already decided -- the recorded summary shows, but the form to record another
    // decision over it does not (that would silently overwrite the customer's real answer).
    mockedList.mockResolvedValue([
      makeEstimate({
        status: 'approved',
        approvalMethod: 'whatsapp',
        approvedByName: 'Jane',
        approvedAt: '2026-01-02T00:00:00.000Z',
      }),
    ]);
    render(<EstimateSection jobId="j-1" />);
    expect(await screen.findByTestId('customer-approval-status')).toHaveTextContent('whatsapp');
    expect(screen.queryByTestId('customer-decision-select')).not.toBeInTheDocument();
    expect(screen.queryByTestId('record-customer-approval')).not.toBeInTheDocument();
  });

  it('creates a new revision, superseding the current one', async () => {
    mockedList.mockResolvedValue([makeEstimate({ status: 'sent' })]);
    mockedCreateRevision.mockResolvedValue(
      makeEstimate({ id: 'e-2', revisionNumber: 2, status: 'draft', parentEstimateId: 'e-1' }),
    );
    render(<EstimateSection jobId="j-1" />);

    await userEvent.click(await screen.findByTestId('create-revision'));

    await waitFor(() => expect(mockedCreateRevision).toHaveBeenCalledWith('e-1'));
    expect(await screen.findByTestId('estimate-e-2')).toBeInTheDocument();
    expect(screen.getByTestId('superseded-revisions')).toBeInTheDocument();
    expect(screen.getByTestId('superseded-revision-e-1')).toHaveTextContent('SUPERSEDED');
  });

  it('renders a superseded revision read-only, with no edit/discount/submit controls', async () => {
    mockedList.mockResolvedValue([
      makeEstimate({ id: 'e-1', revisionNumber: 1, status: 'superseded' }),
      makeEstimate({ id: 'e-2', revisionNumber: 2, status: 'sent', parentEstimateId: 'e-1' }),
    ]);
    render(<EstimateSection jobId="j-1" />);

    await screen.findByTestId('superseded-revisions');
    const supersededRow = screen.getByTestId('superseded-revision-e-1');
    expect(within(supersededRow).queryByRole('button')).not.toBeInTheDocument();

    // The active (non-superseded) revision is the one with live controls.
    expect(screen.getByTestId('estimate-e-2')).toBeInTheDocument();
    expect(screen.getByTestId('create-revision')).toBeInTheDocument();
  });
});
