# Phase 2 — Module Classification

**Criteria:**
- **MVP**: Without this, the product cannot function as a basic garage management tool. A garage must be able to check a car in, manage the repair lifecycle, and get paid.
- **V1.1**: Valuable but not required for launch. Adds operational depth. Builds after first paying customers validate core.
- **Later**: Nice-to-have, high complexity, or targets a more mature customer segment.

---

## MVP Modules

### 1. Dashboard
**Classification: MVP**  
Reason: The first screen every role sees daily. Without a useful dashboard, the product feels empty. Needs: today's job count, jobs needing attention (waiting approval, overdue), revenue snapshot. Scope is minimal — not a full analytics suite.

### 2. Workshop Board (Kanban)
**Classification: MVP**  
Reason: This IS the product for daily operations. A visual representation of every job's status is the primary interface mechanics, advisors, and managers use all day. Without it, there is no workflow management. Columns: Checked In / Diagnosing / Waiting Approval / Waiting Parts / Repairing / QC / Ready / Delivered.

### 3. Customers
**Classification: MVP**  
Reason: Every job is tied to a customer. Need to search, create, view, and edit customer records. Includes contact info, vehicles owned, balance, history. Without this, the check-in workflow breaks.

### 4. Vehicles
**Classification: MVP**  
Reason: The garage services vehicles, not just customers. Vehicle profiles (plate, make, model, year, VIN, mileage history) are required for job creation and service history. Tightly coupled with Customers module.

### 5. Check-In
**Classification: MVP**  
Reason: The entry point of every job. Must be fast (under 60 seconds for returning customers). Requires: customer lookup, vehicle selection, mileage entry, complaint recording, mechanic assignment. This is the first user-visible workflow.

### 6. Repair Orders / Job Cards
**Classification: MVP**  
Reason: The central record of a job. Everything else (parts, labor, invoices, history) attaches to a job card. Without it, there is no data structure for the business. The job card drives the entire repair lifecycle.

### 7. Diagnosis
**Classification: MVP**  
Reason: A formal diagnosis step (separate from repair tasks) is needed for estimate accuracy and customer communication. Mechanics must record findings. Simple text + photo initially.

### 8. Repair Tasks
**Classification: MVP**  
Reason: Breaking work into tasks (with status per task) is how mechanics track progress and how QC verifies completeness. Without tasks, a job is a black box.

### 9. Estimates / Quotations
**Classification: MVP**  
Reason: Legal and financial requirement. No garage can work on a car without customer consent. The estimate documents what was agreed. Must include line items (parts, labor), totals, and a way to record customer approval.

### 10. Customer Approval
**Classification: MVP**  
Reason: Approval must be captured with a method and timestamp. Even if MVP is just an "Approved" checkbox (recording in-person approval), this protects the garage legally. WhatsApp integration can come later.

### 11. Parts
**Classification: MVP**  
Reason: Parts management is tightly integrated with repair workflow. Need to list parts per job, track ordered/arrived/installed status. Without this, "Waiting Parts" status is meaningless and purchasing is untracked.

### 12. Labor
**Classification: MVP**  
Reason: Labor is a core billing line item on every invoice. Need to track labor per job (at minimum: rate × hours or fixed price). Detailed time tracking can be V1.1.

### 13. QC
**Classification: MVP**  
Reason: A QC step with pass/fail before invoice is a quality gate that protects the garage. Even a simple checklist is sufficient for MVP. Without it, there's no formal handover between mechanic and advisor.

### 14. Invoices
**Classification: MVP**  
Reason: The financial document that gets the garage paid. Must include line items from estimate, totals, and payment status. Required for cash flow.

### 15. Payments
**Classification: MVP**  
Reason: Recording cash/card payments against invoices is core. Must support partial payments and show outstanding balance. Without this, the garage can't track who owes what.

### 16. Roles / Permissions
**Classification: MVP**  
Reason: A mechanic must not see financial data. An accountant must not create job cards. Basic role-based access control is required from day one for security and usability. Start with 4 roles: Owner, Manager, Advisor/Reception, Mechanic.

### 17. Garage Settings
**Classification: MVP**  
Reason: Minimum configuration needed: garage name, address, logo, currency, tax rate, working hours, labor rates. Without this, invoices can't be branded and calculations are incorrect.

### 18. Subscription
**Classification: MVP**  
Reason: This is a SaaS — the billing mechanism must exist at launch. At minimum: subscription status visible to owner, link to billing portal. Full subscription management can be external (Stripe billing portal).

**Pricing (decided):** **$30 USD/month per garage**, one subscription per garage, billed monthly. No other tiers exist for Phase 1. Any prior tiered-pricing language for this module was only ever a reviewer's proposal, never ratified, and is superseded by this figure.

---

## V1.1 Modules

### 19. Customer Debts
**Classification: V1.1**  
Reason: Partial payments create debts automatically via the Payments module (MVP). A dedicated Debts screen that aggregates all outstanding balances, filters by age, and enables bulk follow-up communication is valuable but can be built from existing payment data. Not required to launch but needed for the accountant role's full value.

### 20. Expenses
**Classification: V1.1**  
Reason: Tracking operational expenses (rent, utilities, salaries, consumables) is necessary for true P&L visibility. However, garages can still use the core system without expense tracking at MVP. This adds financial completeness for accountant users.

### 21. Appointments
**Classification: V1.1**  
Reason: Many garages operate on a walk-in basis, especially in markets like Lebanon and MENA. Appointment scheduling adds value but is not required for core workflow. Can operate with check-in only at MVP. V1.1 adds calendar view, appointment-to-job-card conversion, and reminder notifications.

### 22. Service History
**Classification: V1.1**  
Reason: Service history is built passively from completed jobs (MVP already captures this data). A dedicated Service History screen with filters, mileage progression, and recommendations is a V1.1 experience layer on top of existing data.

### 23. Recommendations
**Classification: V1.1**  
Reason: Service advisors noting "replace brake pads at next visit" is a retention and revenue tool. Tightly linked to Service History. The recommendation engine (e.g., "60,000 km service due") can be rule-based initially.

### 24. Photos / Documents
**Classification: V1.1**  
Reason: Photo capture during check-in and repair (pre-existing damage, during repair, after repair) is valuable for dispute resolution and customer trust. MVP can support a basic file upload; a structured media library with before/after organization and customer-facing gallery is V1.1.

### 25. WhatsApp Communication
**Classification: V1.1**  
Reason: WhatsApp is the dominant communication channel in MENA markets. Sending estimates, approvals, and ready notifications via WhatsApp is a strong differentiator. However, MVP can use manual copy-paste or SMS links. WhatsApp Business API integration (with message templates and approval flows) is a V1.1 integration requiring API setup and compliance.

### 26. Notifications
**Classification: V1.1**  
Reason: In-app and push notifications for job status changes, approvals, and overdue alerts improve team responsiveness. MVP can surface the same information via the dashboard "Needs Attention" list. A proper notification center (with read/unread, history, preferences) is V1.1.

### 27. Employees
**Classification: V1.1**  
Reason: Basic employee records (name, role, mechanic bay assignment) are needed at MVP as part of Roles/Permissions. A full employee module with work schedules, performance metrics, job history per mechanic, and payroll prep is V1.1.

### 28. Suppliers
**Classification: V1.1**  
Reason: Parts can be ordered manually in MVP (advisor knows which supplier to call). A Suppliers module that tracks supplier contacts, pricing agreements, and purchase order history adds efficiency but is not required for core workflow.

---

## Later Modules

### 29. SaaS Administration
**Classification: Later**  
Reason: The internal admin panel for managing all garages, subscriptions, feature flags, and support. Required for scaled operations but built incrementally as the SaaS grows. Initially, the founder manually manages onboarding via direct database access or a simple admin script.

### 30. Multi-Location Garage Groups (Chain Management)
**Classification: Later**  
*(No prior entry for this existed in this document — added here as the place to record the owner's Phase 1 scope decision, since chain/multi-branch management is the module this decision applies to.)*  
Reason/Note: Phase 1 product experience is one paid subscription per garage — no chain/multi-branch UI in
Phase 1. The underlying architecture reserves an Account/Organization layer above Garage (see
`11_engineering_handoff.md`) so multiple garages can later be grouped under one paying account without a
breaking migration.

---

## Summary Table

| Module | Classification | Rationale Summary |
|---|---|---|
| Dashboard | MVP | Daily entry point |
| Workshop Board | MVP | Core operational UI |
| Customers | MVP | Required for check-in |
| Vehicles | MVP | Required for job creation |
| Check-In | MVP | Entry point of every job |
| Repair Orders/Job Cards | MVP | Central data record |
| Diagnosis | MVP | Required for estimate |
| Repair Tasks | MVP | Mechanic workflow |
| Estimates/Quotations | MVP | Legal consent |
| Customer Approval | MVP | Consent recording |
| Parts | MVP | Job completion |
| Labor | MVP | Billing line item |
| QC | MVP | Quality gate |
| Invoices | MVP | Get paid |
| Payments | MVP | Cash flow |
| Roles/Permissions | MVP | Security & usability |
| Garage Settings | MVP | Branding & config |
| Subscription | MVP | SaaS billing — $30 USD/mo per garage, flat, no tiers |
| Customer Debts | V1.1 | Accountant feature |
| Expenses | V1.1 | P&L completeness |
| Appointments | V1.1 | Scheduling value-add |
| Service History | V1.1 | Retention feature |
| Recommendations | V1.1 | Revenue feature |
| Photos/Documents | V1.1 | Dispute protection |
| WhatsApp Communication | V1.1 | MENA differentiator |
| Notifications | V1.1 | Workflow alerting |
| Employees | V1.1 | HR & performance |
| Suppliers | V1.1 | Procurement efficiency |
| SaaS Administration | Later | Internal ops tooling |
| Multi-Location Garage Groups (Chain Management) | Later | Phase 1 = one subscription per garage, no chain UI |
