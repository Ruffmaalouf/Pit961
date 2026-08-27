# Phase 7 — Edge Cases

---

## Edge Case 1: Customer Rejects Only One Estimate Item

**What Triggers It**: Customer receives a multi-line estimate. Approves labor and alignment but rejects replacement of a specific part (e.g., "don't replace the air filter, I'll buy it myself").

**How the System Behaves**:
- Estimate moves to status: "Partially Approved"
- Each line item gets an individual approval status: Approved / Rejected / Pending
- Approved items are locked and ready to proceed
- Rejected items are flagged — associated parts should NOT be ordered
- System prompts advisor: "Some items were rejected. Do you want to proceed with approved items only, or revise the estimate?"

**What the UI Shows**:
- Estimate tab: line items show green checkmark (approved) or red X (rejected)
- Parts tab: rejected parts shown in gray with status "Not Proceeding — Customer Rejected"
- Job total updates to reflect only approved items: "Approved Total: $185 (of $235 estimated)"
- Warning banner: "This job has partially approved estimate. Check with customer before starting."

**Data State**:
- `estimate.status` = "partially_approved"
- Each `estimate_item.approval_status` = "approved" | "rejected" | "pending"
- Invoice will only include approved line items (rejected items excluded)
- History log: "Customer rejected: Air Filter Replacement ($25) — via WhatsApp, 14:32"

---

## Edge Case 2: Garage Discovers Additional Work During Repair

**What Triggers It**: Mechanic is repairing front control arm bushings and notices the brake pads are dangerously worn (not in original estimate).

**How the System Behaves**:
- Mechanic cannot add billable work unilaterally
- Mechanic raises a flag: "Additional work discovered" with description
- Job status is paused on that task (not moved to complete)
- Advisor receives notification: "Ahmed flagged additional work on Job #047 — BMW 328i"
- Advisor reviews and creates a "Supplemental Estimate" (new estimate linked to same job)
- Supplemental estimate goes through full approval flow before work proceeds

**What the UI Shows**:
- Work tab: "Additional Findings" section shows mechanic's note with photos
- Banner on job: "Additional work pending approval — [View Supplement]"
- Estimate tab: Original estimate + Supplement section showing new items
- Status shows "Waiting Approval" (again, even if repair was in progress)

**Data State**:
- `job.has_supplement` = true
- `estimate.type` = "supplemental", linked to `parent_estimate_id`
- Supplement status tracked independently
- Final invoice can include both original + approved supplemental items

---

## Edge Case 3: Wrong Part Arrives

**What Triggers It**: Advisor orders Part A for the BMW; supplier sends Part B. Mechanic tries to install it and it doesn't fit.

**How the System Behaves**:
- Mechanic uses "Flag Issue" on the part in the Parts tab
- Reason: "Wrong Part" — with optional note/photo
- Part status changes: "Arrived" → "Issue: Wrong Part"
- Advisor receives notification immediately
- Job is blocked — status remains "Waiting Parts" (cannot move to Repairing until resolved)

**What the UI Shows**:
- Parts tab: Part shown with 🔴 "Wrong Part" badge
- Workshop Board: Job card shows "Parts Issue" indicator
- Advisor dashboard: "Action Needed: Wrong part for Job #047"
- Advisor options: Contact supplier, re-order correct part, mark for return

**Data State**:
- `job_part.status` = "issue_wrong_part"
- New part order created for correct item (linked to original order as "replacement for")
- Wrong part tracked for return — `part_return.reason` = "wrong_part", `part_return.supplier_reference` = order number

---

## Edge Case 4: Part Gets Returned

**What Triggers It**: Part was ordered but job was cancelled, wrong part arrived, or part not needed after diagnosis revision.

**How the System Behaves**:
- Advisor initiates "Return Part" action on the part record
- Required fields: Reason (wrong part / job cancelled / excess order), Supplier reference number, Return date
- Part status: "Returning" → "Returned"
- Expense/cost record adjusted: part cost removed from job if returned before use

**What the UI Shows**:
- Parts tab: Part shows "Returned" status in gray
- If part was on invoice: advisory message: "This part was returned. Invoice may need revision."
- Return log visible in job history: "Part returned: Front Bushing Set — Supplier Credit Expected"

**Data State**:
- `job_part.status` = "returned"
- `job_part.return_date`, `job_part.return_reason` populated
- Part cost removed from job's parts_cost_total if returned before invoiced
- If already invoiced: requires invoice revision workflow (Edge Case 11)

---

## Edge Case 5: Customer Supplies Their Own Part

**What Triggers It**: Customer arrives with their own part ("I bought this oil filter online, can you install it?").

**How the System Behaves**:
- Advisor adds part to job with type: "Customer-Supplied"
- No cost, no selling price for this part — $0 on invoice for the part line
- Labor for installation still billed
- System automatically adds warranty disclaimer to estimate and invoice

**What the UI Shows**:
- Parts tab: Part shows "👤 Customer Supplied" badge in a distinct color (purple/teal)
- Estimate tab: Part line shows at $0 with note "Customer-supplied — no warranty"
- When sending estimate to customer: disclaimer text auto-appended: "Customer-supplied parts carry no warranty from this garage."
- If customer-supplied part causes additional damage: separate job, no liability on garage (documented)

**Data State**:
- `job_part.supplied_by` = "customer"
- `job_part.cost` = 0, `job_part.price` = 0
- `estimate.has_customer_parts` = true → triggers disclaimer
- Warranty flag set: cannot create warranty return claim for customer-supplied parts

---

## Edge Case 6: Vehicle Stays Overnight

**What Triggers It**: Job not completed by closing time. Vehicle remains in workshop overnight.

**How the System Behaves**:
- At close of day, system detects jobs still in Checked In / Repairing / QC / Ready status
- Manager/Owner sees "Overnight Vehicles" list on dashboard
- Vehicle must be noted (keys location, customer notified, bay locked?)
- Job continues next day — no data changes, just notification/tracking

**What the UI Shows**:
- Workshop Board: Job card shows 🌙 "Overnight" badge after closing time passes
- Dashboard "Needs Attention": "3 vehicles overnight — [View list]"
- Owner can set "Overnight note": "Keys in cabinet B3, customer notified at 18:15"

**Data State**:
- `job.overnight` = true (auto-set when still active past closing_time)
- `job.overnight_note` (optional text)
- `job.customer_notified_at` (timestamp of notification)
- No financial or status changes — job resumes in the morning

---

## Edge Case 7: Job Has Two Mechanics

**What Triggers It**: A job requires two technicians (e.g., engine replacement needs a second pair of hands, or partial reassignment mid-job).

**How the System Behaves**:
- Primary mechanic is assigned at check-in
- Secondary mechanic can be added from the Work tab
- Each task can be individually assigned to either mechanic
- Both mechanics can see the full job on their My Jobs list
- Time tracking is per mechanic per task

**What the UI Shows**:
- Job Detail → Overview: "Mechanics: Ahmed (Primary), Khalil (Secondary)"
- Work tab: Each task shows assigned mechanic avatar
- Both mechanics see the job in their mobile view
- Invoice: Labor split can be shown as one combined line or separate lines

**Data State**:
- `job.primary_mechanic_id`, `job.secondary_mechanic_id`
- `task.assigned_mechanic_id` per task
- `task_time_log`: entries per mechanic per task
- Labor billing: total hours from all mechanics combined, or separate labor lines

---

## Edge Case 8: Customer Partially Pays

**What Triggers It**: Job ready, invoice $235. Customer has $150 cash and promises to pay the rest in 3 days.

**How the System Behaves**:
- Advisor records payment of $150 → Invoice status: "Partial"
- Remaining $85 is automatically added to customer's debt ledger
- System asks: "Release vehicle? Balance of $85 will be recorded as outstanding." → Confirm
- Job can be moved to "Delivered" even with partial payment (configurable policy)

**What the UI Shows**:
- Invoice tab: Paid: $150 | Balance: $85 | Status: Partial (orange badge)
- Customer profile: Outstanding Balance: $85 (highlighted in orange)
- Next time customer arrives: "This customer has an outstanding balance of $85. Remind them to settle before proceeding?" → Yes/No/Remind later
- Advisor dashboard: Outstanding balance list shows this customer

**Data State**:
- `invoice.status` = "partial"
- `invoice.total_paid` = 150, `invoice.balance` = 85
- `payment[0].amount` = 150, `payment[0].method` = "cash"
- `customer_debt.amount` = 85, `customer_debt.due_date` = (agreed date or +7 days)

---

## Edge Case 9: Customer Doesn't Pay at All

**What Triggers It**: Customer refuses to pay, disappears, or disputes the invoice entirely. Vehicle may be held at garage.

**How the System Behaves**:
- Job remains in "Ready" status — vehicle not delivered
- Invoice status: Unpaid
- After configurable period (e.g., 7 days): escalation alert to Owner
- Owner can: send reminder, apply late fee, escalate to debt, flag as dispute, or write off

**What the UI Shows**:
- Workshop Board: Job card remains in "Ready" column with 🔴 "Overdue — No Payment" badge
- Invoice tab: Status = Unpaid, Days Outstanding counter
- Owner Dashboard: "Needs Attention: Vehicle held awaiting payment — Job #047, $235 unpaid, 7 days"
- Owner options: [Send Reminder] [Apply Late Fee] [Mark as Dispute] [Write Off]

**Data State**:
- `job.delivery_blocked` = true (vehicle held)
- `invoice.status` = "unpaid"
- `invoice.days_outstanding` calculated field
- If written off: `invoice.status` = "written_off", amount shows in loss report

---

## Edge Case 10: Vehicle Returns for Same Problem Under Warranty

**What Triggers It**: Customer returns with the same complaint within the garage's warranty period (e.g., 30 days / 1,000 km — configurable).

**How the System Behaves**:
- When checking in the vehicle: system detects recent completed job on same vehicle with same complaint pattern
- System flags: "⚠️ This vehicle was serviced 15 days ago for a similar complaint. Possible warranty return?"
- Advisor selects: "Create Warranty Return Job" or "Create New Regular Job"
- If warranty return: linked to parent job, no charge to customer (or partial charge depending on policy)

**What the UI Shows**:
- Check-in screen: Banner "Previous visit detected: Job #047 — Control Arm Bushings — 15 days ago. Create warranty job?"
- Warranty job detail: Banner linking to parent job
- Estimate: $0 (or partial for non-warranty items)
- Job History: Links to parent job

**Data State**:
- `job.warranty_return` = true
- `job.parent_job_id` = original job ID
- `job.warranty_reason` (text)
- No invoice amount or $0 invoice
- Original mechanic notified of warranty return

---

## Edge Case 11: Invoice Needs Correction After Payment

**What Triggers It**: Customer paid $235 but advisor discovers they were overcharged — a part was listed twice. Or: discount was not applied that was promised.

**How the System Behaves**:
- Advisor cannot edit a paid invoice directly
- Owner opens "Invoice Correction" workflow
- Owner creates a Credit Note linked to the original invoice
- Credit note amount = overcharge amount
- Result: Original invoice (paid, $235) + Credit note (-$25) = Net $210 owed

**What the UI Shows**:
- Invoice tab: Original invoice shows PAID. Below it: "Credit Note #CN-001 — $25" with reason
- Net balance shown: Effective amount: $210
- If customer should receive refund: "Refund $25 pending" with refund method selection

**Data State**:
- Original invoice unchanged (audit trail)
- `credit_note.linked_invoice_id`, `credit_note.amount`, `credit_note.reason`
- `credit_note.created_by` = Owner (required)
- Refund recorded as negative payment entry

---

## Edge Case 12: Mechanic Accidentally Marks Task Complete

**What Triggers It**: Mechanic marks "Replace Front Bushings" as complete but hasn't done the work yet. Fat finger on mobile.

**How the System Behaves**:
- Within a grace period (5 minutes): Mechanic can undo completion directly
- After grace period: Mechanic must request reversal → notifies Manager
- Manager reviews and confirms reversal with a reason
- Reversal logged in job history

**What the UI Shows**:
- Immediately after marking complete: "Undo" button visible for 5 minutes (toast notification)
- After 5 min: Task shows "Complete" — Mechanic sees "Request Correction" button
- Manager sees notification: "Ahmed requested task correction — Replace Front Bushings"
- History log: "Task 'Replace Front Bushings' marked complete by Ahmed — Reversed by Manager (Khalil) — Reason: Marked in error"

**Data State**:
- `task.status` = "in_progress" (reversed)
- `task_correction_log.original_status`, `task_correction_log.corrected_by`, `task_correction_log.reason`
- Time log adjusted if timer was stopped

---

## Edge Case 13: Customer Owns Five Vehicles

**What Triggers It**: A fleet customer (taxi company, family with many cars) checks in their 5th vehicle. All 5 vehicles appear in their profile.

**How the System Behaves**:
- No artificial limit on vehicles per customer
- Customer profile shows all vehicles in a list
- At check-in, advisor picks the specific vehicle being brought in
- Each vehicle has its own independent service history
- Customer's outstanding balance is aggregated across all vehicles' jobs

**What the UI Shows**:
- Customer profile: "Vehicles (5)" — tab with list of all plates + make/model
- Check-in Step 2: Vehicle selection shows all 5 — most recently serviced highlighted
- Dashboard: Fleet customers (>3 vehicles) can be tagged — shown with a 🚘 fleet indicator

**Data State**:
- `customer.vehicle_ids` = [id1, id2, id3, id4, id5]
- No limit enforced at data model level
- `customer.is_fleet` auto-set when vehicle count > 3 (configurable)

---

## Edge Case 14: Same Vehicle Changes Owner

**What Triggers It**: Customer sells their car. New owner brings the same vehicle (same plate) to the garage. Or: the previous advisor linked the vehicle to the wrong customer.

**How the System Behaves**:
- Advisor searches by plate → finds existing vehicle under old owner
- Advisor uses "Transfer Vehicle" action: old owner disassociated, new owner assigned
- Old service history remains with the vehicle (visible to new owner — this is vehicle history, not financial history)
- Old financial records remain with the old customer

**What the UI Shows**:
- Vehicle profile: "Transfer Ownership" button → search for/create new customer → confirm
- After transfer: Vehicle shows under new owner. Banner on vehicle profile: "Ownership transferred from [Old Name] on [Date]"
- Old customer profile: Vehicle removed from their list, but service history entries remain in customer history as "Vehicle now owned by another customer"

**Data State**:
- `vehicle.customer_id` updated to new owner
- `vehicle_ownership_log`: previous_customer_id, new_customer_id, transferred_at, transferred_by
- Historical jobs remain linked to vehicle (not moved to new customer account)

---

## Edge Case 15: Car Arrives Without Appointment

**What Triggers It**: Walk-in customer at 10:00 AM when workshop is fully booked.

**How the System Behaves**:
- No blocking of check-in for walk-ins — system does not enforce capacity limits (V1.1 feature)
- Walk-in is checked in normally, marked as "Walk-In" (vs. "Appointment")
- Advisor sets promised time honestly based on actual capacity

**What the UI Shows**:
- Check-in: "Source" field = Walk-In (default) or Appointment
- Workshop Board: Walk-in cards may show a different subtle indicator
- Advisor has visibility of current bay load before setting promised time

**Data State**:
- `job.source` = "walk_in" | "appointment"
- No other behavioral difference at MVP

---

## Edge Case 16: Customer Cancels After Diagnosis (Who Pays for Diagnosis?)

**What Triggers It**: Mechanic spends 1 hour diagnosing. Customer receives estimate and decides not to proceed ("it's too expensive, I'll go elsewhere").

**How the System Behaves**:
- Garage may charge a diagnosis fee (configurable in Settings: "Diagnosis Fee Policy")
- Policy options: (a) Free diagnosis, (b) Fixed diagnosis fee, (c) Fee waived if repair proceeds
- If fee applies: Advisor creates a reduced invoice for diagnosis only
- Customer must pay diagnosis fee before vehicle is released

**What the UI Shows**:
- When customer rejects estimate: Prompt: "Customer rejected estimate. Apply diagnosis fee? ($50 — configured)"
- Options: [Charge Diagnosis Fee] [Waive Fee] [Create Custom Amount]
- If fee charged: invoice created for diagnosis fee only, linked to job
- Job status: "Cancelled — Diagnosis Fee Charged" or "Cancelled — No Charge"
- History: "Customer declined estimate. Diagnosis fee of $50 charged. Vehicle released."

**Data State**:
- `job.status` = "cancelled"
- `job.cancellation_reason` = "customer_declined_estimate"
- `invoice.type` = "diagnosis_fee" (if applicable)
- `estimate.status` = "rejected"

---

## Edge Case 17: Repair Requires Outsourced Service

**What Triggers It**: Job requires work the garage can't do in-house — e.g., wheel alignment at a specialist, engine rebuilding at a machine shop, windscreen replacement.

**How the System Behaves**:
- A repair task can be flagged as "Outsourced"
- Outsourced task has: Supplier name, expected return date, cost to garage, billed amount to customer
- Vehicle may leave the garage temporarily (noted on job card)
- When vehicle returns: task marked "Returned from Outsource"

**What the UI Shows**:
- Work tab: Task with "🔗 Outsourced" badge, showing supplier name and expected date
- Workshop Board: Job shows "Outsourced — Waiting Return" in the Repairing column
- Parts & Stock: Outsource costs treated as a line item (like a part) for margin calculation

**Data State**:
- `task.outsourced` = true
- `task.outsource_supplier`, `task.outsource_cost`, `task.outsource_billed`
- `task.outsource_sent_at`, `task.outsource_returned_at`
- Margin calculation includes outsource cost in COGS
