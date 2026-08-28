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
| WP-4 | Auth/JWT tests (`GarageOS.Tests.Integration/Auth/`: LoginTests, RefreshTokenTests, LogoutTests, ForgotPasswordTests, ResetPasswordTests, MeEndpointTests, AccountLockoutTests, JwtValidationTests, AuthorizationPolicyMutualExclusionTests, PlatformAdminRouteInventoryTests, RateLimitingTests) | **DONE** | 2026-08-27 | 46 tests, all passing against real PostgreSQL 15.19. Includes a concurrent-refresh-reuse regression test (`Refresh_ConcurrentPresentationOfSameToken_ExactlyOneWinsAndReuseDetectionStillFires`, fires two genuinely concurrent `/refresh` calls via `Task.WhenAll`) added to close a HIGH finding from Security Reviewer's post-implementation gate. Platform-admin/garage-tenant mutual exclusion asserted both via `IAuthorizationService.AuthorizeAsync` directly and live endpoints. Zero live platform-admin route proven via `EndpointDataSource` reflection + controller-type reflection + live 404 probes. |
| WP-5 | Authorization boundary tests (`GarageOS.Tests.Unit/{Authorization,Estimates,Architecture}/`: `DiscountLimitHandlerTests`, `EstimateApprovalThresholdHandlerTests`, `EstimateDiscountServiceTests`, `EstimateApprovalServiceTests`, `EstimateMutationBoundaryTests`, `AuthorizationAttributeMisuseTests`) | **DONE** | 2026-08-28 | 25 tests, all passing. Mandatory matrix covered: Manager 15.00%->allowed, 15.01%->denied (`exceeds_manager_cap`); Owner 40%->allowed; advisor/mechanic/accountant->denied (`role_not_permitted`, Theory); tenant mismatch/missing `garage_id` claim->denied (`tenant_mismatch`) for both policies; $500.00->no approval required; $500.01->`requires_owner_approval` regardless of role (Theory over owner/manager/advisor). Bypass-protection architecture tests went through 4 adversarial-hardening rounds this session (round 1-2 by QA Automation Engineer, round 3 by QA Lead's own gate review, round 4 by Security Reviewer's own gate review) -- see `IMPLEMENTATION_MAP.md`'s WP-5 row and `KNOWN_ISSUES.md` KI-16/KI-17 for the full finding-by-finding history. No fake Phase 3 HTTP endpoints -- confirmed by grep, zero controllers reference the new services. |
| WP-6 | Email service tests | NOT STARTED | — | — |
| WP-7 | Branding config tests | NOT STARTED | — | — |
| WP-8 | Vitest + RTL, Playwright | NOT STARTED | — | — |
| WP-9 | CI-enforced execution of all of the above | SKELETON IN PLACE | 2026-08-28 | `.github/workflows/ci.yml` builds + applies both DbContexts' migrations + runs `GarageOS.Tests.Unit`/`GarageOS.Tests.Integration` against a real Postgres 15 GitHub Actions service container on push/PR to `main`. The WP-4/WP-5-suites-must-exist precondition is now satisfied (both DONE, 151/151 passing) -- that is no longer what blocks gating-completeness. Still open, at minimum: (1) a WP-6 Resend-SDK-isolation regression check (with its own negative test -- introduce a prohibited reference, prove it fails, remove it, prove it passes) wired into CI, not a one-off manual grep; (2) a WP-7 placeholder-brand ("Rashid") regression check, same negative-test discipline, wired into CI; (3) WP-8 frontend build/tests once the frontend exists; (4) actually integrating the frontend suite into `ci.yml` alongside the backend suite; (5) verifying the CI gate's negative behavior -- that a genuine violation actually fails the pipeline, not just that the happy path passes; (6) a final QA/Security/Architect review of the CI configuration itself, not just of the WPs it runs; (7) the pipeline has still never run in anger (no push/PR has triggered it yet -- device-side `dotnet test` remains the verified source of truth). Additionally, `GarageOS.Tests.Integration.Auth.JwtValidationTests.Me_TamperedSignature_ReturnsUnauthorized` is a known-intermittent test (fails under full-batch load, passes 100% reliably in isolation -- see WP-5's row and `KNOWN_ISSUES.md`) that does NOT block WP-6 or WP-7, but MUST be diagnosed and made deterministic before WP-9 can be accepted -- an intermittent security-relevant test is not acceptable CI behavior and will not be normalized as such. |

**Environment note (2026-08-27, superseded — kept for history):** `.NET 8 SDK`
restore/build was originally verified working in the device-bridge shell via a
throwaway `dotnet new webapi` project, before any PIT961 application test existed.

**Current verified state (2026-08-28):** WP-2, WP-3, WP-3B, WP-4, and WP-5
application test suites now exist and pass. **151/151 tests passing** — 30
`GarageOS.Tests.Unit` (`DemoOptionsBindingTests` from WP-2, `TenantGuardTests` from
WP-3, `GarageInsertBoundaryTests` from WP-3B, the 6-file WP-5
`Authorization`/`Estimates`/`Architecture` suite) + 121 `GarageOS.Tests.Integration`
(`HealthCheckTests`/`ProblemDetailsTests` from WP-2, the 12-resource
`TenantIsolation/` suite + `PlatformAdminUnreachabilityTests` +
`QueryFilterCoverageTests` from WP-3, `AccountProvisioningServiceTests` +
`AccountProvisioningConcurrencyTests` from WP-3B, the 11-file `Auth/` suite from
WP-4; WP-5 added zero new integration tests, out of its own scope) — run against
real PostgreSQL 15.19 (device-local instance; no Docker/Testcontainers/PG14/SQLite/
InMemory substitute). Migrations `MakeGaragesAccountActiveIndexUnique` (WP-3B),
`AddUserLockoutColumns` (WP-4), and `AddPendingOwnerApprovalEstimateStatus` (WP-5)
applied cleanly to both `pit961_integration_test` and `pit961_dev`. Seed flow
smoke-tested end-to-end against real `pit961_dev` via the actual DI-wired
`IAccountProvisioningService` (idempotent re-run and fresh-after-TRUNCATE run both
verified). One pre-existing, unrelated environmental flake noted under WP-5's row
(`JwtValidationTests.Me_TamperedSignature_ReturnsUnauthorized` — intermittent under
full-batch load, 100% reliable in isolation, not caused by any WP-5 change). See the
per-WP rows above for the breakdown.
