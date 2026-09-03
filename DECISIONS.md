# DECISIONS.md

Architecture and product decisions made by the owner (Ralph) for PIT961 ("GarageOS" internal codename), logged in the order they were made. This is the authoritative decision log referenced throughout `11_engineering_handoff.md`.

Format: one entry per decision — date, decision, rationale/scope, owner.

---

## 1. Design source of truth

**Date:** 2026-08-26 (logged)
**Decision:** `prototype.html` is canonical for visual design — theme, typography, components, and navigation. Brand identity (product name, logo) is a separate concern and remains undecided.
**Rationale / Scope:** Where any other document (e.g. `09_design_system.md`) conflicts with `prototype.html` on visual design, `prototype.html` governs. Log any unavoidable implementation discrepancy in `DESIGN_IMPLEMENTATION_DIFFERENCES.md`. Brand identity is explicitly carved out of this decision — see decision 6.
**Owner:** Ralph

---

## 2. Pricing

**Date:** 2026-08-26 (logged)
**Decision:** $30 USD/month per garage, billed monthly, one subscription per garage. No pricing tiers for Phase 1.
**Rationale / Scope:** Keeps Phase 1 billing simple. No other tiers, add-ons, or usage-based pricing are in scope until after Phase 1.
**Owner:** Ralph

---

## 3. Multi-garage readiness

**Date:** 2026-08-26 (logged)
**Decision:** One garage per account in the Phase 1 UI. An `accounts` table is added above `garages` in the schema now, so multi-garage ownership is additive later, not a breaking migration.
**Rationale / Scope:** `accounts` holds billing identity (Stripe customer, subscription status/plan, trial dates); `garages.account_id` links every garage to its owning account. For Phase 1, an account has exactly one garage, enforced at the application layer (not a hard DB constraint). Row-level tenant isolation via `garage_id` is unchanged and remains the isolation key — `account_id` is never used for tenant isolation.
**Owner:** Ralph

---

## 4. Platform Admin

**Date:** 2026-08-26 (logged)
**Decision:** Platform admin is a separate `platform_admins` identity with its own distinct JWT claim, structurally and mutually exclusive with garage-tenant tokens. The platform admin UI and `/api/v1/platform/*` endpoints are deferred to Phase 6 (SaaS).
**Rationale / Scope:** The `platform_admins` table and the JWT claim design must be decided and reserved now, during Phase 1 Foundation, so the `users` table design and JWT-issuance code don't need rework when Phase 6 arrives. A platform-admin token must never satisfy a garage-tenant authorization policy, and vice versa.
**Owner:** Ralph

---

## 5. Hosting

**Date:** 2026-08-26 (logged)
**Decision:** Hosting provider selection is deferred and is not a Phase 1 blocker. Infrastructure and deployment are kept provider-neutral throughout Foundation work.
**Rationale / Scope:** Before staging deployment, the Technical Architect must present a full hosting recommendation to the owner (frontend/backend/database hosting, file storage, backup strategy, secrets management, CI/CD, approximate cost, scalability headroom, Lebanon/MENA latency). Nothing is deployed to staging or production until the owner reviews and approves that recommendation. Only the hosting *provider* is deferred — the technology stack itself (React/TypeScript/Vite, ASP.NET Core 8, PostgreSQL, EF Core 8) is decided and unchanged.
**Owner:** Ralph

---

## 6. Branding

**Date:** 2026-08-26
**Decision:** The final customer-facing product/brand name is undecided. "RASHID" and "GarageOS" are both rejected as decided final brands. "PIT961" is the internal project codename. Product display name, email "From" name, logo, and JWT issuer/audience must all be configurable, never hardcoded. Internal namespaces, the solution name, and package names may permanently keep a codename (`PIT961` or `GarageOS`) — that is not something to rename later, since customers never see it.
**Rationale / Scope:** Branding must not be architecturally load-bearing. This is a light-touch configurability requirement (`Branding:ProductDisplayName`, `Branding:EmailFromName`, `Branding:LogoUrl`, `Jwt:Issuer`, `Jwt:Audience`) — not a white-labeling subsystem. No multi-brand theming, per-garage brand overrides, or brand admin UI is in scope now. See `11_engineering_handoff.md` §7A (Branding & Configuration).
**Owner:** Ralph

---

## 7. Authorization

**Date:** 2026-08-26
**Decision:** Authorization is built on ASP.NET Core's policy-based framework (`IAuthorizationRequirement` + `AuthorizationHandler<T>`) from Phase 1, scoped initially to exactly two concrete policies: the Manager discount cap (≤15%, reject above unless actor is Owner) and the $500 estimate-approval threshold (routes to "Pending Owner Approval" instead of sending). No generic rules engine, DSL, or admin-configurable rule editor is built for Phase 1.
**Rationale / Scope:** The framework's *shape* must be able to express role membership, granular permissions, contextual business rules, amount-based limits, tenant-boundary checks, and resource/ownership checks without redesign later — but only the two named policies are actually implemented now. Adding the next rule later should mean writing one new handler, not restructuring existing authorization code. This is also where the platform-admin/garage-tenant mutual exclusion (decision 4) plugs in: a platform-admin claim has no `garage_id` to match, so it fails every garage-tenant tenant-boundary check by construction. See `11_engineering_handoff.md` §28 (Authorization).
**Owner:** Ralph

---

## 8. Email

**Date:** 2026-08-26
**Decision:** Resend is the approved Phase 1 email provider, accessed only through an `IEmailService` abstraction (`ResendEmailService` is the sole class allowed to reference the Resend SDK). SMS and WhatsApp are explicitly deferred to their respective feature phases (WhatsApp/notifications land with Phase 5 — Communication) and must not block Phase 1 email work.
**Rationale / Scope:** Phase 1 email capabilities: password reset and general account-related transactional email (invites, account status changes). Email verification on registration is optional/TBD — not currently a specified requirement of the registration flow — and will be added to `IEmailService` if and when the approved registration flow requires it. The Resend API key is a secret, managed via the existing environment-variable-based configuration approach, never hardcoded. The "From" display name comes from `Branding:EmailFromName` (decision 6), not a hardcoded brand string. See `11_engineering_handoff.md` §11A (Email Service).
**Owner:** Ralph

---

## 9. Phase 1 execution plan amendment (v2)

**Date:** 2026-08-27 (logged)
**Decision:** Ralph approved a Company Dispatcher-run amendment pass on `13_phase1_execution_plan.md` following the pre-implementation activation review (CTO/Technical Architect/QA Lead/Security Reviewer). Fourteen findings were resolved and the plan was revised to v2, routed through Technical Architect, Backend Engineer, Database Engineer, DevOps Engineer, QA Lead, QA Automation Engineer, and Security Reviewer. Key changes: explicit test-project scaffolding added to WP-2/WP-8; Docker local-dev environment added across WP-1/WP-2/WP-8 (no production orchestration); a new WP-3B (Account/Garage Provisioning Service) created so the one-garage-per-account rule has an actual owning, testable code path while keeping multi-garage support a one-line future relaxation, not a migration; WP-3's tenant-isolation QA scope corrected to the 12 resources that actually have Phase 1 schema (Expenses/Attachments/Reports deferred to their own phases per a new project-wide Standing Rule); WP-5's authorization acceptance criteria rewritten to handler/service-level testing only (no fabricated Phase 3 endpoints) with mandatory boundary tests (15.00%/15.01% discount, $500.00/$500.01 estimate) and an explicit single-mutation-path bypass-protection requirement; WP-4 rewritten to remove any live platform-admin login/JWT-issuance endpoint from Phase 1 scope (schema/claim/policy architecture only, validated by test-only token construction — a live endpoint is Phase 6, gated by MFA/rate-limiting/audit-logging/session-security requirements not yet needed); WP-9's CI dependency graph corrected to include WP-3B/WP-4/WP-5; JWT configuration ownership split explicitly across WP-2 (pattern)/WP-4 (`JwtOptions`)/WP-7 (`BrandingOptions`); JWT signing-key security, `appsettings.json` secrets handling, and password-reset anti-enumeration (no artificial-delay-only defense) criteria added; the Resend-SDK-isolation and no-"Rashid" checks moved from one-time manual review into CI-enforced grep steps; a new "Phase 1 Quality Gates" section added defining the QA/Security gate pipeline and BLOCKER/CRITICAL/HIGH severity thresholds for Phase 1 acceptance.
**Rationale / Scope:** None of this reopens an owner decision — DECISIONS.md #1–#8 are unchanged and still govern. This is plan-quality remediation only: closing gaps the activation review found in how the plan's own acceptance/QA/security-review criteria were worded, not a change to approved architecture, product scope, or business rules. No application code was written; only `13_phase1_execution_plan.md` (and this entry) were amended. The amended plan is subject to the same owner-approval gate as v1 before Phase 1 implementation may begin.
**Owner:** Ralph

---

## 10. Docker/containerization removed from Phase 1 (supersedes the Docker portion of Decision #9)

**Date:** 2026-08-27 (logged)
**Decision:** Docker/containerized local development is deferred and is **not** part of Phase 1. This explicitly supersedes the Docker-related additions made in Decision #9's v2 amendment (the `docker/` directory, `docker-compose.yml`, backend/frontend `Dockerfile`s, the `docker compose up` acceptance criterion, and Testcontainers as the integration-test mechanism) — those additions are removed from `13_phase1_execution_plan.md` (now v3) and from `11_engineering_handoff.md`. Decision #9 itself is left unedited as the historical record of what v2 contained; this entry documents what changed and why, so the decision history stays legible rather than silently rewritten.
**Rationale / Scope:** The Owner wants the initial implementation to use the native ASP.NET Core, React/Vite, and PostgreSQL development toolchains directly (`dotnet run`/`dotnet watch`, `npm run dev`/`npm run build`), with minimal infrastructure complexity in Phase 1. Testcontainers is replaced with a non-Docker integration-test model: a locally installed/reachable PostgreSQL instance with a dedicated PIT961 integration-test database, a configuration-supplied test connection string, and automated reset/cleanup between test runs (Respawn/truncation-based, per the amended WP-2); in CI, PostgreSQL is provisioned via the CI provider's native service-container support (e.g. GitHub Actions `services:`), which requires no Docker installation or dependency from developers or the project. Integration tests continue to run against real PostgreSQL, not an in-memory/SQLite substitute, so PostgreSQL-specific schema behavior (§9) is still faithfully exercised. The project remains container-friendly (environment-variable-driven configuration, no host-specific coupling baked into application code) so containerization may be reconsidered later, before staging/production, if it provides a concrete benefit at that time — no alternative container/orchestration technology is substituted in Docker's place now. DevOps Engineer's Phase 1 scope (CI, environment configuration, secrets, build pipelines, logging/health checks, deployment-readiness documentation) is unchanged; only containerization is removed from it.
**Owner:** Ralph

---

## 11. Phase 1 Execution Plan v3 — Owner approval and implementation authorization

**Date:** 2026-08-27
**Decision:** Ralph approved Phase 1 Execution Plan v3 (`13_phase1_execution_plan.md`) in full, including the v2 amendment (14 items) and the v3 Docker-removal amendment (Decision #10). The plan's status is changed from "PROPOSED / Awaiting Owner Approval" to "APPROVED — PHASE 1 IMPLEMENTATION AUTHORIZED". The v3 Docker-removal amendment was routed through specialist re-review — Technical Architect: PASS, QA Lead: PASS, QA Automation Engineer: PASS, DevOps Engineer: PASS. Security re-review was explicitly not required for that amendment, because removing Docker/Testcontainers in favor of native `dotnet run`/`npm run dev` workflows and CI-native PostgreSQL service containers did not alter authentication, authorization, tenant isolation, secrets handling, or any other security control.
**Rationale / Scope:** This decision authorizes the Company Dispatcher to begin Phase 1 implementation starting with WP-1 (Repo & Environment Bootstrap) and WP-10 (Engineering Tracking Docs) in parallel, followed by the approved dependency graph. No Docker, docker-compose, Dockerfiles, Testcontainers, Kubernetes, Podman, or replacement container/orchestration technology may be introduced in Phase 1 without a separate, explicit Owner approval. Production deployment and production data modification remain excluded from this authorization and continue to require separate explicit Owner approval per standing company policy.
**Owner:** Ralph


---

## 12. Phase 2 Owner Decisions (8, ratified) and Phase 2 Execution Plan approval

**Date:** 2026-09-03
**Decision:** Ralph reviewed the Phase 2 planning report and approved the Phase 2 direction. `PIT961_OS_SPEC.md` is accepted as a design/specification artifact only — major PIT961 OS implementation remains NOT AUTHORIZED, with the trigger for reconsidering PIT961 OS MVP implementation set at Phase 2 Milestone 4's Final Acceptance Gate (the complete Customer → Vehicle → Job → Estimate → Approval → Repair Tasks/Parts → Invoice → Payment operational loop). `14_phase2_execution_plan.md`'s status is changed from "PROPOSED — Awaiting Owner Approval" to "APPROVED — PHASE 2 IMPLEMENTATION AUTHORIZED". All 8 of the plan's "Owner Decisions Required" are now explicitly decided and binding for Phase 2:

1. **Job status state machine — Option A.** `checked_in → estimate_pending → awaiting_approval → approved → in_progress → completed → invoiced → closed`, with terminal/exception paths `cancelled` and `deleted`. Implemented through a governed state-machine architecture; no arbitrary raw `Job.Status` assignment; `JobStatusService` is the sole authoritative writer.
2. **Pending owner approval — Owner only.** Only the Owner role may clear an Estimate from `pending_owner_approval`. Managers must not be able to clear this gate. The frontend may display the state appropriately, but backend authorization remains authoritative — this preserves the meaning of the existing $500 owner-approval rule (Decision #7 in this log).
3. **Re-quote after customer approval — Option A.** An approved estimate may not be silently edited in place. Pricing/scope changes after customer approval create a new Estimate revision linked via `ParentEstimateId`; the prior revision becomes superseded and immutable; owner-approval state and customer-approval state both restart independently. No edits to an already-approved/superseded revision through alternate endpoints.
4. **Customer/Vehicle delete — soft delete.** Historical Jobs/Estimates/Invoices/Payments retain their historical references; historically-referenced Customer/Vehicle records are never physically destroyed.
5. **Duplicate vehicle plate — warn, do not hard-block.** A duplicate `GarageId + PlateNumber + PlateCountry` returns a visible warning, not a blocking database unique constraint. The UX must make the possible duplicate obvious.
6. **Voided invoice with payments.** A formal refund/credit-note workflow is out of scope for Phase 2. An invoice with any recorded, non-voided payment may not be voided — `InvoiceService` must fail closed when `TotalPaid > 0` or equivalent persisted payment evidence exists. No silent erasure of financial history. Refunds/credit notes are a later product decision.
7. **Work/parts after estimate supersession — retain as-is.** RepairTasks and JobParts that already exist when an Estimate is superseded are not automatically deleted or cancelled; they remain attached to the Job and retain their real operational history. May be reconciled during invoicing.
8. **Outsourced JobPart supplier tracking — no additional schema in Phase 2.** No speculative supplier fields, supplier cost fields, expected-return dates, or outsourcing workflow on `JobPart` unless another already-approved requirement genuinely needs them. Revisit in a later phase if actual garage operation proves the need.

**Rationale / Scope:** This authorizes Phase 2 implementation to begin per the approved dependency graph, starting with P2-WP1 (KI-8 remediation, prerequisite) and P2-WP9 (design/prototype audit) in parallel, then P2-WP2 (Customer/Vehicle), then P2-WP3 (Job/Floor), with a mandatory Owner Visual Checkpoint at Milestone 1 before any further work package begins. Standing company policy is unchanged and remains fully in force: repository stays intentionally public with strict secret hygiene; no Docker/Testcontainers/Kubernetes/Podman; no production deployment or production-data change without separate explicit Owner approval; QA (BLOCKER/CRITICAL) and Security (CRITICAL/HIGH) remain independent, non-self-clearing gates; CI is extended, not rebuilt, and no mandatory test may use `continue-on-error`. Normal implementation details within the approved plan belong to specialists and do not require further Owner approval — Owner escalation is reserved for a genuine Owner-level blocker or the next milestone checkpoint.
**Owner:** Ralph
