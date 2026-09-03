# 14 — PIT961 Phase 2 Execution Plan

Status: **APPROVED — PHASE 2 IMPLEMENTATION AUTHORIZED** (Owner approval 2026-09-03; see DECISIONS.md #12)
Prepared by: Company Dispatcher, coordinating Product Director, Product Manager, Business Analyst, Design Lead, CTO, Technical Architect, Database Engineer, QA Lead, Security Reviewer
Date: 2026-09-02
Supersedes nothing. Extends `13_phase1_execution_plan.md` / Phase 1 (ACCEPTED, see `FINAL_PHASE1_REPORT.md`). Governed by `DECISIONS.md` (all 11 entries remain binding and are not reopened by this document).

---

# Phase 2 Objective

Close the gap between "a strong technical foundation" (Phase 1) and "a garage-management product capable of running a real garage owner/operator's daily operations in Lebanon" at the approved $30/month/garage price point — by wiring the domain schema and business-rule services that Phase 1 already built (but never exposed) into real APIs and real, prototype-aligned screens, delivered as coherent end-to-end vertical slices rather than disconnected feature work.

**Scope note (added per Product Director review):** Phase 2 delivers this capability feature-complete and demoable against a real backend on a dev/staging stack — it does not itself put a live garage onto the product. Any actual go-live requires its own deployment step, which remains explicitly out of scope for this plan (see Explicitly Excluded / Deferred Scope) and its own separate Owner approval, per standing company policy. "Daily operations" in this document means "the workflow a garage would use daily," not "deployed to a garage today."

# Business Outcome

Today, a garage cannot do anything in PIT961 except log in and see an empty shell. The entire domain schema for the core garage workflow — Customer, Vehicle, Job, Estimate (with revisioning and a discount/approval-threshold model), RepairTask, JobPart, Invoice, Payment, garage-scoped numbering, and per-garage settings — already exists as real, migrated, tenant-isolated PostgreSQL tables. Two of the most sensitive pieces of business logic in the whole product (the 15% manager discount cap and the $500 owner-approval threshold) are already built as authoritative, unit-tested application services. None of it is reachable through an API, and none of it has a screen. Phase 2's business outcome is to make that investment usable: a garage staff member can check in a customer and vehicle, open a job, quote it, get it approved (respecting the discount cap and approval threshold automatically), do the work, invoice it, and get paid — end to end, against a real backend, in a browser the Owner can click through at every milestone.

# Included Scope

- Customer and Vehicle management (create, view, edit; duplicate-plate warning, not a hard block).
- Job intake and a real Job status lifecycle, replacing the current free-text default with an enforced state machine and a full audit trail (`JobHistoryEntry`).
- Estimate creation, line items, revisioning (a new revision supersedes and locks the prior one), and wiring the **existing** `EstimateDiscountService` (15% manager cap, Owner-exempt) and `EstimateApprovalService` ($500 threshold, evaluated pre-discount) to real endpoints — plus the one genuinely new piece of business logic this requires: an explicit, Owner-only action to clear an estimate out of `pending_owner_approval`.
- Repair Tasks and Job Parts as real, status-tracked, assignable work items.
- Invoice generation from an approved Job and Payment recording (with idempotency), invoice status reconciliation, and Owner-only void.
- A concurrency-safe, atomic per-garage numbering mechanism for Jobs and Invoices.
- The Floor board, Customers, Jobs, Money, and Parts nav sections becoming genuinely functional, built against `prototype.html` as the design source of truth.
- Closing KI-8 (DbContext bypass-guard coverage gap) before Milestone 1 begins, and KI-5 (no column defaults) / KI-9 (no index-coexistence assertion) as part of Phase 2's own migration work.

# Explicitly Excluded / Deferred Scope

- **Multi-location/multi-garage ownership** — architecture-ready only (the `accounts`-above-`garages` seam from Phase 1 is preserved and not touched); **not activated** in Phase 2. Any change to the one-garage-per-account constraint requires a separate, explicit Owner approval.
- **WhatsApp and SMS** — fully deferred, including architecture-only preparation. `Customer.Whatsapp` remains schema-only and structurally inert. Resend/`IEmailService` is the sole Phase 2 communication channel.
- **Live platform-admin UI/endpoints** — remain deferred to Phase 6; no Phase 2 work depends on them.
- **Editable discount-limit/approval-threshold settings UI** — Phase 2 wires the *existing* `GarageSettings.DiscountLimitPercent`/`EstimateApprovalThreshold` values read-only into the new services; it does not build an editing surface (this Phase 1 Technical Architect condition is not lifted by this plan).
- **Reports** as an analytics surface (nav item may exist as an inert stub; no reporting/BI work, and KI-6's shared-Postgres-credential deferral explicitly stays deferred because Phase 2 adds no raw-SQL/BI/reporting tooling).
- **Team module** beyond a read-only mechanic picker needed for Job/RepairTask assignment.
- **Customer/vehicle merge or duplicate-record resolution** — no schema or workflow support exists; deferred.
- Any production deployment or production-data change (always requires separate, explicit Owner approval).
- Deciding or hardcoding a final consumer-facing brand name.

# User Personas

Per the Owner-approved `06_permission_matrix.md`: **Owner** (full control, no discount cap, sole voider/deleter), **Manager** (discount ≤15%, edits estimates/jobs, cannot delete), **Advisor/Reception** (customer/vehicle/job intake, estimate creation and customer-approval recording, cannot discount or delete), **Mechanic** (own assigned job/task only), **Accountant** (view-only across invoices/payments/cost data, no edit rights).

# Core User Journeys

1. **Intake**: Advisor creates a Customer, attaches a Vehicle, opens a Job with intake details and a real garage-sequenced Job number; the Job appears on the Floor board immediately.
2. **Quote & approve**: Advisor builds an Estimate with line items on the Job; a Manager applies a discount (hard-denied above 15% unless the actor is Owner); the estimate is submitted, and anything at or above $500 Subtotal auto-routes to `pending_owner_approval` (a routing signal, not a rejection) until the Owner explicitly clears it; the customer's own approval/rejection is recorded separately by the Advisor.
3. **Work**: Once approved, Repair Tasks and Job Parts become actionable — a Mechanic starts/completes their own assigned tasks; parts move `needed → ordered → arrived → installed`.
4. **Bill & collect**: A completed, approved Job generates an Invoice; Payments are recorded (possibly partial, possibly multiple) with an idempotency key; Invoice status reconciles to unpaid/partial/paid; only the Owner can void, and only with a reason.
5. **History**: The Customer and Vehicle each show their full Job/Estimate/Invoice history via the existing `JobHistoryEntry` audit trail and the entities' own relationships — no separate history subsystem is required.

# Current Foundation Reused

Everything below is real, already in the repository, and Phase 2 reuses it rather than rebuilding it: the full domain schema for Customer, Vehicle, Job (+`JobHistoryEntry`, soft-delete, cancellation, warranty-return/`ParentJobId`), Estimate (+`EstimateItem`, revisioning via `ParentEstimateId`), RepairTask, JobPart, Invoice, Payment, `GarageSequence`, `GarageSettings`; the `ITenantOwned`/`ICurrentTenant`/`TenantGuard` tenant-isolation pattern; the ASP.NET Core policy-based authorization framework and its existing handlers (`DiscountLimitRequirement`, `EstimateApprovalThresholdRequirement`, `GarageTenantRequirement`); the two authoritative application services `EstimateDiscountService` and `EstimateApprovalService` (wired to endpoints for the first time in Phase 2, not rewritten); the Auth/Config controller pattern as the template for every new controller; `IEmailService`/Resend; the existing dark/amber design system, 76px nav rail, and authenticated shell; the real-Postgres CI pipeline (GitHub Actions service containers, xunit, Vitest, Playwright) and its placeholder-brand/Resend-boundary guard scripts.

---

# Work Package Matrix

Unique identifiers `P2-WP1`…`P2-WP12` (no reuse of Phase 1 `WP-*` numbers).

## P2-WP1 — Close KI-8 (DbContext.Add bypass-guard coverage gap)
- **Objective**: Fix the boundary-test regex gap for `DbContext.Add(object)` overloads before any new authoritative service is built, so every Phase 2 mutation-boundary test inherits a sound guard from day one.
- **Business/customer value**: Indirect — protects the integrity of every discount/price/status control Phase 2 exposes.
- **Responsible**: Backend Engineer. **Collaborating**: QA Automation Engineer, Security Reviewer.
- **Dependencies**: None — this is the Phase 2 critical-path starting point.
- **Backend work**: Widen `SourceScanUtilities`/the relevant boundary-test pattern to catch non-generic `DbContext.Add(object)` call shapes, mirroring the KI-16 fix already applied to `.Update`/`.Attach`.
- **DB/Design/Frontend work**: None.
- **API surface**: None (test-tooling fix only).
- **Security requirements**: Re-run the existing negative-gate proof pattern (deliberate bypass attempt on a temp branch) to prove the fix actually catches the gap, then delete the branch, same discipline as WP-9's negative-gate proofs.
- **Tenant/authorization requirements**: N/A.
- **Test requirements**: Unit tests for the widened pattern; confirm zero false positives against the current codebase (same method QA Lead used for KI-16).
- **CI requirements**: No new job; existing architecture-test job picks this up automatically.
- **Owner visual checkpoint**: None (backend/tooling only).
- **Acceptance criteria**: The widened guard is proven to catch a deliberate `DbContext.Add(object)` bypass on a temp branch; zero regressions in the existing 181 backend tests; KI-8 marked CLOSED in `KNOWN_ISSUES.md`.
- **Out of scope**: Any other KI.

## P2-WP2 — Customer & Vehicle API + Screens
- **Objective**: Real Customer/Vehicle CRUD, backend and frontend.
- **Business/customer value**: The literal first step of every garage workflow — nothing else in Phase 2 can proceed without it.
- **Responsible**: Backend Engineer (API), Frontend Engineer (screens). **Collaborating**: Database Engineer (indexes), UI/UX Designer (screen detail against `prototype.html`), Design Lead (consistency review).
- **Dependencies**: P2-WP1.
- **Backend work**: `CustomerService`, `VehicleService` (thin, standard CRUD, no business-rule complexity); duplicate-plate detection returns a warning, not a block.
- **Frontend work**: Customers list/detail/create-edit screens; Vehicle create-edit nested under a Customer.
- **Database work**: `Customer(GarageId, Phone)` and `Vehicle(GarageId, PlateNumber, PlateCountry)` non-unique indexes; confirm/add `Vehicle.CustomerId → Customer.Id` FK; migrations `AddCustomerPhoneLookupIndex`, `AddVehiclePlateLookupIndex`. Soft-delete for Customer/Vehicle is an open Owner decision (see Owner Decisions Required) — schema left additive either way.
- **Design work**: Design Lead's pre-flagged audit of `prototype.html`'s Customers section (assessed medium-confidence-covered) resolved by UI/UX Designer before screen build starts.
- **API surface**: `POST/GET/PUT /api/v1/customers[/{id}]`, `POST/GET/PUT /api/v1/customers/{customerId}/vehicles[/{id}]`. Auth: `GarageTenantRequirement`. GarageId always derived from the JWT claim, never accepted from the client body.
- **Security requirements**: Tenant-isolation tests (cross-garage read/write must 404/403); no client-supplied `GarageId` accepted anywhere.
- **Tenant requirements**: Full `ITenantOwned` query-filter coverage confirmed for both new tables' access paths.
- **Authorization requirements**: Delete restricted to Owner per permission matrix; Advisor/Manager/Owner may create/edit.
- **Test requirements**: Unit (service logic), integration (API + tenant isolation, duplicate-plate warning behavior), frontend component tests (form validation, empty/loading/error states), E2E (create customer → attach vehicle, visible in UI).
- **CI requirements**: Extends existing backend/frontend/E2E jobs; no new CI job needed.
- **Owner visual checkpoint**: **Yes** — Milestone 1 checkpoint (see Milestones section): dev stack running, Owner can create a real customer and vehicle in the browser.
- **Acceptance criteria**: Real API + real screens, no mocked data; duplicate-plate warning demonstrable; role-based create/edit/delete matches the permission matrix; tenant-isolation tests pass.
- **Out of scope**: Customer/vehicle merge or dedupe resolution; fleet-specific workflows beyond the existing `IsFleet` flag.

## P2-WP3 — Job Intake, Status Machine & Floor Board
- **Objective**: Real Job creation and a governed status lifecycle with full audit trail, and a functional Floor board.
- **Business/customer value**: The operational spine of the whole product — every downstream milestone (estimate, work, invoice) hangs off a real Job.
- **Responsible**: Backend Engineer, Frontend Engineer. **Collaborating**: Technical Architect (status-machine + `JobStatusService` design), Database Engineer (numbering + indexes), Business Analyst (status-machine ratification — see Owner Decisions Required), QA Lead.
- **Dependencies**: P2-WP2.
- **Backend work**: `JobService` (create/edit non-status fields); new `JobStatusService` as the **sole** writer of `Job.Status`, writing a `JobHistoryEntry` inside the same transaction for every transition; atomic `GarageSequence` increment (`UPDATE ... RETURNING`, executed as a scalar SQL round-trip, not through EF change-tracking) for `JobNumber`.
- **Frontend work**: Job creation form and the Floor board (both low-risk per Design Lead — Floor is the already-accepted WP-8 pattern) may proceed without waiting on P2-WP9. The **Job detail view is a separate, medium-risk screen** (a different information architecture than Floor's board/card view, per Design Lead) and is gated by P2-WP9 the same as Money/Parts — it is not covered by Floor's low-risk exemption and its own Owner Visual Checkpoint element may not be marked done until P2-WP9's findings for Job detail are resolved.
- **Database work**: `Job(GarageId, Status)` index; lookup table `job_statuses` (recommended over a bare CHECK constraint, per Database Engineer, so a future status can be added by data rather than migration) seeded with the recommended state machine; confirm FKs `Job.CustomerId`, `Job.VehicleId`, self-referencing `Job.ParentJobId`; migrations `AddJobStatusLookupTable`, `AddJobFloorBoardIndex`; KI-5 (column defaults) closed as part of this migration work.
- **Design work**: Job detail screen is Design Lead's flagged medium-risk gap against `prototype.html` — UI/UX Designer resolves before build.
- **API surface**: `POST /api/v1/jobs`, `GET /api/v1/jobs`, `GET /api/v1/jobs/{id}`, `PUT /api/v1/jobs/{id}` (non-status fields only), `GET /api/v1/jobs/{id}/history`, `POST /api/v1/jobs/{id}/status-transitions` (body carries the transition intent, never a raw target status string accepted verbatim without validation against the allowed-transition table).
- **Security requirements**: `JobStatusTransitionRequirement(from, to, role)` as a proper `AuthorizationHandler<T>`, not ad hoc role `if`s in the controller — this is the same authoritative-service pattern as `EstimateDiscountService`, made binding by CTO for every Phase 2 mutation.
- **Tenant requirements**: Cross-tenant status-transition attempt must fail (403/404) — explicit test.
- **Authorization requirements**: Transition matrix per role (Reception-stage transitions: Advisor/Manager/Owner; Work-stage transitions: Mechanic on own assigned job only; `→closed`: Owner only; `→deleted` (soft): Owner only) — **pending Owner ratification, see Owner Decisions Required**.
- **Test requirements**: Unit (status-machine legality), integration (transition API + tenant isolation + role gating), concurrency test (two simulated simultaneous Job creations never receive the same `JobNumber`), frontend (Floor board renders by status, no refresh needed after create), E2E (create Job → visible on Floor → open Job detail). **Added per Security Reviewer review (HIGH finding)**: `JobHistoryEntry` — the audit-trail table backing `GET /api/v1/jobs/{id}/history` and the Customer/Vehicle history views — must get its own explicit cross-tenant and mismatched-owner test, the same as every other tenant-owned table in this plan; audit tables are a common place for a tenant-filter to be silently missed (filtering by `JobId` alone and forgetting a direct/cross-tenant history-query path), and this table had not been named as requiring that test until this review caught it.
- **CI requirements**: Extends existing jobs.
- **Owner visual checkpoint**: **Yes** — Milestone 1 checkpoint (Customer→Vehicle→Job→Floor board, fully clickable).
- **Acceptance criteria**: Job creation persists to Postgres with a unique sequential number under concurrent load; every transition writes a `JobHistoryEntry`; Floor board reflects real data with no mock fallback.
- **Out of scope**: Job cancellation/warranty-return UX beyond basic status support already in schema.

## P2-WP4 — Estimate, Discount & Approval Wiring
- **Objective**: Wire the **existing** `EstimateDiscountService`/`EstimateApprovalService` to real endpoints and screens, plus estimate revisioning and the new Owner-clear-approval action.
- **Business/customer value**: The highest-compliance-value package in Phase 2 — it activates the two business rules the company has already invested the most rigor in (15% cap, $500 threshold) and stops them being dead code.
- **Responsible**: Backend Engineer, Frontend Engineer. **Collaborating**: Technical Architect, Business Analyst (revisioning/approval-semantics ratification), Security Reviewer (this package touches the two most sensitive mutation paths in the product).
- **Dependencies**: P2-WP3.
- **Backend work**: `EstimateService`/`EstimateItemService` (thin CRUD, must never accept a client-supplied `Total`, `DiscountAmount`, or `Status` for the fields the two authoritative services own); `EstimateRevisionService` (creates a `ParentEstimateId`-linked child, sets the prior revision's `Status = superseded`, both approval states reset independently on the new revision); new `ClearOwnerApproval` method added to the **existing** `EstimateApprovalService` (same mutation path, Owner-only).
- **Frontend work**: Estimate builder (line items), discount control (Manager sees a visibly capped 15% control; Owner sees no cap), submit-for-approval action, a visually distinct "pending owner approval" gated state (not a disabled form field), customer-approval recording action, revision creation.
- **Database work**: `Estimate.Status` gains a `superseded` terminal value (same lookup-table-vs-CHECK question resolved consistently with P2-WP3); `Estimate(GarageId, JobId, RevisionNumber)` and `Estimate(ParentEstimateId)` indexes; **confirm FKs** `Estimate.JobId → Job.Id` and the self-referencing `Estimate.ParentEstimateId → Estimate.Id` (load-bearing for revisioning integrity — added per Database Engineer review, matching the FK-confirmation language already used in P2-WP2/WP3/WP5) and `EstimateItem.EstimateId → Estimate.Id`; migrations `AddEstimateSupersededStatus`, `AddEstimateRevisionIndex`.
- **Design work**: This is Design Lead's **highest-risk flagged gap** — a dedicated Design Lead/UI-UX-Designer audit pass against the real `prototype.html` for the Estimate approval-gate states (P2-WP9) is a **hard gate on this package's Owner Visual Checkpoint sign-off**: frontend implementation may begin once the audit opens, but Milestone 2 may not be marked done until P2-WP9's findings for the Estimate/Money screens are resolved (see Design Mapping section).
- **API surface**: `POST /api/v1/jobs/{jobId}/estimates`, `POST/PUT /api/v1/estimates/{id}/items[/{itemId}]`, `POST /api/v1/estimates/{id}/revisions`, `POST /api/v1/estimates/{id}/discount` (body: `{percent}` only — existing `EstimateDiscountService`), `POST /api/v1/estimates/{id}/submit-for-approval` (existing `EstimateApprovalService`), `POST /api/v1/estimates/{id}/owner-clear-approval` (Owner-only, new method on the same service), `POST /api/v1/estimates/{id}/customer-approval` (records customer accept/reject — separate concept from owner threshold routing), `GET /api/v1/estimates/{id}`, `GET /api/v1/jobs/{jobId}/estimates`.
- **Security requirements**: Boundary tests confirming no code path other than the two authoritative services can write `DiscountAmount`/`Total`/approval-relevant `Status` values (extends the existing `EstimateMutationBoundaryTests` pattern); explicit test that a Manager request for >15% is denied server-side even if the frontend control were bypassed.
- **Tenant requirements**: Cross-tenant discount/approval attempts must fail; revision creation on another garage's estimate must fail.
- **Authorization requirements**: `DiscountLimitPolicy` (Manager-or-Owner + service-level 15% enforcement), `OwnerOnlyPolicy` for `owner-clear-approval`, staff-role policy for customer-approval recording — all reusing existing policy-based authorization infrastructure, no new pattern.
- **Test requirements**: The full 15.00%/15.01% and $500.00/$500.01 boundary tests already established as the Phase 1 pattern, extended to the new endpoints; revision-supersession test; frontend permission-driven UX tests (Manager cap visible, Accountant read-only/cost-hidden). **Added per QA Lead review (both CRITICAL findings)**: (1) a superseded-estimate mutation-rejection test — attempting `/discount`, `/submit-for-approval`, `/owner-clear-approval`, or `/customer-approval` on an estimate whose `Status = superseded` must be rejected on every one of those endpoints, since this is the specific invariant Owner Decision #3 depends on to justify revision-supersession over in-place edits; (2) a same-estimate/same-revision concurrency test — two simultaneous mutation attempts on the same estimate (e.g., a Manager discount and an Owner clear-approval racing, or two concurrent revision-creation calls racing on `RevisionNumber`) must not produce a lost update or a duplicate `RevisionNumber`. **Added per Security Reviewer review (both HIGH findings)**: (3) a named cross-tenant `ParentEstimateId` test — creating a revision whose `ParentEstimateId` points at another garage's estimate must be rejected, distinct from the existing "revision creation on another garage's estimate must fail" tenant test, since this is an IDOR-via-foreign-key-reference attack shape on a resource the caller *does* own, not a missing-GarageId bypass; (4) a named negative authorization test — a Manager (not just an unauthenticated/wrong-tenant actor) must be explicitly proven denied on `/owner-clear-approval`, given the plan itself notes "a Manager-clearable gate would functionally reintroduce a bypass."
- **CI requirements**: Extends existing jobs.
- **Owner visual checkpoint**: **Yes** — Milestone 2 checkpoint: Owner can build an estimate, watch a >15% discount get denied live, watch a ≥$500 estimate auto-route to pending approval, and clear it as Owner.
- **Acceptance criteria**: Both existing services are reachable only through their designed endpoints; no discount or approval-threshold logic is duplicated or reimplemented in the controller/frontend; revisioning correctly supersedes prior estimates; QA and Security gates both clean on this package specifically before it counts as done (Owner named this package's controls explicitly).
- **Out of scope**: Making `DiscountLimitPercent`/`EstimateApprovalThreshold` editable per garage.

## P2-WP5 — Repair Tasks & Job Parts
- **Objective**: Real, status-tracked, assignable work items once an estimate is approved.
- **Business/customer value**: Connects the quote to the shop floor — the actual repair work.
- **Responsible**: Backend Engineer, Frontend Engineer. **Collaborating**: Business Analyst (parts-sourcing rule ratification — see Owner Decisions Required), Database Engineer.
- **Dependencies**: P2-WP4 for runtime activation only. **Data-flow clarification (per Technical Architect review)**: task/part activation reads `Job.Status` (specifically the `approved`/`in_progress` transition, written solely by `JobStatusService` after it has itself consulted `Estimate.Status`) — it does **not** read `Estimate.Status` directly. This keeps `JobStatusService` the single arbiter of the approval signal and avoids a second, independent read path into Estimate that could drift from Job's own state. **Parallelization note**: `RepairTask`/`JobPart` are structurally independent tables from Job/Estimate core, so their schema, entity, and repository-layer work may be scaffolded in parallel with P2-WP4 — only the runtime activation/business-logic wiring described above genuinely depends on P2-WP4 being complete. The Critical Path section's strict sequencing is a safe default, not a hard architectural requirement, for this specific package.
- **Backend work**: `RepairTaskService`, `JobPartService` — both follow the authoritative-service pattern for their own status fields; both feed into `JobStatusService` transitions (`approved→in_progress→completed`).
- **Frontend work**: Task list with start/complete actions scoped to the assigned Mechanic's own tasks; Parts list with status lifecycle (`needed→ordered→arrived→installed`, `returned` branch).
- **Database work**: `RepairTask(GarageId, JobId, Status)`, `JobPart(GarageId, JobId, Status)` indexes; confirm FKs to `Job`; migrations `AddRepairTaskStatusIndex`, `AddJobPartStatusIndex`.
- **Design work**: Design Lead's flagged medium/high-risk gap (Parts is workflow UI, not simple CRUD) — resolve via UI/UX Designer audit (P2-WP9) before build, same hard-gate discipline as P2-WP4: Milestone 3's Owner Visual Checkpoint may not be marked done until P2-WP9's findings for Parts are resolved.
- **API surface**: `POST/GET/PUT /api/v1/jobs/{jobId}/repair-tasks[/{id}]`, `POST /api/v1/repair-tasks/{id}/status-transitions`, `POST/GET/PUT /api/v1/jobs/{jobId}/parts[/{id}]`, `POST /api/v1/parts/{id}/status-transitions`.
- **Security requirements**: Mechanic-role tests confirming a Mechanic cannot mark another mechanic's task complete (per permission matrix: "Mark other mechanic's task" = Manager/Owner only).
- **Tenant requirements**: Standard cross-tenant isolation tests.
- **Authorization requirements**: Per permission matrix (Add/edit tasks: Manager/Owner only; start/pause/complete own task: Mechanic; parts add/edit: Advisor/Manager/Owner).
- **Test requirements**: Unit, integration (including the own-task-only Mechanic restriction), frontend, E2E covering one task through to completion and one part through to installed.
- **CI requirements**: Extends existing jobs.
- **Owner visual checkpoint**: **Yes** — Milestone 3 checkpoint.
- **Acceptance criteria**: Tasks/parts only activate once gated by an approved estimate; Mechanic scoping enforced server-side; real data, no mocks.
- **Out of scope**: Outsourced-parts vs. outsourced-tasks conceptual unification (Business Analyst flagged this as unresolved — treated as two independent, already-separate schema concepts for Phase 2, not merged).

## P2-WP6 — Invoice & Payment
- **Objective**: Generate an invoice from an approved, completed Job and record payments, with reconciliation and Owner-only void.
- **Business/customer value**: The point at which the garage actually gets paid — completes the commercial loop.
- **Responsible**: Backend Engineer, Frontend Engineer. **Collaborating**: Database Engineer (atomic invoice numbering, void constraint), Business Analyst (void/refund rule — see Owner Decisions Required), Security Reviewer (idempotency on payment recording).
- **Dependencies**: P2-WP5 (a Job must be `completed` before invoicing, per the Job status machine).
- **Backend work**: `InvoiceService` (generation from Job/approved Estimate totals, atomic `GarageSequence` increment for `InvoiceNumber`, Owner-only void requiring `VoidReason`), `PaymentService` (idempotency-key enforced recording — **the idempotency key is server-generated, never accepted from the client**, closing an ambiguity Security Reviewer flagged — `TotalPaid` reconciliation across possibly-multiple `Payment` rows, status transition unpaid→partial→paid). **Void guard rail (added per Business Analyst review)**: `InvoiceService.Void` must reject voiding an Invoice that has any recorded, non-voided `Payment` row against it — voiding is only permitted while `TotalPaid = 0`. This closes the money-received-but-no-invoice-records-it gap Business Analyst identified; a voided-invoice refund/credit workflow remains out of scope per Owner Decision #6, but this guard prevents the specific dangerous case that gap would otherwise allow.
- **Frontend work**: Invoice detail/generation screen, payment recording form, invoice status display.
- **Database work**: `Invoice(GarageId, Status)`, `Invoice(GarageId, JobId)`, `Payment(InvoiceId)` indexes; **confirm FKs** `Invoice.JobId → Job.Id` and `Payment.InvoiceId → Invoice.Id` (added per Database Engineer review, matching the FK-confirmation language already used elsewhere in this plan); `CHECK (VoidedAt IS NULL OR VoidReason IS NOT NULL)` constraint on `Invoice`; migrations `AddInvoiceVoidReasonCheck`, `AddInvoicePaymentIndexes`.
- **Design work**: Part of the Money section — same audit discipline as P2-WP4's Estimate screens.
- **API surface**: `POST /api/v1/jobs/{jobId}/invoices`, `GET /api/v1/invoices/{id}`, `POST /api/v1/invoices/{id}/void` (Owner-only, requires reason), `POST /api/v1/invoices/{id}/payments`.
- **Security requirements**: Idempotency-key test (duplicate payment submission does not double-count); void authorization test (only Owner).
- **Tenant requirements**: Standard cross-tenant isolation tests.
- **Authorization requirements**: Per permission matrix (Accountant: view-only, no edit, cost visible; void: Owner only).
- **Test requirements**: Unit (reconciliation math), integration (idempotency, void constraint, status transitions), frontend, E2E (full Job→Invoice→Payment path). **Added per QA Lead review (MAJOR finding)**: a payment-reconciliation concurrency test — two simultaneous, legitimate partial payments on the same invoice must both be recorded and `TotalPaid` must reconcile correctly with no lost update, distinct from the existing idempotency test (which covers duplicate submission of the *same* payment, not two different concurrent ones). **Added per Business Analyst review**: a test proving void is rejected when the Invoice has any recorded, non-voided payment (see the void guard rail above).
- **CI requirements**: Extends existing jobs.
- **Owner visual checkpoint**: **Yes** — Milestone 4 checkpoint: the full end-to-end loop, Customer through Payment, demonstrable in one sitting.
- **Acceptance criteria**: An invoice cannot be generated from an unapproved or superseded estimate; payments reconcile correctly against partial-payment scenarios; void requires Owner + reason; idempotency proven under a duplicate-submission test.
- **Out of scope**: Refund/credit-note workflow for a voided invoice with existing payments (Business Analyst flagged this as undefined — see Owner Decisions Required).

## P2-WP7 — Cross-Cutting: Boundary-Test Pattern Replication
- **Objective**: Ensure every new authoritative service (Job status, Estimate revision/approval, RepairTask/JobPart status, Invoice/Payment) has its own mutation-boundary test, mirroring `EstimateMutationBoundaryTests`.
- **Business/customer value**: This is what makes "backend remains authoritative, no PATCH bypasses" actually true across the whole new surface, not just the two original services.
- **Sole-writer confirmation (added per CTO review)**: splitting an aggregate into multiple services (e.g. Job into `JobService`+`JobStatusService`, Estimate into `EstimateService`+`EstimateItemService`+`EstimateRevisionService`) is only safe if each split service owns a disjoint slice of writable state, with zero overlap. This is binding for every split pair in P2-WP3 through P2-WP6: `JobService` must treat `Job.Status` as read-only/delegate-only — only `JobStatusService` may write it. `EstimateService` owns header/non-financial fields only; `Subtotal`/`Total`/`DiscountAmount` are written solely by `EstimateDiscountService` (a line-item change in `EstimateItemService` triggers a recompute call into that same authoritative path, it does not write `Total` itself). `RepairTaskService`/`JobPartService` are each the sole writer of their own entity's `Status`. `InvoiceService` owns generation/void only; `TotalPaid` and payment-driven status transitions are written solely by `PaymentService`'s reconciliation method, never independently by `InvoiceService`. Each new service's boundary test in this package must assert its specific sole-writer boundary, not just "no bypass exists" in the abstract.
- **Responsible**: QA Automation Engineer. **Collaborating**: Backend Engineer (as each service lands), Security Reviewer.
- **Dependencies**: Runs alongside P2-WP3 through P2-WP6, one boundary-test suite per new authoritative service, not a single end-of-phase task.
- **Backend/DB/Frontend/Design work**: None (test-only).
- **API surface**: N/A.
- **Security requirements**: Each boundary test must be reviewed by Security Reviewer as part of that package's own gate, not batched at the end.
- **Test requirements**: One `*BoundaryTests.cs` file per new authoritative service; CI-enforced (fails the build if bypassed).
- **CI requirements**: Runs inside the existing backend test job.
- **Owner visual checkpoint**: None.
- **Acceptance criteria**: Every Phase 2 authoritative service has a passing, CI-enforced boundary test proving no alternate mutation path exists.
- **Out of scope**: Retrofitting boundary tests onto Phase 1 services beyond what already exists.

## P2-WP8 — Frontend Screen Delivery (Customers, Jobs, Money, Parts against `prototype.html`)
- **Objective**: Deliver the actual screens for each milestone against the approved visual design system, with every required UX state.
- **Business/customer value**: This is literally what the Owner and garage staff will see and use.
- **Responsible**: UI/UX Designer (detailed flows), Frontend Engineer (implementation). **Collaborating**: Design Lead (consistency review/sign-off per screen).
- **Dependencies**: Runs alongside P2-WP2/3/4/5/6, screen-by-screen, not as one bulk task at the end.
- **Design work**: For each screen, confirm against the real `prototype.html` (not the keyword-level assessment used for this planning pass) whether existing design coverage applies as-is or needs a compatible extension; any extension is logged in `DESIGN_IMPLEMENTATION_DIFFERENCES.md` per the existing entry format. No shadcn-default styling, no generic SaaS dashboard aesthetic, no new navigation pattern — every screen extends the existing dark/amber system and 76px rail.
- **Frontend work**: Loading, empty, error, validation, and permission-driven UX states on every screen (e.g., Manager's capped discount control visually distinct from the gated pending-approval state; Accountant's read-only chrome with cost fields structurally absent, not merely blanked).
- **API surface**: Consumes the endpoints defined in P2-WP2 through P2-WP6; no mocked data permitted for any milestone's acceptance.
- **Test requirements**: Frontend component tests per screen/state; Playwright E2E per milestone's full click-through path.
- **CI requirements**: Extends existing frontend/E2E jobs.
- **Owner visual checkpoint**: Tied to each milestone (see Milestones section).
- **Acceptance criteria**: Every Phase 2 screen is demonstrated live against real data at its milestone checkpoint; every required UX state is present; Design Lead sign-off recorded per screen in `DESIGN_IMPLEMENTATION_DIFFERENCES.md` where an extension was needed.
- **Out of scope**: Any new navigation paradigm or visual identity change.

## P2-WP9 — Design Audit Pass (prototype.html vs. Phase 2 screens)
- **Objective**: Resolve Design Lead's flagged uncertainty (keyword-level assessment only, not a pixel audit) for Jobs, Money, and Parts specifically, before UI/UX Designer begins detailed work on those sections.
- **Business/customer value**: Prevents rework and prevents an off-brand/inconsistent screen from shipping.
- **Responsible**: Design Lead. **Collaborating**: UI/UX Designer.
- **Dependencies**: None — can start immediately, in parallel with P2-WP1.
- **Design work**: Open the real `prototype.html`/`support.js`, confirm exactly which states/screens are already designed vs. only named, for the Job detail, Estimate/approval-gate, Invoice, and Parts-status-lifecycle views specifically.
- **Test requirements**: N/A.
- **Owner visual checkpoint**: None (internal design artifact).
- **Acceptance criteria**: A written confirmation, per screen, of "prototype covers this as-is" or "compatible extension specified, logged in DESIGN_IMPLEMENTATION_DIFFERENCES.md" — delivered before P2-WP4/P2-WP5/P2-WP8 frontend work starts on those sections.
- **Out of scope**: Any redesign of Floor or Customers (already assessed lower-risk) or of the Login/shell (already Phase-1-accepted).

## P2-WP10 — Known-Issue Hardening (KI-5, KI-9 as riders on Phase 2 migrations)
- **Objective**: Add column defaults (KI-5) and Postgres index-coexistence assertions (KI-9) as part of the normal Phase 2 migration work, not as separate initiatives.
- **Responsible**: Database Engineer, QA Automation Engineer.
- **Dependencies**: Rides along with P2-WP3 (Job), P2-WP4 (Estimate), P2-WP6 (Invoice) migrations.
- **Acceptance criteria**: Every new/changed column in Phase 2 migrations has an explicit default where one is meaningful; at least one catalog-level test asserts the new indexes actually exist post-migration, extending the pattern QA Lead already established for KI-9's original scope.
- **Out of scope**: Retrofitting defaults onto every Phase 1 column (KI-5's original Phase 1 scope stays tracked as-is beyond what Phase 2 touches).

## P2-WP11 — CI Extension
- **Objective**: Wire every Phase 2 test suite into the existing CI pipeline incrementally.
- **Responsible**: DevOps Engineer. **Collaborating**: QA Automation Engineer.
- **Dependencies**: Runs alongside every package that introduces tests.
- **CI requirements**: No new job categories — extends `build-and-test` (backend), `build-and-test-frontend`, and the `e2e` job with the new suites; preserves the placeholder-brand guard, the Resend-boundary guard, and fail-closed behavior exactly as Phase 1 established them; no Docker introduced at any point.
- **Acceptance criteria**: Every Phase 2 work package's tests run in CI as part of that package's own acceptance criteria, not bolted on at the end of the phase.
- **Out of scope**: Any deployment step.

## P2-WP12 — Phase 2 Final Company Gate
- **Objective**: The company-wide close-out gate for Phase 2, mirroring the rigor of the Phase 1 Final Company Gate.
- **Responsible**: Company Dispatcher, convening QA Lead and Security Reviewer for independent re-verification against the real repository (not tracking-doc labels).
- **Dependencies**: All of P2-WP1 through P2-WP11.
- **Acceptance criteria**: See Phase 2 Final Acceptance Gate section below.

---

# Dependency Graph

```
P2-WP1 (close KI-8) ─────────────┐
P2-WP9 (design audit) ───────────┤
                                  ▼
P2-WP2 (Customer/Vehicle) ──► P2-WP3 (Job/Status/Floor) ──► P2-WP4 (Estimate/Discount/Approval)
                                                                      │
                                                                      ▼
                                                              P2-WP5 (Tasks/Parts)
                                                                      │
                                                                      ▼
                                                              P2-WP6 (Invoice/Payment)

P2-WP7 (boundary tests)  — rides alongside WP3–WP6, one suite per new service
P2-WP8 (frontend delivery) — rides alongside WP2–WP6, screen by screen
P2-WP10 (KI-5/KI-9 hardening) — rides alongside WP3, WP4, WP6 migrations
P2-WP11 (CI extension) — rides alongside every package introducing tests

All of the above ──────────────────────────────────────────────────► P2-WP12 (Final Gate)
```

# Critical Path

P2-WP1 → P2-WP2 → P2-WP3 → P2-WP4 → P2-WP5 → P2-WP6 → P2-WP12.
P2-WP9 (design audit) must complete before P2-WP4 and P2-WP5's frontend work specifically (its highest-risk screens), but does not block the backend-only start of P2-WP2/P2-WP3.

# Parallelization Strategy

P2-WP1 (KI-8 fix) and P2-WP9 (design audit) run in parallel from day one — neither depends on the other. Within each milestone package, backend API work (e.g., P2-WP3's `JobService`/`JobStatusService`) and database migration work (indexes, lookup table) can proceed in parallel with Design Lead/UI-UX-Designer's screen-detail work for that same milestone, converging at Frontend Engineer's implementation step. P2-WP7 (boundary tests) and P2-WP10 (KI-5/KI-9 hardening) are riders, not separate sequential phases — they land inside each package's own PR, keeping CI green continuously rather than in one large end-of-phase test-writing push. P2-WP11 (CI extension) is likewise continuous, not a discrete late-phase task.

# Known-Issue Treatment

| KI | Severity | Classification | Rationale |
|---|---|---|---|
| KI-5 (no column DEFAULTs) | LOW | **B — Close naturally during Phase 2** | Rides along with P2-WP3/WP4/WP6 migrations, per Database Engineer and CTO. |
| KI-6 (shared Postgres credential across DbContexts) | LOW | **C — Accepted deferred** | Its own stated trigger ("before any raw-SQL/BI/reporting tooling") is not met — Phase 2 adds no such tooling. |
| KI-7 (dual tenant-enforcement shares one root of trust) | Informational | **D — Informational** | Unchanged by Phase 2; already accepted as a Phase 1 forward note. |
| KI-8 (DbContext.Add(object) bypass-guard gap) | MEDIUM | **A — Phase 2 prerequisite** | Phase 2 is about to add ~6 new authoritative services that all depend on this exact guard category; fix once, before Milestone 1, not six times after. See P2-WP1. |
| KI-9 (no index-coexistence assertion) | MEDIUM | **B — Close naturally during Phase 2** | Phase 2 adds ~8–10 new indexes; extend the assertion pattern as part of that same migration work. See P2-WP10. |
| KI-10 (email lookup case-sensitivity, no documented decision) | MEDIUM | **C — Accepted deferred** | Auth-surface issue, not touched by Phase 2's business-domain expansion; unrelated critical path. |
| KI-11 (rate-limit test coverage asymmetric) | LOW | **C — Accepted deferred** | Auth-surface issue, unrelated to Phase 2. |
| KI-12 (no malformed-body tests on auth endpoints) | LOW | **C — Accepted deferred** | Auth-surface issue, unrelated to Phase 2. |
| KI-13 (email whitespace untested) | LOW | **C — Accepted deferred** | Bundled with KI-10's eventual follow-up; not Phase 2. |
| KI-14 (Retry-After header fixed 60s) | LOW | **C — Accepted deferred** | Unrelated to Phase 2 surface. |
| KI-15 (orphaned refresh-token row on crash) | LOW | **C — Accepted deferred** | Auth-surface issue, unrelated to Phase 2. |
| KI-16 (architecture source-scan naming-convention gap) | — | **Already CLOSED** per `KNOWN_ISSUES.md` | **Discrepancy noted**: `FINAL_PHASE1_REPORT.md`'s own "Open Known Issues" list still shows KI-16 as an open LOW item — that is stale relative to `KNOWN_ISSUES.md`, which is the more current, authoritative record. Recorded here per the Owner's "if a document and implementation differ, record the difference explicitly" instruction; not silently resolved in either document by this plan. |

# Design Mapping

`prototype.html` remains the canonical visual source of truth (`DECISIONS.md` #1) and, per Design Lead's assessment, already contains meaningful designed content for all 8 nav sections plus Estimate/Invoice/Vehicle/Payment concepts — this is not a from-scratch design phase. Design Lead's confidence by section: **Floor** — low risk, already WP-8-accepted pattern. **Customers** — medium confidence, likely adequate CRUD coverage. **Jobs** (detail view specifically, distinct from the Floor board), **Money** (Estimate approval-gate states, Invoice, Payment), and **Parts** (multi-state sourcing workflow) — flagged as genuine open questions requiring the dedicated audit in P2-WP9 before UI/UX Designer builds those screens. Any confirmed gap becomes a compatible extension logged in `DESIGN_IMPLEMENTATION_DIFFERENCES.md`, never a silent redesign; the existing RASHID-placeholder branding entry stays as-is and is not "fixed" by inventing a brand — all Phase 2 mockups keep using the configuration-driven `Branding:ProductDisplayName` placeholder.

# Security Gates

Per standing policy, every Phase 2 work package that introduces a tenant-owned resource or an authorization-protected mutation must pass an independent Security Reviewer gate before being considered done — any CRITICAL or HIGH finding blocks completion. Specific requirements carried into this plan: GarageId is always derived from authenticated context, never trusted from a client-supplied value, on every new endpoint; every new tenant-owned table has cross-tenant and mismatched-owner tests; the discount-cap and approval-threshold mutation paths (P2-WP4) get Security Reviewer's most detailed attention, consistent with how they were treated in Phase 1; payment idempotency (P2-WP6) is explicitly security-tested, not just functionally tested; no PATCH-style generic update bypass is introduced anywhere; the repository stays public-safe (no real secrets, no production data, no PATs) at every commit, per the Owner's standing repository-hygiene order.

# QA Gates

QA Lead runs an independent gate per package (not merely at phase end); any BLOCKER or CRITICAL finding sends the package back to the owning engineering role. QA is instructed, per standing company policy, to attempt to find failures rather than confirm the code runs — specific boundary cases to target: the 15.00%/15.01% and $500.00/$500.01 estimate boundaries (extended pattern from Phase 1), concurrent Job/Invoice numbering under simulated simultaneous creation, Mechanic own-task-only enforcement, Owner-only void/clear-approval enforcement, invoice-from-unapproved-estimate rejection, and payment idempotency under duplicate submission.

# Definition of Done

A Phase 2 work package is done only when: its real API is implemented and reachable; its real frontend screen consumes that API with no mocked data; its authoritative-service/boundary-test pattern is in place and CI-enforced, including the sole-writer boundary named for its split (see P2-WP7); its tenant-isolation and authorization tests pass; its QA gate is clean (no BLOCKER/CRITICAL); its Security gate is clean (no CRITICAL/HIGH); its milestone's Owner Visual Checkpoint has been demonstrated on a running dev stack with the exact local URL and any needed dev credentials provided, **and re-demonstrates the full chain from Customer through that milestone's newest capability, not only the newest slice in isolation** (per Product Manager review — this is the specific safeguard against Phase 1's "looks done but isn't wired to anything real" failure mode recurring at a later milestone); and any design extension it required is logged in `DESIGN_IMPLEMENTATION_DIFFERENCES.md`.

**Plan-wide acceptance criterion (added per Technical Architect review, applies to Milestones 1-4 uniformly, not only the fully-detailed Milestone 1-2 endpoints)**: no endpoint at any milestone may accept a client-supplied `Total`, `DiscountAmount`, `TotalPaid`, or any business-authoritative `Status` field directly — every such field is set only by its sole-writer authoritative service acting on intent (a percentage, a transition name, an amount), never on a caller-supplied final value.

# Phase 2 Final Acceptance Gate

Phase 2 is accepted only when all twelve work packages meet their individual Definition of Done above, the full Customer→Vehicle→Job→Estimate→Approval→Repair Tasks→Parts→Invoice→Payment→History journey is demonstrable end-to-end in one sitting against real data, QA Lead and Security Reviewer have both independently re-verified the real repository (not tracking-doc labels) and recorded a clean gate, all specialist reviewers listed in this document have recorded a PASS or their findings have been remediated, and Company Dispatcher has delivered a Phase 2 closeout report to the Owner in the same structure as `FINAL_PHASE1_REPORT.md`. No Phase 2 work package may be silently marked done on the strength of its own label — the Final Gate re-verifies against real code, tests, and CI, exactly as the Phase 1 Final Company Gate did.

---

# Phase 2 Owner Decisions Required

**RATIFIED 2026-09-03 -- see DECISIONS.md #12 for the full binding text of all 8 decisions.**
All eight rows below were decided per the Owner's stated "Product recommendation" column in every case (options (a) throughout), with no amendment. This table is left otherwise unedited as the historical record of what was proposed; DECISIONS.md #12 is the authoritative decided text.

| # | Question | Options | Product recommendation | Technical impact | Business impact | Blocks implementation? |
|---|---|---|---|---|---|---|
| 1 | Ratify the Job.Status state machine? | (a) Adopt Business Analyst's recommended machine (`checked_in→estimate_pending→awaiting_approval→approved→in_progress→completed→invoiced→closed` + `cancelled`/`deleted`) as-is. (b) Amend it. | Adopt as-is — it's derived directly from the existing permission matrix's Reception-stage/Work-stage language and requires no schema rework either way. | Low either way (lookup-table design absorbs future changes without a migration). | Defines exactly what staff can do at each stage. | **Yes** — P2-WP3 cannot be built against an unratified state machine. |
| 2 | Who may clear an Estimate out of `pending_owner_approval`? | (a) Owner only (mirrors the existing "Owner has no discount cap" pattern). (b) Owner or Manager. | Owner only. | Trivial (one authorization policy). | Keeps the $500 control meaningful — a Manager-clearable gate would functionally reintroduce a bypass. | **Yes** — P2-WP4 needs this before the endpoint can be built. |
| 3 | Post-customer-approval re-quote rule | (a) New revision supersedes/locks the prior one; both approval types restart independently (Business Analyst's recommendation). (b) Allow in-place edits to an approved estimate. | (a) — in-place edits to an approved/priced document is a real business-integrity risk (silent price changes after customer sign-off). | (a) requires the `superseded` status value (already planned); (b) would require reworking `EstimateMutationBoundaryTests`' whole approach. | (a) protects the customer and the garage's own record; (b) is operationally simpler but weaker. | **Yes** — P2-WP4's revisioning design depends on this. |
| 4 | Soft-delete for Customer/Vehicle? | (a) Add soft-delete (matches Job's pattern; protects historical Job/Estimate/Invoice references from an orphaned FK). (b) Hard-delete restricted to Owner role only, no soft-delete. | (a) — Database Engineer's recommendation; a customer referenced by years of invoice history should never disappear from those records. | (a) is a small additive schema change; (b) needs no schema change but risks referential-integrity gaps later. | (a) preserves audit/history integrity for accounting purposes. | No — can default to (a) and be revisited, but flagged so it isn't silently decided by an engineer mid-build. |
| 5 | Duplicate vehicle plate: hard-block or warn? | (a) Allow with a warning (Business Analyst's recommendation — `PlateCountry` exists precisely to disambiguate). (b) Hard unique constraint on `(GarageId, PlateNumber, PlateCountry)`. | (a). | (a) is already the planned index design; (b) would need a unique constraint and a conflict-handling UX. | (a) avoids blocking a legitimate re-registered-plate scenario common in Lebanon. | No — (a) is the default; escalate only if the Owner wants (b). |
| 6 | Voided-invoice refund/credit handling | (a) Out of scope for Phase 2, **and Phase 2 blocks voiding an invoice that already has recorded payments** (added per Business Analyst review as a guard rail — see P2-WP6), so the undefined refund/credit scenario cannot actually occur. (b) Build a formal credit-note/refund workflow now. | (a) — Business Analyst's original concern (silently allowing money-received-but-unvoidable-record states) is resolved by the guard rail, not just labeled a known limitation. | (a) needs a one-line service check, already specified; (b) is a meaningfully larger package. | (a) means a garage must issue a manual adjustment/new invoice for the rare paid-then-needs-voiding case in Phase 2; (b) delays the whole Invoice/Payment milestone. | No — (a) with the guard rail is the shipped default; P2-WP6 does not ship without the guard rail. |
| 7 | Disposition of in-flight RepairTasks/JobParts when an Estimate is superseded mid-repair | (a) Retain as-is — work already performed or parts already ordered under a since-superseded estimate are not auto-cancelled or hidden; they stay attached to the Job and are billed/reconciled at invoicing regardless of which Estimate revision authorized them. (b) Auto-cancel/flag them pending manual review. | (a) — Business Analyst's concern was that this was silently implied rather than decided; (a) is the lower-risk default (never destroys already-performed work) and matches how RepairTask/JobPart are modeled (owned by Job, not by a specific Estimate revision). | Neither option requires new schema; this is a service-logic/UX decision only. | (a) avoids losing record of real work/parts already committed; (b) adds a review workflow not otherwise in Phase 2 scope. | No — defaults to (a); flagged so it is a stated decision, not an implicit gap, per Business Analyst's review finding. |
| 8 | Supplier-accountability tracking for outsourced Job Parts | (a) No Phase 2 requirement — `JobPart` has no `OutsourceSupplier`/cost/return-date fields (unlike `RepairTask`, which does); outsourced-part and outsourced-task remain two independent, unmerged schema concepts in Phase 2. (b) Add outsourcing fields to `JobPart` to match `RepairTask`'s. | (a) — Business Analyst confirmed no business-rule basis exists today for requiring this; treating it as a scope confirmation rather than a silent gap, per their review finding. | (a) needs nothing; (b) is new schema/migration work. | (a) means a garage cannot track supplier accountability for an outsourced *part* the way it can for outsourced *labor* in Phase 2. | No — defaults to (a); revisit in a later phase if it proves to matter operationally. |

---

# Specialist Review of This Plan

Every specialist below reviewed the assembled draft independently and returned **CONCERNS** with specific, concrete findings — not a rubber stamp. All findings judged legitimate were remediated directly in this document (the edits are marked inline throughout, e.g. "added per Business Analyst review", "added per Security Reviewer review"); no finding was dismissed. Full verdict text is quoted in the Owner report (sections 22–30). Post-remediation status:

- **Product Director** — objective/deployment narrative inconsistency (the objective implied Phase 2 puts a garage live, which contradicts the excluded-deployment scope) — **remediated** with the Scope Note under Phase 2 Objective.
- **Product Manager** — P2-WP9's gate needed to be a hard gate, not soft parallelism, and each milestone's checkpoint needed to re-verify the full chain, not just the newest slice — **remediated** in P2-WP4/P2-WP5's Design work fields and the Definition of Done.
- **Business Analyst** — voided-invoice-with-payments needed a guard rail, not just a "known limitation" label; two dropped edge cases (in-flight tasks/parts disposition, JobPart supplier-accountability) needed explicit Owner Decision entries — **remediated**: guard rail added to P2-WP6, Owner Decisions #7 and #8 added.
- **Design Lead** — the Job detail screen's risk tier was ambiguous, at risk of inheriting Floor's low-risk exemption — **remediated** in P2-WP3's Frontend work field, explicitly gated by P2-WP9 like Money/Parts.
- **CTO** — the split-service pattern (JobService/JobStatusService, etc.) needed an explicit sole-writer-per-field confirmation to avoid reintroducing a second mutation path internally — **remediated** with the Sole-Writer Confirmation added to P2-WP7.
- **Technical Architect** — WP5's activation gate needed to explicitly read Job.Status, not Estimate.Status directly; WP5's backend/schema work can parallelize with WP4; the no-client-supplied-Total/Status rule needed to be stated plan-wide, not only for the fully-detailed Milestone 1-2 endpoints — **remediated** in P2-WP5's Dependencies field and the Definition of Done.
- **Database Engineer** — P2-WP4 and P2-WP6 were missing the FK-confirmation language used consistently elsewhere in the plan — **remediated** in both packages' Database work fields. (KI-5 reclassification and the lookup-table-vs-CHECK visibility were both judged non-issues by the reviewer themself — no change needed.)
- **QA Lead** — two CRITICAL gaps: no test for superseded-estimate mutation rejection (the exact invariant Owner Decision #3 depends on), and no concurrency test for simultaneous mutations on the same estimate/revision; one MAJOR gap: no payment-reconciliation concurrency test — **remediated**, all three added to P2-WP4/P2-WP6 Test requirements.
- **Security Reviewer** — two HIGH findings: no test for a cross-tenant `ParentEstimateId` IDOR shape, and no explicit tenant-isolation test for `JobHistoryEntry`; two MEDIUM findings: the owner-clear-approval negative test was implicit not named, and Payment idempotency-key ownership (client vs. server-generated) was unspecified — **remediated**: all four addressed in P2-WP3/P2-WP4/P2-WP6 (idempotency key is now explicitly specified as server-generated).

No BLOCKER/CRITICAL (QA) or CRITICAL/HIGH (Security) finding remains open in this document as a result of this remediation pass. This plan is judged ready for the Owner Decisions Required section to be the only remaining gate before implementation authorization.

---

# Milestones (Owner-Visible Progress)

**Milestone 1 — Customer → Vehicle → Job → Floor board** (P2-WP1, P2-WP2, P2-WP3, P2-WP9 in parallel): the Owner can create a real customer, attach a real vehicle, open a real job with a real sequential job number, and see it on the Floor board — all against a live backend and PostgreSQL, no mock data.

**Milestone 2 — + Estimate, Discount, Approval** (P2-WP4): the Owner can build an estimate on that job, watch a Manager's discount attempt above 15% get denied live, watch a ≥$500 estimate auto-route to pending owner approval, and clear it as Owner.

**Milestone 3 — + Repair Tasks, Parts** (P2-WP5): the Owner can see the approved estimate convert into actionable tasks and parts, watch a Mechanic start/complete their own task, and watch a part move through its sourcing lifecycle.

**Milestone 4 — Full operational slice: + Invoice, Payment** (P2-WP6): the Owner can generate an invoice from the completed job, record a payment, and see the invoice reconcile to paid — the complete Customer-to-cash loop, clickable end to end in one sitting.

Each milestone's Owner Visual Checkpoint follows the standing rule: dev stack started, backend and frontend health confirmed, exact local URL and dev login credentials provided, the new workflow stated explicitly, and the instance left running when practical.
