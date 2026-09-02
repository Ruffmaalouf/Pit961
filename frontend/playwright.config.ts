import { defineConfig, devices } from '@playwright/test';

/**
 * WP-8 e2e configuration.
 *
 * These specs run against a REAL running stack: the Vite dev/preview server on
 * PLAYWRIGHT_BASE_URL and the real backend it is pointed at via
 * VITE_API_BASE_URL. There is no mock server — that is deliberate.
 *
 * Chromium only for WP-8; the cross-browser matrix is out of scope.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5173',
    trace: 'off', // WP-9 Security Reviewer finding (MEDIUM): CI retry traces embed request
    // headers/bodies (bearer token, seeded dev password) into the always-uploaded
    // playwright-report artifact. screenshot: 'only-on-failure' below already covers
    // CI debugging needs without that exposure.
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // Pinned to the full Chromium build rather than Playwright's default
        // headless-shell binary: on the real device this suite runs against,
        // `npx playwright install` reliably fetches full Chromium but the
        // separate headless-shell download stalls indefinitely. `channel:
        // 'chromium'` makes the full browser do the headless run too, so
        // only one binary is ever required. Verified working end-to-end on
        // the real device before this WP-8 acceptance was signed off.
        channel: 'chromium',
      },
    },
  ],
});
