# P04 — Subscription lifecycle

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Freeze explicit commercial behavior and effective dates before adding cache complexity.

### LIF-01 — Implement paid upgrades with preview and payment gating

**Status:** Todo · **Priority:** P0 · **Phase:** P04 · **Suggested model:** Sonnet 5.5

**Dependencies:** [BIL-08](P03-billing-integration.md#bil-08--verify-simulator-and-stripe-adapter-behavior)

**Outcome:** Support Pro-to-Max upgrade with explicit proration preview and confirmation.

**Acceptance criteria:**

- [ ] A preview is account-authorized and identifies the effective price/date; stale previews are revalidated.
- [ ] Failed or incomplete upgrade payment keeps existing rights; confirmed upgrades preserve usage and apply the new limit once.

**Verification:** Successful, failed, authentication-required, concurrent, and repeated-upgrade scenarios.

**Evidence:** Pending.

### LIF-02 — Implement scheduled downgrades

**Status:** Todo · **Priority:** P0 · **Phase:** P04 · **Suggested model:** Opus / high

**Dependencies:** [BIL-08](P03-billing-integration.md#bil-08--verify-simulator-and-stripe-adapter-behavior), [ENT-03](P02-personal-accounts-and-plans.md#ent-03--resolve-current-subscription-entitlements-from-postgresql)

**Outcome:** Store current plan, pending plan, and the effective transition date.

**Acceptance criteria:**

- [ ] Max rights remain until the scheduled boundary; UI displays both current and next plan.
- [ ] Requests at/after the boundary resolve correct rights even if the worker or provider event is late; excess projects are preserved.

**Verification:** Fake-time boundary tests including a sleeping worker and late webhook.

**Evidence:** Pending.

### LIF-03 — Implement cancellation, undo, and resubscription

**Status:** Todo · **Priority:** P0 · **Phase:** P04 · **Suggested model:** Sonnet 5.5

**Dependencies:** [BIL-08](P03-billing-integration.md#bil-08--verify-simulator-and-stripe-adapter-behavior), [LIF-02](#lif-02--implement-scheduled-downgrades)

**Outcome:** Support cancel-at-period-end and safe reversal/re-entry.

**Acceptance criteria:**

- [ ] Cancellation retains paid rights through the paid period; undo works only while applicable and retains history.
- [ ] An ended personal subscription falls back to Free; resubscription reuses account mappings without duplicating active subscriptions or deleting data.

**Verification:** Cancel/undo/end/resubscribe browser and duplicate-event scenarios.

**Evidence:** Pending.

### LIF-04 — Implement renewal failures, grace, and payment recovery

**Status:** Todo · **Priority:** P0 · **Phase:** P04 · **Suggested model:** Opus / high

**Dependencies:** [BIL-08](P03-billing-integration.md#bil-08--verify-simulator-and-stripe-adapter-behavior), [ENT-03](P02-personal-accounts-and-plans.md#ent-03--resolve-current-subscription-entitlements-from-postgresql)

**Outcome:** Translate renewal outcomes into explicit access deadlines.

**Acceptance criteria:**

- [ ] Confirmed failure uses the baseline three-day grace; recovery/billing endpoints stay available after paid features are restricted.
- [ ] Successful recovery advances authoritative validity correctly; an API outage cannot indefinitely extend an expired paid grant.

**Verification:** Renewal success/failure/recovery and grace-expiry tests with provider downtime.

**Evidence:** Pending.

### LIF-05 — Add annual prices and monthly quota windows

**Status:** Todo · **Priority:** P1 · **Phase:** P04 · **Suggested model:** Sonnet 5.5

**Dependencies:** [LIF-01](#lif-01--implement-paid-upgrades-with-preview-and-payment-gating), [LIF-02](#lif-02--implement-scheduled-downgrades), [ENT-05](P02-personal-accounts-and-plans.md#ent-05--implement-transactional-usage-accounting)

**Outcome:** Support annual sandbox subscriptions while keeping monthly usage allowances.

**Acceptance criteria:**

- [ ] Monthly/annual prices share capability packages; cadence changes use scheduled boundaries.
- [ ] Quota windows preserve the original anchor, handle month-end/leap-year boundaries, and do not grant a full year's quota or reset on an upgrade.

**Verification:** Annual Billing simulation and calendar-boundary unit/integration matrix.

**Evidence:** Pending.

### LIF-06 — Handle refunds and disputes as audited cases

**Status:** Todo · **Priority:** P1 · **Phase:** P04 · **Suggested model:** Sonnet 5.5

**Dependencies:** [BIL-07](P03-billing-integration.md#bil-07--add-reconciliation-and-protected-billing-diagnostics), [LIF-04](#lif-04--implement-renewal-failures-grace-and-payment-recovery)

**Outcome:** Record refund/dispute state independently of automatic entitlement decisions.

**Acceptance criteria:**

- [ ] Partial refunds preserve access by baseline policy; full refunds/disputes create operator-review cases.
- [ ] An operator can apply a documented access decision with reason/audit; repeated events do not repeat financial actions or delete data.

**Verification:** Sandbox refund/dispute fixtures and permission/audit tests.

**Evidence:** Pending.

### LIF-07 — Complete lifecycle UI and data-over-limit behavior

**Status:** Todo · **Priority:** P1 · **Phase:** P04 · **Suggested model:** Sonnet 5.5

**Dependencies:** [LIF-01](#lif-01--implement-paid-upgrades-with-preview-and-payment-gating), [LIF-02](#lif-02--implement-scheduled-downgrades), [LIF-03](#lif-03--implement-cancellation-undo-and-resubscription), [LIF-04](#lif-04--implement-renewal-failures-grace-and-payment-recovery), [LIF-05](#lif-05--add-annual-prices-and-monthly-quota-windows)

**Outcome:** Explain current/pending/grace/restricted states and safe cleanup actions.

**Acceptance criteria:**

- [ ] Users see effective dates, payment actions, quota windows, and over-limit reasons without implementation jargon.
- [ ] Blocked paid operations do not block authorized deletion/archive, account recovery, invoice access, or cancellation management.

**Verification:** Browser state matrix and API regression for restricted accounts.

**Evidence:** Pending.

### LIF-08 — Verify all personal subscription transitions

**Status:** Todo · **Priority:** P0 · **Phase:** P04 · **Suggested model:** Opus / high

**Dependencies:** [LIF-06](#lif-06--handle-refunds-and-disputes-as-audited-cases), [LIF-07](#lif-07--complete-lifecycle-ui-and-data-over-limit-behavior)

**Outcome:** Freeze the personal billing behavior before cache/deployment work.

**Acceptance criteria:**

- [ ] Transition matrix covers duplicates, out-of-order events, concurrency, time boundaries, and restored payments.
- [ ] Stripe Test Clocks and the application clock are coordinated explicitly; provider-specific limitations and test evidence are documented.

**Verification:** Full lifecycle suite and a reproducible operator/user demonstration.

**Evidence:** Pending.
