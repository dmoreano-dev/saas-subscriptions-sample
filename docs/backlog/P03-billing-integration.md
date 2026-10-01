# P03 — Billing integration

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Introduce payment integration only after capabilities and isolation work. Simulator tests and actual Stripe interoperability are separate evidence.

### BIL-01 — Implement the billing gateway and deterministic simulator

**Status:** Todo · **Priority:** P0 · **Phase:** P03 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ENT-08](P02-personal-accounts-and-plans.md#ent-08--verify-the-personal-plan-checkpoint), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Define normalized billing commands/results and a local simulator.

**Acceptance criteria:**

- [ ] Scenarios cover success, failure, timeout, duplicate/delayed/out-of-order events, and recovery with controllable time.
- [ ] Simulator endpoints are test-only; hosted configuration rejects their activation; no real-money keys are accepted in the lab profile.

**Verification:** Deterministic simulator scenario suite and hosted-startup rejection test.

**Evidence:** Pending.

### BIL-02 — Configure Stripe Sandbox catalog and account mappings

**Status:** Todo · **Priority:** P0 · **Phase:** P03 · **Suggested model:** Sonnet 5.5

**Dependencies:** [BIL-01](#bil-01--implement-the-billing-gateway-and-deterministic-simulator), [ENT-01](P02-personal-accounts-and-plans.md#ent-01--create-the-versioned-plan-and-price-catalog)

**Outcome:** Add the Stripe adapter, pinned SDK/API behavior, and environment-specific product/price mapping.

**Acceptance criteria:**

- [ ] Provider customer IDs map to application accounts, with uniqueness and cross-account checks.
- [ ] Server resolves allowed prices; secrets remain outside source control; local/CI/hosted sandbox fixtures cannot collide.

**Verification:** Sandbox catalog synchronization and mapping constraint tests.

**Evidence:** Pending.

### BIL-03 — Implement idempotent Checkout creation

**Status:** Todo · **Priority:** P0 · **Phase:** P03 · **Suggested model:** Opus / high

**Dependencies:** [BIL-02](#bil-02--configure-stripe-sandbox-catalog-and-account-mappings), [ENT-02](P02-personal-accounts-and-plans.md#ent-02--implement-account-scoped-permissions-and-resource-isolation)

**Outcome:** Create hosted Checkout sessions for authorized personal accounts.

**Acceptance criteria:**

- [ ] Caller-selected plan keys resolve to server-approved amounts/prices; customer/account references come from trusted mappings.
- [ ] Double clicks, concurrent requests, and ambiguous provider timeouts cannot silently create duplicate base subscriptions; reconcile before retrying uncertain creation.

**Verification:** Checkout ownership, tampering, retry, and duplicate-subscription scenarios.

**Evidence:** Pending.

### BIL-04 — Persist verified Stripe webhook deliveries

**Status:** Todo · **Priority:** P0 · **Phase:** P03 · **Suggested model:** Opus / high

**Dependencies:** [BIL-02](#bil-02--configure-stripe-sandbox-catalog-and-account-mappings), [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations)

**Outcome:** Add the raw-body webhook endpoint and durable inbox.

**Acceptance criteria:**

- [ ] Validate signature, timestamp, provider environment/account, payload size, and supported event types; reject forged deliveries.
- [ ] Persist before returning success; event IDs are uniquely constrained; retained payloads are minimized/protected.

**Verification:** Forged signature, body mutation, duplicate, storage failure, and valid CLI delivery tests.

**Evidence:** Pending.

### BIL-05 — Process durable integration work safely

**Status:** Todo · **Priority:** P0 · **Phase:** P03 · **Suggested model:** Opus / high

**Dependencies:** [BIL-04](#bil-04--persist-verified-stripe-webhook-deliveries), [ENT-03](P02-personal-accounts-and-plans.md#ent-03--resolve-current-subscription-entitlements-from-postgresql)

**Outcome:** Implement PostgreSQL-backed inbox/outbox processing and normalized subscription projections.

**Acceptance criteria:**

- [ ] Leases, bounded retries, backoff, failed-work inspection, and restart recovery are implemented.
- [ ] Serialize/guard account changes and use current provider state where needed; creation timestamps alone do not order events; state and outbox changes commit together.

**Verification:** Worker crash, lease expiry, retry, stale-event, and projection consistency tests.

**Evidence:** Pending.

### BIL-06 — Build checkout return, billing portal, and invoice views

**Status:** Todo · **Priority:** P1 · **Phase:** P03 · **Suggested model:** Sonnet 5.5

**Dependencies:** [BIL-03](#bil-03--implement-idempotent-checkout-creation), [BIL-05](#bil-05--process-durable-integration-work-safely), [ENT-07](P02-personal-accounts-and-plans.md#ent-07--build-plan-usage-and-capability-aware-react-screens)

**Outcome:** Complete the personal billing UI and protected portal session creation.

**Acceptance criteria:**

- [ ] Return pages show backend-confirmed pending/success/action-required state even if the browser misses the initial event.
- [ ] Portal and invoice access are account-authorized; portal settings cannot bypass application plan-change rules; payment details remain hosted.

**Verification:** Browser abandonment/return and cross-account portal/invoice tests.

**Evidence:** Pending.

### BIL-07 — Add reconciliation and protected billing diagnostics

**Status:** Todo · **Priority:** P0 · **Phase:** P03 · **Suggested model:** Opus / high

**Dependencies:** [BIL-05](#bil-05--process-durable-integration-work-safely)

**Outcome:** Compare provider state with local state and expose narrow operator repair actions.

**Acceptance criteria:**

- [ ] A reconciliation run records differences, resumes safely, and reuses idempotent processing to repair missed events.
- [ ] Only platform operators can inspect/retry/reconcile selected records; secrets are redacted and repairs are audited.

**Verification:** Missed-event repair, repeated reconciliation, and unauthorized-operator tests.

**Evidence:** Pending.

### BIL-08 — Verify simulator and Stripe adapter behavior

**Status:** Todo · **Priority:** P0 · **Phase:** P03 · **Suggested model:** Sonnet 5.5

**Dependencies:** [BIL-06](#bil-06--build-checkout-return-billing-portal-and-invoice-views), [BIL-07](#bil-07--add-reconciliation-and-protected-billing-diagnostics), [FND-06](P00-foundation.md#fnd-06--create-the-automated-verification-foundation)

**Outcome:** Prove a paid activation through both local and provider-backed workflows.

**Acceptance criteria:**

- [ ] Simulated rules pass independently of Stripe, and actual Sandbox checkout/webhook activation is separately evidenced.
- [ ] A browser redirect alone cannot grant paid access; provider outage and delayed confirmation have documented recoverable states.

**Verification:** Adapter contract tests plus recorded Stripe Sandbox end-to-end purchase.

**Evidence:** Pending.
