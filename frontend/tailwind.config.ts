import type { Config } from 'tailwindcss';
import animate from 'tailwindcss-animate';

/**
 * PIT961 design tokens — dark theme only, no light mode.
 * Values mirror app/styles/globals.css custom properties 1:1.
 */
const config: Config = {
  darkMode: 'class',
  content: [
    './index.html',
    './app/**/*.{ts,tsx}',
    './components/**/*.{ts,tsx}',
    './features/**/*.{ts,tsx}',
    './hooks/**/*.{ts,tsx}',
    './layouts/**/*.{ts,tsx}',
    './pages/**/*.{ts,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        surface: {
          app: 'var(--surface-app)',
          card: 'var(--surface-card)',
          'card-item': 'var(--surface-card-item)',
          input: 'var(--surface-input)',
          header: 'var(--surface-header)',
        },
        border: {
          subtle: 'var(--border-subtle)',
          DEFAULT: 'var(--border-default)',
        },
        text: {
          primary: 'var(--text-primary)',
          'muted-1': 'var(--text-muted-1)',
          'muted-2': 'var(--text-muted-2)',
          'muted-3': 'var(--text-muted-3)',
        },
        accent: {
          primary: 'var(--accent-primary)',
          'primary-hover': 'var(--accent-primary-hover)',
          'primary-ink': 'var(--accent-primary-ink)',
        },
        status: {
          success: 'var(--status-success)',
          warning: 'var(--status-warning)',
          critical: 'var(--status-critical)',
        },
        rail: {
          icon: 'var(--rail-icon-muted)',
          label: 'var(--rail-label-muted)',
        },
      },
      borderRadius: {
        pill: '5px',
        control: '8px',
        button: '10px',
        card: '12px',
        panel: '13px',
      },
      fontFamily: {
        sans: ['"IBM Plex Sans"', 'system-ui', 'sans-serif'],
        mono: ['"IBM Plex Mono"', 'ui-monospace', 'monospace'],
      },
      fontSize: {
        micro: ['7.5px', { lineHeight: '1.1' }],
        eyebrow: ['11px', { lineHeight: '1.2' }],
      },
      letterSpacing: {
        eyebrow: '0.05em',
        crumb: '0.14em',
        rail: '0.11em',
      },
      boxShadow: {
        'focus-accent': '0 0 0 3px var(--accent-focus-ring)',
        'brand-mark': '0 2px 10px #e2892f40, inset 0 1px 0 #ffd3a0',
        'rail-active': 'inset 0 0 0 1px #2f373c, 0 1px 0 #0006',
        'rail-blade': '0 0 10px #e2892f99',
      },
      keyframes: {
        spin: {
          from: { transform: 'rotate(0deg)' },
          to: { transform: 'rotate(360deg)' },
        },
      },
      animation: {
        spin: 'spin 0.7s linear infinite',
      },
    },
  },
  plugins: [animate],
};

export default config;
