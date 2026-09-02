/**
 * Shared e2e constants.
 *
 * SEEDED DEVELOPMENT ACCOUNT ONLY. This is the real development seed account
 * from the backend's dev data — it must never be used against staging or
 * production, and no other credentials belong in this repository.
 */
export const SEEDED_DEV_USER = {
  email: 'ralph@performanceautogarage.example',
  password: 'DevSeed-Pass1!',
} as const;

/**
 * Base URL of the backend the app under test is talking to. Used only to read
 * the branding config directly so the e2e assertions compare the UI against
 * the live API response rather than a hardcoded product name.
 */
export const API_BASE_URL = process.env.VITE_API_BASE_URL ?? 'http://localhost:5289';

export interface BrandingConfigResponse {
  productDisplayName: string;
  emailFromName: string;
  logoUrl: string;
  supportEmail: string;
}
