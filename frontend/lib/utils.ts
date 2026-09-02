import { type ClassValue, clsx } from 'clsx';
import { extendTailwindMerge } from 'tailwind-merge';

/**
 * tailwind-merge has to be told about our custom fontSize scale keys.
 *
 * Without this it cannot tell `text-micro` (a font size) from `text-rail-label`
 * (a colour), treats them as the same class group, and silently drops the
 * first one — which showed up as 14px nav-rail labels instead of 7.5px. Any
 * new named entry in `theme.extend.fontSize` must be added here too.
 */
const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      'font-size': [{ text: ['micro', 'eyebrow'] }],
    },
  },
});

/** shadcn-standard class merge helper. */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
