import { useEffect, useRef } from 'react';
import { bootstrapBranding } from '@/features/branding/api';
import { bootstrapSession } from '@/features/auth/session';

/**
 * One-shot app boot.
 *
 * Branding and the silent session refresh run in parallel: the login screen
 * needs branding before authentication, and the session bootstrap decides
 * whether we land on /login or in the shell. Neither rejects — both resolve
 * into their store's terminal state.
 *
 * The ref guard keeps this to a single run under React 18 StrictMode's
 * double-invoked effects in development.
 */
export function useAppBootstrap(): void {
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;

    void Promise.allSettled([bootstrapBranding(), bootstrapSession()]);
  }, []);
}
