import { expect, test } from '@playwright/test';
import { SEEDED_DEV_MANAGER, SEEDED_DEV_USER } from './fixtures';

/**
 * Milestone 2 (P2-WP4 frontend) real end-to-end journey.
 *
 * Runs against a real PostgreSQL-backed backend and a real built/served
 * frontend — no mocks, same one-command real-stack orchestration as
 * Milestone 1's journey spec. Two real logins are used (the dev seed shares
 * one password across all seeded users — DevelopmentSeeder.DevSeedPassword —
 * so both are real accounts, not fixtures):
 *
 *  - Owner (Ralph): Login -> Customer -> Vehicle -> Job -> Estimate -> add a
 *    line item -> apply a >15% discount (Owner is unrestricted) -> submit
 *    -> land on the real $500 owner-approval threshold's PENDING OWNER
 *    APPROVAL state -> clear it as Owner -> record the customer's separate
 *    approval -> create a revised quote (re-quote).
 *  - Manager (Rima Haddad): Login -> Customer -> Vehicle -> Job -> Estimate
 *    -> the 15% discount cap is genuinely enforced in the browser (warned +
 *    disabled above it, a real backend-accepted write at exactly 15%) ->
 *    submitting an above-threshold estimate lands on the same PENDING OWNER
 *    APPROVAL state, but the Clear action is not offered to a Manager.
 *
 * The exact 15.00%/15.01% boundary and the Manager-gets-403-if-forced case
 * are proven once, precisely, at the HTTP layer by backend/GarageOS.Tests/
 * GarageOS.Tests.Integration/Estimates/EstimatesApiTests.cs and at the
 * component layer by frontend/features/estimates/__tests__/
 * EstimateSection.test.tsx; this spec's job is only to prove the real
 * click-through against a real running stack, not to re-litigate the
 * boundary itself.
 */

async function login(
  page: import('@playwright/test').Page,
  credentials: { email: string; password: string } = SEEDED_DEV_USER,
) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(credentials.email);
  await page.getByLabel('Password').fill(credentials.password);
  await page.getByRole('button', { name: 'Log in' }).click();
  await expect(page).toHaveURL(/\/floor$/);
}

/** Creates a real Customer -> Vehicle -> Job and lands on the Job Detail
 * page, returning the created Job. Shared by both the Owner and Manager
 * journeys below so each starts from its own fresh, real records. */
async function createCustomerVehicleJob(
  page: import('@playwright/test').Page,
  namePrefix: string,
): Promise<{ id: string; jobNumber: string }> {
  const stamp = Date.now();
  const firstName = 'E2E';
  const lastName = `${namePrefix}${stamp}`;
  const phone = `+961 3 ${String(stamp).slice(-6)}`;
  const plateNumber = `${namePrefix.slice(0, 3).toUpperCase()}${stamp}`;

  await page.getByTestId('nav-item-customers').click();
  await page.getByTestId('new-customer-button').click();
  await page.getByLabel(/first name/i).fill(firstName);
  await page.getByLabel(/^last name$/i).fill(lastName);
  await page.getByLabel(/^phone$/i).fill(phone);
  const createCustomerResponse = page.waitForResponse(
    (r) => r.url().includes('/api/v1/customers') && r.request().method() === 'POST' && r.status() === 201,
  );
  await page.getByRole('button', { name: /create customer/i }).click();
  await createCustomerResponse;
  await expect(page).toHaveURL(/\/customers\/[0-9a-f-]{36}$/);

  await page.getByTestId('add-vehicle-button').click();
  await page.getByLabel(/plate number/i).fill(plateNumber);
  await page.getByLabel(/plate country/i).fill('LB');
  await page.getByLabel(/^make$/i).fill('Honda');
  await page.getByLabel(/^model$/i).fill('Civic');
  const createVehicleResponse = page.waitForResponse(
    (r) => r.url().includes('/api/v1/vehicles') && r.request().method() === 'POST' && r.status() === 201,
  );
  await page.getByRole('button', { name: /add vehicle/i }).click();
  await createVehicleResponse;

  await page.getByTestId(/^new-job-for-vehicle-/).click();
  await expect(page).toHaveURL(/\/jobs\/new/);
  await page.getByLabel(/customer complaint/i).fill(`E2E: ${namePrefix} estimate journey`);
  const createJobResponse = page.waitForResponse(
    (r) => r.url().includes('/api/v1/jobs') && r.request().method() === 'POST' && r.status() === 201,
  );
  await page.getByTestId('create-job-submit').click();
  const jobResponse = await createJobResponse;
  const createdJob = (await jobResponse.json()) as { id: string; jobNumber: string };
  await expect(page).toHaveURL(new RegExp(`/jobs/${createdJob.id}$`));
  return createdJob;
}

test.describe('Milestone 2 — real Estimate/Money workflow journey', () => {
  test('Owner: a real Estimate can be created, discounted, routed through owner approval, customer-approved and re-quoted', async ({
    page,
  }) => {
    await login(page, SEEDED_DEV_USER);
    await createCustomerVehicleJob(page, 'Estimate');

    // --- Create the Estimate: one line item, subtotal above the $500 -----
    // owner-approval threshold so the pending-owner-approval path is a real,
    // server-driven outcome, not a hardcoded UI state.
    await expect(page.getByTestId('estimate-empty-state')).toBeVisible();
    await page.getByTestId('start-create-estimate').click();
    await page.getByTestId('item-description-0').fill('Full brake job — pads, rotors, labor');
    await page.getByTestId('item-quantity-0').fill('1');
    await page.getByTestId('item-unit-cost-0').fill('300');
    await page.getByTestId('item-unit-price-0').fill('600');

    const createEstimateResponse = page.waitForResponse(
      (r) => r.url().includes('/api/v1/estimates') && r.request().method() === 'POST' && r.status() === 201,
    );
    await page.getByTestId('submit-create-estimate').click();
    const estimateResponse = await createEstimateResponse;
    const createdEstimate = (await estimateResponse.json()) as { id: string; subtotal: number };
    expect(createdEstimate.subtotal).toBe(600);
    await expect(page.getByTestId('estimate-subtotal')).toHaveText('$600.00');

    // --- Owner applies a discount above the 15% Manager cap --------------
    // (Owner is unrestricted.)
    await page.getByTestId('discount-percent-input').fill('20');
    const discountResponse = page.waitForResponse(
      (r) => r.url().includes(`/api/v1/estimates/${createdEstimate.id}/discount`) && r.request().method() === 'POST',
    );
    await page.getByTestId('apply-discount').click();
    const discountResult = await discountResponse;
    expect(discountResult.status()).toBe(200);
    await expect(page.getByTestId('estimate-discount')).toHaveText('-$120.00'); // 600 * 20%
    await expect(page.getByTestId('estimate-total')).toHaveText('$480.00');

    // --- Submit: subtotal (pre-discount, $600) is above $500, so this
    // real backend call routes to pending_owner_approval regardless of the
    // discount just applied or the actor's role (EstimateApprovalThresholdHandler
    // is role-blind by design). ---------------------------------------------
    const submitResponse = page.waitForResponse(
      (r) => r.url().includes(`/api/v1/estimates/${createdEstimate.id}/submit`) && r.request().method() === 'POST',
    );
    await page.getByTestId('submit-for-approval').click();
    const submitResult = await submitResponse;
    expect(submitResult.status()).toBe(200);
    expect(((await submitResult.json()) as { status: string }).status).toBe('pending_owner_approval');
    await expect(page.getByTestId('pending-owner-approval-banner')).toContainText('PENDING OWNER APPROVAL');

    // --- Owner clears it (Owner Decision #2 — logged in as Ralph, owner) -
    const clearResponse = page.waitForResponse(
      (r) =>
        r.url().includes(`/api/v1/estimates/${createdEstimate.id}/clear-owner-approval`) &&
        r.request().method() === 'POST',
    );
    await page.getByTestId('clear-owner-approval').click();
    const clearResult = await clearResponse;
    expect(clearResult.status()).toBe(200);
    await expect(page.getByTestId('pending-owner-approval-banner')).toHaveCount(0);
    await expect(page.getByText('Sent', { exact: true })).toBeVisible();

    // --- Customer approval is recorded separately from Owner approval ----
    await page.getByTestId('customer-decision-select').selectOption('approved');
    await page.getByTestId('customer-method-select').selectOption('whatsapp');
    await page.getByTestId('customer-name-input').fill('E2E Customer');
    const customerApprovalResponse = page.waitForResponse(
      (r) =>
        r.url().includes(`/api/v1/estimates/${createdEstimate.id}/customer-approval`) &&
        r.request().method() === 'POST',
    );
    await page.getByTestId('record-customer-approval').click();
    const customerApprovalResult = await customerApprovalResponse;
    expect(customerApprovalResult.status()).toBe(200);
    await expect(page.getByTestId('customer-approval-status')).toContainText('whatsapp');

    // --- Re-quote: create a new revision, superseding this one -----------
    const revisionResponse = page.waitForResponse(
      (r) =>
        r.url().includes(`/api/v1/estimates/${createdEstimate.id}/revisions`) && r.request().method() === 'POST',
    );
    await page.getByTestId('create-revision').click();
    const revisionResult = await revisionResponse;
    expect(revisionResult.status()).toBe(201);
    const revision = (await revisionResult.json()) as { id: string; revisionNumber: number; status: string };
    expect(revision.revisionNumber).toBe(2);
    expect(revision.status).toBe('draft');

    await expect(page.getByTestId(`estimate-${revision.id}`)).toBeVisible();
    await expect(page.getByTestId('superseded-revisions')).toBeVisible();
    await expect(page.getByTestId(`superseded-revision-${createdEstimate.id}`)).toContainText('SUPERSEDED');
  });

  test('Manager: the 15% discount cap and the Owner-only clear-approval gate are real in the browser, against the real backend', async ({
    page,
  }) => {
    await login(page, SEEDED_DEV_MANAGER);
    await createCustomerVehicleJob(page, 'MgrCap');

    await expect(page.getByTestId('estimate-empty-state')).toBeVisible();
    await page.getByTestId('start-create-estimate').click();
    await page.getByTestId('item-description-0').fill('Timing belt + water pump');
    await page.getByTestId('item-quantity-0').fill('1');
    await page.getByTestId('item-unit-cost-0').fill('400');
    await page.getByTestId('item-unit-price-0').fill('600');
    const createEstimateResponse = page.waitForResponse(
      (r) => r.url().includes('/api/v1/estimates') && r.request().method() === 'POST' && r.status() === 201,
    );
    await page.getByTestId('submit-create-estimate').click();
    const createdEstimate = (await (await createEstimateResponse).json()) as { id: string };

    // --- Above the cap: the UI itself refuses to send the request --------
    await page.getByTestId('discount-percent-input').fill('20');
    await expect(page.getByTestId('manager-discount-cap-warning')).toBeVisible();
    await expect(page.getByTestId('apply-discount')).toBeDisabled();

    // --- At exactly the cap: a real 200 from the real backend ------------
    await page.getByTestId('discount-percent-input').fill('15');
    await expect(page.getByTestId('apply-discount')).toBeEnabled();
    const discountResponse = page.waitForResponse(
      (r) => r.url().includes(`/api/v1/estimates/${createdEstimate.id}/discount`) && r.request().method() === 'POST',
    );
    await page.getByTestId('apply-discount').click();
    const discountResult = await discountResponse;
    expect(discountResult.status()).toBe(200);
    await expect(page.getByTestId('estimate-discount')).toHaveText('-$90.00'); // 600 * 15%

    // --- Submit lands on the same real pending-owner-approval state, but
    // the Manager is never offered the Clear action. ----------------------
    const submitResponse = page.waitForResponse(
      (r) => r.url().includes(`/api/v1/estimates/${createdEstimate.id}/submit`) && r.request().method() === 'POST',
    );
    await page.getByTestId('submit-for-approval').click();
    expect((await submitResponse).status()).toBe(200);
    await expect(page.getByTestId('pending-owner-approval-banner')).toContainText('PENDING OWNER APPROVAL');
    await expect(page.getByTestId('owner-only-notice')).toBeVisible();
    await expect(page.getByTestId('clear-owner-approval')).toHaveCount(0);
  });
});
