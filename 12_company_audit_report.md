---
title: PIT961 "Garage OS" — Company Project Audit
prepared_by: AI Company Dispatcher (Chief of Staff)
date: 2026-08-26
specialists_consulted: Product, Design, Architecture, Frontend, Backend, Database, QA, Security
scope: Read-only audit. No code was written or modified.
---

# PIT961 "Garage OS" — Company Project Audit

**Owner:** Ralph
**Prepared by:** Dispatcher, on independent findings from 8 specialist reviews (Product, Design, Architecture, Frontend, Backend, Database, QA, Security)
**Method:** Every specialist independently read the project's source documents on disk (`D:\Ralph\PIT961`) — 11 numbered planning docs, a design-tool prototype (`prototype.html` + `support.js`), and a leftover local script (`extract.js`) — and cross-checked them against each other. No code was written, no files were modified.

---

## Addendum — Owner Resolutions Applied (2026-08-26, second pass)

The owner resolved the five primary blockers from §7 below. Documents have been edited accordingly (by Product/Design and Architecture specialists, working directly on the files in `D:\Ralph\PIT961`). This addendum records what changed; §0–§7 below are left as the original audit for history, with resolved items marked.

**1. Design source of truth → `prototype.html` is canonical.** `09_design_system.md` was corrected to describe the prototype's actual dark theme, orange (`#e2892f`) primary accent, IBM Plex Sans/Mono type, custom CSS nav glyphs, and 5px-radius badge component, with RTL/responsive sections kept as an explicitly "not yet implemented" future pass rather than deleted. `04_information_architecture.md` and `05_screen_inventory.md` were corrected to the prototype's actual nav taxonomy (Floor/Clock/Jobs/Customers/Money/Parts/Team/Reports) and its single-page Job Detail (vs. the originally documented 7 tabs). Screens with no prototype counterpart (Check-In wizard, Estimate builder, Settings) are now flagged "designed on paper, not yet prototyped" rather than left as silent contradictions. **Two small loose ends remain in `09_design_system.md`** (the Workshop Stage color-mapping table and the Timeline/History component header still show the old blue palette) — cosmetic, non-blocking, easy fast-follow.

**2. Pricing → $30 USD/month per garage, ratified.** Updated in `02_module_classification.md` and throughout `11_engineering_handoff.md` (Product Summary, §44 Subscription/Stripe Flow). Prior tiered-pricing language (a reviewer's proposal, never ratified) is marked superseded.

**3. Multi-garage → one subscription per garage in Phase 1; architecture reserved for later.** `11_engineering_handoff.md` §9 now has a new `accounts` table sitting above `garages` (billing/ownership only — `stripe_customer_id`, `subscription_status`, `plan`, `trial_ends_at`); `garages.account_id` FK added; billing/trial fields moved off `garages` onto `accounts`. Phase 1 enforces one garage per account at the **application layer**, not a DB constraint, so multi-garage later is additive, never a migration. Tenant isolation (`garage_id` on every business table) is unchanged. `02_module_classification.md` got a new explicit module entry documenting this Phase-1 UI scope decision.

**4. Platform Admin → architected as a separate identity.** `11_engineering_handoff.md` §60 now specifies a `platform_admins` table structurally independent of `users`/`garage_id`, a distinct JWT claim (e.g. `aud: platform-admin`) mutually exclusive with garage-tenant tokens, and five capability groups (account/garage management, subscription/billing, support ops incl. impersonation — flagged as needing its own audit-event type, platform config, cross-account reporting). Table + claim design is reserved now; the actual admin UI/endpoints remain Phase 6 scope.

**5. Hosting → deferred, provider-neutral.** `11_engineering_handoff.md` §3 now states explicitly that the hosting decision is not a Phase 1 blocker, that Foundation-phase code should stay provider-neutral (Docker, env-var config, portable SQL), and that the Technical Architect must present a full hosting recommendation (frontend/backend/DB/storage/backups/secrets/CI-CD/cost/scalability/Lebanon-MENA latency) before any staging deployment, for owner review. Nothing has been deployed.

**Contradictions also fixed while reconciling the above:** the internal-approval gate is now an explicit step in `01_garage_workflow.md`; the QC "Senior Technician" reference is removed (Manager/Owner only, matching the permission matrix); the Manager revenue/expense self-contradictions in `03_role_experiences.md` are resolved; the permission matrix's Accountant "Admin"-on-view-only-rows mislabeling is fixed with a legend footnote; Manager can now see the debt data the matrix already lets them send a reminder about; the $500 estimate-approval threshold now explicitly applies to whichever role creates the estimate, not Manager only; Mechanic-hidden PII fields (phone/WhatsApp/last name) are now explicit; the `ready_to_repair` job status is now documented as a Waiting-Parts sub-status, not an undocumented 9th board column; a reserved (unused-in-Phase-1) currency/display-rate field was added to the schema per the standing reviewer warning; `DESIGN_IMPLEMENTATION_DIFFERENCES.md` was created and now logs all of the above deviations, as the engineering handoff's own process requires. `extract.js` was moved to `_archive/` (it hardcoded a real local username/path and isn't needed).

**The remaining owner decisions, recategorized after this resolution pass, are below in the main dispatcher reply.**

---

## Addendum 2 — Final Three BLOCKS CODING Decisions Resolved (2026-08-26, third pass)

The owner resolved the three remaining BLOCKS CODING items. Documents updated:

**1. Brand name → deliberately left undecided; architecture made brand-neutral.** "RASHID" and "GarageOS" are both explicitly rejected as decided final brands (RASHID documented as leftover prototype placeholder in `09_design_system.md`'s new Branding section; "PIT961" is the internal codename). `04_information_architecture.md`'s wireframes no longer assert "GarageOS" as settled product copy. `11_engineering_handoff.md` gained a new **§7A Branding & Configuration** section requiring: configurable product display name, configurable email-from name, config-driven (not hardcoded) JWT issuer/audience, replaceable logo assets, and no coupling of customer-facing copy to internal namespaces/package names — while explicitly permitting internal code (namespaces, repo name) to keep the PIT961/GarageOS codename permanently. `DECISIONS.md` was created (project root) logging this and all other decisions to date.

**2. Authorization → policy-based framework, minimal initial scope.** `11_engineering_handoff.md` §28 was expanded in place: an ASP.NET Core requirements/handlers architecture (not scattered role checks) that can express roles, permissions, contextual rules, amount-based limits, tenant boundaries, and resource ownership — with Phase 1 scoped to exactly two concrete handlers (`DiscountLimitHandler` for the 15% Manager cap, `EstimateApprovalThresholdHandler` for the $500 rule), explicitly no generic rules engine.

**3. Email → Resend behind `IEmailService`.** New **§11A Email Service** in `11_engineering_handoff.md` specifies `IEmailService`/`ResendEmailService`, Phase 1 capabilities (password reset, transactional email; email verification flagged optional/TBD pending the registration-flow design), and defers SMS/WhatsApp provider choice to their own feature phases without blocking anything now.

**Blocker status: zero remaining owner decisions block Phase 1.** The Phase 1 engineering execution plan is being presented to the owner for approval separately (see `13_phase1_execution_plan.md`).

---

## 0. Dispatcher Summary (read this first)

**COMPLETED**
Product/UX planning is unusually mature for a pre-code project: an 11-stage garage workflow, an 18-module MVP scope with clear rationale, a 5-role permission model, and 17 well-structured edge cases. The technical architecture is equally mature: stack is decided (React/TS + ASP.NET Core 8 + PostgreSQL), multi-tenancy is a real row-level design with explicit enforcement rules and mandated isolation tests, auth (JWT + rotating refresh tokens) is fully specified, and payment idempotency / job-number concurrency are correctly solved. A working interactive prototype exists covering the core workshop-board and payment flows.

**IN PROGRESS**
Nothing is currently "in progress" — **zero implementation code exists**. There is no frontend repo, no backend repo, no database, no `git init`. The project is 100% in the documentation/design-handoff stage. The 49KB engineering handoff (`11_engineering_handoff.md`) is ready enough that Phase 1 (bootstrap) could start immediately once the owner decisions below are made.

**BLOCKED / FAILED**
Nothing has failed. The main blocker to starting implementation is not technical readiness but **unresolved conflicts and undecided questions** listed below — several specialists independently flagged the same handful of blockers (see §7).

**QA STATUS**
QA reviewed the only tangible running artifact (the prototype) against the documented rules and found two concrete, reproducible defects: it leaks cost/margin data to a role that should never see it, and it hard-blocks vehicle delivery in a way that contradicts both the edge-case doc and the engineering handoff's own config flag. QA also found the permission matrix, role docs, and product workflow doc contradict each other in four places, and identified ~10 real-world failure modes (concurrency, offline handling, mid-session deactivation, subscription suspension, customer merge) that no document currently addresses.

**SECURITY STATUS**
No live attack surface exists yet (nothing is deployed), so this is a plan review, not a penetration test. The core design — tenant isolation, auth, file-upload handling — is genuinely above average for a pre-build document. But there is **no secrets-management strategy at all**, a leftover script (`extract.js`) hardcodes a real Windows username and internal file paths and isn't excluded by `.gitignore`, the $500 estimate-approval control doesn't clearly bind the Advisor role, and PCI scope / customer-approval-link security / platform-admin isolation are all named as goals without a concrete mechanism.

**DECISIONS REQUIRED FROM OWNER** — see the full list in §7. The five that block the most downstream work:
1. Is `prototype.html` or `09_design_system.md` the canonical visual design? They currently describe two different products.
2. What is the actual pricing/packaging model? ($30/mo appears in the prototype and handoff but was never formally decided.)
3. Is multi-garage/chain support ever in scope, even as a schema consideration?
4. What is the platform-admin identity model? (The current schema architecturally cannot represent one.)
5. Final hosting providers (frontend, backend, DB, file storage) — everything in Phase 1 (CI/CD, secrets, backups) waits on this.

**RECOMMENDED NEXT ACTION**
Do not start coding yet. Hold one owner decision session against §7 of this report (most items are quick yes/no/pick-one calls), record the answers in a `DECISIONS.md` file, patch the ~6 document contradictions in §5, then greenlight Phase 1 (Foundation) exactly as scoped in `11_engineering_handoff.md` §68/§71. Nothing here requires new research — it requires the owner's judgment calls.

---

## 1. Project State Overview

The `D:\Ralph\PIT961` folder contains:

- **Product/design docs 01–07, 09–11** (`01_garage_workflow.md` through `11_engineering_handoff.md`) — no `08_*.md` exists. The gap sits exactly where a "states/interactions" or "business rules" doc would belong, and several findings below (loading/empty/error states, business-rules-as-reviewer-commentary) trace back to this same absence. **Owner should confirm whether "08" was lost, renamed, or never written.**
- **`prototype.html` + `support.js`** — a self-contained interactive mockup built with a design-canvas tool (dark theme, orange accent, IBM Plex fonts). It is real, working HTML/CSS/JS, but it is **not** built on the approved React/TypeScript stack and is not intended to be extended in place — `support.js` is a generic runtime bundled by the design tool itself, not GarageOS application code.
- **`extract.js`** — a small leftover personal utility script that already did its job (or a near-duplicate of it); it targets a `garage-os-v2/` subfolder that doesn't exist in this repo and hardcodes a real local Windows path and username. It should be deleted or excluded, not treated as a pending task (see §5, §7).
- **No `frontend/`, `backend/`, or database of any kind.** No `package.json`, no `.csproj`, no `src/` tree, no `git init`.

**Bottom line:** this project has not yet started implementation. It has, unusually, already done a large and mostly high-quality amount of the planning work that engineering teams normally do badly or not at all (multi-tenancy model, financial-integrity rules, idempotent payments, phased build order). The work remaining before Phase 1 can start cleanly is decision-making and document reconciliation, not more research.

---

## 2. COMPLETED

These are genuinely solid, internally consistent, and ready to build from as-is.

| Item | Role | Notes |
|---|---|---|
| Garage workflow (11-stage lifecycle, `01`) | Product | Thorough, independently validated against real garage-floor operations. |
| MVP module scope (18 MVP / 10 V1.1 / 1 Later, `02`) | Product | Defensible cuts with stated rationale for every deferral. |
| Edge-case coverage for operational scenarios (17 cases, `07`) | Product/QA | Unusually rigorous for a pre-engineering doc (trigger/behavior/UI/data-state per case). |
| Role model & permission matrix (overall) | Product/Backend | 5-role model and matrix agree closely once the flagged contradictions (§5) are patched. |
| Database technology & ORM decision (PostgreSQL 15+, EF Core 8) | Architecture | Fully decided. |
| Multi-tenancy strategy (row-level, `garage_id`, EF Core global filters + mandatory explicit checks) | Architecture/Database | Explicitly designed, with a worked cross-tenant test example and a mandated isolation-test matrix across 11 resource types. |
| Auth mechanism (JWT + rotating/revocable refresh tokens, Argon2id-equivalent hashing) | Backend | Fully specified and implementation-ready. |
| Job/invoice number concurrency (per-garage sequence table, atomic `UPDATE...RETURNING`) | Backend/Database | Correctly avoids the classic `MAX(id)+1` race condition. |
| Payment idempotency (client UUID + unique index + 9-step transactional recording) | Backend/Database | Resolves a gap the product review itself flagged — a real example of review feedback being closed. |
| Financial-integrity & correction model (immutability, reversal/void/refund, revision-preserves-approval) | Backend/Database | Well specified. |
| Core transactional schema (12 of ~26 entities have full DDL: garages, users, customers, vehicles, jobs, repair_tasks, estimates, estimate_items, job_parts, invoices, payments, job_history) | Database | Good baseline to build EF Core entities from directly. |
| File-upload security design (MIME/extension/size validation, tenant+entity ownership check, non-guessable storage keys, signed URLs, no inline HTML/SVG execution) | Security | Solid; only AV scanning is left as an open question (§7). |
| SQL-injection mitigation (parameterized queries mandated) | Security | Correctly and simply handled. |
| Payment-record modal in the prototype | Frontend/Design | Closely matches the documented spec (amount input, partial/full buttons, balance recalculation) — a legitimate reference for the real React component. |
| Workshop-board 8-stage taxonomy | Frontend/Design | Data model (stage names/order) matches the docs exactly, even though the visual/interaction pattern (timeline vs. Kanban card) differs. |

---

## 3. PARTIALLY COMPLETED

| Item | Priority | Role | Dependencies | Acceptance Criteria |
|---|---|---|---|---|
| Prototype screen coverage (~9 of 30 documented screens; Job Detail collapsed from 7 tabs into 1 page) | High | Design/Frontend | Decision in §7.1 | Missing screens (Check-in, Estimate builder, Settings, Manager/Accountant views) get mockups; tab-vs-single-page IA is reconciled with `05_screen_inventory.md`. |
| Fine-grained permission matrix (`06`) not translated into concrete authorization policies (amount-based rules like the 15% discount cap and $500 threshold have no policy design) | Critical | Backend | §7.11 decision | Explicit policy names + evaluation logic documented for every amount-based rule; covered by the permission test suite. |
| Customer Approval flattened onto `estimates` table with no dedicated approval-event table | High | Database | None | Either ratify the flattened design explicitly, or add a `customer_approvals` table capturing per-event method/timestamp/approver. |
| Role/permission model stored as flat `users.role TEXT`, with no schema path toward "granular permissions later" (as the handoff itself says is the goal) | Medium | Backend/Database | None | Either accept TEXT-role as final for MVP and drop `Role`/`Permission` from the entity list, or add a lightweight `permissions` table now. |
| Mechanic-scope enforcement specified at the Job level but not explicitly required on every child entity (tasks, parts, diagnosis, photos) | Medium | Backend | None | Tenant/permission test suite explicitly asserts a reassigned mechanic loses access to a job's child records, not just the job record. |
| CSRF protection under-specified given cookie-based refresh tokens | High | Backend/Security | Cookie vs. bearer-token decision | SameSite policy + anti-forgery/double-submit pattern documented and tested. |
| General API rate limiting covers only auth-adjacent endpoints, not general write/search/export traffic | Medium | Backend/Security | None | Rate-limiting policy extended to authenticated general API traffic. |
| SignalR realtime events scoped by garage (good) but not by role/sensitive field | Medium-High | Backend/Security | None | Confirm realtime payloads never include cost/margin/salary fields, or move to role-scoped sub-groups. |
| Delivery-with-balance override is a single binary garage setting, not the per-transaction, role-gated, audited override the product review originally specified | Medium | Backend | §7.13 decision | Either document the simplification as an accepted deviation, or add a per-delivery override action with an audit event. |
| Diagnosis Fee Policy and Warranty Period exist in the engineering handoff's settings JSON but are missing from the Settings screen field list in `04`/`05` | Medium | Product/Design | None | Add both fields to the canonical Settings screen spec so Product/Design and Engineering agree. |
| Testing strategy (`11` §63) is well-structured but has no coverage thresholds or CI-gating rule, and no rounding/precision test call-out for money math | Medium | Backend/QA | None | Add explicit coverage/CI-gating expectations and a decimal-rounding test suite before Phase 7. |
| Same-complaint-pattern detection (Edge Case 10) has no defined matching rule for free-text complaints | Medium | Engineering/Product | §7.22 decision | A concrete, testable rule replaces the unimplementable "same complaint pattern" language. |
| Estimate item-level "supplemental estimate" workflow has no defined trigger for re-entering "waiting approval" | Low | Backend | None | Define the trigger condition (e.g., any revision, or only above a $ threshold). |
| Seed data / mandatory E2E test scenario (`11` §61–62) references QC and Customer Approval flows that don't yet have schema | Low | Backend/QA | Database findings above | Re-verify the E2E scenario is fully persistable once QC/Approval/Audit schemas exist. |
| Documented "Data Integrity Checks Needed" and QC state-machine transition rules live only inside reviewer commentary (`10_product_review.md`), not in a canonical spec | High | Product/Backend | None | Extract these rules into `06` or a dedicated business-rules doc as authoritative requirements. |

---

## 4. MISSING

Organized roughly by how early they block work.

### Foundational / blocks everything
| Item | Priority | Role | Dependencies | Acceptance Criteria |
|---|---|---|---|---|
| No frontend or backend repository exists — no scaffold, no `git init`, despite a `.gitignore` already anticipating the exact stack | Critical | Frontend/Backend | Owner decisions in §7 (esp. hosting, visual-source-of-truth) | `frontend/` and `backend/` scaffolds exist, build/run, and are committed, per `11_engineering_handoff.md` §71 steps 10–22. |
| No secrets/credential storage strategy anywhere (JWT signing key, Stripe key, DB connection string) | Critical | Architecture/Backend/DevOps | Hosting decision | A section documents where secrets live at runtime, that they're never committed, and a rotation policy; CI secret-scanning added. |
| No chosen email/SMS delivery provider — blocks password reset (an MVP item) and every background-job notification | Critical | Backend/Architecture | None | Provider selected and wrapped in an abstraction before Phase 1 closes out. |
| Required engineering tracking docs never created (`IMPLEMENTATION_MAP.md`, `PROGRESS.md`, `DECISIONS.md`, `KNOWN_ISSUES.md`, `TEST_STATUS.md`, `DESIGN_IMPLEMENTATION_DIFFERENCES.md`) | High | Dispatcher/Frontend | None | All six exist in `docs/`, kept current from Phase 1 onward. |

### Data model
| Item | Priority | Role | Dependencies | Acceptance Criteria |
|---|---|---|---|---|
| No ERD or consolidated relationship diagram for the schema | High | Database/Architecture | None | One ERD/FK summary produced from existing DDL, reviewed before migrations are written. |
| Quality Control has no table schema despite being an MVP module with a defined API field list | High | Database/Backend | None | `quality_control` (+ items) tables added before Phase 3. |
| Audit log has no table schema despite being a named entity and MVP checklist item | High | Database/Backend | None | Decide and schema an `audit_log` (or extended `job_history`) table with append-only enforcement and a retention policy. |
| `refresh_tokens` table described only in prose, no DDL | Medium | Database/Backend | None | Table added to schema before auth work begins. |
| `garage_settings` has no table despite being called out as needing type-safety | Medium | Database/Backend | None | Typed columns or table added for currency/tax/labor-rate/warranty/diagnosis-fee fields. |
| `ON DELETE` behavior unspecified for every foreign key | Medium | Database | ERD work | Each FK gets an explicit delete-behavior decision consistent with "never hard-delete financial data." |
| Soft-delete pattern applied only to `jobs`, not customers/vehicles/estimates/invoices | Medium | Database | None | Add soft-delete columns to financial/customer entities, or document why `jobs` alone needs it. |
| Payment "reversal" is a documented API endpoint with no corresponding schema field or table | Medium | Database/Backend | None | Decide and schema how a reversal is persisted (never as a mutation of the original row). |
| `VehicleOwnershipHistory` named as an entity with zero further elaboration anywhere | Low | Database | None | Remove from entity list, or add rationale + minimal schema. |

### Business logic / API surface
| Item | Priority | Role | Dependencies | Acceptance Criteria |
|---|---|---|---|---|
| Account/garage self-registration has no API contract | High | Backend | Pricing decision (§7.2) | `POST /auth/register` (or equivalent) defined, including trial-start behavior. |
| Team-invite acceptance flow has no endpoint (only the invite-send endpoint exists) | High | Backend | None | Invite-token lifecycle + accept endpoint defined before Phase 2. |
| Notification system has a named entity but no schema, delivery channel, or preferences API | High | Backend/Product | §7 channel decision | `notifications` table, delivery mechanism, and preferences API defined before Phase 5. |
| Notification business rules (which event → which channel → which role → default timing) not specified anywhere, despite Product docs implying configurable thresholds | High | Product | None | A notification-rules table exists covering at minimum estimate-follow-up, overdue jobs, ready-for-pickup, warranty-return. |
| "Merge duplicate customers" permission exists with no workflow, data-migration, or FK-repointing design | Medium-High | Backend/Product | None | A design note (or edge-case writeup) covers which record survives, FK repointing, and audit trail. |
| Bad-debt / write-off endpoints don't exist (Finance API is read-only) | Medium | Backend | None | Mutation endpoints + audit events defined before Phase 4. |
| Concurrency/conflict handling for simultaneous edits (two staff editing the same job/estimate) is entirely absent | High | Backend | None | Optimistic-locking strategy (e.g., row-version + 409 on stale write) defined and tested. |
| Offline/poor-connectivity behavior for the mechanic mobile flow is undefined, despite being explicitly flagged in product review | Medium-High | Product/Frontend | None | At minimum, a documented behavior (even "retry button, no queue" for MVP) for task-complete/photo-upload failures. |
| Role-deactivation/role-change mid-session behavior unspecified (token invalidation timing, in-flight task reassignment) | High | Backend/Security | None | Deactivation invalidates the session within a bounded window; orphaned in-progress tasks surface for reassignment. |
| Customer-facing "secure estimate approval link" has no token/expiry/single-use/scope design | High | Backend/Security | None | Token design (entropy, expiry, single-use, tenant+job binding, excluded sensitive fields) documented before V1.1 build. |
| WhatsApp integration has no provider, template-approval workflow, or consent/opt-in data model | Medium (V1.1) | Architecture/Backend | None | Integration spec added before Phase 5. |
| Data import/migration tooling (CSV import of existing customers/history) has no module classification at all | Medium | Product | None | Given an explicit classification (even if "Later, manual CSV only"). |

### Design / frontend coverage
| Item | Priority | Role | Dependencies | Acceptance Criteria |
|---|---|---|---|---|
| Check-in flow (the most time-critical flow in the product) has zero prototype/mockup coverage | Critical | Design | §7.1 | Mockups for all 4 check-in steps exist in the chosen visual system. |
| No responsive/mobile implementation anywhere despite mobile being mandatory for two roles | Critical | Design/Frontend | §7.1 | A ≤768px mobile mockup exists (mechanic view, owner dashboard). |
| Estimate creation/approval UI (Screens 14–15) not implemented anywhere despite being one of the most financially load-bearing flows | High | Design/Frontend | §7.1 | Mockups exist before the Estimate API is wired to real UI. |
| Settings screens (Garage Profile, Team Management, Subscription) completely absent from the prototype | High | Design | §7.1 | Design coverage exists before Phase 6 (SaaS/billing) implementation. |
| No empty, loading, or error states implemented anywhere, despite being fully specified per-screen in the docs | High | Design/Frontend | None | At least one reusable pattern per state type is designed and shown. |
| RTL/Arabic support (a named design principle) has zero implementation or mockup | Medium | Design | §7 scoping decision | At least one RTL-mirrored mockup of a core screen exists if RTL is confirmed in scope. |
| No test scaffolding (Vitest/RTL/Playwright) exists — expected, since no app exists yet | Medium | Frontend/QA | Repo scaffold | Test scaffolding stood up alongside the app scaffold in Phase 1, not deferred. |
| No data-retention, backup, or privacy policy exists at the product level | High | Product/Owner | Legal input | Retention period, backup/restore expectations, and regional compliance scope documented. |

---

## 5. INCORRECT

Real contradictions between documents, or between the prototype and the documented rules — these should be patched before they're used as a build reference.

| Item | Priority | Role | Detail |
|---|---|---|---|
| Prototype leaks cost/margin data to the Advisor role | High | QA/Security | Permission matrix says Advisor gets `None` on cost/margin; the prototype only hides money from Technician. Reproducible today. |
| Prototype hard-blocks delivery on any outstanding balance | High | QA | Contradicts Edge Case 8 and the engineering handoff's own `allowDeliveryWithBalance: true` config flag; product review calls exactly this behavior a "product-killer" for cash-heavy markets. |
| Manager's dashboard revenue visibility contradicts itself across two docs | High | Product/QA | `06_permission_matrix.md` gives Manager `None` on revenue KPIs; `03_role_experiences.md` says "Revenue visible but not cost/margin" for the same role. `03` also separately contradicts itself on expense visibility for Manager. |
| Permission matrix internally inconsistent on customer debt (Manager can send a payment reminder but cannot view any debt) | Medium | Product/QA | Needs a defined visibility scope for Manager before the reminder feature ships. |
| Platform Admin requirement is architecturally unimplementable with the current schema | Critical | Architecture/Database | `users.garage_id` is `NOT NULL`, but the handoff separately mandates platform admins never be represented as garage users. No alternate identity model exists. |
| Job state machine introduces a `ready_to_repair` status with no corresponding board column in the approved 8-column IA | High | Architecture/Product | Undocumented deviation from the workflow docs; the handoff's own process requires deviations to be logged in `DESIGN_IMPLEMENTATION_DIFFERENCES.md`, which doesn't exist yet. |
| Multi-currency/hyperinflation warning from product review was never incorporated into the schema | High | Database/Product | Reviewer explicitly warned this "must be designed in V1.0 to avoid a breaking schema change later"; still just a single `currency` field. |
| Permission matrix's "Admin" label used inconsistently with its own legend (Accountant marked "Admin" on view-only financial rows) | Medium-High | Product/Backend/Security | Legend defines Admin as "full control including voids/overrides"; if implemented literally off the label, Accountant could be over-granted void/override rights the matrix elsewhere explicitly denies. |
| Sensitive-field DTO rules (§30) omit customer PII (phone/WhatsApp) from the Mechanic-restricted field list, though the permission matrix requires it hidden | High | Backend/Security | Risk of the job-card DTO for mechanics shipping with full contact fields present by default. |
| $500 estimate-approval threshold is specified only for Manager; Advisor has no equivalent ceiling in the matrix | High | Security/Backend | As written, Advisor could create/approve an estimate of any size with no Owner sign-off. |
| QC executor role contradiction | Medium | Product | Workflow doc (`01`) says QC can be done by "Manager / Senior Technician / dedicated QC role"; permission matrix restricts it to Owner/Manager only, and no "Senior Technician" role exists in the 5-role model at all. |
| Internal approval gate missing from the core workflow | High | Product | `06`'s $500 Owner-approval rule has no corresponding step in `01_garage_workflow.md`'s estimate flow, and no state exists for a threshold-blocked estimate in the Kanban model. |
| `extract.js` hardcodes a real developer Windows username and internal file paths; not excluded by `.gitignore` | High | Security | Fixable now, before first `git init` — see §7. |
| `.gitignore` excludes `appsettings.Development.json` variants but not plain `appsettings.json` | Medium | Backend/DevOps | A real production secret placed directly in `appsettings.json` (a common mistake) would be committed. |
| `job_parts.supplier_id` is an unconstrained pseudo-FK (no `suppliers` table exists yet, unlike every other FK in the schema) | Medium | Database | Intentional given Suppliers is V1.1 — should be documented as such rather than discovered as a "bug" mid-build. |

---

## 6. BLOCKED

| Item | Blocked on | Role |
|---|---|---|
| All frontend/backend implementation | Owner decisions in §7 (visual source of truth, hosting, pricing) | Frontend/Backend/Architecture |
| Real QA test execution (only test *plans* can be written now) | Backend/database/frontend existing to run tests against | QA |
| Platform-admin (Phase 6/SaaS) work | Platform-admin identity model decision (§7.4) | Architecture/Backend |
| WhatsApp/notification module (Phase 5) | Provider selection + notification-rules spec | Backend/Product |
| Schema finalization for QC, Customer Approval, Audit Log | Database findings in §4 | Database |
| CI/CD, backup strategy, secrets management | Hosting provider decision (§7.5) | Architecture/DevOps |

---

## 7. NEEDS OWNER DECISION

**Items 1–5 below are RESOLVED — see the Addendum at the top of this document.** The remaining items (6–25) have been recategorized into BLOCKS CODING / CAN BE DECIDED DURING IMPLEMENTATION / CAN WAIT UNTIL BEFORE LAUNCH, delivered separately to the owner. This original list is kept for history.

These are genuine judgment calls only Ralph can make. Ordered by how much they block.

1. **Visual source of truth (Critical).** `prototype.html` (dark theme, orange accent, IBM Plex fonts, single-page Job Detail, no RTL/mobile) and `09_design_system.md` (light theme, blue accent, Inter font, emoji icons, tabbed Job Detail, RTL-first) describe two different products. Pick one as canonical and revise the other, or explicitly split authority (e.g., prototype governs data density/layout ideas, 09 governs the actual token system).
2. **Pricing & packaging (Critical).** "$30/month" appears in the prototype and engineering handoff as if settled, but no product doc actually ratifies it — it originated as a reviewer's recommendation. Confirm base price, per-seat pricing, multi-garage pricing, and trial terms.
3. **Multi-garage / "Garage Groups" (High).** Not in MVP scope anywhere, but a reviewer warned a 5-garage chain will hit a hard wall almost immediately. Decide: fully out of scope, a documented future module, or a schema consideration to bake in now.
4. **Platform-admin identity model (Critical, architecturally blocking).** The current schema cannot represent a platform admin at all (`users.garage_id` is `NOT NULL`). Decide the separation mechanism (distinct table, distinct JWT audience, separate login surface) before Phase 6.
5. **Final hosting providers (High).** Frontend, backend, database, and file storage are all still "X or equivalent." Everything in Phase 1 (CI/CD, secrets management, backup strategy, region for MENA latency) waits on this.
6. **Currency / hyperinflation display (High).** A reviewer explicitly warned this needs a schema-safe field added in V1.0 even if the feature ships later, to avoid a breaking migration. Still unresolved. Decide now.
7. **Data retention / privacy / regional compliance (High).** No policy exists anywhere for customer PII retention, backup/restore guarantees, or MENA-region data-protection requirements. Needs owner + likely legal input.
8. **Onboarding/setup wizard (High).** A reviewer called this "a V1.0 requirement, not V1.1"; it has no module classification at all. Decide explicitly rather than let it default to "not built."
9. **Permission-matrix fidelity (Critical).** Decide whether MVP authorization implements the full ~150-rule granular matrix (including amount-based rules like the 15% discount cap and $500 threshold) or a coarser role-based approximation — and if the latter, which rules are allowed to be lost for MVP.
10. **$500 estimate-approval threshold scope (High).** Does it apply to every role that can create/approve estimates (including Advisor), or Manager only? Is it evaluated pre- or post-discount?
11. **QC executor role (Medium).** Keep QC as Manager/Owner-only (per the permission matrix) and fix the workflow doc's "Senior Technician" reference, or formally add a Senior Technician role.
12. **Delivery-with-balance override (Medium).** Keep the simple binary garage setting, or build the per-transaction, role-gated, audited override the product review originally called for?
13. **Postgres Row-Level Security as defense-in-depth (High).** The tenant-isolation design is application-layer only (EF Core query filters + explicit checks); native Postgres RLS as a second layer was never evaluated. Worth an explicit sign-off given this is a compliance-adjacent, multi-tenant SaaS.
14. **Backup/DR targets (High).** No RPO/RTO or backup cadence defined anywhere, despite financial history being mandated immutable.
15. **File-upload malware scanning (Medium).** Left as "consider" — needs a yes/no cost decision before broader document uploads ship.
16. **PCI scope confirmation (High).** Confirm card data is captured exclusively via Stripe-hosted fields and never transits the GarageOS backend, and add that constraint explicitly to the spec.
17. **Labor billing split (Medium).** When two mechanics work one job, is billing combined hours or separate labor lines? Directly affects invoice totals and margin math.
18. **Bad debt vs. write-off (Low-Medium).** The matrix treats these as two distinct Owner-only actions; the edge-case doc only models one state. Define the distinction (does bad debt stop collections while staying on the books, vs. write-off removing it from P&L?).
19. **Subscription-suspended-with-open-job behavior (Medium).** If a shop's subscription lapses while a car is mid-repair, can existing jobs still be progressed/closed/invoiced? Currently only "new writes are blocked" is specified.
20. **Same-complaint-pattern detection rule (Medium).** Edge Case 10's matching logic ("same complaint pattern") is currently unimplementable free-text matching. Pick a concrete rule (e.g., any job on the vehicle within the warranty window triggers the banner, regardless of complaint text).
21. **Fleet-customer vehicle-count threshold (Low-Medium).** Mechanism is designed; no default numeric threshold exists.
22. **RASHID branding in the prototype (Low-Medium).** Sidebar logo/label says "RASHID," not GarageOS. Confirm this is a stale template artifact before it's used as any kind of visual reference.
23. **Data import/migration tooling scope (Medium).** Two reviewers called Day-1 CSV import a near-requirement for the sales motion; currently has no module classification.
24. **Missing "08" document (Medium).** Confirm whether a doc was lost, renamed, or intentionally skipped — several findings above (states/interactions spec, business-rules spec) plausibly belong there.
25. **`extract.js` disposition (Low, easy).** Delete it or move it out of the deliverable tree before `git init` — it hardcodes a real local username/path and targets a folder structure this repo doesn't use.

---

## 8. Recommended Sequence

This is not a build plan — it's the order in which the above should be resolved so implementation doesn't start on contradictory ground:

1. Owner decision session against §7 (most items are single sitting-length calls; a handful — pricing, compliance — may need external input).
2. Record every answer in a new `DECISIONS.md`.
3. Patch the ~14 document contradictions in §5 (mostly small edits to `01`, `03`, `06`, `09`, or the engineering handoff).
4. Close the "Foundational / blocks everything" row in §4 (repo scaffold, secrets strategy, email/SMS provider).
5. Greenlight Phase 1 exactly as scoped in `11_engineering_handoff.md` §71 — it is otherwise ready to execute.

No further specialist research is needed to reach this point; the remaining work is judgment calls and document housekeeping.
