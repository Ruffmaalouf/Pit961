# Phase 3 — Role Experiences

---

## Role 1: Owner

### Who They Are
The garage owner. May or may not be present daily. Thinks in revenue, not individual jobs. Needs the business at a glance and the ability to intervene on exceptions.

### Primary Goal
Know if the business is healthy today. Spot problems. Make strategic decisions.

### What They See
- **Full Dashboard**: Revenue today/this week/this month, jobs in each status, outstanding payments, gross profit estimate, parts cost
- **Workshop Board**: Read + manage all jobs across all status columns
- **All job details**: Including costs, margins, and financial data
- **Customer list**: Including outstanding debts and full history
- **Employees section**: Who's working, performance per mechanic (jobs completed, hours)
- **Expenses**: Full P&L view
- **Settings**: All garage settings including subscription management
- **Reports**: Revenue, jobs by mechanic, parts costs, customer retention

### What They Can Do
- Create, edit, delete any record
- Approve estimates above the standard threshold
- Void invoices
- Apply discounts
- Manage roles and create user accounts
- Configure garage settings, labor rates, tax rates
- View and export reports
- Manage subscription

### What They CANNOT Do
- Nothing. Owner has full access.

### Key Screens
1. Dashboard (primary daily view)
2. Workshop Board (operational overview)
3. Reports (weekly review)
4. Customer Debts (cash flow health)
5. Settings (occasional)

### Experience Notes
- Owner may access from home via mobile — dashboard must work well on phone
- Revenue numbers should be prominent and in large type
- "Needs Attention" section surfaces jobs requiring owner decision
- Should receive WhatsApp/SMS alerts for jobs overdue > 1 day

---

## Role 2: Manager

### Who They Are
Senior operational staff. Present daily. Runs the workshop floor and coordinates between reception, mechanics, and suppliers. Often empowered to make financial decisions up to a threshold.

### Primary Goal
Keep jobs moving. Ensure no job gets stuck. Handle escalations.

### What They See
- **Dashboard**: Operational metrics only (jobs by status, overdue, waiting approval, throughput, mechanic load). Manager does **not** see Revenue KPIs (invoiced/collected) — `06_permission_matrix.md` gives Manager `None` on Revenue KPIs; this is authoritative, corrected from a prior "revenue visible" claim.
- **Workshop Board**: Full access — all jobs, all columns
- **Job Details**: All tabs including estimate and invoice (view), but cost/margin data hidden
- **Customers**: Full view and edit
- **Parts management**: Full access
- **Employees**: Can view technician schedule and assignments
- **Expenses**: View only, read-only (matches `06_permission_matrix.md`: Manager = View on "View expense records", None on Create/Edit/Delete expense)

### What They Can Do
- Create, edit, and move job cards
- Assign and reassign mechanics
- Approve estimates (up to $500 threshold; above that → Owner)
- Order parts
- Mark QC pass/fail
- Send customer notifications
- Add and edit parts pricing (not cost)

### What They CANNOT See
- Gross margin per job
- Parts cost (only parts price/markup visible)
- Revenue KPIs (invoiced/collected) — dashboard shows operational data only, per `06_permission_matrix.md`
- Ability to create, edit, or delete expense records (view-only access to expense records, see above)
- Salary/payroll data
- Full financial reports

### Key Screens
1. Workshop Board (primary daily view — open all day)
2. Job Detail → Work tab, Parts tab
3. Parts Management
4. Notifications queue

### Experience Notes
- Manager needs quick access to "blocked" jobs — filter by Waiting Approval or Waiting Parts
- Should be able to bulk-notify customers whose jobs are ready
- Needs a mechanic load view: who has how many active jobs

---

## Role 3: Service Advisor / Reception

### Who They Are
The customer-facing role. Handles check-ins, customer communication, estimates, and invoicing. May not have deep technical knowledge.

### Primary Goal
Make customers feel cared for. Get approvals quickly. Collect payment efficiently.

### What They See
- **Dashboard**: Only their own today — jobs checked in by them, jobs awaiting their action
- **Workshop Board**: All jobs (read), move jobs to their relevant states (cannot move Mechanic states)
- **Check-In screen**: Full access
- **Customers**: Create and edit
- **Vehicles**: Create and edit
- **Job Detail**: Overview, Estimate, Invoice, History tabs. CANNOT see cost price — only selling price
- **Estimates**: Create and send
- **Invoices**: Create and view
- **Payments**: Record payments
- **Appointments**: Create and view

### What They Can Do
- Check in vehicles (create job cards)
- Create estimates
- Send estimates to customer (WhatsApp/SMS)
- Record customer approval
- Create invoices
- Record payments
- Mark job "Ready for Pickup" notification sent
- Add customer notes

### What They CANNOT See
- Parts cost price (only selling price)
- Gross margin
- Other advisors' drafts (unless manager)
- Employee salaries
- Expense records
- Financial reports

### Key Screens
1. Check-In flow (start of each interaction)
2. Job Detail → Estimate tab (send and manage estimates)
3. Job Detail → Invoice tab (collect payment)
4. Customer profile (look up history, contact)
5. Workshop Board (monitor job status)

### Experience Notes
- Check-In must be under 60 seconds for returning customers
- Estimate screen must show what the customer will see (preview of customer-facing estimate)
- Invoice payment screen should be prominent with large "Record Payment" button
- Outstanding balance warning when customer arrives: "John has an unpaid balance of $85"

---

## Role 4: Technician / Mechanic

### Who They Are
Works on vehicles. Typically not office-based. Uses phone or workshop tablet. May have limited digital literacy.

### Primary Goal
Know what to work on next. Record what was done. Keep jobs moving.

### What They See
- **My Jobs**: List of jobs assigned to them, sorted by priority/urgency
- **Job Detail (simplified)**: 
  - Overview: Customer name, vehicle, complaint, promised time. NO financial data.
  - Work tab: Diagnosis text field, list of repair tasks with START/PAUSE/COMPLETE buttons
  - Parts tab: List of parts for their job (name, status). NO pricing.
  - Media tab: Upload photos only
  - History: Read only
- **NO access to**: Estimate, Invoice, Dashboard financials, any other mechanic's jobs

### What They Can Do
- View their assigned jobs
- Mark tasks: In Progress / Paused / Complete
- Add diagnosis text
- Upload photos
- Request a part (flag "need part X" to advisor)
- Add technical notes per task
- Mark job "Ready for QC"

### What They CANNOT See
- Any price, cost, revenue, or financial data
- Other mechanics' jobs (unless they're also assigned)
- Customer contact information (just customer first name)
- Estimate or invoice content

### Key Screens
1. My Jobs list (home screen on mobile)
2. Job Detail → Work tab (primary workspace)
3. Job Detail → Parts tab (check if parts arrived)
4. Photo upload (quick capture)

### Experience Notes
- Mobile-first design. Buttons must be large (thumb-friendly, 44px+ tap target)
- No sidebar — full-screen, simple layout
- START/PAUSE/COMPLETE buttons are the primary interaction
- Visual status: green = done, orange = in progress, gray = pending
- Minimize text reading — icons and colors do the heavy lifting
- If internet is slow, work states should update and sync when connection recovers

---

## Role 5: Accountant

### Who They Are
Financial oversight. May be part-time or external. Reviews invoices, expenses, and reconciles payments. Does not interact with the workshop floor.

### Primary Goal
Ensure financial records are accurate. Track revenue vs. expenses. Prepare for reporting.

### What They See
- **Financial Dashboard**: Revenue, collections, outstanding payments, expenses, gross profit
- **Invoice list**: All invoices, filter by status (paid/partial/unpaid)
- **Payment records**: All payments with method, date, amount
- **Customer Debts**: Full list with aging
- **Expenses**: Full view and create
- **Reports**: Revenue reports, P&L summary

### What They Can Do
- View all invoices and payments
- Create and edit expense records
- Export payment reports
- Add payment notes / reconciliation flags
- View customer debt aging

### What They CANNOT Do
- Create job cards or estimates
- Modify invoice line items (view only after creation)
- Access Workshop Board or job operational details
- Manage roles or settings
- Void invoices (Owner only)

### Key Screens
1. Financial Dashboard
2. Invoice list (filter, search, export)
3. Customer Debts list
4. Expenses
5. Reports / Export

### Experience Notes
- Accountant view can be simpler — less visual chrome, more data-dense tables
- Export to CSV/PDF is essential
- Date range filtering on all financial lists
- Should see GST/VAT split on invoices if applicable

---

## Role Access Summary

| Feature/Data | Owner | Manager | Advisor | Mechanic | Accountant |
|---|---|---|---|---|---|
| Workshop Board | Full | Full | View+limited | Own jobs | None |
| Job Financial Details | Full | View (no margin) | Sell price only | None | Full |
| Parts Cost Price | Full | Hidden | Hidden | Hidden | Full |
| Gross Margin | Full | Hidden | Hidden | Hidden | Full |
| Customer Contact Info | Full | Full | Full | Name only | Full |
| Create Estimates | Full | Full | Full | None | None |
| Approve Estimates | Full | Up to $500 | None | None | None |
| Create Invoices | Full | Full | Full | None | None |
| Record Payments | Full | Full | Full | None | View |
| Expenses | Full | View | None | None | Full |
| Reports | Full | Limited | None | None | Full |
| Settings | Full | None | None | None | None |
| User Management | Full | None | None | None | None |
