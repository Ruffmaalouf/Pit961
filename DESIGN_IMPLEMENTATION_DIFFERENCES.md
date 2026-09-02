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
- **Proposed change:** Frontend token layer uses the prototype-accurate values instead: border `#1e2225` (page-card) / `#1e2427`-family (item-card), muted-text scale `#8b959b/#7c858c/#6c757a/#5f696e/#4f585d`, green `#59a97a`, amber `#c98a2f`, red `#d1564c`, purple `#8b7bd6`, type weights capped at 400/500/600, input focus border `#e2892f` (not blue), 76px icon-only sidebar rail (not 220px labeled), toast bottom-center/orange/~2.4s (not bottom-right/navy/3s). `09_design_system.md` should be corrected to match.
- **Reason:** Prototype is canonical per item 1; this entry closes the loop with the specific corrected values needed for the WP-8 engineering token file, which the general item-1 note didn't enumerate.
- **Impact:** No functional/business-logic impact. Frontend Tailwind/shadcn theme config must use the values above, not the doc's remaining stale numbers.

---

*Log format for future entries:*

```
## <N>. <short title>

- Approved design behavior:
- Implementation limitation:
- Proposed change:
- Reason:
- Impact:
```
