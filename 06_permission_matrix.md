# Phase 6 — Role & Permission Matrix

## Permission Levels
- **None**: No access to the module — not visible in UI
- **View**: Can read records, cannot create or modify
- **Create**: Can view and create new records
- **Edit**: Can view, create, and modify existing records
- **Delete**: Can view, create, edit, and delete records
- **Admin**: Full control including configuration, voids, overrides

> **Footnote:** Admin should never be used to mean unrestricted view-only access — use View for that.
> (Corrected the Accountant cells on "View invoice", "View payment history on invoice", and "View payment
> records" from Admin to View accordingly — those are view-only actions.)

---

## Full Permission Matrix

| Module / Feature | Owner | Manager | Advisor / Reception | Mechanic | Accountant |
|---|---|---|---|---|---|
| **DASHBOARD** | | | | | |
| Revenue KPIs (invoiced, collected) | Admin | None | None | None | View |
| Gross Profit | Admin | None | None | None | View |
| Parts Cost Totals | Admin | None | None | None | View |
| Jobs by Status (count) | Admin | View | View | View (own) | View |
| Needs Attention List | Admin | View | View | View (own) | None |
| Mechanic Load Overview | Admin | View | None | None | None |
| | | | | | |
| **WORKSHOP BOARD** | | | | | |
| View all job cards | Admin | View | View | View (own) | None |
| Move job between columns | Admin | Edit | Edit (limited) | Edit (own, Work stages) | None |
| Filter by mechanic | Admin | Edit | View | None | None |
| | | | | | |
| **CHECK-IN / JOB CREATION** | | | | | |
| Search customer/vehicle | Admin | Admin | Create | None | None |
| Create new customer | Admin | Admin | Create | None | None |
| Edit customer details | Admin | Admin | Edit | None | None |
| Delete customer record | Delete | None | None | None | None |
| Create new vehicle | Admin | Admin | Create | None | None |
| Edit vehicle details | Admin | Admin | Edit | None | None |
| Create job card | Admin | Admin | Create | None | None |
| Assign mechanic | Admin | Admin | Edit | None | None |
| Set promised time | Admin | Admin | Edit | None | None |
| | | | | | |
| **JOB DETAIL — OVERVIEW** | | | | | |
| View job overview | Admin | View | View | View (own) | None |
| Edit complaint / notes | Admin | Edit | Edit | None | None |
| Reassign mechanic | Admin | Edit | None | None | None |
| Change job status | Admin | Edit | Edit (Reception stages) | Edit (Work stages) | None |
| Delete job | Delete | None | None | None | None |
| | | | | | |
| **DIAGNOSIS** | | | | | |
| View diagnosis notes | Admin | View | View | Edit (own) | None |
| Enter/edit diagnosis | Admin | Edit | None | Edit (own assigned) | None |
| Add additional findings | Admin | Edit | None | Edit (own assigned) | None |
| | | | | | |
| **REPAIR TASKS** | | | | | |
| View all tasks | Admin | View | View | View (own job) | None |
| Add/edit tasks | Admin | Edit | None | None | None |
| Start/Pause/Complete own task | Admin | Edit | None | Edit (own assigned) | None |
| Mark other mechanic's task | Admin | Edit | None | None | None |
| Add task notes | Admin | Edit | View | Edit (own) | None |
| | | | | | |
| **ESTIMATES** | | | | | |
| View estimate (selling prices) | Admin | View | Edit | None | View |
| View estimate (cost prices) | Admin | None | None | None | View |
| View gross margin per estimate | Admin | None | None | None | View |
| Create estimate | Admin | Create | Create | None | None |
| Edit estimate line items | Admin | Edit | Edit | None | None |
| Apply discount | Admin | Edit (≤15%) | None | None | None |
| Apply large discount (>15%) | Admin | None | None | None | None |
| Send estimate to customer | Admin | Edit | Edit | None | None |
| Record customer approval | Admin | Edit | Edit | None | None |
| Record customer rejection | Admin | Edit | Edit | None | None |
| Delete/void estimate | Admin | None | None | None | None |
| | | | | | |
| **PARTS** | | | | | |
| View parts list (name, status) | Admin | View | View | View (own job) | None |
| View parts cost price | Admin | View | None | None | View |
| View parts selling price | Admin | View | View | None | View |
| Add parts to job | Admin | Edit | Edit | None | None |
| Update part status (ordered/arrived) | Admin | Edit | Edit | None | None |
| Mark part installed | Admin | Edit | None | Edit (own job) | None |
| Flag wrong/damaged part | Admin | Edit | Edit | Edit (own job) | None |
| Delete part from job | Admin | Edit | None | None | None |
| | | | | | |
| **LABOR** | | | | | |
| View labor line items | Admin | View | View | None | View |
| Set labor rate per job | Admin | Edit | Edit | None | None |
| Edit default labor rates | Admin | None | None | None | None |
| | | | | | |
| **QC** | | | | | |
| View QC status | Admin | View | View | View (own) | None |
| Mark QC Pass | Admin | Edit | None | None | None |
| Mark QC Fail (return to repair) | Admin | Edit | None | None | None |
| Add QC notes | Admin | Edit | None | None | None |
| | | | | | |
| **INVOICES** | | | | | |
| View invoice (line items, total) | Admin | View | View | None | View |
| View payment history on invoice | Admin | View | View | None | View |
| Create invoice | Admin | Create | Create | None | None |
| Edit invoice (before payment) | Admin | Edit | Edit | None | None |
| Edit invoice (after payment) | Admin | None | None | None | None |
| Void invoice | Admin | None | None | None | None |
| Print / email invoice | Admin | View | Edit | None | View |
| | | | | | |
| **PAYMENTS** | | | | | |
| View payment records | Admin | View | View | None | View |
| Record payment | Admin | Edit | Edit | None | None |
| Edit/delete payment record | Admin | None | None | None | None |
| Override payment (refund/correction) | Admin | None | None | None | None |
| | | | | | |
| **CUSTOMER DEBTS** | | | | | |
| View all outstanding balances | Admin | None | None | None | Admin |
| View own customer's balance | Admin | View | View | None | Admin |
| Send payment reminder | Admin | Edit | Edit | None | Edit |
| Mark as bad debt | Admin | None | None | None | None |
| Write off debt | Admin | None | None | None | None |
| | | | | | |
| **EXPENSES** | | | | | |
| View expense records | Admin | View | None | None | Admin |
| Create expense | Admin | None | None | None | Create |
| Edit expense | Admin | None | None | None | Edit |
| Delete expense | Admin | None | None | None | None |
| | | | | | |
| **REPORTS** | | | | | |
| Revenue report | Admin | None | None | None | View |
| P&L report | Admin | None | None | None | View |
| Jobs by mechanic | Admin | View | None | None | None |
| Parts cost report | Admin | None | None | None | View |
| Export to CSV/PDF | Admin | None | None | None | Edit |
| | | | | | |
| **SERVICE HISTORY** | | | | | |
| View service history (customer) | Admin | View | View | View (own jobs) | None |
| View service history (vehicle) | Admin | View | View | View (own jobs) | None |
| Add recommendation note | Admin | Edit | Edit | None | None |
| | | | | | |
| **MEDIA / PHOTOS** | | | | | |
| View photos | Admin | View | View | View (own job) | None |
| Upload photos | Admin | Edit | Edit | Edit (own job) | None |
| Delete photos | Admin | Edit | None | None | None |
| View documents | Admin | View | View | None | View |
| Upload documents | Admin | Edit | Edit | None | Edit |
| | | | | | |
| **CUSTOMERS (module)** | | | | | |
| Search customers | Admin | View | View | None | View |
| View customer profile | Admin | View | View | None | View |
| Create customer | Admin | Create | Create | None | None |
| Edit customer contact info | Admin | Edit | Edit | None | None |
| Merge duplicate customers | Admin | None | None | None | None |
| Delete customer | Delete | None | None | None | None |
| | | | | | |
| **EMPLOYEES** | | | | | |
| View employee list | Admin | View | None | None | None |
| View mechanic assignments | Admin | View | None | None | None |
| Create/edit employee records | Admin | None | None | None | None |
| View salary/payroll data | Admin | None | None | None | Admin |
| | | | | | |
| **SETTINGS — GARAGE PROFILE** | | | | | |
| View garage settings | Admin | View | None | None | None |
| Edit garage profile | Admin | None | None | None | None |
| Upload garage logo | Admin | None | None | None | None |
| | | | | | |
| **SETTINGS — LABOR RATES** | | | | | |
| View labor rates | Admin | View | View | None | View |
| Edit default labor rates | Admin | None | None | None | None |
| | | | | | |
| **SETTINGS — TEAM** | | | | | |
| View team members | Admin | View | None | None | None |
| Invite new user | Admin | None | None | None | None |
| Change user role | Admin | None | None | None | None |
| Deactivate user | Admin | None | None | None | None |
| | | | | | |
| **SETTINGS — SUBSCRIPTION** | | | | | |
| View subscription status | Admin | None | None | None | None |
| Manage billing | Admin | None | None | None | None |
| Upgrade/downgrade plan | Admin | None | None | None | None |
| | | | | | |
| **NOTIFICATIONS** | | | | | |
| Receive overdue alerts | Yes | Yes | Yes | Yes | No |
| Receive approval alerts | Yes | Yes | Yes | No | No |
| Receive payment alerts | Yes | No | Yes | No | Yes |
| Configure notification preferences | Admin | Own | Own | Own | Own |

---

## Special Rules

1. **Data visibility scoping**: Mechanic can only see their own assigned jobs. If a job has two mechanics, both can see it. If a job is reassigned, the previous mechanic loses access.

2. **Cost vs. Price split**: The system stores both cost_price and selling_price per part. The selling_price is visible to Advisor and Manager. The cost_price is visible only to Owner and Accountant. Gross margin = selling_price − cost_price, visible only to Owner and Accountant.

3. **Estimate approval threshold**: Estimates under $500 can be created and sent by whichever role created them (Manager **or** Advisor) — this rule applies regardless of which of those two roles is acting, not Manager only. Above $500, the system flags "Requires Owner approval" — the Owner must explicitly approve before the estimate can be sent, and it is evaluated against the estimate's **pre-discount subtotal** (not the post-discount total).

4. **Invoice void**: Only Owner can void an invoice. This creates an audit record with the reason. Voided invoices cannot be deleted — they remain in history with VOIDED status.

5. **Job deletion**: Only Owner can delete a job card. Deletion is soft (archived, not removed from database). Must provide a reason.

6. **Customer data privacy**: Customer phone and WhatsApp number are visible to Owner, Manager, and Advisor. Mechanic only sees the customer's first name on the job card. Customer **phone number, WhatsApp number, and last name must be omitted entirely** (not merely hidden with CSS) from any Mechanic-facing job data or API response — same principle as "first name only," applied at the data layer, not just the UI layer.

7. **Multi-garage isolation**: Each garage's data is completely isolated. A user account belongs to one garage only. No cross-garage data access even if the same owner has two garages (separate accounts with potential future SSO).
