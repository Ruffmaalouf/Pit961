# IMPLEMENTATION_MAP.md

Live map of every Phase 1 work package to its implementation location. Updated
as each WP starts/lands. See `13_phase1_execution_plan.md` for full scope and
acceptance criteria per WP, and `PROGRESS.md` for narrative status.

| WP | Title | Specialist | Status | Location / Notes |
|----|-------|------------|--------|-------------------|
| WP-1 | Repo & Environment Bootstrap | Technical Architect | **DONE** | Repo root: `.git/`, `.gitignore` (hardened), `README.md`, `frontend/`, `backend/`. Initial commit `ba3ee96`. |
| WP-10 | Engineering Tracking Docs | Dispatcher / Technical Architect | **DONE** | This file + `PROGRESS.md`, `TEST_STATUS.md`, `KNOWN_ISSUES.md` (repo root). |
| WP-2 | Backend Solution Scaffold | Backend Engineer | **DONE** | `backend/` — ASP.NET Core 8 modular monolith (`GarageOS.Api`/`GarageOS.Application`/`GarageOS.Domain`/`GarageOS.Infrastructure`) + `GarageOS.Tests.Unit`/`GarageOS.Tests.Integration`. Commit `a6bb20a`. QA: PASS (QA Automation Engineer). Security: PASS (Security Reviewer). |
| WP-3 | Schema / Tenant Isolation | Database Engineer + Backend Engineer | **DONE** | `backend/GarageOS.Domain` (16 entities), `backend/GarageOS.Application` (`ICurrentTenant`, `TenantGuard`), `backend/GarageOS.Infrastructure` (`AppDbContext`/`PlatformDbContext`, 16 entity configs, both migration sets, dev seeder). Commits `61b4bf9`, `067e4d1`, `0fc3201`, `fb975b9`. Database Engineer review: APPROVE (follow-ups remediated). QA Automation: PASS. Security: PASS. QA Lead: **ACCEPT WITH TRACKED FOLLOW-UPS** (no BLOCKER/CRITICAL). |
| WP-3B | Account/Garage Provisioning Service | Backend Engineer + Database Engineer | **DONE** | `backend/GarageOS.Application/Abstractions/IAccountProvisioningService.cs`, `.../Accounts/GarageProvisioningDetails.cs`, `.../Common/Account{AlreadyHasGarage,NotFound}Exception.cs`; `backend/GarageOS.Infrastructure/Data/Provisioning/AccountProvisioningService.cs` (locks the parent `accounts` row via `FOR UPDATE`, not `garages` -- phantom-row fix); migration `MakeGaragesAccountActiveIndexUnique` (partial unique index `garages_account_active_idx`, with documented pre-flight duplicate-check); `DevelopmentSeeder.cs`/`Program.cs` updated to the service exclusively; bypass-protection source-scan test (`GarageInsertBoundaryTests`); 8 behavioral + 2 concurrency integration tests. Zero HTTP endpoint added, per Owner instruction. Commit `<pending>`. Database Engineer review: **ACCEPT** (brief-stage required change confirmed incorporated). QA Automation: **PASS WITH FINDINGS** (0 BLOCKER/CRITICAL; 2 MEDIUM follow-ups tracked as KI-8/KI-9). |
| WP-4 | Authentication / JWT / Platform Admin claim | Backend Engineer | NOT STARTED | Depends on WP-3. |
| WP-5 | Authorization Policies (discount 15%, $500 threshold) | Backend Engineer + Technical Architect review | NOT STARTED | Depends on WP-4. |
| WP-6 | Email (IEmailService / Resend) | Integration Engineer | NOT STARTED | Interface can scaffold alongside WP-3/WP-7; final wiring after WP-4. |
| WP-7 | Branding Configuration | Backend Engineer + Technical Architect review | NOT STARTED | Depends on WP-2. |
| WP-8 | Frontend Tooling Scaffold | Frontend Engineer | NOT STARTED | `frontend/` — React 18 + TS + Vite + Tailwind + shadcn. Tooling half depends on WP-2; auth/branding wiring half depends on WP-4/WP-7. |
| WP-9 | CI Pipeline | DevOps Engineer + Technical Architect | SKELETON IN PLACE | `.github/workflows/ci.yml` created early (during WP-3's own "wired into CI" gate closure per the plan's "each WP wires its own suite in" principle) — builds `backend/GarageOS.sln`, applies both DbContexts' migrations, runs `GarageOS.Tests.Unit`+`GarageOS.Tests.Integration` against a real PostgreSQL 15 GitHub Actions service container. Not yet gating-complete: WP-9's own scope (Resend-SDK-leakage grep, "Rashid"-placeholder grep, frontend build once WP-8 exists, gating after WP-4/WP-5 land) is still open. |

**Standing note:** no Docker/containerization anywhere in this repo (Owner decision, `DECISIONS.md` #10). Local dev: `dotnet run`/`dotnet watch`, `npm run dev`, PostgreSQL 15+ installed/reachable directly.
