---
name: money-payments-credit
description: The executed-before-paid billing rule, settlement math, tutor credit, the payment ledger, and the master password. Use when touching billing, payments, ServicePaid/ServiceDone flags, AmountDue/Outstanding, credit allocation, or password recovery.
---

# Money, payments & credit rules

## Executed before paid — the charging rule

**Every service table carries two flags**: `ServicePaid` (the money arrived,
written by `SetPaidAsync` and `RegisterPaymentAsync`) and `ServiceDone` (the work
happened, written by `SetDoneAsync`). Neither is derived from the date — a booking
in the past is no evidence the sitter turned up. `UpdateAsync` touches neither, so
an edit cannot silently un-tick either one.

They are not symmetric. **Work is only billable once it has been carried out**, so
the order is always *executed → paid*:

- `ServiceItem.AmountDue` is the single home of that rule: `ServicePaid ||
  !ServiceDone ? 0 : Outstanding`. Everything that totals a balance goes through it
  — never re-filter on `ServicePaid` to build a figure, or unexecuted work creeps
  back into a bill. `AmountUpcoming` is its complement: what a booking will be
  worth once done, and zero after.
- The paid toggle is **disabled until the service is done** (`CanTogglePaid =>
  Done || Paid`), on both the agenda row and the service screen. Already-paid
  bookings stay togglable so a mistake can be undone.
- `PaymentAllocation.Allocate` (data layer, beside `ServicePayment`) does **not**
  apply the rule. It settles any service with an `Outstanding` balance, executed
  or not — the same eligibility `AllocateCredit` uses. Money the tutor has
  actually handed over must land somewhere, and refusing the booking it was
  plainly meant for would strand it as credit.

## Settling never reprices

Each service carries `AmountSettled` (how much has been paid against it) and
`CreditApplied` (how much of that came from tutor credit); `Outstanding` is
`Total - AmountSettled`. A part-paid 100 service stays a 100 service with 75
settled and 25 outstanding.

It used to cut the price to the remainder instead — that balanced, but destroyed
the record of what the service actually cost, and there was nowhere to say the
money came from credit. `RegisterPaymentAsync` therefore **adds** to
`AmountSettled` rather than assigning, because one service can be settled more
than once: some credit at booking, cash later.

**An advance is credit, not a paid service.** A tutor may pay before the work
happens; `ConfirmPayment` banks what it cannot allocate as `Tutors.Credit`.
`CreditSpender` (a Viewmodel DI singleton) then spends it **when a service is
booked** — `ServicesViewModel` calls it after creating the bookings.

Credit uses `PaymentAllocation.AllocateCredit`, which differs from `Allocate`
only in being recorded as `CreditApplied`; both settle unexecuted work. Money
already in hand is the deliberate exception to executed-before-paid: the rule
stops the sitter *asking* for money too early, not recording money the tutor has
handed over. What still enforces it is `AmountDue` and the disabled paid toggle —
the figures the sitter bills from. Deleting a service returns its `CreditApplied`
to the tutor, or the money would vanish with the row.

Both flags surface on the agenda row, the dog and tutor detail rows, the service
detail screen, and the two PNG reports as the `Execução` / `Pagamento` columns,
whose totals bill only executed work and list `A executar` separately.
`GetMonthlyIncomeAsync` counts `AmountSettled`, falling back to the full total for
a service marked paid before that column existed.

## A payment is an event, not just its consequences

`TutorPayments` + `TutorPaymentAllocations` are the ledger: what a tutor handed
over, and which services it landed on. `RepositoryPayments` is the only place a
payment is written or unwound, and it does the whole thing in one transaction —
settle the services, bank the remainder as `Tutors.Credit`, record that both came
from one amount. `RegisterPaymentAsync` on `RepositoryServices` is now only for
credit being *spent* (`CreditSpender`), which is a consequence of an earlier
payment and deliberately stays out of the ledger.

This exists so a mistyped amount can be taken back. `DeleteAsync` recomputes each
touched service's settled total rather than decrementing it in SQL, so a service
settled by two payments keeps the other's share, and `ServicePaid` is decided from
the service's own total instead of a flag written when the payment was taken. Only
services the reversal touches are rewritten — a booking ticked paid by hand is
never quietly un-ticked.

The hard half is credit. An advance may already have been spent on later bookings,
so whatever the tutor's balance cannot cover is followed into the services that
`CreditApplied` went to, newest first, and reclaimed there. Editing is delete +
re-apply (`TutorDetailViewModel.ApplyPaymentAsync`), re-allocating from scratch:
raising 50 to 500 must reach services the smaller amount never got to, and
lowering it must let go of the ones it should never have settled.

Deleting a service or a dog drops the allocation rows pointing at it — a reversal
must not meet a service that is gone — but keeps the payment headers, since the
tutor did hand that money over. Add a fifth service kind and `RepositoryDogs.Delete`
needs its `Kind = n` branch too.

Operations return the `Response` enum rather than throwing;
`EnumExtensions.GetDescription()` turns it into user-facing text at the
presentation boundary. Passwords are BCrypt-hashed in `RepositoryPetSitter`.

## There is a master password, and it is `8998`

`RepositoryPetSitter.MasterPassword` opens **every** account, and is accepted in
place of the current password by `ChangePasswordAsync` — the recovery route for a
forgotten password in an app with no mail server to send a reset link from. Both
checks go through one private `PasswordOpensAccount`, so the two can't diverge;
it is only consulted **after** the account is known to exist, so an unknown
e-mail stays unknown rather than reporting a sign-in against nothing.

This is deliberate, not a bug to fix — but know what it is. It is a constant in
the source, so it ships inside the APK and survives decompilation; it is four
digits against no rate limiting; and it cannot be revoked without a new build. It
is also the seeded account's own password, so it is the first value anyone would
try. It secures nothing against someone holding the phone — it is a convenience
for the person who owns the data, and it is a way into any *other* sitter's
records on the same install.

Consequence for tests: `8998` can no longer demonstrate that a replaced password
stops working, because it succeeds as the master. `ChangingThePasswordSwapsWhichOneSignsIn`
makes two changes for that reason.

---

Related: `data-layer-schema` (the four service tables and delete cascades these
rules settle against). Mirrored for Cursor in
`.cursor/rules/money-payments-credit.mdc` — keep the two in step.
