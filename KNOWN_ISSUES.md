# KNOWN_ISSUES.md

Open issues tracked during Phase 1 implementation. Each entry: severity,
description, affected WP(s), status.

---

### KI-1 — PostgreSQL 15+ not yet provisioned in the repo-work environment

**Severity:** Blocking for WP-2's integration-test-database acceptance
criterion (not blocking WP-2's solution-scaffold/unit-test work).
**Affects:** WP-2, WP-3, WP-3B, WP-4, WP-5, WP-9 (anything needing
`GarageOS.Tests.Integration` to actually run against real PostgreSQL).
**Description:** The plan (WP-2) requires PostgreSQL **15+** installed
locally or otherwise reachable, with a dedicated PIT961 integration-test
database, for `GarageOS.Tests.Integration` to run. The device-bridge shell
used for repository work is not root and has no `sudo`, so system packages
cannot be installed there; Ubuntu's default apt archive in this environment
only offers PostgreSQL 14 (not 15+). CI is unaffected — GitHub Actions'
native `services:` PostgreSQL provisioning is independent of this local
constraint and will use a pinned 15+ image as planned.
**Status: RESOLVED (2026-08-27), per Owner direction (option A — user-local,
no downgrade/Docker/Testcontainers/InMemory).** PostgreSQL 15.19 was installed
**user-locally, without root**, using the official PostgreSQL apt.postgresql.org
`.deb` packages (`postgresql-15`, `postgresql-client-15`, plus the standard
Ubuntu `libpq5`), extracted with `dpkg-deb -x` (no system install, no `apt-get
install`) into a session-local prefix, `initdb`'d, and run via `pg_ctl` on a
non-default port against a session-local data directory. This is real
PostgreSQL 15.19, not a downgrade, not Docker/Testcontainers, not SQLite/EF
InMemory. Nothing PostgreSQL-related was added to the repository or to
`.gitignore` beyond normal application/test configuration — the extracted
binaries live outside the repo entirely, exactly as the Owner directed ("the
repository should only contain normal application/test configuration and
documentation — not bundled PostgreSQL binaries").
`GarageOS.Tests.Integration`'s full suite (2/2 at the time) was run and passed
against this real PostgreSQL 15.19 instance; the unreachable-DB fail-loud path
was also verified separately. This resolved the local/verification side of
KI-1 as of WP-2.

**CI side update (2026-08-27, WP-3):** a CI skeleton now exists —
`.github/workflows/ci.yml`, added during WP-3's own "wired into CI" gate
closure (the brief makes this WP-3's own acceptance criterion, not WP-9's;
see `PROGRESS.md`'s WP-3 entry). It provisions PostgreSQL 15 via GitHub
Actions' native `services:` container (not a substitute — this is real
Postgres 15, matching the plan's WP-2/WP-9 CI design), applies both
DbContexts' migrations, and runs the full `GarageOS.Tests.Unit` +
`GarageOS.Tests.Integration` suite on every push/PR to `main`.

*(Historical note, as originally written 2026-08-27, WP-3: at that point
WP-9 still needed the WP-4/WP-5 suites to exist, the Resend-SDK-isolation
grep check, and the "Rashid"-placeholder grep check. All three have since
landed — see the 2026-08-28 update immediately below.)*

**WP-9 status update (2026-08-28, after WP-4/WP-5/WP-6/WP-7):**

DONE:
- WP-4/WP-5 test suites exist and pass (both formally ACCEPTED).
- Resend-SDK-isolation CI regression check implemented
  (`scripts/ci/check-no-resend-outside-service.sh`) and wired into
  `.github/workflows/ci.yml` as a blocking step, with its negative-test
  proof run and independently re-run by both WP-6's QA Lead and Security
  Reviewer gates.
- "Rashid"-placeholder CI regression check implemented
  (`scripts/ci/check-no-legacy-brand.sh`) and wired into
  `.github/workflows/ci.yml` as a blocking step, with its negative-test
  proof run and independently re-run by both WP-7's QA Lead and Security
  Reviewer gates.

DONE (added 2026-09-02, WP-8):
- WP-8 frontend (React/TS/Vite) built, tested, and integrated. Frontend
  build/typecheck, 79/79 Vitest+RTL, and 4/4 real-backend Playwright e2e
  all pass on-device. `build-and-test-frontend` and `e2e-frontend` jobs
  wired into `.github/workflows/ci.yml` alongside the existing backend
  `build-and-test` job.
- A CRITICAL (vitest RCE, GHSA-5xrq-8626-4rwp) and a HIGH (vite
  `server.fs.deny` bypass, GHSA-fx2h-pf6j-xcff) dependency finding from
  Security Reviewer's WP-8 gate were fixed by a major-version bump (vite
  ^5.4.21->^7.3.6, vitest ^2.1.9->^4.1.11, react-router-dom
  ^6.30.6->^7.18.3) and independently re-verified via a from-scratch
  `npm install && npm audit` (0 vulnerabilities).

STILL OPEN (WP-9 is NOT gating-complete):
- The pipeline has still never actually run in anger — no push/PR has
  triggered it yet; device-side `dotnet test`/`npm test`/`npx playwright
  test`/script execution remains the verified source of truth so far.
- Verifying the CI gate's negative behavior end-to-end in the actual CI
  environment (not just proven locally on-device).
- A final QA/Security/Architect review of the CI configuration itself, not
  just of the WPs it runs.
- ~~Deterministic resolution of the intermittent JWT security test~~ **DONE
  (2026-09-02)** —
  (`GarageOS.Tests.Integration.Auth.JwtValidationTests.Me_TamperedSignature_ReturnsUnauthorized`)
  root-caused and fixed; see KI-18 below (now RESOLVED) for full detail.

WP-9's job going forward is to extend this skeleton and close the remaining items above, not to build CI from scratch.
**Note for real developer machines:** a developer with normal admin/root on
their own machine should simply install PostgreSQL 15+ the standard way (the
official installer, or `apt`/`brew` with the PGDG repo added) — the user-space
`dpkg-deb -x` extraction above was needed only because this specific bridged
shell has no root/sudo; it is not the recommended developer setup and is not
documented as such in `README.md`.
**Owner input needed?** No — resolved within Backend Engineer's normal WP-2
scope, per the Owner's 2026-08-27 "CONTINUE PHASE 1 — WP-2 AUTHORIZED"
direction.

---

### KI-2 — Cloud-workspace sandbox cannot reach NuGet (informational, not a project blocker)

**Severity:** Informational.
**Affects:** None directly — noted so a future session doesn't waste time
retrying `dotnet restore` from the wrong environment.
**Description:** Anthropic's cloud sandbox (used for non-device work) has an
egress allowlist that does not include `nuget.org`/`api.nuget.org`, so
`dotnet restore`/`dotnet build` fail there for any project with NuGet
dependencies (even the default ASP.NET Core Web API template). The
device-bridge shell (where this repo actually lives) **does** reach
`nuget.org` and successfully restores/builds a real ASP.NET Core project
once the .NET SDK is installed user-space (see `PROGRESS.md`). All backend
work for this project is therefore done via the device-bridge shell, not
the cloud sandbox.
**Status:** Resolved (by using the correct environment). No action needed.

---

### KI-3 — `/api/demo/config` ships unauthenticated (LOW, **CLOSED** in WP-4)

**Severity:** LOW (non-gating; flagged by Security Reviewer during WP-2's
security-review step).
**Affects:** WP-2 (`GarageOS.Api/Endpoints/DemoEndpoints.cs`).
**Description:** `/api/demo/config` is mapped unconditionally (no environment
gate, unlike `/api/diagnostics/throw`) and returns a static, non-secret string
with no authentication. Not an exposure today, but per its own doc comment
it exists only to prove options-binding works end-to-end for WP-2 and should
be removed once real feature endpoints exist.
**Status:** **CLOSED.** `DemoEndpoints.cs` deleted and its `MapDemoEndpoints()`
call removed from `Program.cs` during WP-4 (real auth endpoints now exist and
supersede its proof-of-pattern purpose), closed opportunistically per the
Owner's standing instruction.
**Owner input needed?** No.

---

### KI-4 — ProblemDetails `exception` extension field has no negative test (LOW, **CLOSED** in WP-4)

**Severity:** LOW (non-gating; flagged by QA Automation Engineer during WP-2's
QA review).
**Affects:** WP-2 (`GarageOS.Tests.Integration/ProblemDetailsTests.cs`).
**Description:** `GlobalExceptionHandler` only adds the `exception` extension
field to the ProblemDetails body in `Development` (verified correct by code
review and by Security Reviewer), but `ProblemDetailsTests` does not yet
assert that the field is *absent* in the `Testing` environment it actually
runs under — it only asserts the fields that are present.
**Status:** **CLOSED.** `UnhandledException_OutsideDevelopment_DoesNotIncludeExceptionExtensionField`
added to `ProblemDetailsTests.cs` during WP-4, closed opportunistically per
the Owner's standing instruction. Passing against the real `Testing`-environment
host.
**Owner input needed?** No.

---

### KI-5 — No SQL-level column `DEFAULT`s in the WP-3 schema (LOW, tracked)

**Severity:** LOW (non-gating; flagged by Database Engineer during WP-3's implementation
review, concurred by QA Lead's final gate).
**Affects:** WP-3 (`backend/GarageOS.Infrastructure/Data/Configurations/*.cs`,
`Migrations/App/20260827120149_InitialSchema.cs`).
**Description:** `11_engineering_handoff.md` §9/brief §2 specify SQL-level `DEFAULT`
clauses on most columns (e.g. `subscription_status DEFAULT 'trial'`,
`discount_limit_percent DEFAULT 15.00`). The implementation instead relies entirely on
C# entity field initializers (e.g. `Status { get; set; } = "checked_in"` on `Job`) to
supply defaults — correct and sufficient for every write path that exists today, since
all Phase 1 writes go through EF Core entity construction (the dev seeder, all test
helpers, and every future WP-5+ service). No SQL `DEFAULT` exists as a safety net for a
write path that bypasses EF entity construction.
**Status:** Tracked, not remediated in WP-3. QA Lead's final gate concurred this is
correctly non-blocking for Phase 1 and recommended closing it opportunistically the next
time any migration touches these tables — no later than either of two triggers: (a) any
raw-SQL/BI/reporting tool is introduced (bundle with KI-6 below — same trigger), or
(b) any bulk-insert/import/migration tooling that could construct rows outside the
Domain entity classes.
**Owner input needed?** No.

---

### KI-6 — `AppDbContext`/`PlatformDbContext` share one Postgres credential (LOW, tracked)

**Severity:** LOW (non-gating; flagged by Security Reviewer during WP-3's security gate).
**Affects:** WP-3 (`GarageOS.Api/appsettings.Development.json`, `Program.cs`).
**Description:** Both DbContexts fall back to the same `GarageOsDb` connection string
using the Postgres `postgres` superuser with local trust-auth. Platform/tenant separation
is enforced entirely at the EF model-graph level (two separate `DbContext` classes) plus
the `platform` schema — proven correct at three independent layers by
`PlatformAdminUnreachabilityTests.cs` — but there is no Postgres role/grant boundary that
would stop a connection authenticated with the app's credential from directly querying
`platform.platform_admins` via raw SQL, if a developer ever wrote one. Not exploitable
today: no raw-SQL code path exists anywhere in WP-3.
**Status:** Tracked for Phase 2+, before any raw-SQL/BI/reporting tooling is added:
separate least-privilege Postgres roles per DbContext (grant `PlatformDbContext`'s role
`USAGE`/`SELECT`/etc. only on schema `platform`, and the App role only on `public`).
**Owner input needed?** No.

---

### KI-7 — Dual tenant-enforcement shares one root of trust: `ICurrentTenant.GarageId` (informational, forward note for WP-4)

**Severity:** Informational (not a WP-3 defect; flagged by Security Reviewer as a
forward-looking note for WP-4's own security review).
**Affects:** WP-4 (JWT/claims implementation) — informational only for WP-3.
**Description:** The global EF Core query filter and `TenantGuard.EnsureOwned` are
genuinely independent code paths for their two named failure modes (a filter bypass via
raw SQL/`.IgnoreQueryFilters()` is still caught by `TenantGuard` if a write path calls
it; a write path that forgets to call `TenantGuard` is still caught by the filter on
every subsequent read). Neither layer, however, is independent of a corrupted or spoofed
`ICurrentTenant.GarageId` value itself — both compare against the same source. Today
`HttpContextCurrentTenant.cs` reads this from unvalidated test-only claims (`TestAuthHandler`,
WP-3 scaffolding only, never registered in `Program.cs`) and fails closed on any
missing/malformed claim (verified by Security Reviewer).
**Status:** Not an action item for WP-3. WP-4's own security review must explicitly
re-verify claim integrity (JWT signature validation, issuer/audience checks) once real
tokens replace `TestAuthHandler`, rather than assuming WP-3's dual-enforcement pattern
alone remains sufficient once a real, attacker-influenced claims source exists.
**Owner input needed?** No.

---

### KI-8 — Bypass-protection regex has a coverage gap for `DbContext.Add(object)` overloads (MEDIUM, **CLOSED** in P2-WP1)

**Severity:** MEDIUM (non-gating; flagged by QA Automation Engineer during WP-3B's QA gate).
**Affects:** WP-3B / P2-WP1 (`GarageOS.Tests.Unit/Architecture/GarageInsertBoundaryTests.cs`).
**Original description:** The source-scanning bypass-protection test's regex patterns
(`Garages\.Add(Range)?\(`, `Set<Garage>().Add`, `INSERT INTO garages`) correctly caught
the known bypass shapes at the time, but would not catch `_db.Add(new Garage {...})` or
`_db.AddRange(garage, otherEntity)` (EF Core's non-generic `DbContext.Add(object)`/
`AddRange(IEnumerable<object>)` overloads) or `context.Entry(x).State = EntityState.Added`.
**Fix (P2-WP1, Backend Engineer; QA Automation Engineer + Security Reviewer gates, two
rounds):** `GarageInsertBoundaryTests.cs` now has (1) two additional direct patterns for
the inline-`new Garage` shape of `Add`/`AddRange`/`AddAsync` and
`Entry(...).State = EntityState.Added`; (2) an indirect variable-name cross-reference
(`HasIndirectGarageInsert`) that tracks every local/field declared with an explicit
`Garage`/`Garage?` type OR a `var name = new Garage` initializer, then checks whether any
tracked name is later passed as the sole argument to `Add`/`AddAsync`/`AddRange`, as one of
several arguments to `AddRange`/`AddRangeAsync`, or as the receiver of
`Entry(...).State = EntityState.Added`; (3) both the direct and indirect checks now run
against `SourceScanUtilities.MaskLiteralsAndComments()` output (the same shared helper
`EstimateMutationBoundaryTests` uses), not raw file text.
**Round 1 → Round 2:** QA Automation Engineer's round-1 gate review returned a BLOCKER —
the `var g = new Garage {...}; db.Add(g);` shape (this codebase's dominant declaration
style) was not caught, since only explicitly-`Garage`-typed declarations were tracked.
Security Reviewer's parallel round-1 review (PASS, no CRITICAL/HIGH) separately flagged a
MEDIUM: multi-argument `AddRange(garageVar, otherVar)` was missed by the sole-argument
check. Round 2 added the `var`-declaration pattern and a whole-argument-list `AddRange`
scan closing both. QA Automation Engineer re-reviewed round 2 and returned **PASS** — the
round-1 BLOCKER is confirmed closed by fixture proof, no regression found, two remaining
non-blocking edge cases noted below.
**Proof:** full 47-test unit suite passes clean with the fix and no fixture present (zero
false positives against the current codebase, both rounds). Six isolated one-statement
bypass fixtures were each added individually, confirmed to make the test FAIL and be
listed as a violation, then deleted with the suite re-confirmed clean: direct inline-new-
Garage `Add`; indirect explicitly-typed-variable `Add`; indirect explicitly-typed-variable
`Entry`+`EntityState.Added`; indirect `var`-declared-variable `Add`; direct inline-new-
Garage `Entry`+`EntityState.Added`; multi-argument `AddRange` with a Garage-typed
identifier alongside another argument. None of the fixture files were committed (confirmed
via `git status` on the working tree before commit).
**Disclosed residual limitations (heuristic text scanner, not a C# type resolver, same
class as KI-16):** does not follow a Garage-typed value through a method parameter, a
cast, a collection/array element, or a property access; a multi-declarator statement
(`Garage g1 = ..., g2 = ...;`) only registers the first name; variable-name tracking is
file-scoped, not method/block-scoped (no false positive triggered by any code that exists
today); fully-qualified instantiation (`new GarageOS.Domain.Garage()`) and a Garage-typed
field assigned outside its declaration statement are untracked (noted by QA Automation
Engineer's round-2 re-review, non-blocking, logged as optional low-priority follow-up, not
a condition of this closure). This remains a CI-time compensating/coding-standard control,
not a runtime enforcement mechanism (Security Reviewer MEDIUM): it has no visibility into
raw ADO.NET/Dapper inserts, `ExecuteSqlRaw`/`ExecuteSqlInterpolated`, bulk-insert
libraries, or reflection/dynamic/expression-tree-built inserts. The DB-level partial
unique index (`garages_account_active_idx`) prevents a *second* active garage per account
but does not by itself prevent an unauthorized *first* garage insert via one of those
uncovered paths — it is a duplicate-prevention control, not a general backstop for "only
AccountProvisioningService may create a Garage." Runtime enforcement of that invariant
(e.g. a distinguished DB role/trigger) is optional future-hardening, out of KI-8's
ticketed scope.
**Status:** **CLOSED.** QA gate: PASS (round 2, after a round-1 BLOCKER was fixed and
re-verified). Security gate: PASS (no CRITICAL/HIGH; MEDIUM findings logged above as
disclosed limitations, non-blocking).
**Owner input needed?** No.

---

### KI-9 — No direct Postgres-catalog assertion that both `garages` indexes coexist after migration (MEDIUM, tracked)

**Severity:** MEDIUM (non-gating; flagged by QA Automation Engineer during WP-3B's QA gate).
**Affects:** WP-3B (migration `MakeGaragesAccountActiveIndexUnique`, `GarageConfiguration.cs`).
**Description:** `GarageConfiguration.cs`'s own comment documents a real bug that shipped
during WP-3's review cycle: two unnamed `HasIndex` calls on `AccountId` silently collapsed
into a single index because neither had a pinned `.HasDatabaseName(...)`. WP-3B's own
migration re-touches this same index (`garages_account_active_idx`), and while
`UniqueIndex_DirectDoubleInsertBypassingService_SecondInsertViolatesConstraint` proves the
unique index itself works, no test queries the real Postgres catalog (e.g.
`SELECT indexname FROM pg_indexes WHERE tablename='garages'`) to assert that **both**
`garages_account_idx` (plain, non-unique) and `garages_account_active_idx` (unique,
partial) exist simultaneously post-migration — i.e., that the original collapse bug
hasn't silently regressed.
**Status:** Tracked. Recommended follow-up: add a short schema-assertion integration test
against `pg_indexes` confirming both index names exist with their expected
unique/partial-filter properties. Low priority — natural fit alongside any future
migration-related work.
**Owner input needed?** No.

### KI-10 — Email lookup is case-sensitive with no documented decision (MEDIUM, tracked)

**Severity:** MEDIUM (non-gating; flagged by QA Automation Engineer during WP-4's QA gate).
**Affects:** WP-4 (`UserAuthLookupRepository.FindByEmailAsync`, login/forgot-password/
reset-password flows), WP-3B (`AccountProvisioningService`, `users_email_idx`).
**Description:** `users.email` is a plain Postgres `text` column with a case-sensitive
unique index (`users_email_idx`), and no code path (login, forgot-password lookup,
provisioning) normalizes/lowercases email on write or read. A user who registers as
`User@Example.com` and later logs in as `user@example.com` receives the same generic 401
as a wrong password — not a security hole (no bypass or data leak — every failure mode
is already indistinguishable by design, per brief §12), but an unaddressed UX/product
decision with zero test coverage of either behavior.
**Status:** Tracked. Recommended follow-up: a Business Analyst/Product decision on
whether email should be case-insensitive (typical for auth systems — e.g. Postgres
`citext` column type or lowercasing on write), then a migration + normalization pass if
so. Not a WP-4 blocker — the *current* behavior (case-sensitive) is internally consistent
and secure, just undocumented as an intentional choice.
**Owner input needed?** Optional — a product decision, not a security/correctness gate.

---

### KI-11 — Rate-limiting test coverage is asymmetric across the four policies (LOW, tracked)

**Severity:** LOW (non-gating; flagged by QA Automation Engineer during WP-4's QA gate).
**Affects:** WP-4 (`RateLimitingTests.cs`).
**Description:** Only `auth-login` (5/min) has an automated test proving its configured
limit is actually enforced (via an isolated `WebApplicationFactory` instance, since the
shared `IntegrationTestFixture` intentionally raises all four limits to 1000/window for
the functional-test suite — see `Program.cs`'s rate-limiter comment). `auth-refresh`
(20/min), `auth-forgot-password` (3/10min), and `auth-reset-password` (5/10min) are wired
identically but have zero automated proof they enforce their configured limits or are
attached to the correct endpoints.
**Status:** Tracked. Recommended follow-up: extend `RateLimitingTests.cs` with 3 more
isolated-host cases (one per remaining policy), mirroring the existing `auth-login` test.
Low priority — the underlying mechanism (`AddAuthRateLimitPolicy`) is identical across
all four policies and is proven correct once; this is closing a coverage symmetry gap,
not a suspected defect.
**Owner input needed?** No.

---

### KI-12 — No malformed/missing request body tests on any auth endpoint (LOW, tracked)

**Severity:** LOW (non-gating; flagged by QA Automation Engineer during WP-4's QA gate).
**Affects:** WP-4 (`AuthController.cs`, all 6 endpoints).
**Description:** No test sends an empty `{}` body, a body missing required fields, or a
non-JSON body to any `/api/v1/auth/*` endpoint. Request DTOs (`AuthContracts.cs`) are
plain records with no `[Required]`/data-annotation validation, so the exact resulting
status code (ASP.NET Core's default model-binding 400 vs. something reaching
`GlobalExceptionHandler`) is unverified.
**Status:** Tracked. Recommended follow-up: a small parameterized test per endpoint
asserting a 400 (not 500) for a malformed/empty body. Low priority — no evidence of an
actual 500 today, this closes a verification gap, not a known defect.
**Owner input needed?** No.

---

### KI-13 — Email whitespace handling untested (LOW, tracked)

**Severity:** LOW (non-gating; flagged by QA Automation Engineer during WP-4's QA gate).
**Affects:** WP-4 (same code paths as KI-10).
**Description:** Same class of gap as KI-10 — a leading/trailing space in a submitted
email (e.g. `" user@example.com"`) is not trimmed anywhere in login/forgot-password/
reset-password, and untested. Likely to surface (or be decided) alongside KI-10's
case-sensitivity decision, since both are "email normalization" and share the same
lookup code path.
**Status:** Tracked, bundle with KI-10's follow-up.
**Owner input needed?** No.

---

### KI-14 — Rate limiter's `Retry-After` header is a fixed 60s regardless of the actual policy window (LOW, tracked)

**Severity:** LOW (non-gating; flagged by Security Reviewer during WP-4's security gate —
cosmetic, not a security issue).
**Affects:** WP-4 (`Program.cs`'s `AddRateLimiter` → `OnRejected` handler).
**Description:** Every 429 response sets `Retry-After: 60` unconditionally, even for the
10-minute-window `auth-forgot-password`/`auth-reset-password` policies — a client that
honors the header literally would retry too early and get rejected again.
**Status:** Tracked. Recommended follow-up: thread the matched policy's actual window
into `OnRejected` (available via `context.Lease`/`RateLimitLease` metadata) rather than a
hardcoded constant. Low priority — purely a client-retry-efficiency nicety, no security
or correctness impact (the server still correctly rejects until the real window elapses).
**Owner input needed?** No.
### KI-15 — Orphaned unrevoked replacement refresh-token row possible on crash between insert and claim (LOW, tracked)

**Severity:** LOW (non-gating; flagged by Security Reviewer during WP-4's targeted
remediation re-review of the refresh-token rotation-race HIGH finding, 2026-08-27).
**Affects:** WP-4 (`AuthService.RefreshAsync` / `InsertRefreshTokenAsync`).
**Description:** `RefreshAsync` inserts the replacement refresh-token row, then calls
`TryClaimForRotationAsync` to atomically claim the old row. If the process is killed or
the DB connection drops strictly between the insert committing and the claim call
executing, the just-inserted replacement row remains in the table as an orphaned,
non-revoked row, and the original parent token remains unrotated/still active. The raw
(unhashed) secret for that orphaned row is held only in a local variable inside the
interrupted `RefreshAsync` call and is never returned to any client (the HTTP response
is only written on success), so the row is dead-by-construction and not externally
reachable/exploitable -- this is a row-hygiene artifact, not a reopening of the
concurrent-reuse race that HIGH finding covered.
**Status:** Tracked. Recommended follow-up: a periodic sweep (or a `CreatedAt`-based
filter in reuse-detection queries) for unclaimed, unrevoked replacement rows older than
a few minutes. Low priority -- not reachable by any client, purely a dead-row cleanliness
item.
**Owner input needed?** No.


---

### KI-16 -- EstimateMutationBoundaryTests' DbContext-variable bypass guard is anchored on the `db`/`_db` naming convention, not full semantic analysis (LOW, tracked)

**Severity:** LOW (non-gating; self-disclosed in the fix's own doc comment when written,
re-confirmed as still live by QA Automation Engineer during WP-5's round-2 adversarial
architecture-test review, 2026-08-27).
**Affects:** WP-5 (`GarageOS.Tests.Unit/Architecture/EstimateMutationBoundaryTests.cs`,
the `\b_?db\s*\.\s*Update(Range)?\s*\(` / `Attach(Range)?` block-list patterns that
close round-1 Bypass A -- a non-generic `DbContext.Update(entity)`/`Attach(entity)` call
that mutates without the text "Estimates.Update("/"Estimates.Attach(" ever appearing).
**Description:** That fix is anchored on this codebase's own `db`/`_db` variable-name
convention for `AppDbContext` (confirmed universal by grep at the time of the fix), not on
the variable's actual declared/inferred TYPE. A future file that names its `AppDbContext`
instance something else (e.g. `AppDbContext context; context.Update(estimate);`) would
evade this specific pattern while every other guard in the same file (the DbSet-rooted
patterns, the ExecuteUpdateAsync statement-scoped check, the AsNoTracking whitelist) stays
fully effective. Zero existing legitimate uses of any non-`db`/`_db`-named `AppDbContext`
variable anywhere in the solution today (confirmed by grep), so this is a forward-looking
gap, not a live, exploitable one against any code that exists now.
**Status:** **CLOSED.** QA Lead's independent WP-5 QA gate review disagreed with
accepting this as a tracked residual: an unqualified (not `db`/`_db`-anchored) pattern
has zero false-positive risk against the current codebase (confirmed by grep --
`.Update(`/`.UpdateRange(`/`.Attach(`/`.AttachRange(` have ZERO legitimate call sites
anywhere in the solution, including inside the allow-listed
`EstimateMutationRepository.cs` itself), so the naming-convention anchor was an
unnecessarily narrow choice, not a necessary one. Fixed by widening
`EstimateMutationBoundaryTests.cs`'s two Bypass-A patterns to match `.Update(Range)?(`/
`.Attach(Range)?(` regardless of the receiver variable's name -- no Roslyn/semantic
analysis needed, a one-line regex change. Re-verified: full solution build clean, 30/30
unit tests pass. No remaining gap for this specific bypass shape.
**Owner input needed?** No.


---

### KI-17 -- Interpolation-hole masking bypass in SourceScanUtilities.MaskLiteralsAndComments (found and closed pre-commit during WP-5 QA gate, 2026-08-27)

**Severity:** Was CRITICAL had it shipped; found and fixed before any commit, so recorded
here for audit-trail completeness rather than as an open risk.
**Affects:** WP-5 (`GarageOS.Tests.Unit/Architecture/SourceScanUtilities.cs`, the shared
masking helper introduced during round-2 remediation and used by both
`EstimateMutationBoundaryTests.cs` and `AuthorizationAttributeMisuseTests.cs`).
**Description:** QA Lead's independent WP-5 QA gate review found that the first version
of `MaskLiteralsAndComments` treated an INTERPOLATED string ($"...", $@"...") exactly
like a plain string literal -- blanking its entire content, including the code inside
`{ }` interpolation holes. But hole content is live, executing C# code, not string data
(`$"{db.Estimates.Update(e)}"` really does call `.Update(` at runtime). QA Lead confirmed
by execution that a real `Estimates.Update(` call hidden inside an interpolation hole
was fully invisible to both `EstimateMutationBoundaryTests` checks (both tests passed
when they should have failed) -- a more serious bypass than either round-2 finding, since
it defeated the primary, unqualified `Estimates.Update(`/`Estimates.Attach(` pattern
itself, not just a narrower anchor choice.
**Status:** **CLOSED**, same day, before any commit. Fixed by giving interpolated
strings hole-aware handling: template text outside `{ }` is masked as before; code
inside a `{ }` hole is left completely unmasked so it stays visible to every downstream
check, while a nested string/char literal declared inside a hole still has its own
content masked (closing the same class of bypass even when nested). Doubled braces
(`{{`/`}}`, C#'s literal-brace escape) are recognized and masked as template text, not
mistaken for a hole. Re-verified: full solution build clean, 30/30 unit tests pass, and
QA Lead's own PoC shape re-run to confirm it is now correctly flagged.
**Owner input needed?** No.

### KI-18 — Intermittent failure: `JwtValidationTests.Me_TamperedSignature_ReturnsUnauthorized` (MEDIUM, RESOLVED 2026-09-02)

**Severity:** MEDIUM (was). A security-relevant assertion (a tampered JWT signature must be
rejected) that was not deterministically observed passing/failing under full-batch test
load was a CI-trust problem, even though every reproduction showed the underlying
production security behavior was intact -- the bug was confirmed to live entirely in the
test's own tampering helper, not in `AddJwtBearer`/JWT validation itself (see Root cause).
**Affects:** WP-4 (`GarageOS.Tests.Integration/Auth/JwtValidationTests.cs`), WP-9
(gating-completeness) -- WP-9 blocker is now cleared.
**Root cause (confirmed by QA Automation Engineer investigation, 2026-09-02):** the test's
`FlipLastChar` helper toggled the *encoded* signature string's final character
(`'A'<->'B'`) rather than the underlying signature bytes. For a 32-byte HMAC-SHA256
signature, base64url's final character carries only 4 real bits (the remaining 2 are
zero-padding per the base64 spec), and `'A'` (`000000`) / `'B'` (`000001`) share identical
top-4 bits. Since `BuildToken` mints a fresh random signature on every call, whenever the
*real* signature's last character already happened to be `'A'` (~1/16 of runs, empirically
confirmed at 0.0618 against a theoretical 0.0625 over 500,000 simulated trials), flipping
it to `'B'` only changed a padding bit that .NET's `Base64UrlEncoder`/`Convert.FromBase64String`
discard on decode -- so the "tampered" token decoded back to byte-identical, still-valid
signature bytes. The token was therefore accepted as genuinely valid by `AddJwtBearer`
(reaching the controller, returning 404 for the synthetic garage/sub, same as the
`Me_ValidToken_...` case) instead of being rejected with 401, causing the assertion to fail
on that ~1-in-16 draw. This is why the test was reliable in small manual isolation reruns
(too few draws to hit the collision) but showed up under repeated/full-suite runs (more
draws, more chances to hit it) -- confirmed to be a probabilistic property of the test
helper itself, not shared `WebApplicationFactory` state, `JwtOptions` mutation, a shared
`HttpClient`, a database-reset race, token-factory state, clock/timing, or parallel-test
interference -- all of which were independently ruled out with evidence (single
non-parallel `[Collection("Integration")]`, `parallelizeTestCollections:false` in
`xunit.runner.json`, immutable `const` signing key/issuer/audience, fresh `CreateClient()`
per call, no DB touch on this code path).
**Fix:** `FlipLastChar` replaced with `FlipSignatureBit`, which decodes the signature
segment to raw bytes, flips every bit of the *first* byte (always a full, padding-free
base64 group for a signature this length), and re-encodes -- deterministically invalidating
the signature on every run, with zero change to `AddJwtBearer`, `TokenValidationParameters`,
`JwtOptions`, or any production JWT validation code. A new regression test,
`FlipSignatureBit_AlwaysProducesADifferentDecodedByteSequence`, iterates 1,000 synthetic
32-byte signatures (fixed seed) asserting the flip always changes the decoded bytes, guarding
against this exact bypass class reappearing. Both changes are confined to
`JwtValidationTests.cs`.
**Verification:** full backend suite (`dotnet test`, real PostgreSQL 15, no Docker/no
substitute) re-run 5 consecutive times after the fix, each a clean 181/181 pass (47 unit +
134 integration, the +1 integration test being the new regression guard) with zero
flakes -- `Me_TamperedSignature_ReturnsUnauthorized` passed deterministically on every run.
**Security Reviewer sign-off:** not required as a gate -- the fix touches only test-helper
code (`JwtValidationTests.cs`'s private `FlipLastChar`/`FlipSignatureBit`), not
`AddJwtBearer` configuration, `TokenValidationParameters`, key resolution, or any production
authentication code path; production JWT signature validation was never actually broken.
Security Reviewer is being looped in via the WP-8 report for awareness given the subject
matter.
**Status: RESOLVED.** WP-9's KI-18 blocker is cleared.
**Owner input needed?** No.

---

### KI-19 — Playwright trace-on-retry could embed bearer token/dev password into CI artifact (MEDIUM, RESOLVED 2026-09-02)

**Severity:** MEDIUM (was). Flagged by Security Reviewer during WP-9's CI security gate.
**Affects:** WP-9 (`frontend/playwright.config.ts`, `.github/workflows/ci.yml`'s `e2e-frontend` job).
**Description:** `playwright.config.ts` set `trace: 'on-first-retry'`, and the CI job sets
`retries: 1`. A flaky-then-passing test in CI would capture a full network trace
(headers + bodies) for the retry, embedded into the always-uploaded `playwright-report`
artifact (7-day retention) — potentially including the login call's `Authorization`
bearer header and the seeded dev account's request body
(`ralph@performanceautogarage.example` / `DevSeed-Pass1!`). Not CRITICAL/HIGH: the
access token is short-lived (15 min, `JwtOptions.AccessTokenLifetimeMinutes`), the
job's Postgres/backend are torn down before the artifact is downloadable, and the dev
password is already public in `frontend/e2e/fixtures.ts`'s own source.
**Status: RESOLVED.** `trace: 'off'` in `playwright.config.ts` (commit `a9833fd`).
`screenshot: 'only-on-failure'` already covers CI debugging needs without this exposure.
**Owner input needed?** No.

### KI-20 — Raw string literal masking gap in SourceScanUtilities.MaskLiteralsAndComments (MEDIUM, tracked, non-blocking)

**Severity:** MEDIUM. Flagged by Security Reviewer during P2-WP4's independent security
gate (2026-09-04), re-running the same architecture-test safety net KI-17/KI-16 already
hardened.
**Affects:** `GarageOS.Tests.Unit/Architecture/SourceScanUtilities.cs`, shared by
`EstimateMutationBoundaryTests.cs`, `JobMutationBoundaryTests.cs`, and any other
source-scan boundary test built on the same masking helper.
**Description:** `MaskLiteralsAndComments` does not correctly recognize C# raw string
literals (`"""..."""`, including interpolated `$"""..."""`). On encountering the opening
`"""`, it treats the first `"` `"` pair as an ordinary interpolated string, closes it
immediately, then starts a second "string" at the third `"` that terminates prematurely
at the first newline inside the raw string body — leaving the remainder of that raw
string's content **unmasked** and passed through to every downstream text-pattern check
as if it were live code, for the rest of that file's scan. The codebase already contains
raw string literals today (`JobMutationRepository.cs`'s `$"""UPDATE garage_sequences...`
and `ResendEmailService.cs`), though neither file is currently scanned by
`EstimateMutationBoundaryTests`' `Estimates.`/`Set<Estimate>()` anchors, so there is no
live bypass today — confirmed independently by the Security Reviewer via direct code
read of every Estimate-mutation-path file, finding no alternate mutation route.
**Status: OPEN, non-blocking.** Not CRITICAL/HIGH: no reviewed file currently exploits
the gap, and the "single mutation path" guarantee holds by direct inspection today, not
only by this test. Risk is forward-looking — a future change adding a raw string literal
to any file a boundary test scans could hide a genuine bypass introduced in that same
change. Routed to `backend-engineer`: fix `SourceScanUtilities` to correctly
mask/hole-handle `"""`/`$"""` raw string literals (mirroring KI-17's hole-aware fix for
ordinary interpolated strings), or fail loud when one is encountered, then re-review with
`security-reviewer`.
**Owner input needed?** No.
