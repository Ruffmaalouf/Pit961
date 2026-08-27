# IMPLEMENTATION_MAP.md

Live map of every Phase 1 work package to its implementation location. Updated
as each WP starts/lands. See `13_phase1_execution_plan.md` for full scope and
acceptance criteria per WP, and `PROGRESS.md` for narrative status.

| WP | Title | Specialist | Status | Location / Notes |
|----|-------|------------|--------|-------------------|
| WP-1 | Repo & Environment Bootstrap | Technical Architect | **DONE** | Repo root: `.git/`, `.gitignore` (hardened), `README.md`, `frontend/`, `backend/`. Initial commit `ba3ee96`. |
| WP-10 | Engineering Tracking Docs | Dispatcher / Technical Architect | **DONE** | This file + `PROGRESS.md`, `TEST_STATUS.md`, `KNOWN_ISSUES.md` (repo root). |
| WP-2 | Backend Solution Scaffold | Backend Engineer | **IN PROGRESS** | `backend/` — ASP.NET Core 8 modular monolith (Api/Application/Domain/Infrastructure) + `GarageOS.Tests.Unit`/`GarageOS.Tests.Integration`. |
| WP-3 | Schema / Tenant Isolation | Database Engineer + Backend Engineer | NOT STARTED | `backend/src/Infrastructure` (EF Core), migrations. Depends on WP-2. |
| WP-3B | Account/Garage Provisioning Service | Backend Engineer + Database Engineer | NOT STARTED | Depends on WP-3. |
| WP-4 | Authentication / JWT / Platform Admin claim | Backend Engineer | NOT STARTED | Depends on WP-3. |
| WP-5 | Authorization Policies (discount 15%, $500 threshold) | Backend Engineer + Technical Architect review | NOT STARTED | Depends on WP-4. |
| WP-6 | Email (IEmailService / Resend) | Integration Engineer | NOT STARTED | Interface can scaffold alongside WP-3/WP-7; final wiring after WP-4. |
| WP-7 | Branding Configuration | Backend Engineer + Technical Architect review | NOT STARTED | Depends on WP-2. |
| WP-8 | Frontend Tooling Scaffold | Frontend Engineer | NOT STARTED | `frontend/` — React 18 + TS + Vite + Tailwind + shadcn. Tooling half depends on WP-2; auth/branding wiring half depends on WP-4/WP-7. |
| WP-9 | CI Pipeline | DevOps Engineer + Technical Architect | NOT STARTED | Skeleton once WP-2+WP-8 buildable; gating-complete after WP-4/WP-5. |

**Standing note:** no Docker/containerization anywhere in this repo (Owner decision, `DECISIONS.md` #10). Local dev: `dotnet run`/`dotnet watch`, `npm run dev`, PostgreSQL 15+ installed/reachable directly.
