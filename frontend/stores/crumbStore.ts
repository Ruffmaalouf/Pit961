import { create } from 'zustand';

/**
 * Lets a page override the header's crumb text (e.g. Job detail shows
 * "JOB-000042 · BMW 328I" instead of the generic "JOBS" nav label), mirroring
 * prototype.html's per-screen `crumb` dict. AppShellLayout falls back to the
 * active nav item's label when no page has set one.
 */
interface CrumbState {
  crumb: string | null;
  setCrumb: (crumb: string | null) => void;
}

export const useCrumbStore = create<CrumbState>((set) => ({
  crumb: null,
  setCrumb: (crumb) => set({ crumb }),
}));

/** Non-React accessor + a tiny hook for the common "set on mount, clear on unmount" pattern. */
export function setCrumb(crumb: string | null) {
  useCrumbStore.getState().setCrumb(crumb);
}
