# Phase 9 — Design System

> **Reconciliation note:** This document has been reconciled to describe the approved prototype (`prototype.html`) as the canonical visual reference. That reconciliation covers VISUAL DESIGN only — colors, typography, layout, components, and nav structure. It does not decide brand identity (product name, logo); see **Branding** below.

---

## Branding

> **Status: undecided — not a Phase 1 blocker.** The final customer-facing product/brand name has not been chosen. It will be decided later and is tracked separately from this design system.

- **"RASHID"** — the wordmark currently rendered in the prototype's sidebar (`prototype.html`) — is **leftover placeholder branding from the prototyping session**. It is not an approved product name and must not be treated as one.
- **"GarageOS"** and **"PIT961"** are internal/project codenames, used in this documentation set for convenience when a neutral name is needed in prose. Neither is an approved final customer-facing brand. "PIT961" is the preferred neutral codename going forward.
- **Implementation requirement:** the product display name, the logo, and any brand-specific colors used purely for identity (e.g. a wordmark color or logo accent — not the functional dark-theme/orange UI palette specified below) must be implemented as **swappable/configurable values**, not hardcoded strings or assets. This lets the eventual real brand be dropped in later without redesigning or reworking the approved visual system.
- This note applies only to the brand-identity layer (product name, logo mark, identity-only colors). It does **not** affect the approved functional design tokens below — the dark theme, orange accent (`--orange`), IBM Plex fonts, component specs, and layout system all remain canonical per the reconciliation note above.

---

## Design Principles

1. **Operational clarity first.** Information must be readable at a glance — a mechanic with greasy hands, a receptionist with a customer standing at the counter. No decorative noise.
2. **Role-aware density.** Owner and Accountant views are data-dense. Mechanic mobile views are touch-optimized with large tap targets and minimal text.
3. **Status at a glance.** Every job card communicates status without requiring the user to open it. Color, icon, and badge carry the message.
4. **Calm unless urgent.** UI is neutral until something needs attention. Then it escalates clearly (orange = warning, red = action required).
5. **RTL-first bilingual design.** Every component must render correctly in both LTR (English) and RTL (Arabic) without layout breaks.

---

## Color System

### Core Palette

**Corrected to match the approved prototype.** The prototype uses a dark theme with an orange primary
accent, not the light theme / blue-primary palette previously documented here.

| Token | Value | Usage |
|---|---|---|
| `--bg` | `#0b0d0e` | App background (`body`) |
| `--bg-elevated` | `#131718` | Card / panel background (e.g. floor bay cards, job cards) |
| `--border` | `#262c30` | Borders, dividers, input borders |
| `--text` | `#e6e9ea` | Primary text (body color) |
| `--text-muted` | `#7c858c` | Secondary text, muted labels |
| `--orange` (primary accent) | `#e2892f` | Primary action, active nav rail item, interactive links/buttons |
| `--orange-hover` | `#f0a458` | Hover state on orange buttons/links |
| `--blue` (secondary) | `#58a6c8` | Status/info accent (e.g. "WORKING" status dot, blueprint texture) — **not** the primary action color |

### Semantic Status Colors

| Token | Value | Meaning | Usage |
|---|---|---|---|
| `--green` | `#10b981` | Done / Paid / Passed | QC Pass, Paid invoice, Completed task |
| `--green-light` | `#d1fae5` | Green background tint | Payment confirmation, success states |
| `--orange` | `#f59e0b` | Warning / Waiting / Partial | Waiting Approval, Waiting Parts, Partial payment |
| `--orange-light` | `#fef3c7` | Orange background tint | Alert banners, overdue debt |
| `--red` | `#ef4444` | Critical / Overdue / Rejected | Overdue jobs, rejected items, unpaid invoices |
| `--red-light` | `#fee2e2` | Red background tint | Critical alert backgrounds |
| `--purple` | `#8b5cf6` | QC Stage | QC column only |
| `--purple-light` | `#ede9fe` | Purple background tint | QC badges |

### Neutral Grays

```
--gray-50:  #f8fafc  — page backgrounds
--gray-100: #f1f5f9  — subtle fills, table row alternates
--gray-200: #e2e8f0  — borders, dividers
--gray-300: #cbd5e1  — input borders
--gray-400: #94a3b8  — placeholder text, muted labels
--gray-500: #64748b  — secondary text
--gray-600: #475569  — body text secondary
--gray-700: #334155  — body text primary
--gray-800: #1e293b  — headings, strong text
```

### Color Application Rules

- **Never use red for purely informational states.** Red = action required or critical.
- **Green is earned.** Only applied when a job/invoice/payment is definitively complete.
- **Orange is the workhorse.** Most "in-progress but needs attention" states use orange.
- **Orange = interactive.** All clickable elements, primary actions, and active nav-rail states use the orange accent (`#e2892f`). Blue (`#58a6c8`) is a secondary status/info color, not the primary interactive color — corrected from the prior blue-primary spec.
- **Overdue = red, regardless of current stage.** A job in "Repairing" is still shown in red if the promised time has passed.

---

## Workshop Stage → Color Mapping

| Stage | Badge Color | Board Column Header |
|---|---|---|
| Checked In | Gray | `#6b7280` |
| Diagnosing | Blue | `#2563EB` |
| Waiting Approval | Orange pulse | `#f59e0b` |
| Waiting Parts | Amber | `#d97706` |
| Repairing | Light Blue | `#3b82f6` |
| QC | Purple | `#8b5cf6` |
| Ready | Green | `#10b981` |
| Delivered | Dark Gray | `#374151` |

---

## Typography

### Font Stack

**Corrected to match the approved prototype.** The prototype loads **IBM Plex Sans** and **IBM Plex Mono**
from Google Fonts, superseding the system-ui / Inter stack previously specified here.

```css
font-family: 'IBM Plex Sans', system-ui, sans-serif;  /* body text, UI labels, headings */
font-family: 'IBM Plex Mono', monospace;               /* nav labels, timestamps, metadata, badges/pills, monetary and plate values */
```

IBM Plex Mono is used extensively for metadata-style text throughout the prototype (nav labels, timestamps,
table row keys, pills, KPI sub-labels). IBM Plex Sans is the default body/UI font. No decorative or serif
fonts anywhere in the UI.

### Type Scale

| Role | Size | Weight | Usage |
|---|---|---|---|
| Page heading | 20px | 800 | Job title, customer name |
| Section title | 14–15px | 700–800 | Card titles, section headers |
| Body | 13–14px | 400–500 | Main content, form fields |
| Label / Caption | 11–12px | 600–700 | Field labels, badges, timestamps |
| Micro | 10–11px | 600 | Job card number, plate labels |

### Text Hierarchy Rules

- Field labels: `11px / font-weight: 700 / uppercase / letter-spacing: 0.05em`
- Field values: `14px / font-weight: 500`
- Card titles: `14–15px / font-weight: 700`
- KPI values: `24px / font-weight: 800`
- Monetary amounts: always `font-weight: 700` or `800`, never light weight

---

## Spacing System

8px base grid. All spacing values are multiples of 4px.

| Token | Value | Usage |
|---|---|---|
| `xs` | 4px | Internal badge padding, tiny gaps |
| `sm` | 8px | Card gap, small component padding |
| `md` | 12–14px | Card body padding, list item padding |
| `lg` | 16–18px | Card header, section spacing |
| `xl` | 20–24px | Page padding, major sections |

Card body padding: `18px`. Card header padding: `14px 18px`. Page content padding: `20px`. Sidebar nav item padding: `9px 16px`.

---

## Component Library

### 1. Job Card (Kanban)

```
┌─────────────────────────────┐  ← Border: 1px var(--gray-200)
│ #047             17:00 ⏰  │  ← Overdue: --red
│ XAB 12345                   │  ← Plate: 13px bold
│ BMW 328i 2011               │  ← Vehicle: 11px --gray-500
│ Vibration 80–100 km/h       │  ← Complaint: 11px 1 line
│ [⏰ OVERDUE]                │  ← Badge only if flagged
│ 👷 Ahmed      👤 John K.   │  ← Footer: 10px
│ [📋 Details] [→ Repairing] │  ← Action buttons
└─────────────────────────────┘
```

**Left border rule:**
- Overdue: `3px solid var(--red)`
- Waiting Approval: `3px solid var(--orange)`
- Customer Waiting: `3px solid var(--blue)`
- Normal: `1px solid var(--gray-200)`

**Hover state:** border-color → `--blue`, box-shadow: `0 4px 12px rgba(37,99,235,.15)`

### 2. KPI Card

Four variants: default (white), blue (primary metric), green (positive), orange (warning), red (alert).

```
┌────────────────────────┐
│ TODAY'S REVENUE        │ ← 11px / uppercase / label
│ $1,840                 │ ← 24px bold
│ ↑ 12% vs yesterday     │ ← 11px sub-label
└────────────────────────┘
```

Border-radius: `12px`. Padding: `16px`.

### 3. Badge

**Corrected to match the approved prototype's `pill()` helper.** Border-radius is ~5px (not 20px), and the
background is a dark tint of the status color with matching *colored* text — not a light background with
dark text.

```css
/* prototype: pill(color, size) helper */
.badge {
  display: inline-flex;
  align-items: center;
  white-space: nowrap;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 9.5px;        /* size is a parameter to pill(); 9–10.5px observed across uses */
  font-weight: 600;
  letter-spacing: .09em;
  padding: 3px 7px;
  border-radius: 5px;
  color: var(--status-color);
  background: var(--status-color) at ~12% opacity (hex suffix `1f`);
}
```

Variants are driven by passing a status color directly (e.g. orange `#e2892f`, blue `#58a6c8`, red
`#d1564c`, green `#59a97a`, purple `#8b7bd6`) rather than separate `b-blue`/`b-green`/etc. classes with a
fixed dark text color — text and background both derive from the same passed-in color.

"Waiting Approval" badge pulse animation: not verified against the approved prototype — the only confirmed
use of `animation: pulse` there is the header's "LIVE" status dot, not a badge. Left as previously specified
pending confirmation.

### 4. Tabs

Horizontal tab row with 2px bottom border on active tab matching `--blue`. No background color change. Font: `13px / font-weight: 600`. Tab color: `--gray-500` (inactive), `--blue` (active).

### 5. Form Fields

```css
input, select, textarea {
  width: 100%;
  padding: 9px 12px;
  border: 1px solid var(--gray-300);
  border-radius: 8px;
  font-size: 14px;
}
input:focus {
  border-color: var(--blue);
  box-shadow: 0 0 0 3px rgba(37,99,235,.1);
}
```

Label: `11px / font-weight: 700 / uppercase / color: --gray-600`. Margin below label: `5px`. Margin below field: `14px`.

### 6. Timeline (History Tab)

Vertical line connecting dots. Each event has: icon dot (30px circle), action title, detail subtitle, timestamp.

```
● [Icon]  Task complete           14px bold
          Ahmed · 13:45           12px --gray-500
          Today, 13:45            11px --gray-400
│
● [Icon]  Diagnosis entered       14px bold
...
```

The vertical line is `::after` pseudo-element on each item except the last.

### 7. Alert Box

Two variants:
- `alert-orange`: `background: --orange-light; border: 1px solid --orange`
- `alert-green`: `background: --green-light; border: 1px solid --green`

Padding: `12px 14px`. Border-radius: `8px`. Always includes an icon (emoji) + title text.

### 8. Modal

Overlay: `rgba(0,0,0,0.5)` full-screen. Modal container: white, `border-radius: 16px`, `padding: 22px`, `max-width: 400px`. Box shadow: `0 20px 60px rgba(0,0,0,0.3)`.

### 9. Toast Notification

Fixed position: `bottom: 22px; right: 22px` (mirrors to left in RTL). Background: `--navy`. Color: white. Border-radius: `10px`. Auto-dismiss: 3 seconds. Slides in from bottom.

### 10. Button System

| Variant | Background | Color | Border |
|---|---|---|---|
| `btn-primary` | `--blue` | white | none |
| `btn-outline` | white | `--gray-700` | `1px --gray-200` |
| `btn-green` | `--green` | white | none |
| `btn-danger` | `--red` | white | none |

Padding: `7px 14px` (default), `5px 10px` (sm). Border-radius: `8px`. Font: `13px / font-weight: 600`. Hover: primary darkens to `--blue-dark`; outline gets `--gray-50` background.

---

## Layout System

### Desktop (≥1024px)

```
┌──────────────┬────────────────────────────┐
│  Sidebar     │  Top Bar                   │
│  220px       ├────────────────────────────┤
│              │                            │
│              │  Content Area              │
│              │  padding: 20px             │
│              │                            │
└──────────────┴────────────────────────────┘
```

### Mobile (≤768px)

Sidebar hides. Content takes full width. Mechanic view designed at 375px.

KPI grid: 2-column. Info grid: 1-column. Two-column layouts stack to 1-column.

### Kanban Board

Horizontal scroll container. Each column: `width: 220px; flex-shrink: 0`. Board has `min-width: max-content` to prevent wrapping. Columns stack only in explicit "list view" switch.

---

## Icons

**Corrected to match the approved prototype.** The prototype does not use emoji for nav icons — it draws
custom CSS/gradient glyphs via a `glyph(key, color)` helper (see `prototype.html`), color-matched to state
(orange `#e2892f` when active, muted gray `#69737a` when inactive). The emoji-based approach below was
considered during design but has been superseded by the custom glyphs in the approved prototype for the
primary nav-rail icons. It is retained below as historical/aspirational context, not the current nav icon
spec; emoji use elsewhere in the UI (outside the nav rail) has not been verified either way.

| Context | Icon (superseded for nav rail — see note above) |
|---|---|
| Dashboard | 📊 |
| Workshop Board | ⊞ |
| Customers | 👤 |
| Jobs | 🔧 |
| Finance | 💰 |
| Parts | 🔩 |
| Settings | ⚙️ |
| Search | 🔍 |
| Overdue | ⏰ |
| Waiting | ⏳ |
| Customer waiting | 🔵 |
| Overnight | 🌙 |
| QC | 🔍 |
| Payment | 💵 / 💳 |
| Vehicle delivered | 🚗 |
| Photo | 📷 |
| Print | 🖨 |

---

## RTL Support

> **Status:** Specified as a design principle but not yet implemented or validated in the approved
> prototype. A follow-up design pass is required before mobile/RTL layouts are built.

The `dir` attribute on `<html>` switches the entire layout:

```css
[dir=rtl] .sidebar { order: 1; }
[dir=rtl] .main { order: 0; }
[dir=rtl] .nav-item { flex-direction: row-reverse; }
[dir=rtl] .tabs { direction: rtl; }
```

Toast notification mirrors to `bottom: 22px; left: 22px` in RTL. All flex-row layouts reverse automatically through `direction: rtl` inheritance. Text alignment inherits from `dir`.

**Arabic font:** When RTL is active, system Arabic fonts (Tahoma, Arial, system-ui) are used. No additional import needed.

---

## Responsive Breakpoints

> **Status:** Specified as a design principle but not yet implemented or validated in the approved
> prototype. A follow-up design pass is required before mobile/RTL layouts are built.

| Breakpoint | Width | Behavior |
|---|---|---|
| Mobile | ≤768px | Sidebar hidden, bottom tabs, 2-col KPI grid |
| Tablet | 769–1023px | Sidebar collapsed (icon-only), 3-col KPI grid |
| Desktop | ≥1024px | Full sidebar 220px, max content width 1200px |

---

## Accessibility Notes

- All interactive elements have explicit cursor: pointer
- Focus states use blue ring: `box-shadow: 0 0 0 3px rgba(37,99,235,.1)`
- Color is never the sole differentiator — status badges include text labels alongside color
- Touch targets on mechanic view: minimum 44px height
- Error and success states include both icon and text
