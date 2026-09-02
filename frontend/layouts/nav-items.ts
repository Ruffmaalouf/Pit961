/**
 * Nav rail items for the garage-tenant shell.
 *
 * All 8 are inert placeholders for WP-8 — only the shell mechanism is in
 * scope, not the feature screens. There is deliberately no Settings item.
 * Platform-admin navigation is a separate surface and is not represented here.
 */
export interface NavItem {
  key: NavGlyphKey;
  label: string;
  /** Route this item will own later; only `/floor` exists today. */
  path: string;
}

export type NavGlyphKey =
  | 'floor'
  | 'clock'
  | 'jobs'
  | 'customers'
  | 'money'
  | 'parts'
  | 'team'
  | 'reports';

export const NAV_ITEMS: NavItem[] = [
  { key: 'floor', label: 'Floor', path: '/floor' },
  { key: 'clock', label: 'Clock', path: '/clock' },
  { key: 'jobs', label: 'Jobs', path: '/jobs' },
  { key: 'customers', label: 'Customers', path: '/customers' },
  { key: 'money', label: 'Money', path: '/money' },
  { key: 'parts', label: 'Parts', path: '/parts' },
  { key: 'team', label: 'Team', path: '/team' },
  { key: 'reports', label: 'Reports', path: '/reports' },
];
