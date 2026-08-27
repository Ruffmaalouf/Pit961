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

---

## 2026-08-27 — WP-2 complete

**PostgreSQL 15+ (KI-1) resolved per Owner direction** — installed user-locally
without root via official PGDG `.deb` packages extracted with `dpkg-deb -x`
(real PostgreSQL 15.19, no downgrade, no Docker/Testcontainers, no
SQLite/EF-InMemory substitute; nothing added to the repository). See
`KNOWN_ISSUES.md` KI-1 for the full method and rationale.

**WP-2 — Backend Solution Scaffold: DONE.** `backend/GarageOS.sln` +
`GarageOS.Api`/`GarageOS.Application`/`GarageOS.Domain`/`GarageOS.Infrastructure`
+ `GarageOS.Tests.Unit`/`GarageOS.Tests.Integration`, per §4 of
`11_engineering_handoff.md`. Commit `a6bb20a`.

- Serilog (console, config-driven), Swagger/OpenAPI, `/health` health check,
  global ProblemDetails exception handling via `IExceptionHandler`.
- `DemoOptions` demonstrates the strongly-typed options/configuration-binding
  pattern later WPs (JwtOptions in WP-4, BrandingOptions in WP-7) will follow.
- Verified live: `dotnet build` clean (0 warnings/errors); `dotnet run` serves
  Swagger UI (200) and `/health` (200); `/api/demo/config` reflects the
  configured `Demo:Message` value (proves options binding is not hardcoded);
  `/api/diagnostics/throw` (Development/Testing only) returns a proper
  `application/problem+json` ProblemDetails envelope (500) with the exception
  message included only because Development.
- **Test results: 4/4 passing** — `GarageOS.Tests.Unit` (2/2, options-binding)
  and `GarageOS.Tests.Integration` (2/2, health check + ProblemDetails shape)
  run against the real local PostgreSQL 15.19 instance above via
  `WebApplicationFactory` + Respawn. Unreachable-DB path also verified
  separately: both integration tests fail loudly with a clear Npgsql
  connection-refused error when Postgres isn't running — 0 skipped, exactly
  per WP-2's "never silently skip" requirement.
- `.gitignore` corrected: `appsettings.Development.json` was being excluded
  from git, contradicting WP-2's own acceptance criteria that it must be
  *committed* with safe non-secret defaults — fixed to track it while still
  excluding `appsettings.Local.json`/`.env` (the actual secret-bearing files).

**AGENT ACTIVITY**

- **AGENT INVOKED: QA Automation Engineer** — Task: validate the WP-2 test
  harness against all four of WP-2's database-handling sub-requirements
  (env/config-only connection string, fail-loud on unreachable DB, Respawn
  reset excluding `__EFMigrationsHistory`, non-parallel collections).
  Status: **Complete — PASS.** One non-blocking note: add a test asserting
  the ProblemDetails `exception` field is absent outside Development
  (tracked as `KNOWN_ISSUES.md` KI-4).
- **AGENT INVOKED: Security Reviewer** — Task: WP-2's security-review
  requirement (no committed secrets, env-var override works, dev/prod secret
  mechanisms documented, `.gitignore` correctness, plus an ad-hoc check that
  the test-only `/api/diagnostics/throw` endpoint can never reach Production).
  Status: **Complete — PASS.** One non-blocking LOW note: `/api/demo/config`
  ships unauthenticated and should be removed once real endpoints exist
  (tracked as `KNOWN_ISSUES.md` KI-3).
- **Not needed for WP-2:** Technical Architect (WP-2 has no architecture
  decisions beyond what WP-1/handoff §4 already specify), Database Engineer
  (no schema in WP-2 — that's WP-3), Frontend/Integration/DevOps Engineers
  (out of WP-2's scope).

**Next:** WP-3 — Database Schema & Tenant Isolation (Database Engineer +
Backend Engineer), and WP-7 (Branding Configuration) / the frontend-tooling
half of WP-8 can run in parallel once picked up, per the approved dependency
graph.
