# Phase 5 — Screen Inventory

---

## Screen 01: Dashboard — Owner View

**Purpose**: Give the Owner a real-time health check of the business  
**Primary User**: Owner  
**Primary Action**: Identify and navigate to problems (overdue jobs, unpaid invoices)  
**Secondary Actions**: Navigate to Workshop Board, navigate to specific job  
**Information Displayed**:
- Today: Cars Checked In, Delivered, Open Jobs, Waiting Approval, Waiting Parts, Overdue
- Revenue: Today's invoiced, Today's collected, Month-to-date collected, Outstanding total
- Gross Profit: Today's revenue minus parts cost (calculated on completed jobs)
- Needs Attention: List of jobs needing action (overdue, waiting approval >4h, partial payment >7d)
- Recent Activity: Last 5 job status changes

**Permissions**: Owner only (full data)  
**Empty State**: "No activity yet today. Check in your first vehicle to get started."  
**Error State**: Data loading failure → Skeleton loader → "Unable to load data. Retry."  
**Mobile Behavior**: Stack KPI cards vertically 2-per-row, Needs Attention list below

---

## Screen 02: Dashboard — Manager View

**Purpose**: Operational status without financial depth  
**Primary User**: Manager  
**Primary Action**: Identify blocked/stuck jobs  
**Information Displayed**:
- Jobs by status (counts per column)
- Mechanic load (jobs per mechanic today)
- Parts expected today
- Overdue jobs list (clickable → job detail)

**Permissions**: Manager role  
**Mobile Behavior**: Same as owner, revenue section hidden

---

## Screen 03: Dashboard — Advisor View

**Purpose**: My pending tasks  
**Primary User**: Service Advisor  
**Information Displayed**:
- My check-ins today
- Jobs waiting for customer approval (sent from my desk)
- Jobs ready for pickup (I need to call customer)
- Outstanding balances on customers I checked in

**Permissions**: Advisor role

---

## Screen 04: Dashboard — Mechanic View (Mobile Primary)

**Purpose**: Quick access to today's tasks  
**Primary User**: Mechanic  
**Information Displayed**:
- My active jobs (in-progress first)
- My pending jobs (assigned but not started)
- Parts due for my jobs
- Completed jobs today (count)

**Permissions**: Mechanic role — NO financial data

---

## Screen 05: Workshop Board (Kanban)

**Purpose**: Real-time visual of all active jobs  
**Primary User**: Manager, Advisor, Owner  
**Primary Action**: Click a job card to open Job Detail  
**Secondary Actions**: Move job to next column (via button on card), filter by mechanic, filter by date  
**Information Displayed Per Card**:
- Job # and vehicle (plate + make/model/year)
- Customer first name
- Assigned mechanic
- Complaint summary (1 line)
- Promised delivery time (red if overdue)
- Status badge
- Visual indicators: 🔴 Overdue, 🟠 Waiting Approval (pulse), 🔵 Customer Waiting

**Columns**: Checked In | Diagnosing | Waiting Approval | Waiting Parts | Repairing | QC | Ready | Delivered

**Permissions**: Owner (full), Manager (full), Advisor (view + limited move), Mechanic (own jobs only on mobile)  
**Empty State Per Column**: Subtle "Empty" placeholder — does not take up visual space  
**Error State**: "Board couldn't load. Pull to refresh."  
**Mobile Behavior**: Horizontal scroll across columns, or switch to list view

---

## Screen 06: Workshop Board — List View

**Purpose**: Tabular view when board has too many cards to scan  
**Primary User**: Manager, Owner  
**Primary Action**: Sort and filter jobs  
**Columns**: Job #, Customer, Vehicle, Mechanic, Status, Promised Time, Last Updated  
**Permissions**: Same as Board

---

## Screen 07: Check-In Step 1 — Search

**Purpose**: Find existing customer/vehicle before creating new records  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Advisor  
**Primary Action**: Search by plate number or phone number  
**Information Displayed**: Search input, recent check-ins (last 5), shortcut "New Customer" button  
**Permissions**: Advisor, Manager, Owner  
**Empty State**: Prompt to search  
**Mobile Behavior**: Full-screen search input, large keyboard-friendly

---

## Screen 08: Check-In Step 2 — Customer Found

**Purpose**: Confirm customer identity and vehicle before proceeding  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Advisor  
**Primary Action**: Confirm and proceed  
**Information Displayed**: Customer name, phone, vehicles list (with plate, make/model), outstanding balance warning if applicable  
**Secondary Actions**: Edit customer details, add new vehicle for this customer  
**Permissions**: Advisor+

---

## Screen 09: Check-In Step 3 — New Customer / New Vehicle

**Purpose**: Create new records for first-time customers  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Advisor  
**Fields**: First name, last name, phone, email (optional), WhatsApp number (optional)  
**Vehicle fields**: Plate number, make, model, year, VIN (optional), color  
**Permissions**: Advisor+

---

## Screen 10: Check-In Step 4 — Job Creation

**Purpose**: Record the current visit details  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Advisor  
**Fields**:
- Mileage at intake (required)
- Customer complaint (text — verbatim from customer)
- Advisor notes (technical interpretation)
- Assign mechanic (dropdown)
- Promised delivery (date/time picker, or "Will advise after diagnosis")
- Customer waiting? (yes/no — triggers blue indicator on board)

**Primary Action**: Create Job  
**Permissions**: Advisor+

---

> **Reconciled to match the approved prototype (`prototype.html`).** The prototype implements Job Detail
> as **sections within a single consolidated page**, not as separate tabs. Screens 11–18 below are
> reframed from "tabs" to "sections of one page" accordingly; the field-level requirements for each section
> are unchanged and still apply.

## Screen 11: Job Detail — Overview Section

**Purpose**: Full summary of the job for any role  
**Primary User**: All  
**Information Displayed**:
- Job number, creation date, status
- Customer: name, phone (click to call), WhatsApp link
- Vehicle: plate, make/model/year, color, mileage at intake
- Complaint (verbatim)
- Advisor notes
- Assigned mechanic
- Promised delivery time (highlighted red if overdue)
- Outstanding balance on this job (if partially paid)

**Primary Action**: Change status (move to next stage)  
**Secondary Actions**: Reassign mechanic, edit complaint notes, send message to customer  
**Permissions**: Advisor+ (financials hidden from Advisor and Mechanic)  
**Mobile Behavior**: Full-screen section (within the consolidated Job Detail page), stacked sub-sections

---

## Screen 12: Job Detail — Work Section

**Purpose**: Diagnosis and repair task management  
**Primary User**: Mechanic (edit), Manager (review), Advisor (read)  
**Information Displayed**:
- Diagnosis section: text field for findings + "Additional findings" for new discovered issues
- Repair Tasks list: each task has Name, Assigned Mechanic, Status (Pending/In Progress/Done), Notes field
- Task status: large START / PAUSE / DONE buttons (mechanic mobile view)
- Time logged per task (mechanic start/end timestamps)

**Primary Action**: Update task status  
**Secondary Actions**: Add diagnosis notes, add new task, add photo per task  
**Permissions**: Mechanic (own tasks only), Manager/Owner (all tasks)

---

## Screen 13: Job Detail — Parts Section

**Purpose**: Track parts for this job  
**Primary User**: Advisor, Manager  
**Information Displayed**:
- Parts list: Part name, Part #, Quantity, Supplier, Cost price (hidden from Advisor), Selling price, Status (Needed/Ordered/Arrived/Installed)
- Expected arrival date per part
- Customer-supplied parts (flagged differently — no warranty)

**Primary Action**: Update part status  
**Secondary Actions**: Add part, mark arrived, mark installed, flag wrong part  
**Permissions**: Advisor (selling price only), Manager (both prices), Mechanic (name + status only)

---

## Screen 14: Job Detail — Estimate Section

**Purpose**: Create, review, and manage the customer-facing quotation  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Advisor  
**Information Displayed**:
- Line items: Description, Qty, Unit Price, Total
  - Parts section (from Parts section, auto-populated)
  - Labor section (hourly or fixed)
  - Additional services (alignment, diagnostics fee, etc.)
- Subtotal, Tax (if applicable), Total
- Approval status: Pending / Approved / Partially Approved / Rejected
- Approval record: Who approved, When, How (in-person / WhatsApp / phone)

**Primary Action**: Send to Customer, Record Approval  
**Secondary Actions**: Edit line items, add discount, preview customer view  
**Permissions**: Advisor+ (create/send), Mechanic (hidden)

---

## Screen 15: Estimate — Customer Preview

**Purpose**: Show advisor how the estimate looks to the customer  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Advisor  
**Information Displayed**: Clean estimate view with garage logo, customer name, vehicle, line items, total, approval button  
**Primary Action**: Copy link / Share via WhatsApp

---

## Screen 16: Job Detail — Media Section

**Purpose**: Photos and documents for this job  
**Primary User**: Mechanic (upload), Advisor (view), Customer-facing  
**Information Displayed**:
- Check-in photos (pre-existing damage)
- During-repair photos (tagged by task)
- After-repair photos
- Documents: signed estimate, supplier invoices

**Primary Action**: Upload photo  
**Secondary Actions**: Tag photo (check-in/during/after), delete photo  
**Permissions**: Mechanic (upload own), Advisor (view all), Owner (delete)

---

## Screen 17: Job Detail — History Section

**Purpose**: Audit trail of every event on this job  
**Primary User**: Manager, Owner  
**Information Displayed**:
- Chronological list: timestamp, actor (name + role), event type, details
- Events: Job created, Mechanic assigned, Status changed, Estimate sent, Approval received, Part ordered, Part arrived, Task started, Task completed, QC passed/failed, Invoice created, Payment recorded, Message sent
  
**Permissions**: Advisor+ (read only), Mechanic (read, own events only)  
**Mobile**: Scrollable timeline

---

## Screen 18: Job Detail — Invoice Section

**Purpose**: Financial settlement of the job  
**Primary User**: Advisor  
**Information Displayed**:
- Invoice number (auto-generated: INV-YYYY-XXXX)
- Date issued
- Line items (from approved estimate)
- Subtotal, Tax, Total
- Payment records: list of payments (date, amount, method)
- Total Paid, Balance Due
- Status badge: Unpaid / Partial / Paid

**Primary Action**: Record Payment  
**Secondary Actions**: Print invoice, Email invoice, Void invoice (Owner only)  
**Permissions**: Advisor (create/view/record payment), Accountant (full view), Mechanic (hidden)

---

## Screen 19: Record Payment Modal

**Purpose**: Capture a payment transaction  
**Primary User**: Advisor  
**Fields**:
- Amount received
- Payment method: Cash / Card / Bank Transfer / Cheque / Other
- Reference number (optional, for bank transfers)
- Notes

**Primary Action**: Confirm Payment  
**Behavior**: If payment = balance → Invoice status → Paid. If partial → Partial. Show confirmation with receipt option.

---

## Screen 20: Customer List

**Purpose**: Search and manage all customers  
**Primary User**: Advisor, Manager, Owner  
**Information Displayed**: Customer name, phone, vehicle count, last visit, outstanding balance (red if >0)  
**Primary Action**: Click to open Customer Profile  
**Secondary Actions**: Search/filter, export  
**Permissions**: Advisor+ (accountant: read only)

---

## Screen 21: Customer Profile

**Purpose**: Complete customer record  
**Primary User**: Advisor, Manager  
**Information Displayed**:
- Contact info (name, phone, WhatsApp, email)
- Vehicles owned (list with plate, make/model)
- Outstanding balance (total across all jobs)
- Service history (last 5 jobs with date, vehicle, work done, amount)
- Communication log (WhatsApp messages sent/received)
- Debt aging: 0-30, 31-60, 60+ days

**Primary Action**: Start Check-In (for this customer), Record Payment (against outstanding balance)  
**Secondary Actions**: Edit contact info, Add vehicle, Send WhatsApp message  
**Permissions**: Advisor+ (financials hidden from Mechanic — Mechanic cannot access this screen)

---

## Screen 22: Vehicle Profile

**Purpose**: Full history of a specific vehicle  
**Primary User**: Advisor, Manager  
**Information Displayed**:
- Vehicle details: plate, make, model, year, color, VIN, current mileage
- Mileage history (graph or list of each visit's mileage)
- All repair orders (date, work done, total, mechanic)
- Recommendations (open service items)

**Primary Action**: Start Check-In for this vehicle  
**Permissions**: Advisor+

---

## Screen 23: Parts Management — All Jobs

**Purpose**: Parts overview across all active jobs  
**Primary User**: Manager, Advisor  
**Information Displayed**:
- Table: Job #, Vehicle, Part Name, Supplier, Status, Expected Date
- Filter by status: Needed / Ordered / Arriving Today / Overdue

**Primary Action**: Update part status  
**Permissions**: Manager (full), Advisor (update status)

---

## Screen 24: Finance — Invoice List

**Purpose**: All invoices with filter and search  
**Primary User**: Accountant, Owner  
**Information Displayed**: Invoice #, Customer, Vehicle, Date, Total, Paid, Balance, Status  
**Filters**: Date range, Status, Customer  
**Primary Action**: Click invoice → Invoice detail  
**Secondary Actions**: Export CSV/PDF

---

## Screen 25: Finance — Customer Debts

**Purpose**: Outstanding balances overview  
**Primary User**: Accountant, Owner  
**Information Displayed**: Customer name, oldest unpaid date, total outstanding, aging breakdown  
**Primary Action**: Open customer profile or send reminder  
**Secondary Actions**: Mark as bad debt (Owner only)

---

## Screen 26: Finance — Expenses

**Purpose**: Log and track operational expenses  
**Primary User**: Accountant, Owner  
**Information Displayed**: Date, Category, Description, Amount, Reference  
**Primary Action**: Add Expense  
**Categories**: Parts purchases, Rent, Utilities, Salaries, Marketing, Equipment, Other

---

## Screen 27: Settings — Garage Profile

**Purpose**: Core garage configuration  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Owner  
**Fields**: Garage name, logo upload, address, phone, WhatsApp number, email, currency, timezone, tax name, tax rate, invoice prefix, invoice starting number

---

## Screen 28: Settings — Team Management

**Purpose**: Manage user accounts  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Owner  
**Information Displayed**: Name, email, role, last active, status (active/invited/suspended)  
**Primary Action**: Invite new user, change role, deactivate  
**Permissions**: Owner only

---

## Screen 29: Settings — Subscription

**Purpose**: Subscription management  
**Prototype status**: Designed on paper, not yet prototyped — needs a follow-up design pass.  
**Primary User**: Owner  
**Information Displayed**: Current plan ($30/mo), billing date, payment method on file, invoice history  
**Primary Action**: Manage billing (links to Stripe portal)  
**Note**: Trial garages see days remaining prominently

---

## Screen 30: Mechanic Mobile — My Jobs

**Purpose**: Mechanic's personal dashboard  
**Primary User**: Mechanic  
**Information Displayed**: Assigned jobs sorted by status (In Progress first, then Pending), job card shows vehicle, complaint summary, promised time (red if late)  
**Primary Action**: Open job  
**Mobile Behavior**: Full screen, large tap targets, swipe to see job details
