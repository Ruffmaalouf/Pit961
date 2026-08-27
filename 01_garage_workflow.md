# Phase 1 — Garage Operation Model

## The Complete Working Day

---

### STEP 0: BEFORE THE DAY STARTS (07:00–08:00)

**WHO:** Manager / Owner  
**WHEN:** Before the workshop opens  
**WHY:** Plan the day, assign bays, review overnight vehicles  
**WHAT THEY NEED:** Yesterday's open jobs, today's appointments, mechanic schedule, parts deliveries expected  
**WHAT HAPPENS NEXT:** Workshop board is set, mechanics know their assignments  
**WHAT CAN GO WRONG:** Mechanic no-show, parts supplier late, key appointment customer cancels

Actions:
- Review Workshop Board: any jobs left from yesterday?
- Check appointment list for the day
- Verify parts orders that should arrive today
- Assign mechanics to bays
- Flag any waiting-approval jobs to follow up with customers

---

### STEP 1: CUSTOMER ARRIVES / CHECK-IN (08:00–18:00, rolling)

**WHO:** Service Advisor / Reception  
**WHEN:** Customer walks in (with or without appointment)  
**WHY:** Create a formal job record; set customer expectations  
**WHAT THEY NEED:** Customer name/phone, vehicle plate, mileage, customer complaint in their own words, promised delivery time  
**WHAT HAPPENS NEXT:** Job card created → assigned to mechanic → appears on Workshop Board  
**WHAT CAN GO WRONG:**
- Customer can't describe problem clearly
- Vehicle has no history in system (new customer)
- Customer drops off car but is unreachable later
- Mileage not recorded (odometer service interval tracking breaks)
- Customer has outstanding balance from previous visit

Reception workflow:
1. Search customer by plate or phone number
2. If new: create customer profile + vehicle profile
3. If returning: confirm vehicle details, note any changes (new owner, new contact)
4. Record mileage at intake
5. Record complaint verbatim (customer's words + advisor's technical interpretation)
6. Photograph vehicle exterior (dents, scratches) — pre-existing damage documentation
7. Assign to mechanic or place in queue
8. Set promised delivery estimate (or leave open until diagnosis)
9. Print or send job reference to customer
10. Customer leaves or waits

---

### STEP 2: DIAGNOSIS (variable — 15 min to 2 hours)

**WHO:** Technician/Mechanic (sometimes Service Advisor observes)  
**WHEN:** As soon as vehicle is in bay  
**WHY:** Identify root cause before touching anything billable  
**WHAT THEY NEED:** Customer complaint, vehicle history (previous repairs, recurring issues), diagnostic tools, lift access  
**WHAT HAPPENS NEXT:** Diagnosis recorded → Estimate created OR customer told nothing wrong found  
**WHAT CAN GO WRONG:**
- Mechanic finds additional problems not related to complaint
- Diagnosis requires specialized equipment not available in-house
- Vehicle has intermittent fault (can't reproduce in workshop)
- Customer insists on specific repair (e.g., "just replace the part, don't diagnose")
- Multiple possible causes — need to test replace to confirm

Mechanic workflow:
1. Open job on mobile (scan QR or find by job number)
2. Read complaint notes
3. Mark job as "Diagnosing" 
4. Perform diagnosis — visual, test drive, OBD scan
5. Record findings (text + photos of worn/broken parts)
6. List all required work: primary complaint + any additional findings
7. Request parts quote from system / supplier
8. Submit diagnosis to Service Advisor for estimate creation
9. Job moves to "Waiting Approval"

---

### STEP 3: ESTIMATE / QUOTATION (15–30 min)

**WHO:** Manager or Service Advisor (creates and, subject to the internal approval gate below, sends)  
**WHEN:** After diagnosis is submitted  
**WHY:** Get customer consent before spending their money  
**WHAT THEY NEED:** Diagnosis notes, parts costs, labor rates, customer contact  
**WHAT HAPPENS NEXT:** Sent to customer for approval (or held for internal Owner approval first — see below)  
**WHAT CAN GO WRONG:**
- Parts prices change between estimate and order
- Customer rejects part of the estimate
- Customer wants time to decide (job sits in limbo)
- Estimate needs to be revised after more diagnosis

Advisor/Manager workflow:
1. Open Job → Estimate section
2. Add line items: parts (with markup), labor, consumables
3. Review total with margin
4. **Internal approval gate (applies regardless of which role created the estimate):**
   - Estimate subtotal **≤ $500**: may be sent to the customer directly by whichever role created it (Manager or Advisor) — no additional internal approval needed.
   - Estimate subtotal **> $500**: requires explicit Owner approval before it can be sent to the customer. The estimate sits in a **"Pending Owner Approval"** state until the Owner approves it; only then can it be sent.
5. Send to customer via WhatsApp/SMS (link or PDF)
6. Log "Estimate Sent" with timestamp
7. Follow up if no response in X hours (configurable)

---

### STEP 4: CUSTOMER APPROVAL (async)

**WHO:** Customer (remotely or in person)  
**WHEN:** After estimate received  
**WHY:** Legal/financial consent — no work without approval  
**WHAT THEY NEED:** Clear line items with prices, ability to approve/reject individual items  
**WHAT HAPPENS NEXT:** Approved items → Parts ordered → Repair begins; Rejected items → noted and filed  
**WHAT CAN GO WRONG:**
- Customer rejects entire estimate (garage may charge diagnosis fee)
- Customer approves verbally but not in writing (dispute risk)
- Customer approves partial estimate (job scope changes)
- Customer unreachable for days

System records: who approved, what was approved, timestamp, method (in-person/WhatsApp/phone).

---

### STEP 5: PARTS ORDERING (same day to 3 days)

**WHO:** Service Advisor / Parts Manager  
**WHEN:** After customer approval  
**WHY:** Can't repair without parts  
**WHAT THEY NEED:** Approved parts list, supplier contacts, pricing  
**WHAT HAPPENS NEXT:** Parts arrive → Assigned to job → Repair begins  
**WHAT CAN GO WRONG:**
- Wrong part ordered (wrong fitment, wrong OEM number)
- Part out of stock (delay)
- Part arrives damaged
- Customer supplied their own part (warranty implications)
- Part price changed (estimate needs revision)
- Multiple suppliers needed for one job

Parts workflow:
1. System shows parts needed for approved jobs
2. Advisor assigns supplier per part
3. Mark as "Ordered" with expected arrival date
4. When part arrives: verify against order, mark as "Arrived"
5. Assign to job — part moves to bay
6. Mechanic marks "Installed" during repair

---

### STEP 6: REPAIR / WORK EXECUTION (1 hour to 3 days)

**WHO:** Technician/Mechanic  
**WHEN:** Parts in hand, bay available  
**WHY:** Core value of the garage  
**WHAT THEY NEED:** Parts, job card with repair tasks, any technical notes, tools  
**WHAT HAPPENS NEXT:** Work completed → QC check  
**WHAT CAN GO WRONG:**
- Mechanic discovers additional issues during repair (need new estimate)
- Part doesn't fit correctly (wrong part or vehicle variant)
- Repair takes longer than estimated (promised time missed)
- Two mechanics needed (job time split)
- Work paused (waiting for additional parts)
- Mechanic error — task marked complete by accident

Mechanic workflow:
1. Open job on mobile
2. START task → timer begins
3. Work on vehicle
4. PAUSE if interrupted
5. Take photos during repair (before/after)
6. Add notes per task
7. COMPLETE each task
8. Mark entire job ready for QC
9. Move car to QC area

---

### STEP 7: QUALITY CONTROL (15–30 min)

**WHO:** Manager or Owner (matching the 5-role model in `06_permission_matrix.md` — there is no Senior Technician or dedicated QC role)  
**WHEN:** After all repair tasks are marked complete  
**WHY:** Ensure repair quality before returning to customer  
**WHAT THEY NEED:** Job card with all completed tasks, vehicle access  
**WHAT HAPPENS NEXT:** QC passed → Invoice created → Customer notified; QC failed → Return to repair  
**WHAT CAN GO WRONG:**
- QC finds incomplete work
- Test drive reveals problem not fixed
- Cleanliness issue (grease on interior)
- QC skipped under time pressure

QC workflow:
1. Open job on tablet/phone
2. Check each repair task against QC checklist
3. Test drive if applicable
4. PASS: Mark QC complete, add notes, mark job "Ready"
5. FAIL: List failed items, reassign to mechanic, job returns to "Repairing"

---

### STEP 8: INVOICE CREATION (5–10 min)

**WHO:** Service Advisor / Accountant  
**WHEN:** After QC passes  
**WHY:** Legal document for payment  
**WHAT THEY NEED:** Approved estimate, any additions/changes during repair, parts costs, labor hours  
**WHAT HAPPENS NEXT:** Customer notified → Payment collected → Vehicle delivered  
**WHAT CAN GO WRONG:**
- Invoice differs from estimate (customer dispute)
- Additional work added without new approval
- Tax calculation error
- Invoice needs to be voided after payment (customer error, overcharge)

---

### STEP 9: CUSTOMER NOTIFICATION & PAYMENT (15–30 min)

**WHO:** Service Advisor / Reception  
**WHEN:** Job is "Ready" and invoice generated  
**WHY:** Collect payment, release vehicle  
**WHAT THEY NEED:** Invoice, payment terminal, customer contact  
**WHAT HAPPENS NEXT:** Paid → Vehicle released; Partial pay → Debt recorded; No pay → Debt recorded, vehicle held  
**WHAT CAN GO WRONG:**
- Customer can't pay full amount (partial payment)
- Customer disputes invoice
- Customer requests bank transfer (payment confirmation delay)
- Customer pays but has old balance (do they settle both?)

Payment workflow:
1. Notify customer via WhatsApp/SMS: "Your vehicle is ready. Total: $X"
2. Customer arrives
3. Review invoice together
4. Record payment (cash/card/transfer/partial)
5. Print/email receipt
6. If fully paid → mark invoice PAID
7. If partial → record amount, remaining balance added to customer debt
8. Release vehicle → record delivery mileage

---

### STEP 10: VEHICLE DELIVERY (5 min)

**WHO:** Service Advisor  
**WHEN:** Payment received (full or agreed partial)  
**WHY:** Formal handover, reset liability  
**WHAT THEY NEED:** Keys, vehicle, invoice copy  
**WHAT HAPPENS NEXT:** Job closed → Service history updated → Job archived  
**WHAT CAN GO WRONG:**
- Customer not satisfied (refuse to take vehicle)
- Key lost
- Vehicle needs to stay overnight (no transport, customer not available)

Delivery:
1. Walk customer to vehicle
2. Explain work done
3. Note next service recommendation
4. Hand over keys
5. Mark job DELIVERED in system
6. Record delivery mileage

---

### STEP 11: POST-VISIT (automated)

**WHO:** System / Manager  
**WHEN:** 24–48 hours after delivery  
**WHY:** Retention, warranty follow-up  
**WHAT THEY NEED:** Customer contact, job details  
**WHAT HAPPENS NEXT:** Customer feedback, next appointment booking  
**WHAT CAN GO WRONG:**
- Customer reports problem (warranty return)
- Customer ignores message

Automated:
- Send follow-up message: "How is your vehicle?"
- Set reminder for next service interval
- Record in service history

---

## End-of-Day Wrap (18:00–19:00)

**WHO:** Manager  
- Review Workshop Board: any jobs that should have been delivered?
- Overnight vehicles: noted and secured
- Tomorrow's appointments: pre-reviewed
- Outstanding customer approvals: follow up
- Cash reconciliation
- Parts due tomorrow: confirmed with supplier
