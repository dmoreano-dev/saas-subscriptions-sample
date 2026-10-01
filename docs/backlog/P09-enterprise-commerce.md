# P09 — Enterprise commerce

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Treat Enterprise as a contract and administration lifecycle, with explicit invoice and access rules.

### CON-01 — Model versioned Enterprise contracts and overrides

**Status:** Todo · **Priority:** P0 · **Phase:** P09 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ORG-07](P08-organizations.md#org-07--verify-the-organization-isolation-checkpoint), [ENT-01](P02-personal-accounts-and-plans.md#ent-01--create-the-versioned-plan-and-price-catalog)

**Outcome:** Create immutable accepted revisions and explicit commercial terms.

**Acceptance criteria:**

- [ ] Store dates, seats, capability overrides, price/currency, invoice terms, renewal, grace, and retention with an acceptance reference.
- [ ] Override precedence is typed/allowlisted; organization admins cannot edit accepted commercial terms; security policy is independent.

**Verification:** Contract validation, immutability, effective-date, and permission tests.

**Evidence:** Pending.

### CON-02 — Build audited operator contract activation

**Status:** Todo · **Priority:** P0 · **Phase:** P09 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CON-01](#con-01--model-versioned-enterprise-contracts-and-overrides), [SEC-06](P06-identity-lifecycle-and-sessions.md#sec-06--build-security-settings-and-protect-privileged-actions), [BIL-07](P03-billing-integration.md#bil-07--add-reconciliation-and-protected-billing-diagnostics)

**Outcome:** Provide the restricted workflow for simulated acceptance and activation.

**Acceptance criteria:**

- [ ] Operator requires platform permission, MFA/recent authentication, and a recorded reason/reference.
- [ ] Activation is idempotent, updates access state/outbox consistently, and cannot be triggered by an ordinary account owner or arbitrary client plan payload.

**Verification:** Operator authorization, duplicate activation, rollback, and audit tests.

**Evidence:** Pending.

### CON-03 — Implement invoice-backed Enterprise subscriptions

**Status:** Todo · **Priority:** P0 · **Phase:** P09 · **Suggested model:** Opus / high

**Dependencies:** [CON-02](#con-02--build-audited-operator-contract-activation), [BIL-02](P03-billing-integration.md#bil-02--configure-stripe-sandbox-catalog-and-account-mappings), [BIL-05](P03-billing-integration.md#bil-05--process-durable-integration-work-safely)

**Outcome:** Generate and reconcile sandbox invoices with payment terms.

**Acceptance criteria:**

- [ ] The baseline contract grants access from its start date before a Net-30 invoice is paid; financial and access state remain distinct.
- [ ] Provider-confirmed paid/manual-payment fixtures update state once; invoice/customer mappings and downloads stay organization-scoped.

**Verification:** Contract-start/open-invoice, due-date, payment, and duplicate-event scenarios.

**Evidence:** Pending.

### CON-04 — Implement amendments, renewals, and overdue policy

**Status:** Todo · **Priority:** P0 · **Phase:** P09 · **Suggested model:** Opus / high

**Dependencies:** [CON-03](#con-03--implement-invoice-backed-enterprise-subscriptions), [ORG-04](P08-organizations.md#org-04--implement-licenses-and-atomic-seat-assignment), [CAC-03](P05-memory-cache-and-hosted-demo.md#cac-03--implement-durable-account-cache-invalidation)

**Outcome:** Apply commercial revisions and deadlines without mutating historical terms.

**Acceptance criteria:**

- [ ] Capacity/pricing/capability changes have explicit effective dates and invalidate access snapshots; reductions below assigned seats require a resolution plan.
- [ ] Overdue grace, renewal/nonrenewal, and recovery follow the contract; restricted organizations retain approved billing/recovery access and SSO requirements.

**Verification:** Future amendment, over-seat reduction, overdue, renewal, and sleeping-worker tests.

**Evidence:** Pending.

### CON-05 — Implement termination, export, retention, and purge

**Status:** Todo · **Priority:** P0 · **Phase:** P09 · **Suggested model:** Opus / high

**Dependencies:** [CON-04](#con-04--implement-amendments-renewals-and-overdue-policy), [ORG-06](P08-organizations.md#org-06--implement-organization-member-offboarding)

**Outcome:** Handle the end of an organization contract deliberately.

**Acceptance criteria:**

- [ ] Termination restricts access while preserving authorized billing/export during the 30-day lab retention period; export is scoped and audited.
- [ ] A separately authorized purge workflow removes intended product data after policy checks; identity/audit/financial records follow their documented retention classes.

**Verification:** Tenant-specific export/purge, retained-record, and cross-account preservation tests.

**Evidence:** Pending.

### CON-06 — Verify the Enterprise commercial journey

**Status:** Todo · **Priority:** P0 · **Phase:** P09 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CON-05](#con-05--implement-termination-export-retention-and-purge)

**Outcome:** Demonstrate contract creation through termination/recovery.

**Acceptance criteria:**

- [ ] UI shows contract revision, licensed capacity, invoice due dates, and effective access without leaking operator controls.
- [ ] Personal Free/Pro/Max and organization billing are regression-tested together; all financial activity remains sandbox-only.

**Verification:** Full Enterprise commercial scenario and personal billing regression.

**Evidence:** Pending.
