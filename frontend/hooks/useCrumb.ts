import { useEffect } from 'react';
import { setCrumb } from '@/stores/crumbStore';

/** Sets the header crumb for as long as the calling page is mounted, then clears it. */
export function useCrumb(crumb: string | null) {
  useEffect(() => {
    setCrumb(crumb);
    return () => setCrumb(null);
  }, [crumb]);
}
