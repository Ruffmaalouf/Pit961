/**
 * Brand-mark helpers.
 *
 * PIT961 has no approved customer-facing brand name yet, so the product
 * display name, logo and brand initial are ALWAYS derived from the runtime
 * branding config (GET /api/config/branding). Never hardcode a letter, a
 * wordmark or a logo asset anywhere in this codebase.
 */

/**
 * First alphanumeric character of the runtime product display name, uppercased.
 * Returns an empty string when there is nothing renderable — callers then show
 * the glyph-only brand mark with no letter.
 */
export function brandInitial(productDisplayName: string | null | undefined): string {
  if (!productDisplayName) return '';

  const match = productDisplayName.trim().match(/[\p{L}\p{N}]/u);
  return match ? match[0].toUpperCase() : '';
}
