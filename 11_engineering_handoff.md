# GarageOS — Engineering Handoff
## React + ASP.NET Core 8 Production Stack

---

## 1. Product Summary

GarageOS is a multi-tenant Garage Management SaaS priced at **$30 USD/month per garage**, billed monthly, one subscription per garage. There are no other pricing tiers for Phase 1.

"GarageOS" (and `PIT961`) are internal project codenames only. The final customer-facing product/brand name is undecided — see [Branding & Configuration](#7a-branding--configuration) (§7A) and `DECISIONS.md`. Nothing about the architecture may depend on the eventual brand choice.

It manages the complete garage workflow:

**Customer Check-In → Diagnosis → Estimate → Customer Approval → Parts → Repair → Quality Control → Invoice → Payment → Delivery → Service History**

Primary roles:

- Owner
- Manager
- Advisor / Reception
- Mechanic / Technician
- Accountant

The product design phase is complete. `prototype.html` is the canonical, approved visual product (dark theme, orange accent, IBM Plex Sans/Mono) and is the source of truth for all visual design, layout, and interaction details. The approved product documents, UX flows, mobile views, edge cases, and functional specifications remain the source of truth for workflow, business rules, and permissions. Where `09_design_system.md` or any other document conflicts with `prototype.html` on visual design, `prototype.html` governs; log the discrepancy in `DESIGN_IMPLEMENTATION_DIFFERENCES.md`.

This document defines the approved engineering architecture and implementation rules.

---

# 2. Critical Engineering Rule

The engineering team must **implement the approved product**, not redesign it.

Do not:

- Replace the approved UX with generic admin screens.
- Simplify flows because they are easier to code.
- Remove responsive/mobile behavior.
- Ignore empty/loading/error states.
- Change financial rules silently.
- Change role permissions silently.
- Change workshop workflow silently.
- Invent new product functionality during implementation.

Any unavoidable implementation difference must be documented in:

`DESIGN_IMPLEMENTATION_DIFFERENCES.md`

with:

- Approved design behavior
- Implementation limitation
- Proposed change
- Reason
- Impact

---

# 3. Approved Technology Stack

## Frontend

- **Framework:** React 18+
- **Language:** TypeScript
- **Build Tool:** Vite
- **Routing:** React Router
- **Styling:** Tailwind CSS
- **Component Primitives:** shadcn/ui and/or Radix UI
- **Server State:** TanStack Query
- **Client State:** Zustand only where local/global UI state is genuinely needed
- **Forms:** React Hook Form
- **Client Validation:** Zod
- **Charts:** Recharts or another lightweight React chart library
- **Drag & Drop:** dnd-kit
- **Realtime Client:** SignalR JavaScript client
- **Testing:** Vitest + React Testing Library
- **E2E:** Playwright

The frontend is a SPA.

There is no requirement for Next.js, SSR, React Server Components, or server actions.

A public marketing website may be built separately later if SEO is required.

---

## Backend

- **Runtime:** .NET 8
- **Framework:** ASP.NET Core 8 Web API
- **Language:** C#
- **API Style:** REST
- **Authentication:** ASP.NET Core authentication + JWT access token + secure refresh token flow
- **Authorization:** Role + policy-based authorization
- **Validation:** FluentValidation
- **Realtime:** ASP.NET Core SignalR
- **Background Jobs:** Hangfire
- **Logging:** Serilog
- **API Documentation:** Swagger / OpenAPI
- **Testing:** xUnit
- **Mocking:** NSubstitute or Moq
- **Integration Testing:** WebApplicationFactory + a real PostgreSQL test database — locally installed/reachable Postgres with a dedicated PIT961 integration-test database and automated reset/cleanup between runs; CI-native Postgres provisioning (e.g. GitHub Actions `services:`). No Docker/Testcontainers dependency. See §3 Infrastructure and WP-2 in the execution plan for the full local/CI model.

---

## Database

- **Database:** PostgreSQL 15+
- **Provider:** Npgsql
- **Primary ORM/Data Access:** EF Core 8
- **Optimized Queries / Reporting:** Dapper may be used for complex dashboard/reporting queries where it materially improves clarity or performance
- **Migrations:** EF Core migrations

Do not add a generic repository layer merely for abstraction.

Business logic belongs in explicit application/domain services, not in controllers.

---

## Infrastructure

**The hosting provider decision is deferred and is NOT a Phase 1 blocker.**

Foundation work (Phase 1) must proceed without assuming any specific host. Keep infrastructure and deployment provider-neutral wherever practical:

- **Containerization is deferred, not built, in Phase 1** (Owner decision, 2026-08-27 — see the Amendment Log in `13_phase1_execution_plan.md`). Local development runs directly on the toolchain (`dotnet run`/`dotnet watch` for backend, `npm run dev` for frontend) — no Docker, docker-compose, or Dockerfile is a Phase 1 dependency or completion gate. The project must nonetheless stay **container-friendly for later**: no architectural choice may make adding Docker harder afterward — in practice this means keeping all configuration environment-variable-driven (next bullet) and keeping the application itself container-compatible (no local-filesystem/host-specific assumptions baked into app code). Docker itself can be reconsidered before staging/production if it provides concrete benefit at that time.
- Environment-variable-based configuration for all connection strings, secrets, and service endpoints.
- Portable SQL/ORM usage (standard PostgreSQL + EF Core 8) — avoid provider-specific database extensions unless justified.

Candidate options previously discussed (not commitments — the final choice is part of the hosting recommendation below):

- **Frontend Hosting:** Vercel, Cloudflare Pages, Azure Static Web Apps, or similar
- **Backend Hosting:** Azure App Service, Railway, Render, Fly.io, container hosting, or similar
- **Database Hosting:** Supabase PostgreSQL, Neon, Azure Database for PostgreSQL, Railway PostgreSQL, or similar
- **File Storage:** Cloudflare R2, AWS S3, or similar
- **CDN:** Cloudflare or similar
- **Caching:** Redis when justified
- **Background Job Storage:** PostgreSQL or Redis-backed Hangfire depending on deployment decision

Decided regardless of hosting provider:

- **Error Monitoring:** Sentry
- **Product Analytics:** PostHog
- **CI/CD:** GitHub Actions
- **Containerization:** deferred entirely from Phase 1 (Owner decision, 2026-08-27). Not a Phase 1 requirement or gate. The stack stays container-friendly (env-var-based config, no host-specific coupling) so Docker can be added later without rework, but no Docker implementation work is done in Phase 1, and no alternative container/orchestration technology is substituted in its place.

**Before staging deployment**, the Technical Architect must present a recommended hosting architecture to the owner, covering:

- Frontend hosting
- Backend/API hosting
- Database hosting
- File storage
- Backup strategy
- Secrets management
- CI/CD pipeline
- Approximate monthly cost
- Scalability headroom
- Lebanon/MENA latency considerations

Nothing gets deployed to staging or production until the owner reviews and approves that recommendation.

Only the hosting *provider* selection is deferred — the technology stack itself (React/TypeScript/Vite, ASP.NET Core 8, PostgreSQL, EF Core 8, etc., as specified above) is decided and unchanged.

Product branding (display name, email sender name, logo, JWT issuer/audience) must be configuration-driven, never hardcoded — see §7A Branding & Configuration.

---

# 4. Solution Structure

Recommended structure:

```text
garageos/
│
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   ├── assets/
│   │   ├── components/
│   │   ├── features/
│   │   ├── hooks/
│   │   ├── layouts/
│   │   ├── lib/
│   │   ├── pages/
│   │   ├── services/
│   │   ├── stores/
│   │   ├── types/
│   │   └── validation/
│   ├── tests/
│   └── package.json
│
├── backend/
│   ├── GarageOS.Api/
│   ├── GarageOS.Application/
│   ├── GarageOS.Domain/
│   ├── GarageOS.Infrastructure/
│   └── GarageOS.Tests/
│
├── docs/
│   ├── IMPLEMENTATION_MAP.md
│   ├── PROGRESS.md
│   ├── DECISIONS.md
│   ├── KNOWN_ISSUES.md
│   ├── TEST_STATUS.md
│   └── DESIGN_IMPLEMENTATION_DIFFERENCES.md
│
├── .github/
└── README.md
```

Note: no `docker/` directory in Phase 1 — containerization is deferred (see §3 Infrastructure).

Do not split the solution into unnecessary projects.

The goal is clean modular architecture, not architectural ceremony.

---

# 5. Team / Agent Execution

The implementation should use the existing Claude team.

The lead / dispatcher is responsible for coordination.

Suggested responsibilities:

## Lead / Dispatcher

- Own implementation sequencing
- Assign work
- Resolve conflicts
- Maintain progress documents
- Enforce design fidelity
- Enforce architecture consistency
- Review agent output
- Prevent duplicate or incompatible implementations

## Frontend Agent

- React UI
- Approved design implementation
- Responsive behavior
- Mobile mechanic UX
- RTL readiness
- Loading states
- Empty states
- Error states
- Accessibility
- SignalR client integration

## Backend Agent

- ASP.NET Core APIs
- Application services
- Authorization
- Multi-tenancy
- Financial logic
- Job workflow
- SignalR hubs
- Background jobs
- Integrations

## Database / Data Agent

- PostgreSQL schema
- EF Core entities/configuration
- Migrations
- Indexing
- Query performance
- Financial integrity
- Tenant isolation
- Seed data

## QA Agent

- Unit tests
- Integration tests
- E2E tests
- Tenant isolation tests
- Role/permission tests
- Financial tests
- Workflow tests
- Regression tests

## Security Agent

- Authentication
- Authorization
- IDOR
- Cross-tenant isolation
- Input validation
- File security
- Admin separation
- Sensitive field exposure

## Design Review Agent

- Compare implementation to approved Claude Design output
- Report visual/interaction mismatches
- Review desktop/mobile/RTL behavior
- Confirm design system consistency

A feature is not complete because one agent says "done."

It is complete only after implementation, testing, security review where relevant, and design review.

---

# 6. Multi-Tenancy Architecture

GarageOS uses **row-level tenant isolation**.

It does not use:

- Database-per-garage
- Schema-per-garage

Every business entity contains:

```sql
garage_id UUID NOT NULL
```

Examples:

- customers
- vehicles
- jobs
- estimates
- invoices
- payments
- expenses
- suppliers
- inventory
- attachments

The authenticated user determines the active garage.

The API must never trust a `garage_id` supplied by the frontend for authorization.

---

## Tenant Resolution

JWT claims should contain:

- `sub` = user id
- `garage_id`
- role
- token/session identifiers as needed

A scoped tenant service should expose:

```csharp
public interface ICurrentTenant
{
    Guid GarageId { get; }
    Guid UserId { get; }
    string Role { get; }
}
```

Business services obtain tenant identity from the authenticated context.

---

## EF Core Tenant Enforcement

Global query filters may be used for tenant-owned entities, but they are **not sufficient by themselves**.

All writes and sensitive lookups must still verify tenant ownership explicitly.

Example concept:

```csharp
builder.Entity<Job>()
    .HasQueryFilter(x => x.GarageId == _currentTenant.GarageId && x.DeletedAt == null);
```

The team must test cross-tenant access intentionally.

Example:

Garage A user requests:

```http
GET /api/jobs/{GarageBJobId}
```

Expected:

- 404 or 403 according to the agreed API security convention
- Never Garage B data

---

## Accounts & Future Multi-Garage Readiness

Phase 1 product experience is **one paid subscription per garage** — there is no chain/multi-branch UI in Phase 1, and none should be designed or built.

The schema introduces a thin `accounts` table (billing/ownership entity) one level above `garages` (see §9) so future multi-garage ownership does not require a breaking migration:

- `accounts` holds billing identity: Stripe customer, subscription status/plan, trial dates.
- `garages.account_id` links every garage to its owning account.
- For Phase 1, an account has **exactly one garage**. This is enforced at the **application layer** (reject creating a second garage under an account that already has one) — intentionally *not* a hard database constraint. Adding multi-garage support later is therefore purely additive (allow more garages per account), never a schema migration.

This resolves the "a garage chain will hit a wall" gap flagged in the prior product review, without adding any Phase 1 UI or workflow for chains.

**Row-level tenant isolation is unchanged.** `garage_id` remains the isolation key on every business table (customers, vehicles, jobs, invoices, etc. — see the list above). `account_id` is purely an ownership/billing grouping one level above `garage_id`; it is never used as a tenant-isolation key, and it must not weaken or complicate the tenant-isolation model described in this section.

---

# 7. Garage Configuration

Each garage needs configurable settings.

Recommended settings include:

```json
{
  "currency": "USD",
  "timezone": "Asia/Beirut",
  "taxRate": 0,
  "taxLabel": "",
  "defaultLaborRate": 20,
  "invoicePrefix": "INV-",
  "workingHours": {
    "open": "08:00",
    "close": "18:00"
  },
  "diagnosisFeePolicy": "waived_if_repaired",
  "diagnosisFeeAmount": 50,
  "warrantyPeriodDays": 30,
  "warrantyMileageKm": 1000,
  "allowDeliveryWithBalance": true
}
```

Reserved for a future dual-currency/display-rate feature (unused in Phase 1 business logic): add a nullable `display_currency TEXT NULL` field to garage settings. This exists purely so a future Lebanon-market hyperinflation/dual-currency display feature does not require a breaking migration.

Prefer a typed settings model/table over storing important business behavior only in unstructured JSON.

JSONB may still be used for optional flexible settings.

---

# 7A. Branding & Configuration

**Branding must not be architecturally load-bearing.** The final customer-facing product/brand
name is undecided (see `DECISIONS.md`). `GarageOS` and `PIT961` are internal codenames only —
neither is an approved customer-facing brand. This section exists so that whenever the real brand
is chosen, adopting it is a configuration change, never a code change.

This is a **light-touch configurability requirement**, not a white-labeling subsystem. Do not build
multi-brand theming, per-garage brand overrides, or an admin brand editor for Phase 1 — the only
obligation is to avoid hardcoding the one current brand assumption in the handful of places below.

## Requirements

- **Product display name** must be configurable (e.g. `Branding:ProductDisplayName` in application
  configuration) and read from configuration everywhere it appears in customer-facing surfaces —
  UI copy, email templates, invoice/estimate templates. Never hardcode it as a string literal in
  component code or template code.
- **Email "From" display name** must be configurable via `Branding:EmailFromName` (consumed by
  `IEmailService`, see §11A). Never hardcode a brand string as the sender name.
- **JWT `Issuer` and `Audience`** must come from configuration (`Jwt:Issuer`, `Jwt:Audience`),
  never hardcoded to any candidate brand string. Treat them as stable technical identifiers,
  independent of the eventual marketing name — they exist for token validation, not branding.
  Changing the product's marketing brand later must never require re-issuing or invalidating
  existing tokens, and must never require a code change.
- **Logos / branding image assets** must be stored as replaceable static assets referenced by a
  configurable path/URL (e.g. `Branding:LogoUrl`), not embedded or hardcoded into component code.
- **Customer-facing copy** (emails, UI strings, document templates) must not be tightly coupled to
  internal identifiers. Internal C#/TypeScript namespaces, the solution name, the npm package name,
  and the repo/codebase name may freely and **permanently** use `PIT961` or `GarageOS` as an
  internal codename — that is intentional and does not need to be renamed later, since customers
  never see it.

## Explicitly out of scope for Phase 1

No multi-brand/white-label system, no tenant-level brand theming, no brand configuration UI/admin
screen. Just don't hardcode the one brand assumption in the five items above.

---

# 8. Core Domain Entities

At minimum:

- Account
- Garage
- GarageSettings
- Subscription
- User
- Role / Permission
- Customer
- Vehicle
- VehicleOwnershipHistory
- Job
- JobStatusHistory
- RepairTask
- Diagnosis
- Recommendation
- Estimate
- EstimateRevision
- EstimateItem
- CustomerApproval
- JobPart
- Supplier
- InventoryItem
- InventoryMovement
- Invoice
- InvoiceItem
- Payment
- Refund / Reversal
- Expense
- ExpenseCategory
- Attachment
- Appointment
- QualityControl
- QualityControlItem
- Notification
- AuditLog
- RefreshToken

---

# 9. Core PostgreSQL Schema

The following schema is conceptual and should be translated into EF Core entities/configurations and migrations.

## accounts

Billing/ownership entity, one level above `garages`. See §6 for the multi-garage readiness rationale.

```sql
CREATE TABLE accounts (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    billing_email TEXT NOT NULL,
    stripe_customer_id TEXT,
    subscription_status TEXT NOT NULL DEFAULT 'trial',
    plan TEXT NOT NULL DEFAULT 'pro',
    trial_ends_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

Subscription statuses:

```text
trial
active
past_due
suspended
cancelled
expired
```

Phase 1: an account has exactly one garage, enforced at the application layer (reject creating a second garage under an account that already has one) — not a database constraint. This keeps future multi-garage support additive rather than a migration.

---

## garages

```sql
CREATE TABLE garages (
    id UUID PRIMARY KEY,
    account_id UUID NOT NULL REFERENCES accounts(id),
    name TEXT NOT NULL,
    phone TEXT,
    address TEXT,
    logo_url TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX garages_account_idx ON garages(account_id);
```

Billing/subscription/trial fields (`subscription_status`, `plan`, `trial_ends_at`, `stripe_customer_id`) live on `accounts`, not `garages` — billing attaches to the paying account, not to one specific garage location. See `accounts` above.

---

## users

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    email TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    name TEXT NOT NULL,
    role TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    avatar_url TEXT,
    last_login TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX users_email_idx ON users(email);
CREATE INDEX users_garage_idx ON users(garage_id);
```

Initial roles:

- owner
- manager
- advisor
- mechanic
- accountant

The architecture should permit granular permissions later.

---

## customers

```sql
CREATE TABLE customers (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    first_name TEXT NOT NULL,
    last_name TEXT,
    phone TEXT NOT NULL,
    whatsapp TEXT,
    email TEXT,
    is_fleet BOOLEAN NOT NULL DEFAULT FALSE,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX customers_garage_idx ON customers(garage_id);
CREATE INDEX customers_phone_idx ON customers(garage_id, phone);
```

---

## vehicles

```sql
CREATE TABLE vehicles (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    customer_id UUID NOT NULL REFERENCES customers(id),
    plate_number TEXT NOT NULL,
    plate_country TEXT NOT NULL DEFAULT 'LB',
    make TEXT NOT NULL,
    model TEXT NOT NULL,
    year INT,
    color TEXT,
    vin TEXT,
    engine TEXT,
    engine_code TEXT,
    transmission TEXT,
    drivetrain TEXT,
    fuel_type TEXT,
    current_mileage INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX vehicles_plate_idx
ON vehicles(garage_id, plate_number, plate_country);

CREATE INDEX vehicles_customer_idx ON vehicles(customer_id);
CREATE INDEX vehicles_vin_idx ON vehicles(garage_id, vin);
```

---

## jobs

```sql
CREATE TABLE jobs (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    job_number TEXT NOT NULL,
    customer_id UUID NOT NULL REFERENCES customers(id),
    vehicle_id UUID NOT NULL REFERENCES vehicles(id),

    primary_mechanic_id UUID REFERENCES users(id),
    secondary_mechanic_id UUID REFERENCES users(id),
    created_by UUID NOT NULL REFERENCES users(id),

    status TEXT NOT NULL DEFAULT 'checked_in',

    mileage_at_intake INT,
    customer_complaint TEXT,
    advisor_notes TEXT,

    promised_at TIMESTAMPTZ,
    customer_waiting BOOLEAN NOT NULL DEFAULT FALSE,

    source TEXT NOT NULL DEFAULT 'walk_in',
    overnight BOOLEAN NOT NULL DEFAULT FALSE,
    overnight_note TEXT,

    is_warranty_return BOOLEAN NOT NULL DEFAULT FALSE,
    parent_job_id UUID REFERENCES jobs(id),

    cancellation_reason TEXT,
    cancelled_at TIMESTAMPTZ,
    cancelled_by UUID REFERENCES users(id),

    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id),
    deletion_reason TEXT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX jobs_number_idx
ON jobs(garage_id, job_number);

CREATE INDEX jobs_garage_status_idx
ON jobs(garage_id, status)
WHERE deleted_at IS NULL;

CREATE INDEX jobs_vehicle_idx ON jobs(vehicle_id);
CREATE INDEX jobs_mechanic_idx ON jobs(primary_mechanic_id);
```

Approved workflow statuses:

```text
checked_in
diagnosing
waiting_approval
waiting_parts
ready_to_repair
repairing
qc
ready
delivered
cancelled
```

Note: `ready_to_repair` is a transient sub-status displayed within the **Waiting Parts** column of the approved 8-column workshop board (Checked In / Diagnosing / Waiting Approval / Waiting Parts / Repairing / QC / Ready / Delivered) — it is not a 9th board column. See §35 for the state machine treatment.

---

## repair_tasks

```sql
CREATE TABLE repair_tasks (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    job_id UUID NOT NULL REFERENCES jobs(id),
    name TEXT NOT NULL,
    description TEXT,
    assigned_mechanic_id UUID REFERENCES users(id),
    status TEXT NOT NULL DEFAULT 'pending',

    outsourced BOOLEAN NOT NULL DEFAULT FALSE,
    outsource_supplier TEXT,
    outsource_cost NUMERIC(12,2),
    outsource_billed NUMERIC(12,2),
    outsource_sent_at TIMESTAMPTZ,
    outsource_returned_at TIMESTAMPTZ,

    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    sort_order INT NOT NULL DEFAULT 0,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

Task statuses:

```text
pending
in_progress
paused
completed
cancelled
```

---

## estimates

```sql
CREATE TABLE estimates (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    job_id UUID NOT NULL REFERENCES jobs(id),

    type TEXT NOT NULL DEFAULT 'standard',
    parent_estimate_id UUID REFERENCES estimates(id),
    revision_number INT NOT NULL DEFAULT 1,

    status TEXT NOT NULL DEFAULT 'draft',

    approval_method TEXT,
    approved_by_name TEXT,
    approved_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,

    subtotal NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    total NUMERIC(12,2) NOT NULL DEFAULT 0,

    notes TEXT,
    created_by UUID REFERENCES users(id),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

Statuses:

```text
draft
sent
approved
partially_approved
rejected
superseded
```

---

## estimate_items

```sql
CREATE TABLE estimate_items (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    estimate_id UUID NOT NULL REFERENCES estimates(id),

    type TEXT NOT NULL,
    description TEXT NOT NULL,
    part_number TEXT,

    quantity NUMERIC(12,3) NOT NULL DEFAULT 1,
    unit_cost NUMERIC(12,2) NOT NULL DEFAULT 0,
    unit_price NUMERIC(12,2) NOT NULL DEFAULT 0,

    approval_status TEXT NOT NULL DEFAULT 'pending',
    sort_order INT NOT NULL DEFAULT 0
);
```

Types:

```text
part
labor
service
misc
```

Approval statuses:

```text
pending
approved
rejected
```

Financial totals should be calculated in backend business logic and validated before persistence.

Do not rely on values supplied by the client.

---

## job_parts

```sql
CREATE TABLE job_parts (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    job_id UUID NOT NULL REFERENCES jobs(id),

    name TEXT NOT NULL,
    part_number TEXT,
    supplier_id UUID,
    supplier_name_snapshot TEXT,

    quantity NUMERIC(12,3) NOT NULL DEFAULT 1,
    unit_cost NUMERIC(12,2) NOT NULL DEFAULT 0,
    unit_price NUMERIC(12,2) NOT NULL DEFAULT 0,

    supplied_by TEXT NOT NULL DEFAULT 'garage',
    status TEXT NOT NULL DEFAULT 'needed',

    ordered_at TIMESTAMPTZ,
    expected_at TIMESTAMPTZ,
    arrived_at TIMESTAMPTZ,
    installed_at TIMESTAMPTZ,

    return_date DATE,
    return_reason TEXT,
    issue_note TEXT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

`supplier_id` is intentionally unconstrained (no FK) until the V1.1 `suppliers` table ships (§66). Migration plan: once `suppliers` exists, add `REFERENCES suppliers(id)` via an EF Core migration and backfill/validate existing values. This is a known, deliberate gap, not a bug.

Part statuses:

```text
needed
searching
ordered
arrived
installed
returned
issue_wrong_part
issue_damaged
```

---

## invoices

```sql
CREATE TABLE invoices (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    job_id UUID NOT NULL REFERENCES jobs(id),

    invoice_number TEXT NOT NULL,

    status TEXT NOT NULL DEFAULT 'unpaid',

    subtotal NUMERIC(12,2) NOT NULL,
    tax_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    total NUMERIC(12,2) NOT NULL,

    total_paid NUMERIC(12,2) NOT NULL DEFAULT 0,

    issued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    voided_at TIMESTAMPTZ,
    voided_by UUID REFERENCES users(id),
    void_reason TEXT,

    created_by UUID REFERENCES users(id),

    display_rate_snapshot NUMERIC(12,4),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX invoices_number_idx
ON invoices(garage_id, invoice_number);
```

`display_rate_snapshot` is reserved for a future dual-currency/display-rate feature and is unused in Phase 1 business logic — it exists purely so a future Lebanon-market hyperinflation/dual-currency feature does not require a breaking migration.

Statuses:

```text
unpaid
partial
paid
voided
written_off
```

Balance:

```text
total - total_paid
```

The backend may expose balance as a computed DTO value instead of storing it physically.

---

## payments

```sql
CREATE TABLE payments (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    invoice_id UUID NOT NULL REFERENCES invoices(id),

    amount NUMERIC(12,2) NOT NULL,
    method TEXT NOT NULL,

    reference TEXT,
    notes TEXT,

    idempotency_key UUID NOT NULL,

    recorded_by UUID REFERENCES users(id),
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX payments_idempotency_idx
ON payments(garage_id, idempotency_key);

CREATE INDEX payments_invoice_idx
ON payments(invoice_id);
```

Methods:

```text
cash
card
bank_transfer
cheque
other
```

Payments must be immutable financial records.

Corrections are performed through reversal/refund records, not by rewriting old transactions.

---

## job_history

```sql
CREATE TABLE job_history (
    id UUID PRIMARY KEY,
    garage_id UUID NOT NULL REFERENCES garages(id),
    job_id UUID NOT NULL REFERENCES jobs(id),

    actor_id UUID REFERENCES users(id),
    actor_name TEXT NOT NULL,
    actor_role TEXT NOT NULL,

    event_type TEXT NOT NULL,
    summary TEXT NOT NULL,
    detail JSONB,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX job_history_job_idx
ON job_history(job_id, created_at);
```

Example events:

```text
job_created
status_changed
mechanic_assigned
estimate_created
estimate_sent
estimate_approved
estimate_rejected
part_added
part_ordered
part_arrived
part_installed
part_returned
task_started
task_paused
task_completed
task_corrected
qc_passed
qc_failed
invoice_created
payment_recorded
payment_reversed
invoice_voided
job_cancelled
job_deleted
message_sent
vehicle_delivered
```

---

# 10. API Conventions

Base path:

```text
/api
```

API versioning should be supported from the beginning.

Example:

```text
/api/v1/customers
```

Controllers remain thin.

Business logic belongs in application services.

Use DTOs.

Never expose EF Core entities directly.

All list endpoints should support appropriate:

- Pagination
- Search
- Filtering
- Sorting

Use consistent error responses.

Recommended problem format:

```json
{
  "type": "validation_error",
  "title": "Validation failed",
  "status": 400,
  "traceId": "...",
  "errors": {
    "phone": ["Phone is required."]
  }
}
```

ASP.NET Core `ProblemDetails` should be used where practical.

---

# 11. Authentication API

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password
GET  /api/v1/auth/me
```

These endpoints issue tokens for garage-tenant users only. Platform admin authentication is a structurally separate flow with its own distinct JWT claim — see §60.

Access token:

- Short-lived
- Approximately 15 minutes

Refresh token:

- Longer-lived
- Rotated
- Revocable
- Stored securely
- Prefer httpOnly secure cookie for web SPA

Refresh tokens should be persisted in a dedicated table with:

- token hash
- user id
- created date
- expiry
- revoked date
- replacement token id
- device/session information if useful

Do not store raw refresh tokens in the database.

---

# 11A. Email Service

## Provider

**Resend** is the approved Phase 1 email provider.

## Abstraction

Application code must depend only on an `IEmailService` abstraction. No controller, command/query
handler, application service, or domain code may reference Resend types or the Resend SDK directly
— only a concrete `ResendEmailService : IEmailService` implementation is permitted to do so.

```csharp
public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendTransactionalAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
    // Email verification is optional/TBD — see "Email Verification" below.
}

public class ResendEmailService : IEmailService
{
    // The only class permitted to reference the Resend SDK/API directly.
}
```

## Phase 1 capabilities

- **Password reset** — backs `POST /api/v1/auth/forgot-password` / `POST /api/v1/auth/reset-password` (§11).
- **General account-related transactional email** — e.g. team invite, account status changes
  (trial ending, subscription past due — see §44 / §45).
- **Email verification** — not currently a specified requirement anywhere in this document.
  §11's Authentication API lists no `register` endpoint (account/garage registration is
  described at a product level in §44, not as an API contract), and nothing here requires
  verifying the email address on signup. If the approved registration flow is later designed
  to require email verification, add a `SendEmailVerificationAsync` method to `IEmailService`
  at that time. Until then, treat email verification as **optional/TBD** as part of
  registration flow design — not a hard Phase 1 requirement on its own.

## Configuration

- The Resend API key is a secret. Manage it via the existing secrets-management approach already
  documented — environment-variable-based configuration (§3 Infrastructure) — never hardcoded.
- The "From" display name comes from `Branding:EmailFromName` (§7A), never hardcoded to a brand string.

## Explicitly out of scope for Phase 1

SMS and WhatsApp are **not** part of Phase 1 email scope and must not block anything here. Those
providers are chosen later, specifically when the WhatsApp/notification features already scoped to
**Phase 5 — Communication** (§68) are actually implemented.

---

# 12. Garage API

```text
GET  /api/v1/garage
PUT  /api/v1/garage
POST /api/v1/garage/setup
GET  /api/v1/garage/settings
PUT  /api/v1/garage/settings
```

Sensitive settings:

Owner only unless explicitly permitted.

---

# 13. Users & Team API

```text
GET  /api/v1/users
POST /api/v1/users/invite
PUT  /api/v1/users/{id}
PUT  /api/v1/users/{id}/role
POST /api/v1/users/{id}/deactivate
POST /api/v1/users/{id}/reactivate
```

Owner manages role assignments by default.

---

# 14. Customers API

```text
GET  /api/v1/customers
POST /api/v1/customers
GET  /api/v1/customers/{id}
PUT  /api/v1/customers/{id}

GET  /api/v1/customers/{id}/vehicles
GET  /api/v1/customers/{id}/jobs
GET  /api/v1/customers/{id}/invoices
GET  /api/v1/customers/{id}/payments
GET  /api/v1/customers/{id}/balance
```

Search should support:

- name
- phone
- WhatsApp
- plate
- VIN

---

# 15. Vehicles API

```text
GET  /api/v1/vehicles
POST /api/v1/vehicles
GET  /api/v1/vehicles/{id}
PUT  /api/v1/vehicles/{id}

GET  /api/v1/vehicles/{id}/history
GET  /api/v1/vehicles/{id}/recommendations

POST /api/v1/vehicles/{id}/transfer
```

Vehicle history must be derived from preserved service/job records.

---

# 16. Jobs API

```text
GET  /api/v1/jobs
POST /api/v1/jobs
GET  /api/v1/jobs/{id}

PUT  /api/v1/jobs/{id}/status
PUT  /api/v1/jobs/{id}/mechanics

POST /api/v1/jobs/{id}/cancel
DELETE /api/v1/jobs/{id}

GET  /api/v1/jobs/{id}/history
```

Deletion means soft deletion and requires a reason.

Financial/historical records must not be cascade-deleted from a soft-deleted job.

---

# 17. Diagnosis API

```text
GET  /api/v1/jobs/{id}/diagnosis
POST /api/v1/jobs/{id}/diagnosis
PUT  /api/v1/jobs/{id}/diagnosis/{diagnosisId}

GET  /api/v1/jobs/{id}/recommendations
POST /api/v1/jobs/{id}/recommendations
```

Support:

- customer complaint
- diagnosis
- recommended repair
- internal note
- customer-visible explanation
- diagnostic codes
- attachments

---

# 18. Repair Tasks API

```text
GET  /api/v1/jobs/{id}/tasks
POST /api/v1/jobs/{id}/tasks

PUT  /api/v1/tasks/{id}
POST /api/v1/tasks/{id}/start
POST /api/v1/tasks/{id}/pause
POST /api/v1/tasks/{id}/resume
POST /api/v1/tasks/{id}/complete
POST /api/v1/tasks/{id}/correct
```

Task time tracking should preserve individual work intervals when enabled.

---

# 19. Estimate API

```text
GET  /api/v1/jobs/{id}/estimates
POST /api/v1/jobs/{id}/estimates

GET  /api/v1/estimates/{id}
PUT  /api/v1/estimates/{id}

POST /api/v1/estimates/{id}/send
POST /api/v1/estimates/{id}/approve
POST /api/v1/estimates/{id}/reject
POST /api/v1/estimates/{id}/revision
```

Estimate approvals must preserve exactly what was approved.

Do not overwrite historical approvals.

---

# 20. Parts API

```text
GET  /api/v1/jobs/{id}/parts
POST /api/v1/jobs/{id}/parts

PUT  /api/v1/parts/{id}
PUT  /api/v1/parts/{id}/status

POST /api/v1/parts/{id}/flag
POST /api/v1/parts/{id}/return
```

Technicians must not receive cost/margin fields unless permission explicitly allows it.

---

# 21. Quality Control API

```text
GET  /api/v1/jobs/{id}/qc
POST /api/v1/jobs/{id}/qc
POST /api/v1/jobs/{id}/qc/pass
POST /api/v1/jobs/{id}/qc/fail
```

Store:

- checklist
- technician/reviewer
- timestamp
- notes
- photos
- fail reason

---

# 22. Invoice API

```text
POST /api/v1/jobs/{id}/invoice
GET  /api/v1/jobs/{id}/invoice

GET  /api/v1/invoices
GET  /api/v1/invoices/{id}

POST /api/v1/invoices/{id}/void
```

Invoices should snapshot relevant descriptive data so historical invoices remain understandable even if master records later change.

---

# 23. Payment API

```text
GET  /api/v1/invoices/{id}/payments
POST /api/v1/invoices/{id}/payments

POST /api/v1/payments/{id}/reverse
```

Payment recording must be idempotent.

The client generates an idempotency UUID.

Backend behavior:

1. Begin DB transaction.
2. Check `(garage_id, idempotency_key)`.
3. If already present, return existing payment.
4. Insert payment.
5. Recalculate invoice paid amount from payment ledger.
6. Update invoice state.
7. Write audit record.
8. Commit transaction.
9. Publish realtime update.

---

# 24. Finance API

Owner / accountant by default.

```text
GET  /api/v1/finance/invoices
GET  /api/v1/finance/payments
GET  /api/v1/finance/debts

GET  /api/v1/finance/expenses
POST /api/v1/finance/expenses
PUT  /api/v1/finance/expenses/{id}

GET  /api/v1/finance/reports/revenue
GET  /api/v1/finance/reports/gross-profit
GET  /api/v1/finance/reports/receivables
GET  /api/v1/finance/reports/technicians
```

Do not label a report "Net Profit" unless the calculation genuinely includes the required operating expenses and accounting rules.

---

# 25. Workshop Board API

```text
GET /api/v1/board
```

Return only active jobs necessary for the board.

Provide optimized DTOs.

Do not return the full job aggregate for each card.

Recommended board card data:

- job id
- job number
- vehicle make/model/year
- plate
- customer name
- status
- priority
- primary mechanic
- promised time
- waiting customer
- parts status summary
- approval state
- payment state
- overdue flag

Target:

**<300 ms typical response**

---

# 26. Real-Time Updates — SignalR

Use ASP.NET Core SignalR.

Example hub:

```text
/hubs/garage
```

On authenticated connection:

- Resolve garage from JWT
- Join only the authenticated garage group
- Never accept arbitrary garage group identifiers from the client

Group:

```text
garage:{garageId}
```

Events:

```text
JobCreated
JobStatusChanged
MechanicAssigned
EstimateSent
EstimateApproved
PartArrived
PartInstalled
QcPassed
QcFailed
InvoiceCreated
PaymentRecorded
VehicleReady
```

Frontend should update TanStack Query caches appropriately.

Avoid unnecessary full-page refreshes.

---

# 27. Background Jobs — Hangfire

Use Hangfire for scheduled/background processing.

Initial jobs:

| Job | Schedule | Purpose |
|---|---|---|
| NotifyOverdueJobs | Every 30 min | Alert owner/manager about overdue active jobs |
| NotifyWaitingApproval | Every 4 hours | Alert advisor when sent estimate has no response |
| DetectOvernightJobs | Garage closing time | Flag active jobs remaining after closing |
| SendDailySummary | Daily | Owner garage summary |
| TrialExpiryWarning | Daily | Notify owner before trial expires |
| ServiceReminder | Daily | Future maintenance reminders |
| PaymentReminder | Daily | Future overdue receivable reminders |

All jobs must operate per tenant/garage timezone where relevant.

`Asia/Beirut` must be supported correctly.

---

# 28. Authorization

Authorization checks two independent dimensions:

1. **Tenant ownership**
2. **User permission**

## Architecture: Requirements + Handlers

Authorization logic must be implemented with **ASP.NET Core's policy-based authorization
framework** — custom `IAuthorizationRequirement` implementations paired with
`AuthorizationHandler<TRequirement>` classes, registered as named policies. Do not scatter
`if (role == "Manager")`-style checks inline across controllers and services.

Shape:

```csharp
public class DiscountLimitRequirement : IAuthorizationRequirement
{
    public decimal MaxPercentForNonOwner { get; } = 15m;
}

public class DiscountAuthorizationContext
{
    public decimal DiscountPercent { get; init; }
    public string ActorRole { get; init; } = default!;
}

public class DiscountLimitHandler
    : AuthorizationHandler<DiscountLimitRequirement, DiscountAuthorizationContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DiscountLimitRequirement requirement,
        DiscountAuthorizationContext resource)
    {
        if (resource.ActorRole == "Owner" || resource.DiscountPercent <= requirement.MaxPercentForNonOwner)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
```

Invocation:

- Simple role/permission-only checks: `[Authorize(Policy = "Jobs.View")]` etc. on controller actions.
- Contextual/resource-based checks (discount amount, estimate total, job assignment, tenant
  boundary): explicit `IAuthorizationService.AuthorizeAsync(user, resource, policyName)` calls
  inside application services, where `resource` carries the data the handler needs to decide.

Register policies and handlers in DI (`AddAuthorization(...)`, `AddScoped<IAuthorizationHandler, ...>`)
at the composition root, not ad hoc per feature.

## What the model must be able to express

Without redesigning this framework later, it must be able to express:

- **Role membership** (Owner/Manager/Advisor/Mechanic/Accountant)
- **Granular permissions** beyond role (e.g. `Jobs.View`, `Invoices.Void`, `Finance.ViewCosts`)
- **Contextual business rules** (time-of-day, job status, and similar situational conditions)
- **Amount-based numeric limits** (discount caps, approval thresholds)
- **Tenant-boundary checks** (`garage_id` match — §6; also the platform-admin/garage-tenant
  mutual exclusion in §60, see below)
- **Resource/ownership-based checks** (e.g. "is this the mechanic assigned to this job" — §29)

This is a requirement on the *shape* of the framework, not an instruction to build all of the
above now — see Phase 1 scope below.

## Phase 1 Scope — only two concrete policies

Do not build a generic rules engine, a rules DSL, or an admin-configurable rule editor for Phase 1.
The goal is that adding the *next* contextual rule later means writing one new
`IAuthorizationRequirement` + one new `AuthorizationHandler`, not restructuring existing
authorization code. Keep it boring and extensible, not exhaustive.

Phase 1 implements exactly these two policies, each as its own handler:

1. **Manager discount cap — `DiscountLimitHandler`.** A discount above **15%** is rejected unless
   the actor is Owner. Resource context: discount percentage/amount + actor role.
2. **Estimate approval threshold — `EstimateApprovalThresholdHandler`.** Any role creating/sending
   an estimate above **$500** routes it to "Pending Owner Approval" instead of sending directly
   (this workflow rule was already documented in the permission matrix; this section specifies its
   enforcement mechanism). Resource context: estimate total + actor role.

Both handlers take the resource (amount + actor role) as their authorization resource, per the
shape shown above.

## Role Summary

### Owner

Full garage access.

### Manager

Operational access, configurable financial access.

### Advisor

Customers, vehicles, check-in, jobs, estimates, customer communication, invoices/payments according to permissions.

### Mechanic

Assigned jobs, diagnosis, tasks, photos, part requests, job progress.

No sensitive purchase cost/margin by default.

### Accountant

Invoices, payments, expenses, receivables, supplier balances, financial reports.

## Policy Examples

```csharp
[Authorize(Policy = "Jobs.View")]
[Authorize(Policy = "Jobs.ChangeStatus")]
[Authorize(Policy = "Invoices.Void")]
[Authorize(Policy = "Finance.ViewCosts")]
```

Prefer permission policies over scattering hardcoded role comparisons throughout controllers.

Roles may map to permissions during MVP.

## Platform Admin / Garage Tenant Mutual Exclusion

This framework is also where the platform-admin/garage-tenant mutual-exclusion requirement (§60)
plugs in: a platform-admin claim has no `garage_id` claim to match, so it fails every garage-tenant
policy's tenant-boundary check *by construction* — no separate carve-out is needed, the
tenant-boundary handler simply never finds a `garage_id` to match against. Likewise, a
garage-tenant token must never satisfy a platform-admin policy (§60).

---

# 29. Mechanic Scope

For mechanic users:

Job queries should default to:

- primary assigned mechanic
- secondary assigned mechanic
- explicitly permitted workshop-wide views

The backend must enforce mechanic scope.

Do not rely on the frontend to hide other jobs.

---

# 30. Sensitive Field Visibility

Sensitive fields:

- unit cost
- total cost
- margin
- garage gross profit
- employee compensation
- sensitive financial reports

API DTOs should omit unauthorized fields entirely.

Do not send sensitive fields and merely hide them with CSS.

---

# 31. Authentication Security

Passwords:

- ASP.NET Core PasswordHasher or Argon2id/bcrypt-equivalent secure password hashing
- Never plaintext
- Never reversible encryption

JWT:

- Short-lived access token
- Strong signing keys
- Correct issuer/audience validation
- Clock skew minimized
- Token lifetime validation enabled

Refresh token:

- Secure
- Rotated
- Revocable
- Stored hashed server-side
- httpOnly Secure cookie for browser deployment where appropriate

Rate-limit:

- Login
- Forgot password
- Password reset
- Invitation acceptance
- Public approval links

ASP.NET Core rate limiting middleware should be configured.

---

# 32. Input Validation

Backend validation is authoritative.

Use FluentValidation.

Frontend Zod validation improves UX but does not replace backend validation.

Validate:

- IDs
- dates
- mileage
- money
- quantities
- status transitions
- file metadata
- phone numbers
- emails
- invoice/payment rules
- tenant ownership

---

# 33. File Uploads

Allowed initial file types:

- JPEG
- PNG
- WEBP
- PDF

Default max file size:

**10 MB**

Use pre-signed upload URLs when possible.

Storage key pattern:

```text
{garageId}/{entityType}/{entityId}/{randomFileId}.{extension}
```

Never use original filename as a trusted storage path.

Private files must not be publicly readable.

Serve through signed URLs with short expiry.

Persist attachment metadata in PostgreSQL.

---

# 34. File Security

Validate:

- MIME type
- allowed extension
- file size
- tenant ownership
- entity ownership

Consider malware scanning before enabling broader document uploads.

Never allow HTML/SVG/script content as ordinary image upload in MVP.

---

# 35. Job State Machine

Job statuses are not arbitrary strings.

Implement allowed transitions.

Example:

```text
checked_in
  ↓
diagnosing
  ↓
waiting_approval
  ↓
waiting_parts
  ↓
ready_to_repair
  ↓
repairing
  ↓
qc
  ↓
ready
  ↓
delivered
```

`ready_to_repair` is not a workshop board column. The approved board has 8 columns (Checked In / Diagnosing / Waiting Approval / Waiting Parts / Repairing / QC / Ready / Delivered). `ready_to_repair` is a transient sub-status shown within the **Waiting Parts** column, signaling parts have arrived and the job is ready to move into Repairing. Treat the diagram above as the underlying status enum's transition order, not as 9 distinct board columns.

Allow controlled exceptions.

Examples:

- diagnosing → cancelled
- waiting_approval → diagnosing
- waiting_parts → waiting_approval when supplemental approval is required
- qc → repairing when QC fails

Every transition must:

1. validate permission
2. validate tenant
3. validate current state
4. validate new state
5. update transactionally
6. write history
7. broadcast SignalR event

---

# 36. Job Number Generation

Job numbers are per garage.

Do not use:

```sql
MAX(job_number) + 1
```

without locking/sequence protection.

Concurrent check-ins could generate duplicate numbers.

Recommended approach:

Create a dedicated per-garage sequence table:

```sql
CREATE TABLE garage_sequences (
    garage_id UUID PRIMARY KEY REFERENCES garages(id),
    next_job_number BIGINT NOT NULL DEFAULT 1,
    next_invoice_number BIGINT NOT NULL DEFAULT 1
);
```

Inside a transaction:

```sql
UPDATE garage_sequences
SET next_job_number = next_job_number + 1
WHERE garage_id = @GarageId
RETURNING next_job_number - 1;
```

Format in application:

```text
47 → 047
```

---

# 37. Invoice Number Generation

Use the same concurrency-safe garage sequence.

Example:

```text
INV-2026-0047
```

Invoice format should be configurable.

The sequence itself must remain unique per garage.

Do not generate invoice numbers on the frontend.

---

# 38. Financial Integrity

Financial history is immutable.

Do not:

- delete payments
- silently edit historical payments
- silently modify approved estimate history
- overwrite issued invoice history
- delete financial data because a job was deleted

Corrections should use:

- reversal
- void
- refund
- new estimate revision
- credit adjustment when introduced

All financial operations should run in PostgreSQL transactions.

---

# 39. Payment Recording

Example:

Invoice:

```text
Total: $235
```

Payment 1:

```text
$150 cash
```

Persist:

```text
Payment A = $150
```

Invoice becomes:

```text
total_paid = $150
balance = $85
status = partial
```

Later:

```text
Payment B = $85
```

Persist:

```text
Payment A = $150
Payment B = $85
```

Invoice becomes:

```text
total_paid = $235
balance = $0
status = paid
```

Never mutate Payment A into $235.

---

# 40. Estimate Versioning

When an approved estimate must materially change:

Do not overwrite the original approved estimate.

Create a new revision or supplemental estimate.

Preserve:

- previous items
- previous prices
- previous approval
- approval timestamp
- approval method
- approver

This is important for disputes and auditability.

---

# 41. Overdue Detection

A job is overdue when:

```text
promised_at < current garage time
AND
status not in (delivered, cancelled)
```

Prefer computing this from UTC timestamps converted to the garage timezone for presentation/business scheduling.

Persist all timestamps in UTC.

---

# 42. Warranty Return Detection

When checking a vehicle in:

Search recent delivered jobs for the same vehicle.

If recent related work exists:

Display:

**Possible warranty return**

Do not automatically classify as warranty.

The advisor/manager must confirm.

Warranty configuration may include:

- warranty days
- warranty mileage
- category/part-specific policy later

---

# 43. Customer Fleet Detection

A customer may be suggested as a fleet customer once the number of active vehicles crosses a configurable threshold.

Do not make fleet classification irreversible.

Allow owner/manager override.

---

# 44. Subscription / Billing

Pricing (Phase 1, final):

**$30 USD/month per garage, billed monthly. One subscription per garage. No other tiers for Phase 1.**

Subscription/billing fields (`subscription_status`, `plan`, `trial_ends_at`, `stripe_customer_id`) live on the `accounts` table (§9), not on `garages` — billing attaches to the paying account. In Phase 1 an account has exactly one garage, so this is functionally identical to "one subscription per garage"; it simply leaves room for an account to own more than one garage later without a schema change (§6).

Support statuses:

```text
trial
active
past_due
suspended
cancelled
expired
```

Recommended initial trial:

**14 days**

Payment provider abstraction should permit Stripe initially without coupling the business layer directly to Stripe types.

---

## Stripe Flow

1. Account + garage register together (Phase 1: one garage per account).
2. Trial begins on the account.
3. No card required initially if approved business strategy remains unchanged.
4. Before expiry, warnings are sent.
5. Trial expiry causes soft lock on the account's garage.
6. Owner subscribes — **$30 USD/month**, billed monthly.
7. Stripe subscription activates the account (and its garage).
8. Webhooks update the account's subscription state.

Webhook events:

```text
checkout.session.completed
invoice.payment_succeeded
invoice.payment_failed
customer.subscription.updated
customer.subscription.deleted
```

Webhook signatures must be verified.

Webhook processing must be idempotent.

---

# 45. Trial / Subscription Enforcement

Use middleware/application policy on write operations.

Expired trial:

- User can log in
- User can view historical data
- New operational writes are blocked
- Subscription banner shown

Do not lock the user out of their historical data unnecessarily.

---

# 46. Dashboard

The owner dashboard must use real backend calculations.

Initial metrics:

- Cars Inside
- Received Today
- Delivered Today
- Open Jobs
- Waiting Approval
- Waiting Parts
- Overdue Jobs
- Revenue
- Payments Collected
- Outstanding Receivables
- Parts Cost
- Labor Revenue
- Estimated Gross Profit
- Expenses

Use Dapper for dashboard aggregations if it produces simpler/faster SQL than EF Core.

Do not load thousands of rows into memory to calculate dashboard totals.

---

# 47. Needs Attention

Backend should expose prioritized actionable items.

Examples:

- overdue job
- estimate waiting too long
- delayed part
- ready vehicle with unpaid balance
- large overdue customer balance
- QC failed
- job stalled

Example endpoint:

```text
GET /api/v1/dashboard/attention
```

---

# 48. Global Search

Endpoint:

```text
GET /api/v1/search?q=...
```

Search:

- customer name
- phone
- WhatsApp
- plate
- VIN
- vehicle
- job number
- invoice number
- part number / OEM number

Always scope by garage.

Return grouped lightweight results.

---

# 49. Frontend Architecture

Use feature-oriented organization.

Example:

```text
src/features/
  auth/
  dashboard/
  workshop/
  customers/
  vehicles/
  jobs/
  estimates/
  parts/
  qc/
  invoices/
  payments/
  finance/
  appointments/
  inventory/
  suppliers/
  team/
  settings/
```

Avoid putting every component in one global `/components` directory.

Global components should be truly reusable.

---

# 50. TanStack Query

Use TanStack Query for server state.

Examples:

```text
customers
vehicle detail
job detail
board
estimate
invoice
payments
dashboard
```

Use invalidation and targeted cache updates.

SignalR events should update/invalidate appropriate query caches.

Do not duplicate backend server data unnecessarily in Zustand.

---

# 51. Zustand

Use Zustand only for appropriate client state such as:

- UI preferences
- temporary workshop board filters
- command palette state
- local app shell behavior
- non-server wizard state where needed

Do not use Zustand as a second database.

---

# 52. Forms

Use:

- React Hook Form
- Zod

Server validation errors should map back to form fields.

Money should never be calculated only in frontend logic.

Frontend calculations are previews.

Backend recalculates authoritative totals.

---

# 53. Responsive Design

Desktop primary users:

- Owner
- Advisor
- Accountant

Mobile primary users:

- Mechanic
- Owner

Do not simply shrink desktop layouts.

Mechanic mobile experience must provide:

- My Jobs
- Job Detail
- Start/Pause
- Diagnosis
- Photo
- Part Request
- Task Completion
- Send to QC

Large tap targets are required.

---

# 54. Arabic / RTL

Support:

- English
- Arabic

Architecture must be RTL-safe.

Test:

- sidebar
- navigation
- cards
- forms
- tables
- modals
- drawers
- icons
- arrows
- date presentation
- currency presentation
- mobile layouts

Do not consider Arabic complete because strings are translated.

---

# 55. Dates and Timezones

Backend stores timestamps in UTC.

Garage has its own timezone.

Default Lebanon garage timezone:

```text
Asia/Beirut
```

Display and scheduling convert appropriately.

Hangfire recurring jobs must respect garage local time.

Do not hardcode a constant UTC offset because Beirut daylight-saving rules can change.

---

# 56. Observability

Use Serilog structured logging.

Include useful fields:

- TraceId
- UserId
- GarageId
- Endpoint
- StatusCode
- Duration

Do not log:

- passwords
- access tokens
- refresh tokens
- card data
- sensitive file content

Use Sentry for exceptions where configured.

Expose health checks:

```text
/health/live
/health/ready
```

Readiness should verify critical dependencies such as PostgreSQL.

---

# 57. Audit Logging

Audit sensitive actions.

Examples:

- role changed
- user deactivated
- estimate approved/rejected
- invoice created
- invoice voided
- payment recorded
- payment reversed
- job cancelled
- job soft-deleted
- job status changed
- cost changed
- garage settings changed
- subscription status changed

Audit records should be append-only.

---

# 58. Security Requirements

Protect against:

- IDOR
- cross-tenant access
- SQL injection
- XSS
- CSRF where applicable
- broken access control
- mass assignment
- brute-force login
- malicious file upload
- token theft
- sensitive-data leakage
- insecure admin endpoints

Use parameterized queries.

Never concatenate user input into SQL.

---

# 59. CORS

Configure explicit allowed origins.

Development example:

```text
http://localhost:5173
```

Production:

Use configured frontend origin(s).

Do not use:

```text
AllowAnyOrigin + AllowCredentials
```

---

# 60. Platform Administration

Platform admin is a genuinely separate identity — not a garage-tenant user, not a role on the `users` table, and not scoped to any `garage_id` or `account_id`.

Do not represent SaaS platform administrators as ordinary garage users with a special role.

## `platform_admins` Table

```sql
CREATE TABLE platform_admins (
    id UUID PRIMARY KEY,
    email TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    mfa_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX platform_admins_email_idx ON platform_admins(email);
```

Structurally independent of `users`: no `garage_id`, no FK to `accounts` or `garages`.

## Authentication & Token Separation

Platform admins authenticate through the same login mechanism/endpoint *pattern* as regular auth (e.g. `POST /api/v1/platform/auth/login`, mirroring §11), but the resulting JWT carries a distinct claim, e.g.:

```text
"aud": "platform-admin"
```

or a boolean claim:

```text
"platform_admin": true
```

This claim is:

- **Required** by every `/api/v1/platform/*` endpoint.
- **Explicitly rejected** by every garage-tenant `[Authorize]` policy — a platform-admin token must never satisfy a garage-scoped policy, and a garage-tenant token must never satisfy a platform-admin policy. Write this as an explicit negative test in the Security Agent's test suite (§63).

## Capability Groups (eventual scope)

- **Garage/account management** — view, suspend, reinstate garages and accounts.
- **Subscription/billing visibility and overrides** — view subscription status, MRR, churn, signups; manually adjust subscription state for support cases.
- **Support/admin operations, including impersonation-for-support** — if impersonation is built, it needs its own dedicated audit-log event type (e.g. `platform_admin_impersonation_started` / `...ended`), distinct from normal `job_history`/`AuditLog` entries, so impersonated actions are traceable back to the platform admin.
- **Platform configuration** — system-wide settings not scoped to any garage.
- **Platform-wide / cross-account reporting** — usage metrics, system health, MRR, churn, signups, aggregated across all accounts.

## Phasing

The platform admin **UI and `/api/v1/platform/*` endpoints are Phase 6 (SaaS) scope** (§68) — do not build them in Phase 1.

However, the `platform_admins` table and the JWT claim design (the `aud`/`platform_admin` distinction above) must be **decided and reserved now**, during Phase 1 Foundation, so that the `users` table design and JWT-issuance code do not need rework later when Phase 6 arrives.

Platform admin authorization must be strongly separated at every layer (schema, token, policy).

---

# 61. Seed Data

Create realistic development data.

Garage:

**Performance Auto Garage**

Users:

- Ralph — Owner
- Sarah Khalil — Advisor
- Ahmed Hassan — Mechanic
- Hassan Ali — Mechanic
- Maya — Accountant

Vehicles:

- BMW 328i 2011
- Mercedes-Benz C300 2014
- BMW X5 2009
- Volkswagen Golf GTI 2017
- Audi A4 2016
- Jeep Wrangler Rubicon 2019

Avoid meaningless:

```text
Test User
Test Car
Item 1
```

---

# 62. Mandatory End-to-End Test Scenario

Implement and test:

Customer:

**John Smith**

Vehicle:

**BMW 328i 2011**

Mileage:

**91,850 km**

Complaint:

**Vibration at 80–100 km/h**

Technician:

**Ahmed**

Diagnosis:

**Worn front control arm bushings**

Repair:

- Two front control arms
- Wheel alignment

Garage cost:

**$110**

Customer parts price:

**$160**

Labor:

**$50**

Alignment:

**$25**

Estimate:

**$235**

Customer approves.

Workflow:

```text
CHECKED IN
→ DIAGNOSING
→ WAITING APPROVAL
→ WAITING PARTS
→ READY TO REPAIR
→ REPAIRING
→ QC
→ READY
```

Invoice:

```text
$235
```

Payment 1:

```text
$150 cash
```

Expected:

```text
Invoice Total: $235
Paid: $150
Balance: $85
Status: PARTIAL
```

Vehicle may be delivered if garage configuration allows delivery with balance.

Later:

Payment 2:

```text
$85
```

Expected:

```text
Invoice Total: $235
Paid: $235
Balance: $0
Status: PAID
Customer Balance: $0
```

Service history contains completed repair.

Original $150 payment remains a separate record.

Job history contains relevant events.

---

# 63. Testing Strategy

## Backend Unit Tests

Test:

- state transitions
- financial calculations
- invoice status logic
- payment idempotency
- estimate revision logic
- permission evaluation
- sequence generation
- subscription write restrictions

## Integration Tests

Test:

- actual ASP.NET Core endpoints
- PostgreSQL persistence
- authentication
- authorization
- transactions
- tenant filters
- SignalR where practical

## Tenant Isolation Tests

For every sensitive resource:

Garage A must never retrieve or modify Garage B data.

Test:

- Customers
- Vehicles
- Jobs
- Estimates
- Parts
- Invoices
- Payments
- Expenses
- Attachments
- Users
- Reports

## Frontend Tests

Test critical components and workflows.

## Playwright

Mandatory core E2E path:

```text
Login
→ Check-In
→ Job
→ Diagnosis
→ Estimate
→ Approval
→ Parts
→ Repair
→ QC
→ Invoice
→ Partial Payment
→ Delivery
→ Final Payment
→ Service History
```

---

# 64. Performance Targets

| Metric | Target |
|---|---:|
| Workshop board API | <300ms typical |
| Job detail API | <250ms typical |
| Check-in creation | <500ms typical |
| Payment recording | <300ms typical |
| Common search | <300ms typical |
| API error rate | <0.1% target |
| Monthly uptime | 99.5%+ initial target |

Do not optimize based on guesses.

Measure.

Add indexes based on real query plans.

---

# 65. MVP — Must Have

- [ ] Garage registration
- [ ] Garage onboarding
- [ ] Authentication
- [ ] Refresh/session handling
- [ ] Team invite
- [ ] Five initial roles
- [ ] Multi-tenant isolation
- [ ] Customer management
- [ ] Vehicle management
- [ ] Check-in
- [ ] Workshop board
- [ ] Job detail
- [ ] Diagnosis
- [ ] Repair tasks
- [ ] Technician assignment
- [ ] Estimate creation
- [ ] Estimate approval recording
- [ ] Parts workflow
- [ ] QC
- [ ] Invoice creation
- [ ] Partial/full payments
- [ ] Outstanding customer balance
- [ ] Service history
- [ ] Audit/job history
- [ ] Owner dashboard
- [ ] Arabic RTL
- [ ] Subscription/trial foundation
- [ ] Platform admin foundation
- [ ] SignalR workshop updates
- [ ] Automated tests

---

# 66. V1.1 / Shortly After MVP

- [ ] Customer secure estimate approval link
- [ ] WhatsApp estimate sharing
- [ ] Photo upload
- [ ] Appointment scheduling
- [ ] Expense management
- [ ] Inventory
- [ ] Suppliers
- [ ] CSV/Excel export
- [ ] Customer service-history portal
- [ ] Automated service reminders
- [ ] Automated payment reminders
- [ ] Advanced technician reporting

---

# 67. Future

- [ ] Native mobile app
- [ ] Supplier marketplace
- [ ] Parts bidding / quote network
- [ ] Supplier subscriptions
- [ ] Parts transaction fees
- [ ] VIN decoding
- [ ] AI diagnostic assistance
- [ ] OCR supplier invoices
- [ ] Demand analytics
- [ ] Fleet features
- [ ] Multi-location garage groups
- [ ] Advanced accounting integrations

---

# 68. Implementation Order

Do not build every module at once.

## Phase 1 — Foundation

- Repository structure
- React app
- ASP.NET Core solution
- PostgreSQL
- EF Core
- Authentication
- Platform admin JWT claim design (schema + claim reserved now; UI/endpoints in Phase 6 — see §60)
- Tenant context
- Authorization
- Logging
- Swagger
- Error handling
- CI

## Phase 2 — Garage Core

- Garage setup
- Users/team
- Customers
- Vehicles

## Phase 3 — First Vertical Slice

- Check-in
- Job
- Workshop board
- Diagnosis
- Tasks
- Mechanic assignment
- Estimate
- Approval
- Parts
- Repair
- QC
- Invoice
- Payment
- Delivery
- Service history

This phase is the first major commercial milestone.

## Phase 4 — Owner Operations

- Dashboard
- Receivables
- Expenses
- Reports
- Attention items

## Phase 5 — Communication

- SignalR completion
- Notifications
- WhatsApp sharing
- Background reminders

## Phase 6 — SaaS

- Trial
- Billing
- Stripe webhooks
- Subscription enforcement
- Platform admin UI/endpoints (table + JWT claim already reserved in Phase 1 — see §60)

## Phase 7 — Production Hardening

- Security audit
- Tenant isolation audit
- Performance testing
- E2E tests
- Responsive review
- Arabic RTL review
- Monitoring
- Backup/restore plan
- Deployment
- Rollback strategy

---

# 69. Definition of Done

A feature is complete only when:

- Approved design is implemented
- Responsive behavior works
- Backend API exists
- Database persistence exists
- Authorization works
- Tenant isolation works
- Validation works
- Loading state works
- Empty state works
- Error state works
- Audit behavior exists where required
- Unit/integration tests pass
- QA reviews it
- Design review passes
- Application builds
- Application runs
- No known critical errors remain

A screen alone is not a finished feature.

---

# 70. Required Engineering Documents

Maintain throughout development:

## `IMPLEMENTATION_MAP.md`

Map:

```text
Design Screen
→ Frontend Route
→ React Components
→ API Endpoint
→ Application Service
→ Database Entities
→ Permission
→ Tests
```

## `PROGRESS.md`

Statuses:

```text
Not Started
In Progress
Blocked
Implemented
Under Review
QA Failed
QA Passed
Complete
```

## `DECISIONS.md`

Architecture decisions and rationale.

## `KNOWN_ISSUES.md`

Open defects.

## `TEST_STATUS.md`

Actual test coverage and latest results.

## `DESIGN_IMPLEMENTATION_DIFFERENCES.md`

Only justified differences from the approved design.

---

# 71. First Engineering Task

Before large-scale implementation:

1. Inspect the entire repository.
2. Read all approved product/design documents.
3. Inspect available team agents.
4. Review the interactive prototype.
5. Review high-fidelity desktop/mobile screens.
6. Create `IMPLEMENTATION_MAP.md`.
7. Create `PROGRESS.md`.
8. Create `DECISIONS.md`.
9. Confirm the final solution structure.
10. Bootstrap React + TypeScript + Vite.
11. Bootstrap ASP.NET Core 8 solution.
12. Configure PostgreSQL.
13. Configure EF Core 8.
14. Configure dependency injection.
15. Configure Serilog.
16. Configure Swagger/OpenAPI.
17. Configure authentication foundations.
18. Configure tenant context.
19. Configure authorization policies.
20. Configure testing projects.
21. Build and run both frontend and backend.
22. Commit the working foundation.
23. Begin the first vertical slice.

Do not return to product design unless a genuine conflict exists in the approved documents.

---

# 72. Final Engineering Principle

The product must remain simple for garage users even if the engineering underneath is sophisticated.

The system should ultimately make the owner feel:

> **I know what is happening in my garage, what is making money, what is waiting, who owes me money, and what needs my attention.**

The engineering team is responsible for making the approved design:

- real
- secure
- fast
- reliable
- maintainable
- commercially deployable

The approved implementation stack is:

```text
React
TypeScript
Vite
Tailwind CSS
TanStack Query
ASP.NET Core 8
C#
PostgreSQL
EF Core 8
Dapper where justified
SignalR
Hangfire
Serilog
FluentValidation
xUnit
Playwright
GitHub Actions
```

This stack supersedes any previous Node.js, Express/Fastify, Next.js, Prisma, Socket.io, or BullMQ recommendations.

**Proceed with implementation using this architecture and the approved product/design documents as the source of truth.**
