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

*Log format for future entries:*

```
## <N>. <short title>

- Approved design behavior:
- Implementation limitation:
- Proposed change:
- Reason:
- Impact:
```
