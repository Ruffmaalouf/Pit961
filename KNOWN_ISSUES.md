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
`GarageOS.Tests.Integration`'s full suite (2/2) was run and passed against
this real PostgreSQL 15.19 instance; the unreachable-DB fail-loud path was
also verified separately. This resolves the local/verification side of KI-1.
The CI side (GitHub Actions' native `services:` PostgreSQL 15+, per WP-2/WP-9)
remains unaffected and unbuilt until WP-9.
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

### KI-3 — `/api/demo/config` ships unauthenticated (LOW, tracked)

**Severity:** LOW (non-gating; flagged by Security Reviewer during WP-2's
security-review step).
**Affects:** WP-2 (`GarageOS.Api/Endpoints/DemoEndpoints.cs`).
**Description:** `/api/demo/config` is mapped unconditionally (no environment
gate, unlike `/api/diagnostics/throw`) and returns a static, non-secret string
with no authentication. Not an exposure today, but per its own doc comment
it exists only to prove options-binding works end-to-end for WP-2 and should
be removed once real feature endpoints exist.
**Status:** Tracked. Remove `DemoEndpoints.cs` (and its `MapDemoEndpoints()`
call in `Program.cs`) once WP-3+ introduces real endpoints that supersede its
purpose as a proof-of-pattern.
**Owner input needed?** No.

---

### KI-4 — ProblemDetails `exception` extension field has no negative test (LOW, tracked)

**Severity:** LOW (non-gating; flagged by QA Automation Engineer during WP-2's
QA review).
**Affects:** WP-2 (`GarageOS.Tests.Integration/ProblemDetailsTests.cs`).
**Description:** `GlobalExceptionHandler` only adds the `exception` extension
field to the ProblemDetails body in `Development` (verified correct by code
review and by Security Reviewer), but `ProblemDetailsTests` does not yet
assert that the field is *absent* in the `Testing` environment it actually
runs under — it only asserts the fields that are present.
**Status:** Tracked as a small test-coverage addition; not blocking WP-2
completion (the underlying behavior is already correct and was independently
verified by Security Reviewer).
**Owner input needed?** No.
