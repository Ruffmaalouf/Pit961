# Phase 4 — Information Architecture

---

## Design Principle
Navigation must serve the busiest person in the building: the Service Advisor who is handling a customer at the front desk with 3 other people waiting. Every primary action must be reachable in 2 clicks. The Workshop Board is the operational heartbeat — it must always be one click away.

---

## Desktop Navigation (Sidebar — Left, Collapsed on Mobile)

> **Reconciled to match the approved prototype (`prototype.html`).** The prototype's nav rail is a fixed,
> 8-item list — `Floor, Clock, Jobs, Customers, Money, Parts, Team, Reports` — verified directly against the
> `rail` array in `prototype.html`. It replaces the previous 7-item list below (`Dashboard, Workshop,
> Customers, Jobs, Finance, Parts & Stock, Settings`).
>
> **No Settings item exists in the approved prototype.** Settings/Garage Profile/Billing/Tax/Working Hours
> functionality described later in this document (Section 7) is **not yet designed** in the prototype and
> has no corresponding nav entry or mockup. It remains a valid Phase 1 requirement — flagged here so its
> absence from the prototype is not mistaken for a decision to cut it.
>
> **Role-gating of nav items is not implemented in the prototype** — all 8 rail items render for every role
> (the prototype only varies role via a 3-way switcher: Owner / Advisor / Technician, and hides monetary
> *values* within screens for Technician, not entire nav items). The role restrictions shown in the diagram
> below are this document's design intent, not a behavior confirmed in the prototype; treat them as a
> requirement for a future pass, not as prototype-verified fact.

Maximum 8 top-level items (was 7 — see reconciliation note above).

```
┌─────────────────────────────────────┐
│  ⬡ [Product Name]   [Garage Name]  │
├─────────────────────────────────────┤
│  Floor        (real-time shop floor / bay status — all roles) │
│  Clock        (promise-time / stage board — all roles)        │
│  Jobs         (all roles)                                     │
│  Customers    (all except Mechanic)                           │
│  Money        (Owner, Acct, Mgr — formerly "Finance")         │
│  Parts        (Owner, Mgr, Adv. — formerly "Parts & Stock")   │
│  Team         (Owner — formerly nested under Settings)        │
│  Reports      (Owner, Acct — formerly nested under Finance)   │
├─────────────────────────────────────┤
│  [User Avatar] Ahmed (Mechanic)     │
│  [Logout]                           │
└─────────────────────────────────────┘
```

*(Nav icons are custom CSS/gradient glyphs in the prototype, not emoji or text glyphs as drawn above — see
`09_design_system.md` Icons section.)*

> **Branding note:** "GarageOS" and "PIT961" are internal/project codenames only — no final customer-facing
> product name or logo has been approved. The prototype's sidebar mark currently reads "RASHID," which is
> leftover placeholder branding from the prototyping session, not an approved product name. The `[Product
> Name]` slot above stands in for whatever brand mark/wordmark is eventually decided; it is not a Phase 1
> blocker. See `09_design_system.md` → Branding for the requirement that this slot stay swappable/configurable.

---

## Top-Level Nav Items & Sub-Pages

### 1. Dashboard
**Roles**: All  
Single page, role-adapted content:
- **Owner**: Revenue KPIs, gross profit, job counts, overdue jobs, needs-attention list
- **Manager**: Job counts by status, mechanic load, parts expected today, overdue
- **Advisor**: My check-ins today, awaiting approval, awaiting payment
- **Mechanic**: My jobs, tasks due, parts expected for my jobs
- **Accountant**: Revenue today/month, outstanding invoices, expense summary

---

### 2. Workshop (Board)
**Roles**: All except Accountant  
Sub-pages:
- **Board View** (Kanban — default): Jobs grouped by status column
- **List View**: Same jobs in sortable table (useful for large garages)
- **[Quick Action: + Check In]**: Always visible as a button in top-right of workshop view

Workshop board columns:
1. Checked In
2. Diagnosing
3. Waiting Approval
4. Waiting Parts
5. Repairing
6. QC
7. Ready
8. Delivered (collapsed by default, expandable)

---

### 3. Customers
**Roles**: Owner, Manager, Advisor, Accountant  
Sub-pages:
- **Customer List**: Search/filter, quick stats, outstanding balance indicator
- **Customer Profile**: (per customer) Info, vehicles, service history, balance, communications
- **Vehicle Profile**: (per vehicle) Specs, mileage history, all repair orders

---

### 4. Jobs
**Roles**: All (content filtered by role)  
Sub-pages:
- **All Jobs**: Full job list with filters (status, date, mechanic, vehicle)
- **Job Detail**: (per job) The approved prototype implements this as **sections within a single consolidated page** — Overview / Work / Parts / Estimate / Media / History / Invoice — not separate tabs (corrected per `05_screen_inventory.md`; underlying field requirements unchanged)
- **Active Jobs**: Shortcut filter — open jobs only

Mechanic sees only:
- **My Jobs**: Their assigned jobs only
- **Job Detail**: Simplified (no financial tabs)

---

### 5. Finance
**Roles**: Owner, Manager (limited), Accountant  
Sub-pages:
- **Invoices**: All invoices, filter by status, search by customer/job
- **Payments**: Payment ledger — all transactions
- **Customer Debts**: Outstanding balances with aging
- **Expenses**: Operational expense log
- **Reports**: Revenue summary, P&L, export

Manager sees: Invoices (view), Payments (view). No Expenses or Reports.

---

### 6. Parts & Stock
**Roles**: Owner, Manager, Advisor  
Sub-pages:
- **Parts in Jobs**: Parts ordered/pending across all active jobs
- **Parts Catalog**: Master parts reference (make/model/part number)
- **Suppliers**: Supplier contact list with pricing notes

---

### 7. Settings
**Roles**: Owner only  
Sub-pages:
- **Garage Profile**: Name, address, logo, contact
- **Billing & Labor Rates**: Default labor rate, overtime rate
- **Tax Settings**: Tax label (VAT/GST/TVA), rate
- **Working Hours**: Open/close hours, days
- **Team**: User accounts, role assignments
- **Subscription**: Plan, billing, upgrade/cancel

---

## Global Elements

### Top Bar (always visible)
```
[≡ Sidebar Toggle]  [Product Name — TBD]  [🔍 Global Search]  [+ Quick Action]  [🔔 Notifications]  [👤 User Menu]
```

*(The `[Product Name — TBD]` slot in the top bar is a placeholder — see the Branding note under
"Desktop Navigation" above. No final brand name/logo is decided; do not read "GarageOS" here as settled.)*

**RTL Toggle**: Available in User Menu → Language/Direction. When toggled, entire UI mirrors to Arabic RTL layout.

---

### Global Search
Accessible via: Top bar search icon, or keyboard shortcut `Ctrl+K` / `⌘K`

Searches across:
- Customer name
- Customer phone number
- Vehicle plate number
- Job number
- Vehicle make/model

Results shown in a floating panel, grouped by type:
```
🔍 "BMW"
──────────────────────
VEHICLES (3)
  BMW 328i — Plate: XAB 123 — John Smith — Job #047 (Active)
  BMW X5 — Plate: MNK 441 — Mariam Hassan — No active job
  BMW 320i — Plate: RRR 009 — Garage Demo — Delivered 3 days ago

JOBS (2)
  #047 — BMW 328i — John Smith — Repairing
  #031 — BMW 520i — Khalil Youssef — Delivered
```

---

### Quick Actions (+ Button, top-right)
Contextual floating action button:
- **+ Check In Vehicle** (all roles except Mechanic/Accountant)
- **+ New Customer** (Advisor and above)
- **+ Record Payment** (Advisor and above)
- **+ Add Expense** (Owner, Accountant)

---

### Notifications Panel (Bell Icon)
Slide-out panel from right. Role-filtered alerts:

**Owner notifications:**
- Job overdue (promised time passed)
- Invoice unpaid > 7 days
- Estimate waiting approval > 24 hours
- Daily revenue summary (end of day)

**Manager notifications:**
- Job moved to Waiting Approval
- Parts arrived for a job
- Job ready for QC
- QC failed — returned to repair

**Advisor notifications:**
- Customer approved estimate
- Customer rejected estimate
- Job ready for pickup
- Payment recorded on their job

**Mechanic notifications:**
- New job assigned to me
- Part arrived for my job
- QC failed — my job returned

---

## Mobile Navigation (Bottom Tab Bar)

Mobile shows bottom navigation instead of sidebar. Max 4 items + overflow.

### Mechanic Mobile:
```
[🏠 My Jobs] [🔧 Job Detail] [📷 Photos] [☰ More]
```

### Advisor Mobile:
```
[📋 Board] [➕ Check-In] [👤 Customers] [☰ More]
```

### Owner Mobile:
```
[📊 Dashboard] [📋 Board] [💰 Finance] [☰ More]
```

"More" reveals: Settings, Notifications, Search.

---

## Breadcrumb Navigation

Used inside deeply-nested sections:
```
Jobs → #047 — BMW 328i (John) → Estimate
```
Back navigation via browser back or explicit Back button on mobile.

---

## Empty States

Each section has a specific empty state (not a generic "Nothing here"):
- **Workshop Board empty**: "No active jobs today. Ready to check in your first vehicle? [+ Check In]"
- **Customer list empty**: "No customers yet. Check in your first vehicle to create a customer record. [+ Check In]"
- **Finance empty**: "No invoices yet. Invoices are created automatically when a job is ready for delivery."
- **Notifications empty**: "You're all caught up. All jobs are on track."

---

## Page Titles & URL Structure

```
/dashboard
/workshop                    (Kanban board)
/workshop/list               (List view)
/customers                   (Customer list)
/customers/:id               (Customer profile)
/customers/:id/vehicles/:id  (Vehicle profile)
/jobs                        (All jobs)
/jobs/:id                    (Job detail)
/jobs/:id/estimate
/jobs/:id/invoice
/checkin                     (Check-in flow)
/finance/invoices
/finance/payments
/finance/debts
/finance/expenses
/finance/reports
/parts
/settings
/settings/team
/settings/subscription
```
