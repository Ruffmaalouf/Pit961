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
Engineer + QA Automation findings), `0fc3201` (remediation of QA Lead's two follow-ups),
`fb975b9` (tracking-doc updates recording WP-3's acceptance).

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

---

## WP-3B — Account/Garage Provisioning Service (2026-08-27)

**Specialist ownership:** Backend Engineer authored the implementation brief
(`/tmp/wp3b_brief.md`, produced in the Dispatcher's cloud-side working context) after
reading the actual current repo state, not just the plan text — and flagged two
deliberate deviations from the plan's literal wording rather than silently doing
something else: (1) the concrete service implementation lives in
`GarageOS.Infrastructure` rather than `GarageOS.Application` (Application cannot
reference Infrastructure/`AppDbContext`); (2) the concurrency lock target is the parent
`accounts` row, not the `garages` row the plan text literally suggested, because Postgres
`FOR UPDATE` cannot lock a not-yet-existing ("phantom") row. Database Engineer reviewed
the brief: **APPROVE WITH CHANGES** — one required addition, a documented pre-migration
duplicate-check query with a remediation step for the unique-index migration. Because
specialist sub-agents cannot reliably operate the Windows device directly, the Dispatcher
acted as **Device Executor** for all file edits below — specialists own
architecture/design/review only.

**Implementation (Device Executor):** New files —
`GarageOS.Application/Abstractions/IAccountProvisioningService.cs`,
`GarageOS.Application/Accounts/GarageProvisioningDetails.cs`,
`GarageOS.Application/Common/AccountAlreadyHasGarageException.cs`,
`GarageOS.Application/Common/AccountNotFoundException.cs`,
`GarageOS.Infrastructure/Data/Provisioning/AccountProvisioningService.cs`, migration
`MakeGaragesAccountActiveIndexUnique` (incorporating Database Engineer's required
pre-flight duplicate-check comment verbatim), `GarageOS.Tests.Unit/Architecture/GarageInsertBoundaryTests.cs`
(source-scan bypass-protection test), `GarageOS.Tests.Integration/Provisioning/AccountProvisioningServiceTests.cs`
(8 tests), `GarageOS.Tests.Integration/Provisioning/AccountProvisioningConcurrencyTests.cs`
(2 tests, N=2 and N=10). Modified — `GarageConfiguration.cs` (`.IsUnique()` on the existing
partial index), `DevelopmentSeeder.cs` (now calls `IAccountProvisioningService` exclusively
for the garage insert; no direct `Garages.Add` remains), `Program.cs` (DI registration +
seed-scope resolution). Zero HTTP endpoint added — verified by grep across `GarageOS.Api`.

**Verification (Device Executor, real PostgreSQL 15.19, no Docker/Testcontainers/PG14/
SQLite/InMemory substitute):** Full solution build: 0 warnings, 0 errors. Full test suite:
**79/79 passing** (5 unit + 74 integration; WP-3B added 11 new tests, zero regressions
against the WP-3 baseline of 68). Migration applied cleanly to both
`pit961_integration_test` and `pit961_dev`. Pre-flight duplicate-check query run live
against `pit961_dev` before migrating (0 rows, as expected on this dataset). Seed flow
smoke-tested end-to-end through the real DI-wired service (not a test double): idempotent
re-run against already-seeded data succeeded with no exceptions; a fresh run after
`TRUNCATE` produced exactly 1 account / 1 garage / 1 garage_settings / 1 garage_sequences /
5 users.

**Database Engineer post-implementation review: ACCEPT.** Confirmed the one brief-stage
required change (pre-flight duplicate-check documentation) was incorporated verbatim.
Confirmed `GarageConfiguration.cs`/migration/model-snapshot are mutually consistent and
`Down()` is a byte-for-byte-equivalent rollback to the pre-WP-3B schema. Re-confirmed the
`accounts`-row-locking design is sound. Re-confirmed multi-location-readiness: the
one-active-garage rule is enforced only by a *partial* unique index plus one service's
in-process check, not a hardcoded 1:1 schema shape — relaxing it later is a small
additive migration.

**QA Automation Engineer gate: PASS WITH FINDINGS** (0 BLOCKER/CRITICAL — does not block
acceptance). Independently confirmed every acceptance-criterion test is real (not
trivially passing): duplicate-rejection, cross-account success, genuine N-way concurrency
(separate `AppDbContext`/connection per attempt, real `Task.WhenAll`), bypass-protection
scan scope justified, seed flow has no leftover direct insert, zero HTTP endpoint.
Raised two MEDIUM findings, both logged as tracked follow-ups rather than gating this WP
(neither reflects a live defect in the shipped code): **KI-8** (bypass-protection regex
doesn't catch the generic `DbContext.Add(object)` overload shape — the DB-level unique
index remains the airtight backstop regardless) and **KI-9** (no direct
`pg_indexes`-catalog assertion that both `garages` indexes coexist post-migration, as a
regression guard against WP-3's original index-collapse bug class).

**Security review:** Not separately invoked for WP-3B — the Owner's specialist
assignment for this WP was Backend Engineer (owner) + Database Engineer (reviewer) only,
and neither the Database Engineer nor QA Automation Engineer gate surfaced any
security-severity concern (no HTTP surface, no auth/tenant-crossing code path touched).

**WP-3B status: ACCEPTED.**

**Next:** Per the Owner's Device Execution Order, moving immediately into WP-4
(Authentication/JWT) device implementation using the already-reviewed specialist brief
(`/tmp/wp4_brief.md`), incorporating Security Reviewer's three required MEDIUM changes
and Technical Architect's six required documentation/completeness changes before/during
implementation.

---

## WP-4: Authentication / JWT / Platform-Admin Claim Foundation (2026-08-27)

**Specialist ownership:** Backend Engineer (implementation), reviewed at brief stage by
Security Reviewer (3 required MEDIUM changes) and Technical Architect (6 required
changes) before device implementation began; post-implementation gates run by Security
Reviewer, QA Automation Engineer, and Technical Architect.

**Implementation:** Full JWT (HS256) access-token issuance + rotating/revocable/
reuse-detected refresh tokens, PBKDF2-HMAC-SHA256 password hashing
(`Microsoft.AspNetCore.Identity.PasswordHasher<T>`, framework-free `IPasswordHasher`
wrapper), and all six `/api/v1/auth/*` endpoints (login, refresh, logout,
forgot-password, reset-password, me) per the approved brief. `AuthService` stays fully
framework-free (no ASP.NET Core/EF/`Microsoft.Extensions.Options` reference), matching
the codebase's existing `ICurrentTenant`/`TenantGuard` pattern — `JwtOptions`/
`PasswordResetOptions` are bound via the normal `AddOptions<T>().ValidateOnStart()`
pipeline in `Program.cs` and re-projected as plain objects for DI.

**Anonymous-lookup problem (brief §1/§7) resolved:** `IUserAuthLookupRepository` is the
one sanctioned `IgnoreQueryFilters()` cross-tenant `Users` read, scoped to exactly the
pre-authentication flows (login/refresh/forgot-password lookups + the 3 lockout/
password-hash writes); `/me` deliberately uses the normal tenant-filtered path instead
(a claims/tenant mismatch resolves to not-found, not a leaked profile).

**Platform-admin isolation:** No live `PlatformAdminAuthController`, no
`/api/v1/platform/*` route of any kind — verified per the Owner's explicit constraint by
`PlatformAdminRouteInventoryTests.cs` (endpoint-inventory reflection over
`EndpointDataSource`, a controller-type reflection scan, and live 404 probes against
plausible platform-admin paths). `GarageTenantRequirement`/`PlatformAdminRequirement`
authorization policies enforce mutual exclusion structurally (claim presence only) —
`TestJwtTokenFactory` mints test-only platform-admin tokens via the REAL production
`ITokenService`, never a hand-rolled parallel token builder, replacing the retired
`TestAuthHandler`.

**Password-reset anti-enumeration (brief §13):** the HTTP request path
(`AuthController.ForgotPassword`) does ZERO user-existence-dependent work by
construction — format-regex check only, then an unconditional enqueue onto a bounded
in-process `Channel` (capacity 1000, drop-oldest — Security Reviewer required change
#3), returning 202 regardless. All existence-dependent work (DB lookup, token
generation, `IEmailService.SendPasswordResetAsync`) happens in
`PasswordResetRequestBackgroundService`, off the request/response path. Test-only
`CapturingEmailService` replaces `NoOpEmailService` in `IntegrationTestFixture` so
forgot/reset-password tests can recover the generated reset link (never exposed over
HTTP).

**Rate limiting made configuration-driven mid-implementation (not in the original
brief):** the shared `IntegrationTestFixture` boots ONE `WebApplicationFactory` for the
entire `Integration` xunit collection, so every TestServer request reports the same
loopback IP — WP-4's own functional tests (login/refresh/forgot-password/
reset-password) would otherwise all compete for one production-sized (5/min etc.)
rate-limit budget and spuriously 429. Fixed by making all four policies'
permit-limit/window values configuration-driven (`RateLimiting:*`, production-safe
fallback defaults matching the brief exactly), with `appsettings.Testing.json` raising
them to 1000 for the shared fixture and a new isolated-host `RateLimitingTests.cs`
proving the tight production limit still genuinely enforces (429 + `Retry-After`) via
its own `WebApplicationFactory` instance. This surfaced and fixed two real bugs along
the way: (1) the rate-limit config was initially read eagerly into `var`s *before*
`builder.Build()`, silently missing any test-only `ConfigureAppConfiguration` override —
fixed by reading `builder.Configuration` lazily inside the `AddRateLimiter` callback,
the same pattern the (working) DB connection-string override already used; (2) the 429
rejection handler's `WriteAsJsonAsync` call was silently overwriting the
`application/problem+json` content-type back to `application/json` — fixed by passing
`contentType` explicitly to the correct overload.

**Migration:** `AddUserLockoutColumns` — EF's diff combined the intended two units
(users lockout columns; `password_reset_tokens` table) into one migration since both
were new uncommitted model changes at generation time (Technical Architect confirmed
this is architecturally fine, not worth splitting). Applied cleanly to both
`pit961_integration_test` and `pit961_dev`.

**Verification:** Full solution builds with 0 warnings/0 errors. **126/126 tests
passing** against real PostgreSQL 15.19 (no Docker/Testcontainers/PG14/SQLite/InMemory
substitute) — 121 `GarageOS.Tests.Integration` (75 pre-existing + 46 new/modified WP-4)
+ 5 `GarageOS.Tests.Unit`, zero regressions, confirmed via full combined suite runs (not
just partitioned batches).

**Security review (post-implementation):** Initial verdict **BLOCKED** — one HIGH
finding: `AuthService.RefreshAsync` read `existing.RevokedAt` in application code and
wrote the revoke in a LATER, separate `SaveChangesAsync` call, leaving a window where
two concurrent presentations of the same still-valid refresh token could both pass the
"not yet revoked" check before either write landed, both minting a live session with
reuse-detection never firing. **Fixed** by making the token "claim" atomic:
`IRefreshTokenRepository.TryClaimForRotationAsync` issues a single conditional SQL
`UPDATE ... WHERE RevokedAt IS NULL` via EF Core's `ExecuteUpdateAsync` (bypassing the
change tracker), and `AuthService.RefreshAsync` restructured to insert the replacement
token row first (FK ordering), then treat a failed claim — whether from prior revocation
or a lost race — identically as the reuse-detection signal (revoking the new row and
every active session for the user). Added
`Refresh_ConcurrentPresentationOfSameToken_ExactlyOneWinsAndReuseDetectionStillFires`
(fires two genuinely concurrent `/refresh` calls via `Task.WhenAll`, verified stable
across 5 repeated runs) as the regression proof. **KI-7 confirmed closed at the code
level** — JWT signature/issuer/audience/expiry validation, `garage_id` claim integrity,
and platform-admin/garage-tenant mutual exclusion all traced end-to-end by the Security
Reviewer, not accepted on test names alone. Two LOW findings logged as KI-11 (rate-limit
coverage asymmetry) and KI-14 (fixed `Retry-After` header value), non-blocking.

**QA review:** **PASS WITH FINDINGS** (0 BLOCKER/CRITICAL) — independently re-verified
by standing up its own real Postgres instance and re-running the full suite (126/126).
2 MEDIUM findings (KI-10: email lookup case-sensitivity undocumented; a password-length
boundary-value gap, closed immediately by adding
`ResetPassword_PasswordExactlyAt{Minimum,Maximum}Length_Succeeds`) + 3 LOW findings
(KI-11 through KI-13: rate-limit coverage asymmetry, missing malformed-body tests, email
whitespace handling) logged as tracked Known Issues.

**Technical Architect review:** **ACCEPT** — all 6 brief-stage required changes
confirmed present as real code (framework-free layering, `JwtOptions`/
`PasswordResetOptions` as plain re-projected objects, `IEmailService` governance
comment, public `GarageTenantRequirement`/`PlatformAdminRequirement`, documented
test-only signing key, multi-location forward-compatibility note on the `garage_id`
claim). Confirmed a clean foundation for WP-5 (its own `IAuthorizationRequirement`s can
compose alongside `GarageTenantRequirement` without modification here). One
non-blocking stale-comment note (`JwtOptions.cs` referenced the wrong test-config
filename) closed immediately.

**Security re-review (targeted remediation gate, formal, 2026-08-27):** Per PIT961
operating rules -- a CRITICAL/HIGH Security finding may only be cleared by the Security
Reviewer after remediation; a passing automated test run does not substitute for the
independent Security gate -- the Dispatcher's own regression-test confirmation of the
refresh-token race fix was **not** treated as closing the HIGH finding. A fresh snapshot
of the remediated device code (containing `TryClaimForRotationAsync`, the restructured
`AuthService.RefreshAsync`, and the new concurrent-reuse regression test) was staged into
the review environment and the Security Reviewer agent was formally re-invoked for a
targeted remediation review (not a full WP-4 restart), scoped exactly to the previously
raised HIGH. The Security Reviewer read the remediated files directly, traced the call
graph to rule out an alternate rotation path, walked through the two-concurrent-request
scenario against real Postgres READ COMMITTED semantics, confirmed both row-count
branches are handled correctly with no path to JWT issuance on a failed claim, confirmed
reuse-detection fan-out is preserved, and confirmed the new regression test genuinely
exercises the race against a real database. One new LOW/informational finding was
raised (an orphaned, unrevoked, but never client-disclosed replacement-token row is
possible if the process crashes between the insert and the claim call) and logged as
KI-15 -- non-blocking. The Security Reviewer's literal verdict, recorded verbatim:

> PASS — HIGH CLOSED

The HIGH finding is now formally CLOSED by the Security Reviewer's own re-review, not by
the Dispatcher's self-assessment.

**WP-4 status: ACCEPTED.** All three required specialist gates -- Security (targeted
re-review, PASS — HIGH CLOSED), QA (PASS WITH FINDINGS, 0 BLOCKER/CRITICAL), Technical
Architect (ACCEPT, no must-fix items) -- have formally signed off.

**Next:** WP-5 (Authorization Policies — 15% discount cap, $500 approval threshold)
builds directly on WP-4's `GarageTenantRequirement`/authorization-policy framework, per
the Owner's Device Execution Order.

## WP-5: Authorization Policies (discount 15% cap, $500 approval threshold)

**Brief:** Backend Engineer drafted the implementation brief (resource-based
`DiscountLimitRequirement`/Handler and `EstimateApprovalThresholdRequirement`/Handler,
policy registration, application-service enforcement points, tenant-boundary layering,
single-authoritative-mutation-path design, bypass-protection strategy, 22-item test
matrix). Technical Architect reviewed and required changes; final verdict **ACCEPT WITH
REQUIRED CHANGES**, incorporated before implementation began. Per the Owner's explicit
instruction, no generic rules engine was built -- two narrow, purpose-built requirement/
handler pairs only.

**Implementation:** Device-executed by the Dispatcher (specialists do not have device
access; the Device Execution Protocol was followed throughout -- specialists own design/
review, the Dispatcher performs the actual device-bound file edits). `DiscountLimitHandler`
enforces a 15.00% manager cap (owner unrestricted, all other roles denied outright).
`EstimateApprovalThresholdHandler` enforces a $500.00 threshold, role-blind past the
tenant check -- its `Succeeded == false` on `requires_owner_approval` is a deliberate
ROUTING signal, not a rejection, and the doc comments on both handlers explicitly warn
against ever attaching either policy via a bare `[Authorize(Policy = ...)]` attribute
(resource-based policies must go through `IAuthorizationService.AuthorizeAsync(user,
resource, policyName)` explicitly). `EstimateMutationRepository` was established as the
sole authoritative writer of `Estimate.DiscountAmount/Total/Status`, using the same
fresh-`AsNoTracking`-read-plus-fresh-refetch-write pattern WP-3B's
`AccountProvisioningService` established, closing the "ambient tracked-instance flush"
bypass vector by construction. Zero new HTTP endpoints, per the Owner's explicit "no fake
Phase 3 endpoints" instruction -- tests stay at handler/application-service/
authorization-infrastructure level.

**Bypass-protection hardening (four rounds, this session):** The Owner's standing
instruction -- "Do not merely grep once and call this satisfied. Create repeatable
automated protection where appropriate" -- was taken seriously and, in retrospect,
justified: three separate reviewers, working independently in sequence, each found a
real, execution-confirmed bypass of the previous round's fix.

- **Round 1 (QA Automation Engineer)**, adversarially writing and running PoC bypass
  code rather than just reading the regexes, found and the Dispatcher fixed 3 real
  bypasses: EF Core's non-generic `DbContext.Update(entity)`/`Attach(entity)` overloads
  evading a DbSet-rooted regex; a long `.Where()`-chain LINQ query pushing
  `ExecuteUpdateAsync(` past a fixed 200-character window; a named-constant
  policy-name reference evading a literal-string block-list on
  `AuthorizationAttributeMisuseTests`. Two lesser test-quality findings (an ordering
  test that didn't actually verify ordering; a rounding test that never exercised a
  real rounding decision) were fixed in the same pass.
- **Round 2 (QA Automation Engineer)**, asked explicitly to try to evade its own
  round-1 fixes, found and the Dispatcher fixed 2 more: a `;` character embedded
  inside an `Estimates`-query `.Where()` string-literal value defeating naive
  `text.Split(';')` statement-scoping; C# attribute-stacking
  (`[HttpPost(...), Authorize(Policy = "...")]`) defeating a "line must start with
  `[Authorize`" real-usage heuristic. Both closed by introducing a shared
  `SourceScanUtilities.MaskLiteralsAndComments` helper that blanks string/char-literal
  and comment CONTENT (preserving length/line-breaks) before any pattern-matching or
  statement-splitting runs.
- **Round 3 (QA Lead's own independent gate review)** went further than either
  requested round and found a bypass of the round-2 fix itself: an interpolated
  string's `{ }` hole contains live, executing code, but the first version of
  `MaskLiteralsAndComments` blanked hole content exactly like literal text --
  `$"{db.Estimates.Update(e)}"` was fully invisible to every downstream check.
  QA Lead's gate verdict was **BLOCKED** on this CRITICAL finding, plus a
  reclassification of the earlier-accepted KI-16 (`db`/`_db` naming-convention
  anchor) from an acceptable tracked residual to MAJOR, since grep confirmed zero
  legitimate `.Update(`/`.Attach(` call sites exist anywhere in the solution. Both
  fixed (hole-aware masking that leaves live code inside `{ }` unmasked while still
  masking nested literals within holes; the Update/Attach patterns widened to be
  fully unanchored). QA Lead re-verified independently against a refreshed snapshot
  (initially, correctly, refusing to sign off from a description alone when the first
  snapshot handoff was stale) and returned **PASS**.
- **Round 4 (Security Reviewer's own independent gate review)**, explicitly asked to
  give the four-times-iterated mechanism real scrutiny rather than assume soundness,
  found a bypass of the round-3 fix: the nested-string branch inside a hole
  unconditionally assumed every nested literal was plain, ignoring its actual `@`/`$`
  prefix -- a nested interpolated string's own hole was wrongly blanked (hiding a
  real call), and a nested verbatim string's backslash was wrongly treated as a
  regular-string escape, running the mask past the literal's real terminator and
  corrupting everything scanned after it. Security Reviewer's gate verdict was
  **BLOCKED** on this HIGH finding. Fixed by detecting the same prefix shapes inside a
  hole that the top-level dispatcher already detects. Security Reviewer re-verified
  independently -- again refusing an unrefreshed-snapshot sign-off first -- with two
  novel payloads beyond the ones it originally reported (mixed verbatim/interpolated
  prefix ordering; three levels of nesting depth) and returned **PASS**, noting no
  further bypass found and that the fix is not depth-limited.

Both KI-16 and KI-17 are recorded CLOSED in `KNOWN_ISSUES.md` with the full history.
No production code was ever affected by any of the four rounds -- every finding was in
`GarageOS.Tests.Unit/Architecture/`, test-only source-scanning infrastructure -- but each
was treated with the same rigor as a production-code finding, consistent with this
session's established practice (and the Owner's explicit standing instruction) that a
passing automated check is not itself proof of soundness against an adversarial reviewer
who actually tries to break it.

**QA Lead gate:** **PASS** (see round 3 above for the interpolation-hole finding this
gate itself surfaced and required fixed before passing). Independently verified the full
22-item mandatory test matrix directly in the test files (not just by name), confirmed no
fake Phase 3 endpoints exist (grep: zero controllers reference the new services), and
independently re-ran the full build/PoC/test cycle itself rather than trusting the
Dispatcher's report.

**Security Reviewer gate:** **PASS** (see round 4 above for the nested-literal-in-hole
finding this gate itself surfaced and required fixed before passing). Independently
verified every item on the Owner's checklist: no client-only enforcement (zero HTTP
surface exists yet to bypass); no role-check shortcuts around either handler; no
alternate write path to the guarded Estimate columns (verified by direct whole-solution
grep, not by trusting the architecture tests' own self-description); tenant mismatch
fails closed in both handlers (read the actual code, not just the tests); a
platform-admin token structurally cannot satisfy either policy and vice versa (verified
via `TokenService.IssuePlatformAdminAccessToken`'s claim shape, not just "the policies
are separate"); Accountant/Mechanic correctly excluded from discount authority and
correctly still role-blind-subject to the $500 threshold; extensibility confirmed --
a future policy needs no change to either existing handler. One non-blocking observation
logged (not a finding): `EstimateApprovalService.RouteStatusAsync` writes a
caller-supplied status string with only the DB CHECK constraint as a backstop today --
flagged for whoever wires the future HTTP endpoint to add application-layer validation
at that point, not urgent now since no endpoint exists yet to supply an untrusted value.

**WP-5 status: ACCEPTED.** Both required specialist gates -- QA Lead (PASS, after one
CRITICAL finding fixed and re-verified) and Security Reviewer (PASS, after one HIGH
finding fixed and re-verified) -- have formally signed off, each via genuine independent
re-verification against a freshly refreshed code snapshot, not by trusting a description
of the fix.

**Next:** Commit WP-5 to git; update tracking docs (done, this entry); WP-6 (Email /
Resend) and WP-7 (Branding Configuration) are both unblocked and ready to start.

---

## WP-7: Branding / Configuration Layer (2026-08-28)

**Owner order:** "OWNER / CONTROL ROOM ORDER — CONTINUE PHASE 1" formally accepted
WP-4 and WP-5, then directed WP-7 before WP-6 (WP-6 consumes
`Branding:EmailFromName`, and WP-8 later consumes the same branding config, so the
branding layer needed to exist first). Specialist owner: Technical Architect (brief),
required collaborator: Backend Engineer, device executor: Company Dispatcher (the
Device Execution Protocol was followed throughout -- specialists have no device
access; the Dispatcher performed every device-bound file edit and test run directly).

**Brief:** Technical Architect confirmed/produced the WP-7 brief against the current
repo (directory-name conventions, `Contracts/` single-file-per-feature-area pattern,
anonymous-endpoint convention, test-project split), verdict **ACCEPT**.

**Implementation.** `BrandingOptions` (ProductDisplayName, EmailFromName, LogoUrl,
SupportEmail) added as a plain `IOptions<T>`-bound configuration class, no
`.Validate(...)` clause (matching `DemoOptions`'s precedent -- none of the four fields
is secret/startup-fatal), registered in `Program.cs` completely independently of the
JWT bearer pipeline. `ConfigController` (`[AllowAnonymous] GET /api/config/branding`)
exposes the four fields through a hand-mapped closed DTO (`BrandingConfigResponse`) --
never a direct serialization of `BrandingOptions` itself, and its sole constructor
dependency is `IOptions<BrandingOptions>`, reflection-proven by
`ConfigControllerDependencySurfaceTests` to be structurally incapable of reaching
`IConfiguration`, `JwtOptions`, or any connection string. `Branding` sections added to
all three appsettings files with placeholder values (e.g. `"Garage Management
Platform"` -- the final customer-facing brand remains undecided per DECISIONS.md #6).
No new migration -- pure configuration/options layer, no schema change.

**Strict JWT/Branding separation -- the Owner's highest-priority requirement --
proven at runtime, not just asserted.** `BrandingJwtDecouplingTests` boots two real
hosts from the exact same compiled `Program` with identical `Jwt:*` configuration but
different `Branding:ProductDisplayName` values, logs in via the real
`/api/v1/auth/login` path on both, decodes both issued JWTs, and asserts the `iss`/
`aud` claims are byte-identical across hosts and still equal the fixed expected
`Jwt:Issuer`/`Jwt:Audience` test values -- not merely "equal to each other by
coincidence," which a shared-broken-default bug could still satisfy.
`BrandingOptionsBindingTests` adds the binding-layer half of the same proof in both
directions (Branding-only config still binds `JwtOptions` to defaults, and
Jwt-only config still binds `BrandingOptions` to defaults).

**Placeholder-brand ("Rashid") CI regression check.** `scripts/ci/check-no-legacy-
brand.sh` implements the Owner-approved acceptance criterion from
`13_phase1_execution_plan.md` (an unrestricted `grep -rlni "rashid"` over
backend/frontend, excluding only node_modules/bin/obj/dist/.git/.review-snapshots),
wired into `.github/workflows/ci.yml` as an unconditional blocking step on every
push/PR. One genuine pre-existing hit was found and fixed along the way: a doc
comment in `GarageInsertBoundaryTests.cs` (predating WP-7) discussed the future
"Rashid"-placeholder check by name in prose -- a legitimate meta-reference, not a
real violation, but it tripped the literal grep -- reworded to avoid the word
entirely rather than special-casing the check itself. The required negative-test
proof (introduce a violation, confirm the check fails, remove it, confirm it passes
again) was run twice this session: once against an initial version of the script that
restricted matching to a fixed list of source-file extensions, and again -- after QA
Lead's gate review correctly rejected that restriction as an unauthorized narrowing of
the approved plan -- against the corrected, fully unrestricted version, across five
file types (.cs/.txt/.yaml/.svg/.resx), each time confirming fail-and-list-every-file
then remove-and-pass-cleanly.

**19 new tests, all passing, zero regressions:** 7 `GarageOS.Tests.Unit`
(`BrandingOptionsBindingTests` x4, `Architecture/ConfigControllerDependencySurfaceTests`
x3) + 12 `GarageOS.Tests.Integration` (`BrandingConfigEndpointTests` x10 [2 facts + an
8-case theory proving no secret field name ever appears in the response],
`BrandingConfigPropagationTests` x1 [dual-boot proof that ProductDisplayName/
EmailFromName propagate from configuration alone, same compiled binary, no recompile],
`BrandingJwtDecouplingTests` x1) -- **170/170 total** (151 prior + 19 new), run against
real PostgreSQL 15.19, no Docker/Testcontainers/InMemory substitute.

**QA Lead gate: PASS (two rounds).** Round 1 found two genuine CRITICAL findings via
independent execution, not code reading alone: (1) the "Rashid" check's file-extension
allow-list was an unauthorized narrowing of the Owner-approved unrestricted command --
QA Lead built a throwaway fixture and proved by running the actual script that .txt/
.yaml/.svg/.resx violations were silently missed; (2) the CI step's comment cited a
PROGRESS.md negative-test record that did not exist at the time. Both fixed (extension
allow-list removed entirely; the comment rewritten to be self-contained and to
correctly state that tracking-doc updates are deferred until both gates pass, per
standing project policy) and independently re-verified in round 2 by QA Lead executing
the corrected script itself against a refreshed snapshot -- not by trusting the fix
description. One non-blocking MINOR noted and fixed opportunistically (the script's
own comment didn't disclose the `--exclude-dir=dist` addition alongside `.git`/
`.review-snapshots`).

**Security Reviewer gate: PASS.** Independently re-verified rather than rubber-
stamping QA's sign-off: re-read `BrandingJwtDecouplingTests`' logic line-by-line and
confirmed it proves genuine equality-to-a-fixed-expected-value, not just
equality-between-hosts; independently re-executed the negative-test proof against
synthetic fixtures across all five previously-gapped extensions plus a case-
sensitivity and a `node_modules`-exclusion check; confirmed `[AllowAnonymous]` scope
is narrow (only two controllers exist, no shared base class bleed risk); confirmed no
SMS/WhatsApp/Twilio code exists anywhere in the backend; confirmed `IEmailService`/
`NoOpEmailService` are untouched by branding. One LOW/advisory note carried forward to
WP-8, not blocking WP-7: validate `LogoUrl`/`SupportEmail` scheme/encoding before ever
rendering them client-side (e.g. block `javascript:`/`data:` schemes, encode a
`mailto:` link) -- no rendering surface exists yet to exploit, since `frontend/` is
still empty pending WP-8.

**WP-7 status: ACCEPTED.** Both required specialist gates -- QA Lead (PASS, after two
CRITICAL findings fixed and re-verified) and Security Reviewer (PASS, independently
re-executed rather than trusting QA's sign-off) -- have formally signed off.

**Next:** Commit WP-7 to git; update tracking docs (done, this entry); proceed
immediately into WP-6 (Email / Resend) per the Owner's standing instruction, no
further Owner permission required.

---

## WP-6: Email / Resend Integration (2026-08-28)

**Owner order:** proceed immediately into WP-6 after WP-7's acceptance, no further
Owner permission required. First, a small process/label reconciliation: `13_phase1_execution_plan.md` and `IMPLEMENTATION_MAP.md` named "Backend Engineer"
alone as WP-6's responsible specialist, predating the roster's dedicated Integration
Engineer specialization for third-party service integrations -- corrected to
Integration Engineer (owns the `ResendEmailService`/Resend SDK boundary) + Backend
Engineer (collaborates on `AuthService`/password-reset wiring), scope unchanged,
committed separately (`0227e44`) before WP-6 implementation began. Device executor:
Company Dispatcher, per the Device Execution Protocol.

**Brief:** Integration Engineer produced the WP-6 implementation brief against the
current repo, resolving several concrete design questions the Owner's order left to
specialist judgment: (1) whether to add `IEmailService.SendTransactionalAsync` now
with no current caller -- decided YES, citing `11_engineering_handoff.md` §11A and
`DECISIONS.md` as already specifying this exact two-method shape as approved Phase 1
infrastructure, not scope creep (independently re-confirmed by both QA Lead and
Security Reviewer, who each read §11A themselves rather than trusting the citation);
(2) a hand-rolled typed-HttpClient POST against Resend's REST API instead of a
third-party Resend SDK NuGet package, since none exists in the repo's dependency
graph and a hand-rolled call makes the "Resend SDK usage cannot leak outside
ResendEmailService" acceptance criterion mechanically true (no SDK type exists to
leak); (3) `ResendOptions.ApiKey` secrets-handling modeled on `JwtOptions`' existing
pattern (`.Validate()` + `.ValidateOnStart()`, fixed obviously-fake test-only value in
appsettings.Testing.json) rather than an environment-conditional validation bypass,
chosen by the Dispatcher during implementation as the simpler, more established-
precedent option once grounded against the real `JwtOptions.cs`/`Program.cs` code.

**Implementation.** `IEmailService` grew `SendTransactionalAsync` alongside the
existing `SendPasswordResetAsync`. `ResendEmailService` (new, `GarageOS.Infrastructure/
Email/`) is the ONLY class in the codebase permitted to reference the Resend API
(Decision #8) -- it builds a `ResendSendEmailRequest` JSON payload, sets the
`Authorization: Bearer` header per-request (not on the shared `HttpClient`, see the
Security Reviewer finding below), POSTs to `https://api.resend.com/emails` via a typed
`HttpClient` (`AddHttpClient<IEmailService, ResendEmailService>`, replacing the WP-4
`NoOpEmailService` registration -- that class is kept, unregistered, as a documented
fallback/swap-proof, and also gained `SendTransactionalAsync`), and propagates
failures rather than swallowing them (the sole Phase 1 caller,
`PasswordResetRequestBackgroundService`, already catches and logs per-item exceptions
as a deliberate at-most-once tradeoff -- swallowing in both places would make failures
silently invisible). `AuthService.ProcessForgotPasswordRequestAsync` is unchanged
except for which `IEmailService` implementation DI resolves. `ResendOptions`
(ApiKey, FromAddress) added, mirroring `JwtOptions`' secrets-handling convention
exactly. `CapturingEmailService` (integration test double) updated for the grown
interface; `ForgotPasswordTests` gained one new assertion proving the password-reset
template (not the generic transactional one) is what actually gets used.

**Resend-SDK-isolation CI regression check.** `scripts/ci/check-no-resend-outside-
service.sh` mirrors WP-7's `check-no-legacy-brand.sh` pattern: an unrestricted grep
(by file extension) for the literal Resend API host across backend/frontend,
allow-listing only `ResendEmailService.cs`, wired into `.github/workflows/ci.yml` as a
blocking step. The required negative-test proof (introduce a violation, confirm the
check fails, remove it, confirm it passes again) was run by the Dispatcher before the
gates, then independently re-run from scratch by both QA Lead and Security Reviewer
during their own gate reviews -- neither trusted the workflow comment's claim alone.

**10 new tests, all passing, zero regressions:** 6 `Email/ResendEmailServiceTests`
(correct recipient/subject/body; `BrandingOptions.EmailFromName` sourcing; `SendTransactionalAsync` uses the caller's own subject/body, not the password-reset
template; failure propagation as `HttpRequestException`, not swallowed; two
log-hygiene tests injecting a real-looking secret and a real reset-link/token and
asserting neither ever appears in any captured log line, on both the success and
failure paths) + 4 `ResendOptionsBindingTests` (binding correctness, no embedded
default key, bidirectional isolation from Jwt:*/Branding:*) -- **180/180 total**
(170 prior + 10 new), run against real PostgreSQL 15.19, no Docker/Testcontainers/
InMemory substitute.

**QA Lead gate: PASS.** Did not just read the code: built and ran both test suites
itself (47/47 unit, 133/133 integration), independently reproduced the CI check's
pass-fail-pass negative-test cycle in a scratch copy, confirmed
`ProcessForgotPasswordRequestAsync`'s anti-enumeration logic is byte-for-byte
unchanged, confirmed `ConfigController` (WP-7) remains structurally incapable of
reaching `ResendOptions`, confirmed KI-10 (email case-sensitivity) untouched and no
new migration was introduced, and independently formed its own judgment on the
`SendTransactionalAsync` scope question by reading §11A itself. One MINOR
documentation-parity note (the isolation script's header comment didn't yet disclose
the same split-literal-detection limitation `check-no-legacy-brand.sh` already
documents) fixed opportunistically before Security review.

**Security Reviewer gate: PASS.** Traced `ResendEmailService`'s actual HTTP call and
logging code line-by-line rather than trusting doc comments or QA's sign-off;
independently re-ran the CI check's negative-test proof a third time; confirmed no
SSRF/injection risk (recipient/subject/body only ever populate the JSON payload, never
the URL), a hardcoded HTTPS base address with no config-driven downgrade path, an
explicit 10-second request timeout so a hung call can't stall the background consumer,
and that `EnsureSuccessStatusCode()`'s default exception message can't leak a Resend
response body since the response content is never read. One LOW/informational finding:
the `Authorization` header was being set on the injected `HttpClient`'s shared
`DefaultRequestHeaders` rather than per-`HttpRequestMessage` -- safe under every call
pattern that exists today (a fresh typed client per resolution, one email per
scoped background-queue item) but a latent race if a future caller ever held a
long-lived/singleton reference to `IEmailService`. Fixed opportunistically (the
request is now built explicitly with its own `Authorization` header) and re-verified
-- 180/180 still passing, both CI checks still passing.

**WP-6 status: ACCEPTED.** Both required specialist gates -- QA Lead (PASS) and
Security Reviewer (PASS, one LOW finding fixed opportunistically) -- have formally
signed off.

**Next:** Commit WP-6 to git; update tracking docs (done, this entry). With WP-6 and
WP-7 both accepted, report to the Owner per the standing ~28-section report
requirement.

---

## WP-8: Frontend Scaffold (React/TS/Vite, real WP-4 auth integration) (2026-09-02)

**Owner order:** "OWNER / CONTROL ROOM ORDER — START WP-8 FRONTEND." React 18 +
TypeScript + Vite + Tailwind + shadcn + Vitest + RTL + Playwright, no Docker
anywhere, `prototype.html` as the visual source of truth (any unimplementable
element logged to `DESIGN_IMPLEMENTATION_DIFFERENCES.md`), real
`POST /api/v1/auth/login` (no fake auth endpoint), access token in-memory
only / refresh token in an httpOnly cookie, real `GET /api/config/branding`
consumption, 10 named Vitest+RTL scenarios + 4 named Playwright scenarios
(the final login test against the real backend and the real seeded
development account only, no fake backend behavior), CI extension is WP-8's
own responsibility, specialist gate order Frontend Engineer -> Design
Lead/UI-UX -> QA Automation -> QA Lead -> Security Reviewer -> Technical
Architect where needed. Specialist owner: Frontend Engineer, device
executor: Company Dispatcher (Device Execution Protocol followed throughout).

**Implementation.** Full scaffold under `frontend/{app,components,features,
hooks,layouts,lib,pages,services,stores,types,validation}/`. Login screen
freshly designed (no prototype precedent -- see
`DESIGN_IMPLEMENTATION_DIFFERENCES.md` item 5) wired to the real WP-4 login
endpoint via a hand-rolled `apiClient` (`services/apiClient.ts`) that injects
the in-memory bearer token, retries exactly once through
`POST /api/v1/auth/refresh` on a 401, and normalizes `ProblemDetails` error
bodies into a typed `ApiError`. Access token lives only in a Zustand store
(`stores/authStore.ts`), never in `localStorage`/`sessionStorage`; the
refresh token is an httpOnly cookie the frontend never reads, only relies on
via `credentials:'include'` -- protected-route hiding is a UX convenience
only, the backend remains the sole authority. `BrandMark`
(`components/brand-mark.tsx`) consumes `GET /api/config/branding` through a
`brandingStore`, derives its glyph from the runtime `productDisplayName` (never
a hardcoded letter/name), and gates `LogoUrl` through `safeHttpUrlOrNull()`
(rejects `javascript:`/`data:`/protocol-relative and any non-http(s) scheme)
before ever using it as an `<img src>`. `AppShellLayout` implements the
76px nav rail + 52px header authenticated shell per `prototype.html`,
including its per-item 7.5px mono-caps captions (`label: name.toUpperCase()`
in the prototype's own rail data) and a truncated
`productDisplayName`-under-brand-mark label with a native `title` tooltip
for the full name on hover (new element, no prototype precedent -- see
`DESIGN_IMPLEMENTATION_DIFFERENCES.md` item 8).

**Real-device-screenshot-caught bug, not just code review.** Visual
verification against the actual running dev server (Playwright-driven
Chromium screenshots, not just reading the diff) caught a genuine
branding-robustness bug: a `LogoUrl` that passes `safeHttpUrlOrNull()` (i.e.
is a syntactically safe `https://` URL) can still 404/DNS-fail at runtime,
which left a broken-image icon with overflowing alt text on screen. Fixed
with an `onError`-driven `logoFailed` state in `BrandMark` that falls back to
the glyph mark, re-armed on every `logoUrl` change; a new test case
(`falls back to the initial when a syntactically-safe logoUrl fails to
load`) locks this in.

**Process error and correction: specialist-review-snapshot staleness.**
Design Lead, Security Reviewer, and QA Lead's first review round ran
against `/root/pit961-frontend/frontend/` -- a copy of the frontend taken
immediately after the initial cloud-sandbox-to-device transfer, before the
brand-mark fix, the Vitest pool-hang fix, and the Playwright
headless-shell-stall fix were made directly on-device. This produced
false-negative findings (issues that were already fixed being reported as
still-present). Caught and corrected mid-review: re-packaged the current
device state into a fresh, verified-matching snapshot, re-synced refreshed
`DESIGN_IMPLEMENTATION_DIFFERENCES.md` content and new screenshots, and
re-dispatched all three specialists with an explicit acknowledgment of the
mix-up and pointers to the corrected artifacts. Logged here in full rather
than omitted, per this project's standing practice of transparent process
reporting (see the WP-5 JWT-flake and WP-3 CORS/seed-password entries for
precedent).

**Security Reviewer's WP-8 gate found a real, previously-missed gap.** An
earlier `npm audit fix --force` pass had NOT actually resolved a CRITICAL
(vitest RCE, GHSA-5xrq-8626-4rwp -- every vitest 2.x release is vulnerable,
no patched 2.x exists) or a HIGH (vite `server.fs.deny` bypass,
GHSA-fx2h-pf6j-xcff -- vite <=6.4.2 is vulnerable, no patched 5.x exists):
the abbreviated `npm audit` summary looked clean because the fix pass only
bumped both packages within their existing vulnerable major version, never
crossing into a patched major. Fixed by a deliberate major-version bump
(vite `^5.4.21`->`^7.3.6`, vitest `^2.1.9`->`^4.1.11`, react-router-dom
`^6.30.6`->`^7.18.3` pulled in by peer requirements, `@vitejs/plugin-react`
`4.3.3`->`^5.2.0` -- the only version whose peer range covers both vite 7
and vite 8 -- `@types/node`->`^22.15.0`). Vitest 4 removed
`test.poolOptions.forks.singleFork` (the single-worker pin this device's
constrained 2-core environment needs to avoid a `threads`-pool hang); the
equivalent is `test.fileParallelism: false`, applied in `vite.config.ts`.
No application code changes were needed for React Router v7 -- only
`Routes`/`Route`/`Navigate`/`Outlet`/`useLocation` are used, all stable
v6->v7. Re-verified clean on the real device after the bump: `npm run
build` (typecheck + Vite build), 79/79 Vitest+RTL, 4/4 Playwright e2e
against the real backend, and a from-scratch `npm install && npm audit`
(0 vulnerabilities).

**QA Lead's CI-wiring re-verification caught a second, more serious process
error.** Round 2 of QA Lead's review could not confirm
`scripts/ci/check-no-legacy-brand.sh` (the WP-7 placeholder-brand CI check)
actually covers `frontend/`, because the copy handed over
(`/root/pit961-review/`) had no `frontend/` directory and no unified git
history. The follow-up attempt to close this by handing over hand-picked
individual files (`ci.yml`, the script) made it *worse*, not better: QA Lead
diffed the handed-over `ci.yml` against a differently-stale copy actually
sitting at the claimed source path and found they disagreed, and found at
least three other mutually inconsistent partial PIT961 copies scattered
across the cloud review environment (an unextracted tarball, a stale
pre-bump snapshot, a doc-extraction artifact with no `frontend/` at all).
QA Lead correctly refused to sign off on hand-picked files it could not
verify came from one real, internally consistent checkout, and flagged this
as an integrity concern, not merely an unconfirmed finding. Fixed properly
this time: packaged the *entire* current git-tracked device repository (not
a file selection) into one tarball, extracted it to a single new location
(`/root/pit961-unified/`), verified `git status --short` is completely
clean against `HEAD` there, and deleted every other stale/partial PIT961
copy in the cloud environment so no inconsistent alternate could be
cross-referenced by mistake again. QA Lead independently re-verified from
scratch against this one location -- including actually executing both CI
guard scripts rather than only reading them -- and passed.

**CI extension (WP-8's own responsibility).** `build-and-test-frontend`
(`npm ci` -> `npm run build` -> `npm test`) and `e2e-frontend`
(`npm ci` -> `npx playwright install --with-deps chromium` ->
`npx playwright test`, `needs: build-and-test-frontend`) added to
`.github/workflows/ci.yml` as siblings of the existing backend
`build-and-test` job. `e2e-frontend` currently has an explicit TODO for
`devops-engineer`/WP-9: starting the real backend + dev seed + Vite server
before the Playwright step is not yet wired into that job (WP-8's own
device verification confirms the suite passes once both processes are up;
what's missing is only the CI orchestration of starting them).

**Design conformance.** `DESIGN_IMPLEMENTATION_DIFFERENCES.md` gained two
new entries: item 5 (freshly-designed login screen, no prototype precedent)
and item 8 (the `productDisplayName` rail label, also no prototype
precedent, truncated with a native `title` hover tooltip for the full
name). Item 7's "76px icon-only sidebar rail" wording was corrected to
"76px icon + short mono-caps micro-caption sidebar rail" after Design
Lead's review round flagged it as inconsistent with the implementation --
verified by reading `prototype.html`'s own rail-item JS data directly
(`label: name.toUpperCase()`), confirming the prototype rail always carried
per-item text captions; the doc's original wording was simply inaccurate,
not the implementation. A hard-clipped-label finding (no visible ellipsis
on a long tenant name) was investigated via `getComputedStyle` inspection
(confirmed `text-overflow:ellipsis` correctly applied, `scrollWidth` 182px
> `clientWidth` 68px, proving active truncation) plus a 4x-device-scale-
factor zoomed screenshot that clearly shows a real rendered ellipsis glyph
-- concluded to be a normal-resolution screenshot rendering artifact in the
original review capture, not a real truncation-treatment bug.

**89 new tests, all passing, zero backend regressions:** 79 frontend
Vitest+RTL (`App.test.tsx`, `routing.test.tsx`, `LoginForm.test.tsx`,
`brand-mark.test.tsx`, `support-email-link.test.tsx`, `safe-url.test.ts`,
`apiClient.test.ts`) + 4 Playwright e2e (`login.spec.ts`, real backend/real
seeded account/real branding endpoint, no mock server) + 1 backend
integration regression guard (`FlipSignatureBit_AlwaysProducesADifferentDecodedByteSequence`,
landed alongside WP-8 this session closing the intermittent JWT-signature-
tamper test flake -- see `KNOWN_ISSUES.md`, not itself a WP-8 deliverable
but verified in the same device session). Backend: still **181/181**, zero
regressions from any WP-8 work (WP-8 touches only `frontend/`,
`.github/workflows/ci.yml`, and `DESIGN_IMPLEMENTATION_DIFFERENCES.md`).

**Design Lead gate: PASS (two rounds).** Round 1 caught the broken-image
logo bug via real screenshot review (fixed). Round 2 raised the
hard-clipped-ellipsis and item-7-wording findings above (both resolved;
PASS) and, independently while reviewing the resolution, correctly flagged
that the visible rail label is a distinct element from the 8 fixed nav-item
captions with no prototype precedent of its own -- leading to new item 8.

**QA Lead gate: PASS (two rounds plus a re-verification).** Round 1 passed
after the specialist-snapshot-staleness correction above. Round 2 raised
the CI-wiring MAJOR finding and, on the first attempted close-out, correctly
rejected an inconsistent hand-picked-files answer as described above;
passed on re-verification against the single unified checkout.

**Security Reviewer gate: PASS (two rounds).** Round 1 found the vitest
CRITICAL / vite HIGH dependency findings described above (fixed). Round 2
independently reinstalled from a clean `node_modules`-free state and
re-ran `npm audit` itself rather than trusting the fix description or any
prior audit output, confirming 0 CRITICAL/HIGH findings and no new
unscoped dependencies introduced by the bump.

**WP-8 status: ACCEPTED.** All three required specialist gates -- Design
Lead (PASS), QA Lead (PASS), and Security Reviewer (PASS) -- have formally
signed off, each after finding and requiring a fix for at least one genuine
issue rather than rubber-stamping.

**Next:** Commit WP-8 tracking-doc updates (this entry); report to the
Owner per the standing ~35-section WP-8 report requirement.

---

## WP-9 — CI/CD Pipeline: real GitHub Actions execution (2026-09-02)

Closed the WP-9 primary gap: `e2e-frontend` previously only installed Playwright and ran
it against nothing (an explicit TODO marked this as open). It now orchestrates a real
PostgreSQL 15 GitHub Actions service container (`pit961_e2e`), both DbContext migrations,
the real ASP.NET Core backend (`dotnet run`, `Development` environment so
`DevelopmentSeeder` fires), and the real Vite dev server -- each gated on genuine
HTTP health-check polling (backend's own `/health`, a root probe for Vite) with a
fail-fast dead-process check, not a fixed sleep -- then runs the real, unmocked WP-8
Playwright suite. Commits `569408f`, `d050ec0` (DevOps-flagged fail-fast fix),
`a9833fd`/KI-19 (Security-flagged Playwright trace-capture fix).

All four WP-9 specialist gates (DevOps Engineer, Technical Architect, QA Lead, Security
Reviewer) passed against the real committed code, after a process error was caught and
fixed mid-review: the cloud-side unified checkout used for specialist review had gone
stale (pre-dated the commit), so three specialists independently and correctly refused
to sign off on a commit they couldn't verify existed. Re-synced from the real device repo
and all four re-reviewed cleanly. Technical Architect's one initial required fix
(decouple the dev-seed from `ASPNETCORE_ENVIRONMENT=Development`) was withdrawn after
evidence that `Testing` has no CORS config at all (would break the real-browser login
flow) and that "seed never fires under Testing" is an existing WP-3-approved contract,
not something WP-9 should reopen.

**Real GitHub remote created** (`https://github.com/Ruffmaalouf/Pit961`, Owner-provided
PAT, per the Owner's explicit WP-9 order instruction not to invent one). Real CI proof,
not local simulation:
- **Happy path:** first run failed exactly as expected (backend correctly failed closed
  on a not-yet-provisioned `CI_E2E_JWT_SIGNING_KEY` secret; the new fail-fast check
  caught it in ~2s instead of the full 60s timeout). Re-ran clean after setting the
  secret: all three jobs green, e2e backend healthy in 5s, frontend in 2s, Playwright
  4/4 in 3.9s. Run: https://github.com/Ruffmaalouf/Pit961/actions/runs/33629806752
- **Negative-gate proof**, three temporary branches/PRs, none merged to `main`: (1) a
  deliberate failing unit test failed CI at the "Test" step; (2) a deliberate "Rashid"
  reintroduction failed CI at the brand-name grep step; (3) a deliberate Resend-API
  reference outside `ResendEmailService.cs` failed CI at the Resend-isolation grep step.
  All three confirmed to fail at exactly the intended step, then PRs closed and branches
  deleted (local and remote) -- `main` confirmed clean afterward.
- **3-consecutive-clean-run stability**: in progress, tracked in this same push.

