/**
 * URL / email hardening for values that arrive from the backend branding
 * config. Branding is tenant-configurable data, so it is treated as untrusted
 * input on the client even though the backend also validates it.
 *
 * Rules enforced here:
 *  - a URL is only ever rendered into `src`/`href` if its scheme is http(s)
 *  - `javascript:`, `data:`, `vbscript:`, `file:`, protocol-relative and
 *    scheme-less values are all rejected
 *  - mailto: hrefs are built with encodeURIComponent, never concatenated raw
 */

const ALLOWED_SCHEMES = new Set(['http:', 'https:']);

/**
 * Control characters (incl. embedded newline/tab) are a classic way to smuggle
 * `java\nscript:` past a naive scheme check, so any occurrence is rejected.
 */
const CONTROL_CHARS = /[\u0000-\u001F\u007F]/;

/**
 * True only when `url` is an absolute http(s) URL that is safe to place in an
 * `<img src>` / `<a href>`. Everything else (including empty, relative,
 * protocol-relative and `javascript:` values) returns false.
 */
export function isSafeHttpUrl(url: string): boolean {
  if (typeof url !== 'string') return false;

  const trimmed = url.trim();
  if (trimmed.length === 0) return false;

  // Protocol-relative ("//evil.example") inherits the page scheme and hides
  // the origin swap — reject outright rather than resolving it.
  if (trimmed.startsWith('//')) return false;

  if (CONTROL_CHARS.test(trimmed)) return false;

  let parsed: URL;
  try {
    parsed = new URL(trimmed);
  } catch {
    return false;
  }

  return ALLOWED_SCHEMES.has(parsed.protocol);
}

/**
 * Returns the URL when it is safe to render, otherwise `null`. Callers should
 * branch on `null` and fall back to a non-image brand mark.
 */
export function safeHttpUrlOrNull(url: string | null | undefined): string | null {
  if (!url) return null;
  return isSafeHttpUrl(url) ? url.trim() : null;
}

/**
 * Deliberately conservative "plausible email" shape check. This is not RFC
 * 5322 — it exists to decide whether we are willing to build a mailto: link,
 * so it errs towards rejecting anything unusual.
 */
export function isPlausibleEmail(value: string): boolean {
  if (typeof value !== 'string') return false;

  const trimmed = value.trim();
  if (trimmed.length === 0 || trimmed.length > 254) return false;
  if (CONTROL_CHARS.test(trimmed)) return false;
  // No whitespace, quotes, angle brackets or address-list punctuation.
  if (/[\s"'<>(),;:\\[\]]/.test(trimmed)) return false;

  return /^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$/.test(trimmed);
}

/**
 * Builds a `mailto:` href for an address that has already passed
 * `isPlausibleEmail`. Returns `null` for anything else so the caller renders
 * plain text (or nothing) instead of a link.
 *
 * The local/domain parts are percent-encoded rather than concatenated, so a
 * value containing e.g. `?body=` cannot inject extra mailto headers.
 */
export function buildMailtoHref(email: string): string | null {
  if (!isPlausibleEmail(email)) return null;

  const trimmed = email.trim();
  const atIndex = trimmed.lastIndexOf('@');
  const local = trimmed.slice(0, atIndex);
  const domain = trimmed.slice(atIndex + 1);

  return `mailto:${encodeURIComponent(local)}@${encodeURIComponent(domain)}`;
}
