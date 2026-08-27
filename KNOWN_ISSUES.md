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
**Status:** Being worked as part of WP-2. Options under evaluation: (a) add
the official PostgreSQL APT repository (apt.postgresql.org) if reachable
from this environment and install user-locally via `apt-get download` +
`dpkg-deb -x` (no root needed), (b) use a portable/relocatable PostgreSQL
15+ binary distribution, (c) developers running actual local development
(outside this bridged shell, e.g. on their own machine) install PostgreSQL
15+ normally per the README instructions — this is the expected real-world
path for the engineering team and is not itself blocked by anything found
here. This issue affects only automated-test execution from within the
sandboxed repository-work shell used during this dispatcher-led bootstrap,
not the project's actual required toolchain.
**Owner input needed?** No — this is a routine environment/tooling detail
within Backend Engineer's/DevOps Engineer's normal WP-2/WP-9 scope, not a
product, architecture, or business-rule question.

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
