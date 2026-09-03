import { expect, test } from '@playwright/test';
import { SEEDED_DEV_USER } from './fixtures';

/**
 * Milestone 1 (P2-WP2/P2-WP3 frontend) real end-to-end journey.
 *
 * Runs against a real PostgreSQL-backed backend and a real built/served
 * frontend — no mocks. Exercises exactly the workflow the Owner asked to be
 * able to click through: Login -> Customers -> Create Customer -> Open
 * Customer -> Add Vehicle -> Create Job -> see the real server-generated Job
 * number -> open Floor -> confirm the Job appears -> open the Job -> perform
 * one valid status transition and confirm the board reflects it.
 *
 * Every value that must be unique (customer name/phone, plate number) is
 * suffixed with a run-local timestamp so repeated runs against a persistent
 * dev database never collide with previous runs' data.
 */

async function login(page: import('@playwright/test').Page) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(SEEDED_DEV_USER.email);
  await page.getByLabel('Password').fill(SEEDED_DEV_USER.password);
  await page.getByRole('button', { name: 'Log in' }).click();
  await expect(page).toHaveURL(/\/floor$/);
}

test.describe('Milestone 1 — real Customer -> Vehicle -> Job -> Floor journey', () => {
  test('a real customer, vehicle and job can be created and are visible on the real Floor Board', async ({
    page,
  }) => {
    const stamp = Date.now();
    const firstName = 'E2E';
    const lastName = `Customer${stamp}`;
    const phone = `+961 3 ${String(stamp).slice(-6)}`;
    const plateNumber = `E2E${stamp}`;

    await login(page);

    // --- Customers ---------------------------------------------------
    await page.getByTestId('nav-item-customers').click();
    await expect(page).toHaveURL(/\/customers$/);
    await expect(page.getByTestId('customer-search-input')).toBeVisible();

    await page.getByTestId('new-customer-button').click();
    await page.getByLabel(/first name/i).fill(firstName);
    await page.getByLabel(/^last name$/i).fill(lastName);
    await page.getByLabel(/^phone$/i).fill(phone);

    const createCustomerResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/api/v1/customers') &&
        response.request().method() === 'POST' &&
        response.status() === 201,
    );
    await page.getByRole('button', { name: /create customer/i }).click();
    await createCustomerResponse;

    // Saving navigates straight to the new customer's real detail page.
    await expect(page).toHaveURL(/\/customers\/[0-9a-f-]{36}$/);
    await expect(page.getByRole('heading', { name: `${firstName} ${lastName}` })).toBeVisible();

    // --- Add a real vehicle -------------------------------------------
    await page.getByTestId('add-vehicle-button').click();
    await page.getByLabel(/plate number/i).fill(plateNumber);
    await page.getByLabel(/plate country/i).fill('LB');
    await page.getByLabel(/^make$/i).fill('Toyota');
    await page.getByLabel(/^model$/i).fill('Corolla');

    const createVehicleResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/api/v1/vehicles') &&
        response.request().method() === 'POST' &&
        response.status() === 201,
    );
    await page.getByRole('button', { name: /add vehicle/i }).click();
    await createVehicleResponse;

    // A freshly generated plate should not collide with anything real on
    // file, so the modal closes on its own (no duplicate warning).
    await expect(page.getByTestId('duplicate-plate-warning')).toHaveCount(0);
    await expect(page.getByText(plateNumber)).toBeVisible();

    // --- Create a real Job for that customer/vehicle -------------------
    await page.getByTestId(/^new-job-for-vehicle-/).click();
    await expect(page).toHaveURL(/\/jobs\/new/);
    await expect(page.getByTestId('selected-customer')).toContainText(firstName);

    await page.getByLabel(/customer complaint/i).fill('E2E: brakes squeak on cold start');

    const createJobResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/api/v1/jobs') &&
        response.request().method() === 'POST' &&
        response.status() === 201,
    );
    await page.getByTestId('create-job-submit').click();
    const jobResponse = await createJobResponse;
    const createdJob = (await jobResponse.json()) as { id: string; jobNumber: string };

    // --- Real server-generated Job number ------------------------------
    await expect(page).toHaveURL(new RegExp(`/jobs/${createdJob.id}$`));
    expect(createdJob.jobNumber).toMatch(/^J-\d+$/);
    await expect(page.getByText(createdJob.jobNumber)).toBeVisible();
    await expect(page.getByText('Checked In')).toBeVisible();

    // --- Floor: the job is really there, no seeded/fake data -----------
    await page.getByTestId('nav-item-floor').click();
    await expect(page).toHaveURL(/\/floor$/);
    await expect(page.getByTestId('floor-board')).toBeVisible();

    const floorCard = page.getByTestId(`floor-card-${createdJob.id}`);
    await expect(floorCard).toBeVisible();
    await expect(page.getByTestId('floor-column-checked_in')).toContainText(createdJob.jobNumber);

    // --- Open the job back from the Floor card --------------------------
    await floorCard.click();
    await expect(page).toHaveURL(new RegExp(`/jobs/${createdJob.id}$`));
    await expect(page.getByTestId('job-detail')).toBeVisible();

    // --- One valid, deterministic status transition ---------------------
    const transitionResponse = page.waitForResponse(
      (response) =>
        response.url().includes(`/api/v1/jobs/${createdJob.id}/status-transitions`) &&
        response.request().method() === 'POST',
    );
    await page.getByTestId('transition-estimate_pending').click();
    const transitionResult = await transitionResponse;
    expect(transitionResult.status()).toBeLessThan(300);
    await expect(page.getByText('Estimate Pending')).toBeVisible();

    // --- The board reflects the transition ------------------------------
    await page.getByTestId('nav-item-floor').click();
    await expect(page).toHaveURL(/\/floor$/);
    await expect(page.getByTestId('floor-board')).toBeVisible();

    await expect(page.getByTestId('floor-column-estimate_pending')).toContainText(createdJob.jobNumber);
    const checkedInColumn = page.getByTestId('floor-column-checked_in');
    await expect(checkedInColumn.getByTestId(`floor-card-${createdJob.id}`)).toHaveCount(0);
  });
});
