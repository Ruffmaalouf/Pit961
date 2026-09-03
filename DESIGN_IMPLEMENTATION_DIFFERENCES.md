# Design Implementation Differences — Phase 2 (P2-WP9 audit)

Source of truth: `prototype.html` (dark theme, `#e2892f` accent, IBM Plex Mono for
data/labels, card-based layout, colored status lanes). Everything below is a net-new or
extended element required to cover P2-WP2–WP6; nothing here restyles or replaces anything
the prototype already shows. Each item should be built in the prototype's existing visual
language and cite the existing pattern it reuses.

Audited by Design Lead (P2-WP9, 2026-09-03). Classifications:

| Area | Classification |
|---|---|
| (a) Customers | needs material interaction specification |
| (b) Job detail | needs minor extension |
| (c) Estimate/Money flows | needs material interaction specification |
| (d) Repair Tasks | needs minor extension |
| (e) Parts | needs minor extension |
| (f) Invoice/Payment | needs minor extension |

## Customers / Vehicles (P2-WP2)

1. **New: Customer detail screen.** The prototype's Customers list currently routes a row
   click into the Job screen as a mock stand-in — this must be replaced with a real
   Customer detail screen: an identity card (name, phone, email, avatar initials — reuse
   the customer-row visual language), a "Vehicles" section listing each vehicle as a
   compact card reusing the VEHICLE card fields already defined in Job detail
   (make/model/year, plate, mileage, VIN), a read-only "Jobs" history list reusing the
   Floor board job-card visual language, and a balance/invoices summary reusing the Money
   screen's invoice-row style.
2. **New: New/Edit Customer form.** Modal or panel styled like the existing "Record
   payment" modal (header + field stack + submit button). Fields: name, phone, email,
   notes.
3. **New: New/Edit Vehicle form.** No equivalent exists anywhere in the prototype. Needed
   as a modal/panel matching the "Record payment" modal pattern, with fields matching the
   existing VEHICLE card (make, model, year, plate, VIN, mileage) plus an owner/customer
   link field. Entry points: "+ Vehicle" on the new Customer detail screen, and from the
   check-in flow (see below).
4. **Extend: Customers list row-click target.** Redirect from the Job screen mock to the
   new Customer detail screen (item 1).

## Job / Floor (P2-WP3)

5. **New: Check-in vehicle form.** The Floor screen's "Check in vehicle" button has no
   designed target. Build as a simple intake form/wizard that assembles already-
   established field groups rather than inventing new visual language: customer
   select-or-create (reuse item 2), vehicle select-or-create (reuse item 3), issue
   description, bay assignment — the last three fields already appear as read fields on
   Floor board job cards and the Job detail VEHICLE/JOB cards, so this form is a
   composition, not a new pattern.

## Estimate / Approval (P2-WP4)

6. **New: Estimate creation / line-item editor.** Nothing in the prototype lets staff
   build or edit an estimate — only a history-feed entry referencing one already sent.
   Add an edit mode inside the Job detail's existing "Work & parts, one list" component:
   each task/part line gets an editable price field (labor + parts), a running estimate
   total displayed prominently, and a "Send estimate" action that surfaces the channel
   choice already implied by the feed's "channel: WhatsApp" annotation as an explicit
   picker at send time.
7. **New: Discount-application control.** No equivalent exists. Add a low-friction inline
   control near the estimate total (e.g. "Apply discount" link opening an inline
   percent/amount field, visually capped at the manager's 15% ceiling). Keep this a single
   inline step — no modal, no confirmation dialog — so its visual weight reads as routine
   manager discretion, deliberately lighter than item 8.
8. **New: Owner-approval-required gate treatment.** No equivalent exists — the
   prototype's "Approved (by customer...)" feed entry is a *customer* approving an
   already-sent estimate, unrelated to this internal policy gate for estimates above $500.
   Needs:
   - A distinct badge/banner reusing the Floor board's existing "WAITING APPROVAL" lane
     color (`#c98a2f`) and label vocabulary, since that color is already established
     in-product for exactly this semantic (a job blocked on a human approval step).
   - An explicit Owner approve/reject action that is a **separate control** from the
     discount control in item 7 — not the same button, not the same modal — so a
     manager's routine discretionary action and an owner-gated approval action cannot be
     visually or interactionally confused.
   - A blocked-state treatment reusing the exact pattern already established for the
     command palette's "deliver" action (blocked with a toast when balance > 0): block
     sending/proceeding until the Owner approves, with a toast explaining why.
9. **Extend: Money screen.** Currently invoice-only ("Invoices, payments and
   receivables"). Add a distinct Estimates list/tab or filter state showing estimates
   awaiting owner-approval as their own status (parallel to the existing RECEIVABLE
   status), so the Owner has one place to see everything blocked on their approval rather
   than hunting per-job.

## Repair Tasks (P2-WP5, task half)

10. **New: "+ Task" affordance.** Add to the combined list header, styled identically to
    the existing "+ Part" affordance (same button treatment and position), to add a
    repair-task line item (description, assigned tech, estimated time).
11. **New: Per-task status control.** Parts cards already show a status ("Part in
    transit") and an "advancePart" action button whose label changes by state. Tasks need
    the same pattern: a status pill (e.g. Not started / In progress / Done) and an
    "advanceTask" action button following the identical visual/interaction treatment.
12. **New: Per-task identifying sub-line.** Parts cards show an OEM number + supplier name
    as their sub-line. Task cards need the equivalent — assigned tech name + estimated
    time — using the same sub-line typographic treatment (IBM Plex Mono, muted color).

## Parts (P2-WP5, parts half)

13. **New: "+ Part" add-part form.** The affordance exists in the combined list but its
    resulting form is undefined. Add an inline form or modal (styled like "Record
    payment") with fields: description, OEM number, supplier (dropdown), quantity, unit
    cost — matching the fields already displayed on the resulting part card.
14. **Extend: Parts & suppliers screen row action.** Unlike the Money screen's per-row
    contextual action button (Resend/Chase/Charge/View), the Parts & suppliers list has
    none. Add an equivalent per-row action (e.g. "Mark received" / "View job") reusing
    Money's row-button pattern for cross-screen consistency.

## Invoice / Payment (P2-WP6)

15. **New: Void invoice action.** No equivalent exists. Add a status-dependent contextual
    action to the Money screen's existing per-row action-button pattern ("Void," alongside
    Resend/Chase/Charge/View). Because voiding is destructive to a customer-facing record,
    require a confirmation modal styled like "Record payment" (header + invoice# +
    customer + a reason field) rather than a silent one-click action. Per Owner Decision
    #6, this action must be disabled/blocked (not merely warned) when the invoice has any
    recorded, non-voided payment — the blocked state should reuse the same
    balance-blocked-with-toast pattern as the command palette's "deliver" action.
16. **New: Refund payment modal.** Deferred — Owner Decision #6 places formal refund/
    credit-note workflow out of scope for Phase 2. Retained here as a forward note for
    whichever later phase picks it up: mirror "Record payment" 1:1 (header "Refund
    payment," invoice number + customer name, itemized original-payment breakdown, amount
    field, method selector, submit button).
17. **Extend: Invoice status vocabulary.** Give voided invoices their own status color in
    the Money screen's list, parallel to and visually distinguishable from the existing
    RECEIVABLE status, so voided state is legible at a glance.

## Flags for follow-on design work

- The prototype currently gives zero UI distinction between manager-level discretionary
  actions (discount up to 15%) and owner-approval-gated actions (estimates ≥$500) — both
  are simply absent. Items 7/8 above deliberately specify separate, non-confusable
  controls for `ui-ux-designer` to wireframe from.
- No brand-dependent decisions were needed for any of the above — all extensions reuse
  existing codename-neutral prototype visual language, so nothing here requires the Owner
  to settle final product branding.
- No platform-admin/garage-tenant conflation risk in this audit — all six areas are
  garage-tenant daily-use surfaces; the owner-approval gate in item 8 is a garage-tenant
  *role* permission (Owner vs. Manager vs. Tech within one tenant), not a platform-admin
  surface.
