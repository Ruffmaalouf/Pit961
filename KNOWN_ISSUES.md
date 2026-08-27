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
`GarageOS.Tests.Integration` suite on every push/PR to `main`. It has not
yet actually been exercised by a push/PR (device-side `dotnet test` remains
the verified source of truth so far). **WP-9 is still not gating-complete**:
it retains its own remaining approved scope — the Resend-SDK-leakage grep
check, the "Rashid"-placeholder grep check, wiring in the frontend build once
WP-8 exists, and the plan's stated "not gating-complete until WP-4/WP-5 suites
exist" condition. WP-9's job going forward is to extend this skeleton, not to
build CI from scratch.
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

### KI-8 — Bypass-protection regex has a coverage gap for `DbContext.Add(object)` overloads (MEDIUM, tracked)

**Severity:** MEDIUM (non-gating; flagged by QA Automation Engineer during WP-3B's QA gate).
**Affects:** WP-3B (`GarageOS.Tests.Unit/Architecture/GarageInsertBoundaryTests.cs`).
**Description:** The source-scanning bypass-protection test's regex patterns
(`Garages\.Add(Range)?\(`, `Set<Garage>().Add`, `INSERT INTO garages`) correctly catch
today's known bypass shapes, but would not catch a hypothetical future call written as
`_db.Add(new Garage {...})` or `_db.AddRange(garage, otherEntity)` (EF Core's non-generic
`DbContext.Add(object)`/`AddRange(IEnumerable<object>)` overloads) or
`context.Entry(x).State = EntityState.Added`. No such violation exists today (verified
by direct grep across the whole backend) — the DB-level partial unique index
(`garages_account_active_idx`) remains the airtight backstop regardless of call-site
shape. This is a test/code-review-aid gap, not a live defect.
**Status:** Tracked. Recommended follow-up: widen `GarageInsertBoundaryTests`'
`BypassPatterns` to include the generic `.Add(Async)?\(\s*new\s+Garage\b` /
`.AddRange\(...Garage...\)` shapes. Low priority — do when next touching that test file.
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
