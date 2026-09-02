# PIT961 OS — Design & Specification (Parallel Track)

Status: **DESIGN/SPECIFICATION ONLY — major implementation NOT authorized.**
Prepared by: Company Dispatcher
Date: 2026-09-02
Authorization: This spec track is authorized by the Owner's Phase 2 kickoff order. Major PIT961 OS implementation remains explicitly gated — see Implementation Readiness at the end of this document.

---

## Concept

PIT961 OS is the Owner/CEO control-center and company operating system: a single place where Ralph can see and steer PIT961-the-company (the AI-staffed development effort) — company activity, product development, AI employees, work delegation, implementation progress, approvals, blockers, QA, security, git, CI, and product milestones — without relying on scrolling back through long chat transcripts. It is inspired by the Owner-supplied Bennett OS reference material as a conceptual model of visibility/orchestration/command-center feel, not as a UI to clone. PIT961 OS's own visual identity must align with PIT961's own design system (dark/amber, IBM Plex), not import Bennett OS's specific styling.

PIT961 OS is a company-operations tool. It is explicitly **not** the garage-management product itself (that's the core SaaS this whole engineering effort builds) — see Architecture Boundary below for how the two stay decoupled.

---

## Core Experience — Modules

### 1. Company Overview
Shows the current company objective, active project, current phase (e.g. "Phase 2 — Planning"), phase progress, active work packages, recent completions, and the next recommended action. **Source**: structured project state — the tracking documents this very planning cycle produced/read (`14_phase2_execution_plan.md`, `IMPLEMENTATION_MAP.md`, `PROGRESS.md`) — parsed into structured data, not scraped ad hoc.

### 2. AI Employees
Lists every company agent (the 16-agent `pit961-company` roster: company-dispatcher, product-director, product-manager, business-analyst, design-lead, ui-ux-designer, cto, technical-architect, database-engineer, backend-engineer, frontend-engineer, integration-engineer, devops-engineer, qa-lead, qa-automation-engineer, security-reviewer) with name, role, current task, status, last activity, assigned project, current blocker, and last verdict. **Hard rule carried over from this spec's own drafting process**: never claim an agent is active unless it genuinely is — this module must reflect real dispatch records, not a static roster rendered as if constantly busy. **Source**: FUTURE INTEGRATION DEPENDENCY — this requires a company orchestration/activity log that does not exist yet as a queryable data source (today, agent activity exists only as this conversation's own tool-call history).

### 3. Live Work
Shows tasks currently running, queued tasks, Owner approvals needed, completed work, failed work, and retried work. **Source**: FUTURE INTEGRATION DEPENDENCY — same as above; would need a durable task/run log outside any single chat session.

### 4. Product Roadmap
Shows phase, work packages, dependencies, milestone status, completion percentage, and Owner visual checkpoints. **Source**: tracking docs / structured project state (`14_phase2_execution_plan.md`'s Work Package Matrix and Milestones sections, `IMPLEMENTATION_MAP.md`) — this is the most immediately buildable module since the source documents already exist in the format this plan uses.

### 5. QA Center
Shows latest test counts, QA gates, failed tests, flaky tests, blocker findings, and remediation status. **Source**: CI/test artifacts — GitHub Actions run results and `TEST_STATUS.md`. Real test counts (181/181 backend, 79/79 frontend, 4/4 Playwright as of Phase 1) are already available via the same GitHub Actions API this dispatcher used to verify CI state for this very report — a genuinely buildable, low-risk module.

### 6. Security Center
Shows security gates, findings by severity, secret scanning, auth/security health, tenant-isolation health, and unresolved security issues. **Source**: partly `KNOWN_ISSUES.md` (structured severity/status data already exists there) and partly FUTURE INTEGRATION DEPENDENCY for live secret-scanning (no such tooling is wired into CI today beyond the placeholder-brand and Resend-boundary grep guards).

### 7. Git/CI
Repository, current branch, HEAD commit, recent commits, open PRs, CI status, failed workflows, green streak, and (eventually) latest deployment. **Source**: GitHub API (`https://api.github.com/repos/Ruffmaalouf/Pit961/...`) — directly buildable today; this dispatcher used exactly this API, unauthenticated (the repo is public by Owner decision), to verify the Phase 1 record correction in this same session. This is the single most "ready right now" module in the entire spec.

### 8. Owner Approvals
A central place for architecture decisions, scope changes, deployment approval, product decisions, pricing decisions, and feature promotion/defer decisions. This session's own "Phase 2 Owner Decisions Required" table (in `14_phase2_execution_plan.md`) is exactly the shape of data this module would need to render. **Source**: a company decision store — today that's `DECISIONS.md` (append-only, human-authored) plus this plan's own Owner Decisions Required table; a structured version of the same content is a small, buildable step.

### 9. Known Issues
ID, severity, owner, phase, status, deferred-until, blocking/non-blocking. **Source**: `KNOWN_ISSUES.md` directly — it already carries almost exactly this shape (KI-N, severity, status, affected area) and is the second-most "ready right now" module after Git/CI.

### 10. Design
Approved source of truth, current implementation, screenshots, deviations, Design Lead verdict. **Source**: `prototype.html` (source of truth) plus `DESIGN_IMPLEMENTATION_DIFFERENCES.md` (deviations, already structured) — buildable now for the deviation log; screenshots of current implementation are a FUTURE INTEGRATION DEPENDENCY (would need an automated screenshot pipeline against the running dev app).

### 11. Product Preview
Eventually lets the Owner launch/view the current development product directly from PIT961 OS. **Source**: FUTURE INTEGRATION DEPENDENCY — requires the garage SaaS to have a reachable dev/staging URL, which does not exist yet (no hosting decision has been made; Phase 2 explicitly does not deploy anywhere).

### 12. Company Activity Log
A human-readable timeline ("Product Manager planned X → Backend Engineer designed Y → Dispatcher executed Z → QA blocked it → Backend remediated → QA passed → Security passed → Owner checkpoint ready"). **Source**: FUTURE INTEGRATION DEPENDENCY — needs the same durable orchestration/activity log as modules 2 and 3; without it, this module can only be manually curated per report, not live.

---

## Architecture Boundary

Three clearly separated data domains, with integration contracts rather than direct database coupling wherever they must connect:

**A. Garage Product Data** — customers, vehicles, jobs, estimates, inventory/parts, payments, etc. This lives entirely inside the PIT961 garage SaaS's own PostgreSQL database (`AppDbContext`), tenant-isolated by `GarageId`, exactly as Phase 1 built it. PIT961 OS must never query this database directly.

**B. Company Operating Data** — projects, phases, work packages, agents, agent runs, findings, approvals, milestones, company decisions. This is PIT961 OS's own domain and would need its own storage, separate from the garage product's tenant data, with no `GarageId`/tenant coupling at all (this is company-internal, single-tenant-for-the-Owner data).

**C. External Development System Data** — GitHub, CI, future hosting, possibly Claude/company-orchestration telemetry. Reached via each system's own API (GitHub REST API for Git/CI, as already proven usable in this session) rather than any local database mirror where a live API call is sufficient.

The core garage SaaS must remain independently deployable and must never take a hard dependency on PIT961 OS existing, being available, or being healthy — PIT961 OS is a read-mostly control layer over B and C, with only read-only, API-mediated visibility into A's *aggregate* state (e.g., "how many garages are active" as a count, never raw tenant data) if and when that's ever wanted, which is explicitly OS LATER scope, not MVP.

---

## Data Sources Summary

| Module | Authoritative source today | Status |
|---|---|---|
| Git/CI | GitHub REST API (`api.github.com/repos/Ruffmaalouf/Pit961`) | **Ready now** — used live in this session |
| Known Issues | `KNOWN_ISSUES.md` (structured) | **Ready now** — needs a parser, not new data |
| Product Roadmap | `14_phase2_execution_plan.md`, `IMPLEMENTATION_MAP.md` | **Ready now** — needs a parser/structured export |
| QA Center | CI test-run artifacts, `TEST_STATUS.md` | **Ready now** |
| Owner Approvals | `DECISIONS.md`, this plan's Owner Decisions table | **Ready now** for a read view; write-back (Owner approving from the OS itself) is OS LATER |
| Design | `DESIGN_IMPLEMENTATION_DIFFERENCES.md`, `prototype.html` | **Partially ready** — deviations ready now; screenshots are a FUTURE INTEGRATION DEPENDENCY |
| Security Center | `KNOWN_ISSUES.md` (partial) | **Partially ready** — live secret-scanning is a FUTURE INTEGRATION DEPENDENCY |
| Company Overview | Composite of the above | **Ready now** as a rollup once the above parsers exist |
| AI Employees | Company orchestration/activity source | **FUTURE INTEGRATION DEPENDENCY** — no durable, queryable agent-run log exists outside individual chat sessions today |
| Live Work | Same as above | **FUTURE INTEGRATION DEPENDENCY** |
| Company Activity Log | Same as above | **FUTURE INTEGRATION DEPENDENCY** |
| Product Preview | A reachable dev/staging URL for the garage SaaS | **FUTURE INTEGRATION DEPENDENCY** — no hosting exists yet |

No API is invented here that doesn't exist — every "Ready now" row is sourced from a file or API this session actually used.

---

## MVP vs. Later

### OS MVP (the smallest valuable Owner control center)
A single-page, read-only dashboard covering exactly the "Ready now" and "Partially ready" rows above: **Git/CI** (live GitHub state), **Known Issues** (parsed from `KNOWN_ISSUES.md`), **Product Roadmap** (parsed from the execution-plan work-package matrix and milestones), **QA Center** (latest CI test counts), **Owner Approvals** (read view of `DECISIONS.md` plus any open Owner-decision items from the current execution plan), and a **Company Overview** rollup tying those together. This alone would have let the Owner see, at a glance and without reading this whole report, that Phase 1 was accepted, that 6 consecutive CI runs were green, and that Phase 2 planning was in progress — genuinely useful, buildable almost entirely from data sources that already exist in the repository and on GitHub.

### OS LATER
AI Employees, Live Work, and Company Activity Log (all three genuinely need a new durable orchestration/activity data source that does not exist yet); Design module's screenshot pipeline; Product Preview (needs a real hosted dev/staging environment, which needs a hosting decision Phase 2 explicitly defers); any write-back capability (Owner approving/deciding from inside the OS rather than in conversation); any cross-garage aggregate view into Domain A garage data.

---

## Visual Direction

Design Lead's direction (spec-level, no implementation): PIT961 OS should read as a command center — dense, glanceable, state-driven (a lot of status chips/badges: PASS/BLOCKED/PENDING, green/amber/red), not a marketing dashboard. It borrows the *interaction principles* the Bennett OS reference material demonstrates — visibility into many concurrent workstreams, clear agent/task awareness, orchestration-at-a-glance — without cloning its specific visual design. Its own identity should extend PIT961's existing dark (#0b0d0e background) / amber (#e2892f–#f0a458 accent) / IBM Plex Sans+Mono system already established by `prototype.html`, so the Owner recognizes it as the same product family as the garage SaaS itself, not a bolted-on third-party tool. A left rail or top-level module switcher (Company Overview, AI Employees, Live Work, Roadmap, QA, Security, Git/CI, Approvals, Known Issues, Design) is the expected shape, consistent with the existing 76px-rail navigation pattern PIT961 already uses — reused as a pattern, not literally shared code, since PIT961 OS is architecturally separate from the garage SaaS (see Architecture Boundary).

---

## PIT961 OS Implementation Readiness

**NOT READY.**

Reasoning: the MVP's "Ready now" data sources are real, but building even the MVP now would pull engineering attention away from the core garage SaaS at the exact moment Phase 2 needs it most (Phase 2 is the first phase that makes the product actually usable by a garage — that is a materially higher-value use of the same engineering hours than a dashboard about the engineering). This matches the Owner's own standing direction: design/spec PIT961 OS now, delay major implementation until the core garage product is sufficiently implemented, QA-tested, stable, and useful.

## Trigger for Major PIT961 OS Implementation

A concrete, checkable milestone: **Phase 2's Milestone 4 (the full Customer→Job→Estimate→Invoice→Payment loop) reaches its Final Acceptance Gate** (see `14_phase2_execution_plan.md`'s Phase 2 Final Acceptance Gate). At that point the garage SaaS is a genuinely operable product, not just a foundation, and building the OS MVP (which is a small, mostly-read-only, already-sourced dashboard) becomes a reasonable parallel investment rather than a distraction from getting the core product to that point. Company Dispatcher should re-raise PIT961 OS MVP implementation to the Owner explicitly at that milestone, not before.
