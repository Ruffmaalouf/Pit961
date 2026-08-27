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

---

## 2026-08-27 — WP-3 complete (ACCEPT WITH TRACKED FOLLOW-UPS)

**WP-3 — Database Schema & Tenant Isolation: DONE / ACCEPTED.** Per the Device Execution
Protocol: Database Engineer produced the implementation brief; Company Dispatcher acted
as Device Executor (implemented, built, migrated against real PostgreSQL 15+, ran the
tenant-isolation test matrix); Database Engineer, QA Automation Engineer, and Security
Reviewer each independently reviewed the resulting implementation; QA Lead ran the final
gate. Commits `61b4bf9` (initial implementation), `067e4d1` (remediation of Database
Engineer + QA Automation findings), `0fc3201` (remediation of QA Lead's two follow-ups).

**Implementation (Device Executor):** `GarageOS.Domain` — 16 entities (`ITenantOwned`
marker on 13, `ISoftDeletable` on `Job`), `PlatformAdmin` kept structurally separate under
`Domain/Platform`. `GarageOS.Application` — `ICurrentTenant`, `TenantGuard.EnsureOwned`
(explicit write-ownership check), `TenantOwnershipException`,
`TenantContextUnavailableException`. `GarageOS.Infrastructure` — `AppDbContext` (16
DbSets, reflection-based global query filters over `ITenantOwned`+`ISoftDeletable`) and
`PlatformDbContext` (schema `platform`, `platform_admins` only, separate migrations-history
table), 16 EF entity configurations (CHECK constraints, precision, indexes, no-cascade
FKs, all cross-checked against `11_engineering_handoff.md` §9 verbatim), both design-time
factories, idempotent dev seeder matching §61 exactly (incl. the mandatory John
Smith/BMW 328i/91850-mileage §62 scenario), `HttpContextCurrentTenant` (fails closed on
every missing/malformed-claim path).

**Bug caught and fixed before any specialist review:** `AppDbContext.OnModelCreating`'s
initial `ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)` call scanned the
whole `GarageOS.Infrastructure` assembly regardless of folder, silently pulling
`PlatformAdminConfiguration` into the App model/migration — a direct violation of the
platform/tenant separation requirement. Fixed with the namespace-excluding predicate
overload; migrations regenerated from the corrected model; verified via `grep -c
platform_admins` on the App migration → 0.

**Database Engineer implementation review: APPROVE WITH FOLLOW-UPS.** Found and the
Device Executor fixed: (1) `GarageConfiguration.cs` was only creating one of two required
indexes on `garages.account_id` (`garages_account_active_idx` silently overwrote
`garages_account_idx`, an EF Core "two unnamed HasIndex calls on the same property
collapse" pitfall compounded by EFCore.NamingConventions recomputing database names
unless explicitly pinned) — fixed with the named `HasIndex(expr, name)` overload plus
explicit `.HasDatabaseName()` on both, applied via a new migration
(`FixGaragesAccountIndex`) against real Postgres, verified via `\di`; (2) missing unit
coverage for `TenantGuard.EnsureOwned`'s happy path — added `TenantGuardTests.cs`;
(3) missing meta-test proving the query-filter loop covers every `ITenantOwned` entity —
added `QueryFilterCoverageTests.cs`, reflection-based in both directions. One item left as
a tracked non-blocker: no SQL-level column `DEFAULT`s (`KNOWN_ISSUES.md` KI-5).

**QA Automation review: PASS WITH NON-BLOCKING NOTES**, one significant finding: the
`GarageId_CannotBeClientSupplied_OnCreate` test was tautological in all 12
tenant-isolation test files — it only proved the server-correctly-set `garage_id`
persisted, never actually attempted a mismatched/attacker-supplied value, so it gave no
real evidence against brief §16's "mismatched-payload case" despite its name. Fixed: every
file now also asserts `TenantGuard.EnsureOwned` throws given a genuinely mismatched
tenant's `garage_id`. Also flagged 3 of 7 child-of-parent resources (Invoices, Payments,
JobHistory) missing the 5th parent-mismatch test — fixed, now 7 of 7.

**Security review: PASS**, no CRITICAL/HIGH findings. Two LOW findings, both correctly
scoped as out-of-WP-3-scope and tracked: shared Postgres credential across both
DbContexts (`KNOWN_ISSUES.md` KI-6, Phase 2+ item) and the dual-enforcement pattern's
shared root of trust in `ICurrentTenant.GarageId` (`KNOWN_ISSUES.md` KI-7, WP-4's own
security review must re-verify once real JWT claims exist). Confirmed
`HttpContextCurrentTenant` fails closed on every path, confirmed `platform_admins`
unreachability at three independent layers, confirmed the QA tautology fix is real by
direct reading.

**QA Lead final gate: ACCEPT WITH TRACKED FOLLOW-UPS.** Independently reconciled the
66-test count by direct `[Fact]` count (not taken on faith), spot-checked the index fix
and tautology fix personally, concurred KI-5 is correctly non-blocking for Phase 1. Raised
two new findings, both closed before reporting WP-3 as accepted: (A) `Vehicles`/`Jobs`
were also missing their own parent-mismatch test (`Vehicle`→`Customer`,
`Job`→`Customer`/`Vehicle`) — fixed, now 9 of 9 eligible resources covered; (B) the brief
itself makes "wired into CI" WP-3's own acceptance criterion (not WP-9's), but no CI
config existed anywhere in the repo — closed by adding `.github/workflows/ci.yml`
(DevOps Engineer specialist-authored, Device Executor-committed): builds, applies both
DbContexts' migrations, and runs the full test suite against a real PostgreSQL 15 GitHub
Actions service container on every push/PR to `main`. This is a skeleton — WP-9 remains
responsible for its own additional scope (Resend-SDK-leakage/"Rashid"-placeholder grep
checks, frontend build once WP-8 exists, gating-complete status after WP-4/WP-5 land).

**Final verified state:** `dotnet build` clean (0 warnings/errors) and `dotnet test`
**68/68 passing** (4 unit + 64 integration) against real PostgreSQL 15.19 on the device,
migrations applied fresh to a dropped-and-recreated database. No Docker, Testcontainers,
PostgreSQL 14 substitute, or SQLite/EF InMemory substitute used anywhere in this WP's
verification, per the Owner's explicit instruction.

**Next:** WP-3B (Account/Garage Provisioning Service) is now unblocked per the Owner's
"do not begin WP-3B until WP-3 is accepted" instruction — WP-3 is now accepted. Not yet
started; awaiting the next instruction. WP-4 (Authentication/JWT) is also now unblocked
(depends on WP-3). WP-7/WP-8 (frontend tooling half) remain approved for parallel work
per the Owner's standing instruction.
