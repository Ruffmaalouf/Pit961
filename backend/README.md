# GarageOS backend (PIT961)

ASP.NET Core 8 modular monolith: `GarageOS.Api` / `GarageOS.Application` /
`GarageOS.Domain` / `GarageOS.Infrastructure`, plus `GarageOS.Tests.Unit` and
`GarageOS.Tests.Integration` under `GarageOS.Tests/`. See the root `README.md`
for the no-Docker Phase 1 constraint and overall project context.

## Prerequisites

- .NET 8 SDK.
- PostgreSQL **15+**, installed locally or otherwise reachable through
  configuration, with a dedicated PIT961 integration-test database (see
  below). No Docker/Testcontainers — see `DECISIONS.md` #10.

## Build & run

```
dotnet build
dotnet run --project GarageOS.Api
```

Serves Swagger UI and `/health` on the configured port (see
`GarageOS.Api/Properties/launchSettings.json`).

## Tests

```
dotnet test
```

`GarageOS.Tests.Unit` needs nothing extra. `GarageOS.Tests.Integration` needs a
real, reachable PostgreSQL 15+ instance:

- Default (no setup): `Host=localhost;Port=5432;Database=pit961_integration_test;Username=postgres`
  — see `GarageOS.Tests/GarageOS.Tests.Integration/appsettings.Integration.json`
  (safe, credential-free default; never a real secret).
- Override for any other local setup or CI via the
  `ConnectionStrings__IntegrationTestDb` environment variable, e.g.:
  ```
  export ConnectionStrings__IntegrationTestDb="Host=127.0.0.1;Port=5432;Database=pit961_integration_test;Username=postgres"
  ```
- If the database is unreachable, the integration tests fail loudly with a
  clear connection error — they never silently skip.

Each run resets the database to a clean state via Respawn
(`IntegrationTestFixture`) before running; integration test collections do
not run in parallel against the shared database (`xunit.runner.json`).

## Secrets

No secret ever lives in `appsettings.json` or `appsettings.Development.json`
— both contain structure and safe non-secret defaults only. For local
secrets (once later WPs introduce any — JWT signing key in WP-4, Resend API
key in WP-6), use either:

- `dotnet user-secrets init` / `dotnet user-secrets set "Key" "value"` from
  `GarageOS.Api/`, or
- a gitignored `appsettings.Local.json` next to `appsettings.json`.

In production, secrets come from environment variables or the host's secret
store — never a hardcoded value, and no host-specific secrets-manager SDK
coupling in Phase 1 (`DECISIONS.md` #5).
