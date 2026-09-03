# DESIGN_IMPLEMENTATION_DIFFERENCES.md

Required by `11_engineering_handoff.md` §2 (Critical Engineering Rule): any unavoidable difference between the approved design documents and the actual implementation must be logged here, with approved design behavior, implementation limitation, proposed change, reason, and impact.

---

## 1. `prototype.html` is canonical over `09_design_system.md`'s original light-theme spec

- **Approved design behavior:** `09_design_system.md` originally documented a light-theme visual design system.
- **Implementation limitation / actual source of truth:** `prototype.html` — a dark-theme, orange-accent, IBM Plex Sans/Mono interactive mockup — is the canonical, owner-approved visual product (owner decision, 2026-08-26).
- **Proposed change:** Treat `prototype.html` as the source of truth for all visual design, layout, and interaction details. `09_design_system.md` is reconciled to match it (owned by Product/Design).
- **Reason:** Owner made `prototype.html` the binding visual approval; it supersedes the earlier light-theme spec wherever the two disagree.
- **Impact:** No functional/business-logic impact. Frontend implementation must match `prototype.html` visually, not the original light-theme design-system doc.

---

## 2. Job Detail is one consolidated page, not 7 tabs

- **Approved design behavior:** Earlier design documentation described Job Detail as a 7-tab interface.
- **Implementation limitation / actual approved design:** `prototype.html` implements Job Detail as a single consolidated page.
- **Proposed change:** Build Job Detail as one consolidated page, matching `prototype.html`.
- **Reason:** `prototype.html` is canonical (see item 1); it reflects the current approved UX, which moved away from the tabbed structure.
- **Impact:** Frontend routing/component structure for Job Detail should not implement 7 separate tabs. No backend/API impact — the underlying data and endpoints remain the same, only presentation consolidates.

---

## 3. Navigation is Floor / Clock / Jobs / Customers / Money / Parts / Team / Reports, not the original Dashboard / Workshop / Finance / Settings taxonomy

- **Approved design behavior:** Earlier documentation (e.g. `04_information_architecture.md`) described a Dashboard / Workshop / Finance / Settings top-level navigation taxonomy.
- **Implementation limitation / actual approved design:** `prototype.html` implements top-level navigation as: Floor, Clock, Jobs, Customers, Money, Parts, Team, Reports.
- **Proposed change:** Implement navigation per `prototype.html`'s taxonomy.
- **Reason:** `prototype.html` is canonical (see item 1) and reflects the current approved information architecture.
- **Impact:** Frontend route/nav structure follows the 8-item taxonomy above. Underlying screens/features map into this taxonomy; no functionality is dropped, only reorganized.

---

## 4. `ready_to_repair` is a Waiting-Parts sub-status, not a board column

- **Approved design behavior:** The approved workshop board has 8 columns: Checked In / Diagnosing / Waiting Approval / Waiting Parts / Repairing / QC / Ready / Delivered. Separately, `11_engineering_handoff.md`'s job status enum and state-machine diagram (§9, §35) list a `ready_to_repair` status alongside the 8 column names, which read as a possible 9th column.
- **Implementation limitation:** A literal 9th `ready_to_repair` board column would contradict the approved 8-column board.
- **Proposed change / resolution:** `ready_to_repair` is a transient sub-status displayed within the **Waiting Parts** column (signaling parts have arrived and the job is ready to move into Repairing), not a separate board column. `11_engineering_handoff.md` §9 and §35 have been updated with this clarification.
- **Reason:** Reconciles the job status enum (used internally for state-machine transitions) with the approved 8-column board UI (used for display).
- **Impact:** No board redesign needed. Frontend renders `ready_to_repair` jobs inside the Waiting Parts column (e.g. via a badge/indicator), not as a separate column. Backend state machine keeps `ready_to_repair` as a distinct enum value for transition logic.

---

## 5. Login screen — freshly designed, not present in `prototype.html`

- **Approved design behavior:** `prototype.html` (the canonical visual source of truth per item 1) contains no login screen at all — the mockup opens directly into the authenticated "Floor" view with a demo-only role switcher standing in for real auth.
- **Implementation limitation:** WP-8 acceptance criteria requires a real login page as the first screen users see, wired to `POST /api/v1/auth/login`. No prototype precedent exists to build from.
- **Proposed change:** Design Lead authored a minimal, token-consistent login screen (centered card on `#0b0d0e`, `#101416` surface, config-driven brand mark and `ProductDisplayName` reusing the sidebar's brand-mark pattern, email + password fields, orange primary button, spinner loading state, red-banner/red-border error states) using only colors, type weights, and radii already ratified from the prototype. Full spec delivered to Frontend Engineer directly (WP-8 design decisions memo, 2026-09-02).
- **Reason:** WP-8 cannot ship without a login screen; no approved design existed to reconcile against, so this is new design work grounded in existing tokens rather than a reconciliation.
- **Impact:** One new screen, zero conflicts with the approved token set. Establishes the canonical input-focus-ring, input-error, and button-loading-spinner patterns, none of which existed in the prototype — these are now the reference for those patterns in future screens.

---

## 6. Destructive/danger button variant — deferred, out of WP-8 scope

- **Approved design behavior:** `09_design_system.md` originally specified a `btn-danger` variant (red background, white text).
- **Implementation limitation:** `prototype.html` contains no destructive button variant anywhere, and no WP-8 screen requires one — the only error-adjacent UI needed for WP-8 (login failure) uses the input-error/banner pattern in item 5, not a button variant.
- **Proposed change:** Do not implement a `destructive`/`danger` button variant in WP-8. Defer its design until a concrete destructive action (delete, void, irreversible cancel) is scoped, so it can be deliberately differentiated from routine manager-level actions rather than reusing a generic red button.
- **Reason:** Avoids locking in an unvalidated visual pattern ahead of a real use case, and avoids the risk of a destructive action looking visually identical to a routine one.
- **Impact:** None for WP-8. Flag to Design Lead before any future screen needs a delete/void/irreversible-cancel action so it gets a dedicated design pass.

---

## 7. Doc/prototype value corrections — `prototype.html` values ratified across remaining color, type, sidebar, focus, and toast specs

- **Approved design behavior:** `09_design_system.md`'s color table, type-weight table, sidebar layout section, input-focus spec, and toast spec still contain several values that don't match the approved `prototype.html` (already established as canonical in item 1), beyond what item 1 covers in general terms.
- **Implementation limitation:** Building against the doc's literal remaining numbers (e.g. border `#262c30`, single muted-text value `#7c858c`, green/amber/red/purple hexes, 700/800 type weights, blue input focus, 220px labeled sidebar, bottom-right navy 3s toast) would produce a UI that doesn't match the approved prototype.
- **Proposed change:** Frontend token layer uses the prototype-accurate values instead: border `#1e2225` (page-card) / `#1e2427`-family (item-card), muted-text scale `#8b959b/#7c858c/#6c757a/#5f696e/#4f585d`, green `#59a97a`, amber `#c98a2f`, red `#d1564c`, purple `#8b7bd6`, type weights capped at 400/500/600, input focus border `#e2892f` (not blue), 76px icon + short mono-caps micro-caption sidebar rail (not a 220px full-word-labeled rail; `prototype.html`'s own rail data already carries a per-item caption via `label: name.toUpperCase()`, rendered at 7.5px, so the correction here is the rail's width and label length, not whether it has a label at all), toast bottom-center/orange/~2.4s (not bottom-right/navy/3s). `09_design_system.md` should be corrected to match.
- **Reason:** Prototype is canonical per item 1; this entry closes the loop with the specific corrected values needed for the WP-8 engineering token file, which the general item-1 note didn't enumerate.
- **Impact:** No functional/business-logic impact. Frontend Tailwind/shadcn theme config must use the values above, not the doc's remaining stale numbers.

## 8. Rail product-display-name label under the brand mark — freshly designed, not present in `prototype.html`

- **Approved design behavior:** `prototype.html`'s rail (see item 7) shows the brand mark followed directly by the 8 nav items; it has no tenant/product-name label anywhere in the rail, only the fixed `RASHID` username badge at the bottom of the page (a different element, not truncated, not part of the rail).
- **Implementation limitation:** WP-8's authenticated shell renders the runtime `productDisplayName` (from `GET /api/config/branding`) directly under the brand mark in the 76px rail (`AppShellLayout.tsx`). Real tenant/garage names are arbitrary-length business names (e.g. "Rashid's Auto & Truck Service Center") that will not fit a 76px-wide rail, and no prototype precedent exists for this element at all.
- **Proposed change:** The label is truncated with `max-w-[68px] truncate` (`overflow: hidden; text-overflow: ellipsis; white-space: nowrap`) and carries a native `title={productDisplayName}` attribute, so hovering reveals the full untruncated name as a browser tooltip. This was flagged in-code to Design Lead at implementation time (see the comment directly above the element in `AppShellLayout.tsx`) rather than treated as a silent side effect of reusing the nav-item label CSS.
- **Reason:** WP-8 needs the real branding endpoint wired end-to-end, including a visible product name in the authenticated shell; no approved design exists for this specific element, so this is new design work using already-ratified rail tokens (7.5px mono uppercase, `text-rail-label` color) rather than a reconciliation against the prototype.
- **Impact:** No functional/business-logic impact. Confirmed via Design Lead re-review (WP-8 sign-off round) that the truncation-plus-tooltip treatment is an acceptable, deliberate answer to the "no approved treatment for long names" gap noted in the code comment — not an incidental bug. Any future dedicated design pass for this element (e.g. a two-line label, a settings-page display name, or an abbreviation scheme) should update this entry.

---


---

## 9. Customer detail screen, Customer form, Vehicle form — freshly designed, not present in `prototype.html` (P2-WP2)

- **Approved design behavior:** `prototype.html`'s Customers & vehicles screen is a list only (KPIs, row data, a "New customer" button with no form content shown); clicking a customer row routes into the Job screen as an explicit mock simplification, not a real Customer detail screen. No Vehicle add/edit form exists anywhere in the prototype — vehicle fields only ever appear read-only, inline, inside a customer row or the Job detail's VEHICLE card.
- **Implementation limitation:** P2-WP2 needs real Customer detail, Customer create/edit, and Vehicle create/edit screens; none exist to reconcile against.
- **Proposed change (Design Lead, P2-WP9 audit, 2026-09-03):** Customer detail screen — identity card (name/phone/email/avatar initials, reusing the customer-row visual language), a Vehicles section listing each vehicle as a compact card reusing the existing VEHICLE-card fields (make/model/year, plate, mileage, VIN), a read-only Jobs history list reusing the Floor board job-card visual language, and a balance/invoices summary reusing the Money screen's invoice-row style. New/Edit Customer form and New/Edit Vehicle form both styled as a modal/panel matching the existing "Record payment" modal pattern (header + field stack + submit button). Customers list row-click target redirects to the new Customer detail screen instead of the Job-screen mock. Full field lists and entry points in `DESIGN_IMPLEMENTATION_DIFFERENCES.md`'s history (superseded content restored below this entry) are further detailed in the P2-WP9 audit record (git history, commit `a81b690`).
- **Reason:** No prototype precedent exists for any of these three screens; grounded in already-ratified visual language rather than inventing new patterns.
- **Impact:** Three new screens/forms for Frontend Engineer to build against, zero conflicts with ratified tokens. Gates the P2-WP2 Owner Visual Checkpoint per `14_phase2_execution_plan.md`.

---

## 10. Check-in vehicle form — freshly designed, not present in `prototype.html` (P2-WP3)

- **Approved design behavior:** The Floor screen's "Check in vehicle" button exists in `prototype.html` but has no designed target screen or form.
- **Implementation limitation:** P2-WP3 needs a real intake flow behind that button.
- **Proposed change (Design Lead, P2-WP9 audit):** A simple intake form/wizard composed entirely from already-established field groups — customer select-or-create (item 9), vehicle select-or-create (item 9), issue description, bay assignment — since all four already appear as read fields on Floor board job cards and the Job detail VEHICLE/JOB cards. A composition, not a new visual pattern.
- **Reason:** No prototype precedent for this specific form; composed from existing tokens/patterns rather than invented.
- **Impact:** One new form for Frontend Engineer. Gates the P2-WP3 Owner Visual Checkpoint.

---

## 11. Estimate line-item editor, discount control, owner-approval gate — freshly designed, not present in `prototype.html` (P2-WP4)

- **Approved design behavior:** `prototype.html` contains no estimate-creation or line-item-editing UI — only two history-feed entries referencing an estimate that was already sent and already approved, plus a static total on the Job card. Nothing distinguishes a manager's routine discretionary discount from an owner-approval-gated action; neither exists in any designed form.
- **Implementation limitation:** P2-WP4 needs (a) an estimate line-item editor, (b) a discount-application control capped at the Manager's 15% ceiling, and (c) a distinct UI treatment for the Owner-approval-required $500+ gate that cannot be visually confused with (b).
- **Proposed change (Design Lead, P2-WP9 audit):** Line-item editing added inside the Job detail's existing "Work & parts, one list" component (editable price per line, a running total, a "Send estimate" action surfacing the channel picker already implied by the feed's "channel: WhatsApp" annotation). Discount control: a single inline step (no modal) near the estimate total, deliberately lightweight. Owner-approval gate: a distinct badge/banner reusing the Floor board's existing "WAITING APPROVAL" lane color (`#c98a2f`, already established in-product for exactly this semantic), a separate Owner approve/reject action (not the same button or modal as the discount control), and a blocked-state treatment reusing the command palette's existing balance-blocked-with-toast pattern. Money screen gets a new Estimates list/filter state (parallel to the existing RECEIVABLE invoice status) so pending owner-approvals are visible in one place.
- **Reason:** No prototype precedent for estimate authoring or the approval-gate distinction; the separate-control requirement is a deliberate design-conflict prevention measure (Owner Decision #2 makes this gate Owner-only, distinct from Manager discretion under Owner Decision policy).
- **Impact:** The most design-work-heavy area of the audit. Gates the P2-WP4 milestone; `ui-ux-designer` should wireframe items (b) and (c) as visibly separate controls before Frontend Engineer builds either.

---

## 12. Repair Task "+ Task" affordance, status control, sub-line — freshly designed, not present in `prototype.html` (P2-WP5, task half)

- **Approved design behavior:** `prototype.html`'s Job detail combines tasks and parts into one deliberate "Work & parts, one list" component — but only the parts side has documented affordances (a "+ Part" button, a status pill, an "advancePart" action button, an OEM/supplier sub-line). No equivalent exists for tasks.
- **Implementation limitation:** P2-WP5's task half needs parity with the parts half within the same combined list.
- **Proposed change (Design Lead, P2-WP9 audit):** A "+ Task" affordance styled identically to "+ Part" (same button treatment/position); a per-task status pill (Not started / In progress / Done) and an "advanceTask" action button following the identical visual/interaction treatment as "advancePart"; a per-task sub-line (assigned tech + estimated time) using the same IBM Plex Mono muted-color typographic treatment as the parts sub-line.
- **Reason:** The combined-list pattern itself is already approved and sufficiently represented; only same-pattern parity extensions are needed, not a new component.
- **Impact:** Same-pattern extension, low design risk. Gates the P2-WP5 milestone alongside item 13.

---

## 13. Add-part form, Parts screen row action — freshly designed/extended, not present in `prototype.html` (P2-WP5, parts half)

- **Approved design behavior:** The "+ Part" affordance exists in the combined Job-detail list, but the form it opens is undefined. The Parts & suppliers screen's list has no per-row contextual action, unlike the Money screen's per-row action button (Resend/Chase/Charge/View).
- **Implementation limitation:** P2-WP5 needs both filled in.
- **Proposed change (Design Lead, P2-WP9 audit):** Add-part inline form/modal styled like "Record payment," fields: description, OEM number, supplier (dropdown), quantity, unit cost — matching the fields already shown on the resulting part card. Parts & suppliers screen gets a per-row action (e.g. "Mark received" / "View job") reusing the Money screen's row-button pattern for cross-screen consistency.
- **Reason:** Parts are otherwise the best-covered secondary entity in the prototype (dedicated screen, timeline stepper, command-palette shortcut); these are the two concrete gaps in an otherwise sufficient area.
- **Impact:** Minor extensions, low design risk.

---

## 14. Void invoice action, invoice status vocabulary — freshly designed/extended, not present in `prototype.html` (P2-WP6)

- **Approved design behavior:** `prototype.html`'s Money screen (invoice list + "Record payment" modal) has no void, refund, or voided-status treatment anywhere.
- **Implementation limitation:** Owner Decision #6 (DECISIONS.md #12) requires Phase 2 to block voiding an invoice that has any recorded, non-voided payment — this needs a UI expression, and voided invoices need to be visually distinguishable in the invoice list.
- **Proposed change (Design Lead, P2-WP9 audit):** A "Void" action added to the Money screen's existing per-row action-button pattern, gated behind a confirmation modal styled like "Record payment" (header + invoice# + customer + a reason field) rather than a silent one-click action; when the invoice has a recorded non-voided payment, the action is disabled/blocked (not merely warned), reusing the same balance-blocked-with-toast pattern as the command palette's "deliver" action, per Owner Decision #6's fail-closed requirement. Voided invoices get their own status color in the invoice list, parallel to and distinguishable from the existing RECEIVABLE status. A full refund/credit-note modal is deferred (Owner Decision #6 places that workflow out of Phase 2 scope) — retained as a forward note only, mirroring "Record payment" 1:1, for whichever later phase picks it up.
- **Reason:** Owner Decision #6 requires this to fail closed, not just be documented as a known limitation; the UI must make the blocked state unmistakable, not merely policy-enforced server-side.
- **Impact:** Gates the P2-WP6 milestone (not part of the Milestone-1 scope this session covers, since P2-WP6 follows P2-WP4/WP5).

---

## 15. Floor board is a status kanban, not the bay/lift visualization; Job detail ships without Work/Parts/Money for Milestone 1 (P2-WP3)

- **Approved design behavior:** `prototype.html`'s "Floor control" (`isOps`) screen is an elaborate shop-floor ops dashboard — a physical bay/lift visualization with technician lanes, cash charts and a promise clock, none of which is backed by any P2-WP3 data (no bay, lift, or technician-schedule concept exists anywhere in the accepted backend). Separately, `prototype.html`'s Job detail (`isJob`, item 2 above) includes a "Work & parts, one list" line-item editor and a "Money" panel with approved-total/collected/balance figures.
- **Implementation limitation:** `GET /api/v1/jobs/floor-board` (the actual accepted P2-WP3 endpoint) returns Jobs grouped into `JobStatuses.OpenBoardOrder`'s columns (`checked_in` → `invoiced`) — a status kanban, not a bay layout. No bay/lift/technician-schedule data exists to render the prototype's ops dashboard against. Similarly, no Estimate/Invoice/Part data exists yet (P2-WP4/WP5/WP6, explicitly out of scope for this order) to populate the Work/Parts/Money cards with anything real.
- **Proposed change (Design Lead, acting under this order's "compatible extension" provision):** Floor board renders as a column-per-status kanban, one column per `FloorBoardResponse.columns[].status` in the order the backend returns them, each column labelled with a friendly title (Checked In / Estimate Pending / Awaiting Approval / Approved / In Progress / Completed / Invoiced) over the ratified enum value shown in mono underneath — reusing the existing card visual language (rounded `#101416` panel, `#1e2225` border, mono meta line) from the Customers-list row and the Job detail's evidence/feed treatments rather than inventing new chrome. Each card shows job number, customer + vehicle display strings, primary mechanic, promised time, and waiting/overnight/warranty badges — all fields `FloorBoardCardDto` actually returns. Job detail ships for Milestone 1 as the header identity card (vehicle/job number/status pill/customer/mechanic/promise time), a Complaint/Notes card (reusing the two-column complaint/diagnosis card, populated from the real `CustomerComplaint`/`AdvisorNotes` fields), a status-workflow sidebar (the prototype's vertical step-list, walking the ratified state chain, with one button per currently-allowed transition rather than a single generic "advance" — the real state machine branches into `cancelled`/`deleted` from most states, which a single advance button can't express), and a Live feed panel reusing the prototype's timeline visual, populated from real `JobHistoryEntry` rows. The Work & parts list and Money panel are omitted entirely for Milestone 1 — not shown disabled or as inert placeholders, simply not rendered — since Estimate/Invoice/Part are P2-WP4/WP5/WP6 concerns this order explicitly excludes ("Do not invent future Estimate/Invoice functionality").
- **Reason:** The prototype's ops-dashboard Floor screen and Work/Money Job-detail cards depict later-phase or unbuilt data (bay/lift scheduling was never scoped for any phase; Estimate/Invoice/Parts are P2-WP4/WP5/WP6); rendering them now would mean fabricated numbers behind a real-looking screen, which the Owner's "no fake JSON, no mocks" instruction for this milestone forecloses. The kanban and the trimmed Job detail are both compositions of already-ratified visual primitives (card panel, mono meta line, step list, timeline feed, status-tinted badge), not invented chrome.
- **Impact:** Frontend Engineer builds Floor as a kanban of real columns/cards, and Job detail without Work/Parts/Money for this milestone. No backend impact — `GET /api/v1/jobs/floor-board`, `GET /api/v1/jobs/{id}`, and `GET /api/v1/jobs/{id}/history` are consumed as-is. Gates the Milestone 1 Owner Visual Checkpoint this order requires. Work/Parts/Money return to Job detail when P2-WP4/WP5/WP6 ship real data behind them.

*Log format for future entries:*

```
## <N>. <short title>

- Approved design behavior:
- Implementation limitation:
- Proposed change:
- Reason:
- Impact:
```
