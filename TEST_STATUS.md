# TEST_STATUS.md

Live automated-test status per work package. Updated as each WP's test suite
is added and run. "Pass locally" means `dotnet test` / `npm test` was actually
executed and observed to pass in this environment, not merely written.

| WP | Suite | Status | Last run | Notes |
|----|-------|--------|----------|-------|
| WP-1 | — (no logic; QA requirement: none) | N/A | — | — |
| WP-2 | `GarageOS.Tests.Unit` | **PASS (2/2)** | 2026-08-27 | Options-binding tests (`DemoOptionsBindingTests`). |
| WP-2 | `GarageOS.Tests.Integration` | **PASS (2/2)** | 2026-08-27 | `HealthCheckTests`, `ProblemDetailsTests`. Run against real PostgreSQL 15.19 (user-space local instance — see `KNOWN_ISSUES.md` KI-1, resolved). Unreachable-DB fail-loud path also verified separately (2/2 fail with a clear connection error, 0 skipped). |
| WP-3 | `GarageOS.Tests.Unit` (`TenantGuardTests`) | **PASS (2/2)** | 2026-08-27 | Happy-path + throw-path coverage for `TenantGuard.EnsureOwned` in isolation, no DB dependency. |
| WP-3 | `GarageOS.Tests.Integration` (`TenantIsolation/`: 12 resources + `PlatformAdminUnreachabilityTests` + `QueryFilterCoverageTests`) | **PASS (62/62)** | 2026-08-27 | Run against real PostgreSQL 15.19 (device-local instance, migrations applied fresh). 9 of the 12 resources carry the full 5-test pattern (incl. parent-mismatch); Customers/GarageSettings/Users have the 4-test pattern (no eligible parent relationship). `QueryFilterCoverageTests` is a reflection-based meta-test proving the global query filter covers every `ITenantOwned` entity and only those. Combined with WP-2's 2 integration tests: **64/64 `GarageOS.Tests.Integration` total.** Combined with WP-2's 2 unit tests: **4/4 `GarageOS.Tests.Unit` total.** Grand total **68/68**. |
| WP-3B | Account/Garage provisioning bypass-protection test | NOT STARTED | — | — |
| WP-4 | Auth/JWT tests (incl. platform-admin/garage-tenant mutual exclusion) | NOT STARTED | — | — |
| WP-5 | Authorization boundary tests (15.00%/15.01% discount, $500.00/$500.01) + single-mutation-path bypass tests | NOT STARTED | — | BLOCKER-severity per Phase 1 Quality Gates if missing/skipped. |
| WP-6 | Email service tests | NOT STARTED | — | — |
| WP-7 | Branding config tests | NOT STARTED | — | — |
| WP-8 | Vitest + RTL, Playwright | NOT STARTED | — | — |
| WP-9 | CI-enforced execution of all of the above | SKELETON IN PLACE | 2026-08-27 | `.github/workflows/ci.yml` builds + applies both DbContexts' migrations + runs `GarageOS.Tests.Unit`/`GarageOS.Tests.Integration` against a real Postgres 15 GitHub Actions service container on push/PR to `main`. Not yet run in anger (no push/PR has triggered it yet — device-side `dotnet test` is the verified source of truth so far). Not gating-complete until WP-4/WP-5 suites exist per the plan's Parallelization section. |

**Environment note (2026-08-27):** `.NET 8 SDK` restore/build verified working
in the device-bridge shell (see `PROGRESS.md`). A throwaway `dotnet new webapi`
project restored and built successfully. No PIT961 application tests have
been written or run yet.
