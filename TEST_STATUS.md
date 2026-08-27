# TEST_STATUS.md

Live automated-test status per work package. Updated as each WP's test suite
is added and run. "Pass locally" means `dotnet test` / `npm test` was actually
executed and observed to pass in this environment, not merely written.

| WP | Suite | Status | Last run | Notes |
|----|-------|--------|----------|-------|
| WP-1 | — (no logic; QA requirement: none) | N/A | — | — |
| WP-2 | `GarageOS.Tests.Unit` | NOT YET ADDED | — | Scaffolding in progress. |
| WP-2 | `GarageOS.Tests.Integration` | NOT YET ADDED | — | Requires a reachable PostgreSQL 15+ instance; see `KNOWN_ISSUES.md`. |
| WP-3 | Tenant-isolation tests (12 in-scope resources) | NOT STARTED | — | Depends on WP-2/WP-3 schema. |
| WP-3B | Account/Garage provisioning bypass-protection test | NOT STARTED | — | — |
| WP-4 | Auth/JWT tests (incl. platform-admin/garage-tenant mutual exclusion) | NOT STARTED | — | — |
| WP-5 | Authorization boundary tests (15.00%/15.01% discount, $500.00/$500.01) + single-mutation-path bypass tests | NOT STARTED | — | BLOCKER-severity per Phase 1 Quality Gates if missing/skipped. |
| WP-6 | Email service tests | NOT STARTED | — | — |
| WP-7 | Branding config tests | NOT STARTED | — | — |
| WP-8 | Vitest + RTL, Playwright | NOT STARTED | — | — |
| WP-9 | CI-enforced execution of all of the above | NOT STARTED | — | Not gating-complete until WP-4/WP-5 suites exist per the plan's Parallelization section. |

**Environment note (2026-08-27):** `.NET 8 SDK` restore/build verified working
in the device-bridge shell (see `PROGRESS.md`). A throwaway `dotnet new webapi`
project restored and built successfully. No PIT961 application tests have
been written or run yet.
