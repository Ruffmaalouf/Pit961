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
| WP-3B | Account/Garage provisioning: creation, duplicate-rejection, cross-account, unknown-account, soft-delete-reactivate, atomicity, no-partial-rows, DB-backstop-bypass, 2-way + 10-way concurrency, bypass-protection source scan | **DONE** | 2026-08-27 | 11 new tests (1 unit + 10 integration), all passing against real PostgreSQL 15.19. See "Current verified state" below. |
| WP-4 | Auth/JWT tests (incl. platform-admin/garage-tenant mutual exclusion) | NOT STARTED | — | — |
| WP-5 | Authorization boundary tests (15.00%/15.01% discount, $500.00/$500.01) + single-mutation-path bypass tests | NOT STARTED | — | BLOCKER-severity per Phase 1 Quality Gates if missing/skipped. |
| WP-6 | Email service tests | NOT STARTED | — | — |
| WP-7 | Branding config tests | NOT STARTED | — | — |
| WP-8 | Vitest + RTL, Playwright | NOT STARTED | — | — |
| WP-9 | CI-enforced execution of all of the above | SKELETON IN PLACE | 2026-08-27 | `.github/workflows/ci.yml` builds + applies both DbContexts' migrations + runs `GarageOS.Tests.Unit`/`GarageOS.Tests.Integration` against a real Postgres 15 GitHub Actions service container on push/PR to `main`. Not yet run in anger (no push/PR has triggered it yet — device-side `dotnet test` is the verified source of truth so far). Not gating-complete until WP-4/WP-5 suites exist per the plan's Parallelization section. |

**Environment note (2026-08-27, superseded — kept for history):** `.NET 8 SDK`
restore/build was originally verified working in the device-bridge shell via a
throwaway `dotnet new webapi` project, before any PIT961 application test existed.

**Current verified state (2026-08-27):** WP-2, WP-3, and WP-3B application test
suites now exist and pass. **79/79 tests passing** — 5 `GarageOS.Tests.Unit`
(`DemoOptionsBindingTests` from WP-2, `TenantGuardTests` from WP-3,
`GarageInsertBoundaryTests` from WP-3B) + 74 `GarageOS.Tests.Integration`
(`HealthCheckTests`/`ProblemDetailsTests` from WP-2, the 12-resource
`TenantIsolation/` suite + `PlatformAdminUnreachabilityTests` +
`QueryFilterCoverageTests` from WP-3, `AccountProvisioningServiceTests` +
`AccountProvisioningConcurrencyTests` from WP-3B) — run against real
PostgreSQL 15.19 (device-local instance; no Docker/Testcontainers/PG14/SQLite/
InMemory substitute). Migration `MakeGaragesAccountActiveIndexUnique` applied
cleanly to both `pit961_integration_test` and `pit961_dev`. Seed flow smoke-tested
end-to-end against real `pit961_dev` via the actual DI-wired
`IAccountProvisioningService` (idempotent re-run and fresh-after-TRUNCATE run both
verified). See the per-WP rows above for the breakdown.
