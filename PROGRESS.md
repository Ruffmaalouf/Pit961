# PROGRESS.md

Narrative log of Phase 1 implementation progress, most recent first. See
`IMPLEMENTATION_MAP.md` for the WP-by-WP status table and `TEST_STATUS.md` /
`KNOWN_ISSUES.md` for test results and open issues.

---

## 2026-08-27 — Phase 1 kickoff

**Owner approval recorded.** `13_phase1_execution_plan.md` v3 approved; status
changed to APPROVED — PHASE 1 IMPLEMENTATION AUTHORIZED (`DECISIONS.md` #11).
Stale wording in the plan's v3 Amendment Log (implying the Docker-removal
change had not been re-routed through specialist review) corrected to record
the actual reviews: Technical Architect — PASS, QA Lead — PASS, QA Automation
Engineer — PASS, DevOps Engineer — PASS; Security re-review was not required
because the change did not touch authentication, authorization, tenant
isolation, or secrets handling.

**WP-1 — Repo & Environment Bootstrap: DONE.**
- `git init`, default branch `main`.
- `.gitignore` hardened: added `appsettings.Local.json` / `appsettings.*.Local.json`
  (the plan's documented local-secrets-override file) alongside the existing
  `appsettings.Development.json`, `.env`, `*secrets*.json`, `*.pfx`, `*.key`,
  and `_archive/` exclusions — this was a real gap found during WP-1's
  security-review step (plain `appsettings.json` is meant to stay committed
  with safe defaults; `appsettings.Local.json` is the intended local-secrets
  file and was not previously excluded).
- `README.md` added: tech stack, no-Docker constraint, repo layout, branch
  strategy (trunk-based `main` + short-lived `wp-<n>-<desc>` feature branches,
  PR-gated once CI/WP-9 exists), getting-started instructions.
- `frontend/` and `backend/` top-level directories created (empty, ready for
  WP-8/WP-2 scaffolding).
- Initial commit `ba3ee96`: existing docs + `.gitignore` + `README.md` only,
  per acceptance criteria. No application code in this commit.
- Security-review requirement satisfied: `.gitignore` confirmed to exclude
  `_archive/` (and `extract.js` within it is not re-added elsewhere).

**WP-10 — Engineering Tracking Docs: DONE.**
`IMPLEMENTATION_MAP.md`, `PROGRESS.md` (this file), `TEST_STATUS.md`,
`KNOWN_ISSUES.md` created at repo root.

**Environment verification for WP-2 (Backend Solution Scaffold):**
- The device-bridge shell used for repository work has no system-package
  (`apt`/`sudo`) install capability (not root; `sudo` is disabled in this
  sandbox) and Ubuntu's default archive only carries PostgreSQL 14, not the
  15+ the plan pins.
- `.NET 8 SDK` was installed **user-space** (no root required) via Microsoft's
  official `dotnet-install.sh` script into a local directory. Verified against
  a real `dotnet new webapi` project: `dotnet restore` and `dotnet build`
  both succeed end-to-end, confirming NuGet package restore (EF Core, Npgsql,
  Serilog, Swashbuckle, etc.) works from this environment.
- PostgreSQL 15+ is not yet provisioned in this environment; this is being
  worked as part of WP-2's integration-test-database setup. See
  `KNOWN_ISSUES.md`.
- No production/staging deployment activity has occurred or is planned in
  this phase.

**Next:** WP-2 — Backend Solution Scaffold (Backend Engineer), in progress.
