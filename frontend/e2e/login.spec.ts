import { expect, test } from '@playwright/test';
import { API_BASE_URL, SEEDED_DEV_USER, type BrandingConfigResponse } from './fixtures';

test.describe('WP-8 login and authenticated shell', () => {
  test('app loads and shows the login screen', async ({ page }) => {
    await page.goto('/login');

    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
    await expect(page.getByLabel('Email')).toBeVisible();
    await expect(page.getByLabel('Password')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Log in' })).toBeVisible();

    // Deliberately absent from the approved design.
    await expect(page.getByText(/remember me/i)).toHaveCount(0);
    await expect(page.getByText(/forgot/i)).toHaveCount(0);
    await expect(page.getByText(/sign up/i)).toHaveCount(0);
  });

  test('real login with the seeded development account enters the authenticated shell', async ({
    page,
  }) => {
    await page.goto('/login');

    await page.getByLabel('Email').fill(SEEDED_DEV_USER.email);
    await page.getByLabel('Password').fill(SEEDED_DEV_USER.password);

    const loginResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/api/v1/auth/login') && response.request().method() === 'POST',
    );

    await page.getByRole('button', { name: 'Log in' }).click();

    expect((await loginResponse).status()).toBe(200);

    await expect(page).toHaveURL(/\/floor$/);
    await expect(page.getByTestId('floor-page')).toBeVisible();
    await expect(page.getByTestId('form-error-banner')).toHaveCount(0);
  });

  test('the authenticated shell renders the sidebar rail and header after a real login', async ({
    page,
  }) => {
    await page.goto('/login');
    await page.getByLabel('Email').fill(SEEDED_DEV_USER.email);
    await page.getByLabel('Password').fill(SEEDED_DEV_USER.password);
    await page.getByRole('button', { name: 'Log in' }).click();

    await expect(page.getByTestId('app-sidebar')).toBeVisible();
    await expect(page.getByTestId('app-header')).toBeVisible();

    for (const key of ['floor', 'clock', 'jobs', 'customers', 'money', 'parts', 'team', 'reports']) {
      await expect(page.getByTestId(`nav-item-${key}`)).toBeVisible();
    }

    await expect(page.getByTestId('nav-item-floor')).toHaveAttribute('data-active', 'true');
    await expect(page.getByTestId('app-header')).toContainText('FLOOR CONTROL');
  });

  test('runtime branding from the real branding endpoint is displayed', async ({ page, request }) => {
    // Read the live branding config so the assertion is against the real API
    // response, never a hardcoded product name.
    const brandingResponse = await request.get(`${API_BASE_URL}/api/config/branding`);
    expect(brandingResponse.ok()).toBeTruthy();

    const branding = (await brandingResponse.json()) as BrandingConfigResponse;
    const productDisplayName = branding.productDisplayName?.trim();
    expect(productDisplayName, 'branding API must return a productDisplayName').toBeTruthy();

    await page.goto('/login');

    await expect(page.getByTestId('product-display-name')).toHaveText(productDisplayName);
    await expect(page.getByTestId('brand-mark')).toBeVisible();

    // ...and the same runtime value is used inside the authenticated shell.
    await page.getByLabel('Email').fill(SEEDED_DEV_USER.email);
    await page.getByLabel('Password').fill(SEEDED_DEV_USER.password);
    await page.getByRole('button', { name: 'Log in' }).click();

    await expect(page.getByTestId('shell-product-display-name')).toHaveText(productDisplayName);
  });
});
