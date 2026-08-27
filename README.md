# PIT961 ("Garage OS" internal codename)

Internal-codename garage-management SaaS. **PIT961** is a project codename only —
the final customer-facing product/brand name is undecided (see `DECISIONS.md` #6).
Do not treat "PIT961" or "GarageOS" as a customer-facing brand anywhere in product
copy, UI, or outbound communication.

## Project documentation (source of truth)

Read these before touching code, in roughly this order:

- `DECISIONS.md` — the authoritative, append-only owner decision log.
- `11_engineering_handoff.md` — the detailed engineering reference (architecture,
  schema, API surface, business rules).
- `13_phase1_execution_plan.md` — the approved Phase 1 execution plan (work
  packages, dependency graph, acceptance criteria, quality gates). **Status:
  APPROVED — Phase 1 implementation authorized (v3, 2026-08-27).**
- `09_design_system.md` / `prototype.html` — design reference; `prototype.html`
  is canonical for visual design per `DECISIONS.md` #1.
- `IMPLEMENTATION_MAP.md`, `PROGRESS.md`, `TEST_STATUS.md`, `KNOWN_ISSUES.md` —
  live engineering tracking docs, updated continuously during implementation.

## Tech stack (Phase 1)

- **Backend:** ASP.NET Core 8 (modular monolith: Api / Application / Domain /
  Infrastructure), EF Core 8, PostgreSQL 15+.
- **Frontend:** React 18 + TypeScript + Vite + Tailwind + shadcn/ui.
- **Email:** Resend, behind an `IEmailService` abstraction (see `DECISIONS.md` #8).
- **Hosting:** deferred (see `DECISIONS.md` #5) — not a Phase 1 blocker.

## No containerization in Phase 1

Per `DECISIONS.md` #10 (Owner decision, 2026-08-27), **Docker/containerization is
explicitly out of scope for Phase 1.** There is no `Dockerfile`, `docker-compose.yml`,
Testcontainers, Kubernetes, or Podman anywhere in this repository, and none may be
added without a separate, explicit Owner approval. Local development uses the native
toolchains directly:

- Backend: `dotnet run` / `dotnet watch` against a locally installed or otherwise
  reachable PostgreSQL 15+ instance.
- Frontend: `npm install` / `npm run dev` / `npm run build`.
- Database: PostgreSQL 15+, installed locally or reachable via configuration. A
  dedicated PIT961 integration-test database is required for `GarageOS.Tests.Integration`
  (see `13_phase1_execution_plan.md` WP-2 for the exact connection-string and
  reset/cleanup model). CI provisions PostgreSQL via the CI provider's native
  service-container support (e.g. GitHub Actions `services:`) — this requires no
  Docker installation from developers or the project.

The project stays container-friendly (environment-variable-driven configuration, no
host-specific coupling in application code) so containerization may be reconsidered
later, before staging/production, if it provides a concrete benefit — that is a
future decision, not a Phase 1 task.

## Repository layout

```
/backend    ASP.NET Core solution (Api / Application / Domain / Infrastructure), scaffolded in WP-2
/frontend   React + TypeScript + Vite application, scaffolded in WP-8
/*.md       Project documentation (see above)
prototype.html / support.js   Canonical visual-design reference (DECISIONS.md #1)
_archive/   Superseded local scripts — never committed (see .gitignore)
```

## Branch strategy

Trunk-based development on `main`:

- `main` is the only long-lived branch. It must always build and pass its test
  suite once WP-2/WP-8 land (CI-enforced per WP-9).
- All work happens on short-lived feature branches named `wp-<n>-<short-description>`
  (e.g. `wp-2-backend-scaffold`, `wp-5-authorization-policies`), branched from
  `main` and merged back via pull request.
- No direct pushes to `main` for anything beyond the initial bootstrap commits
  (WP-1/WP-10). Every subsequent change lands via PR, gated by CI (WP-9) once
  the pipeline exists.
- QA and Security sign-off (per `13_phase1_execution_plan.md`'s Phase 1 Quality
  Gates) are required before a feature branch's PR is merged for any
  WP carrying a QA or Security-review requirement.
- Commit messages should reference the work package they implement (e.g.
  `WP-3: add garages/accounts schema and tenant-isolation tests`).

## Getting started (once WP-2/WP-8 land)

```
# Backend
cd backend
dotnet restore
dotnet run --project src/Api

# Frontend
cd frontend
npm install
npm run dev
```

Local secrets (JWT signing key, Resend API key, DB connection strings with
credentials) are never committed. Use `dotnet user-secrets` or a gitignored
`appsettings.Local.json` for the backend, and a gitignored `.env.local` for the
frontend. See `13_phase1_execution_plan.md` WP-2 for the exact mechanism.
