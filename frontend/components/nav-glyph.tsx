import type { CSSProperties } from 'react';
import type { NavGlyphKey } from '@/layouts/nav-items';

/**
 * The nav rail's CSS-drawn glyphs, transcribed from the approved prototype's
 * `glyph(key, color)` helper. No emoji, no icon font, no external SVG assets.
 * Colour is passed in: orange when active, muted grey when inactive.
 */
export function navGlyphStyle(key: NavGlyphKey, color: string): CSSProperties {
  switch (key) {
    case 'floor':
      return {
        width: 17,
        height: 13,
        background: `repeating-linear-gradient(90deg,${color} 0 3px,transparent 3px 6px)`,
        borderBottom: `1.6px solid ${color}`,
      };
    case 'clock':
      return {
        width: 15,
        height: 15,
        borderRadius: '50%',
        border: `1.6px solid ${color}`,
        background: `linear-gradient(180deg,transparent 46%,${color} 46%,${color} 54%,transparent 54%)`,
      };
    case 'jobs':
      return {
        width: 13,
        height: 16,
        border: `1.6px solid ${color}`,
        borderRadius: 3,
        background: `linear-gradient(180deg,transparent 26%,${color} 26%,${color} 36%,transparent 36%,transparent 56%,${color} 56%,${color} 66%,transparent 66%)`,
      };
    case 'customers':
      return {
        width: 15,
        height: 15,
        borderRadius: '50% 50% 5px 5px',
        border: `1.6px solid ${color}`,
        background: `radial-gradient(circle at 50% 28%,${color} 0 3.2px,transparent 3.4px)`,
      };
    case 'money':
      return {
        width: 18,
        height: 12,
        border: `1.6px solid ${color}`,
        borderRadius: 3,
        background: `radial-gradient(circle at 50% 50%,${color} 0 2.4px,transparent 2.6px)`,
      };
    case 'parts':
      return {
        width: 16,
        height: 16,
        clipPath: 'polygon(50% 0,93% 25%,93% 75%,50% 100%,7% 75%,7% 25%)',
        background: `radial-gradient(circle at 50% 50%,#0f1213 0 3.4px,${color} 3.6px)`,
      };
    case 'team':
      return {
        width: 18,
        height: 13,
        borderBottom: `1.6px solid ${color}`,
        background: `radial-gradient(circle at 27% 38%,${color} 0 3.2px,transparent 3.4px),radial-gradient(circle at 73% 38%,${color} 0 3.2px,transparent 3.4px)`,
      };
    case 'reports':
      return {
        width: 17,
        height: 14,
        background: `linear-gradient(180deg,transparent 58%,${color} 58%) 0 0/4px 100% no-repeat,linear-gradient(180deg,transparent 22%,${color} 22%) 6.5px 0/4px 100% no-repeat,linear-gradient(180deg,transparent 42%,${color} 42%) 13px 0/4px 100% no-repeat`,
      };
  }
}

/** Active = accent orange, inactive = muted rail grey (approved prototype values). */
const ACTIVE_GLYPH_COLOR = '#e2892f';
const INACTIVE_GLYPH_COLOR = '#69737a';

export function NavGlyph({ glyphKey, active }: { glyphKey: NavGlyphKey; active: boolean }) {
  return (
    <span className="flex h-[17px] items-end" aria-hidden>
      <span
        data-glyph={glyphKey}
        style={navGlyphStyle(glyphKey, active ? ACTIVE_GLYPH_COLOR : INACTIVE_GLYPH_COLOR)}
      />
    </span>
  );
}
