# P02 — Personal accounts and plans

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Deliver the complete personal-account model first, using PostgreSQL-backed authorization and simulated commercial fixtures.

### ENT-01 — Create the versioned plan and price catalog

**Status:** Todo · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-06](P01-jwt-essentials.md#jwt-06--verify-and-explain-the-jwt-checkpoint), [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations)

**Outcome:** Seed the Free/Pro/Max/Enterprise capability fixtures and separate prices.

**Acceptance criteria:**

- [ ] Published versions are immutable once referenced; monthly/annual prices are distinct from capabilities and external price IDs.
- [ ] Free works without a Stripe customer or subscription; capability keys, value types, limits, and quota units are explicit.

**Verification:** Seed idempotency, version immutability, and catalog query tests.

**Evidence:** Pending.

### ENT-02 — Implement account-scoped permissions and resource isolation

**Status:** Todo · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Opus / high

**Dependencies:** [JWT-06](P01-jwt-essentials.md#jwt-06--verify-and-explain-the-jwt-checkpoint), [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations)

**Outcome:** Centralize active membership, permission, and resource-ownership checks.

**Acceptance criteria:**

- [ ] Personal owners receive scoped permissions; platform operator privileges are distinct; Pro/Max are never roles.
- [ ] Reads, writes, raw SQL paths, relationships, and future job contexts reject unverified account IDs and cross-account resource references.

**Verification:** Two-user/two-account BOLA and cross-account write/FK tests.

**Evidence:** Pending.

### ENT-03 — Resolve current subscription entitlements from PostgreSQL

**Status:** Todo · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ENT-01](#ent-01--create-the-versioned-plan-and-price-catalog), [ENT-02](#ent-02--implement-account-scoped-permissions-and-resource-isolation), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Build effective capability resolution with explicit time and account scope.

**Acceptance criteria:**

- [ ] Resolve Free defaults or a subscription's effective version, access deadlines, pending changes, and restrictions without calling Stripe.
- [ ] A stale plan label in a JWT cannot override current state; unknown capabilities deny access; response includes version and validity metadata.

**Verification:** Capability matrix, missing-capability, and effective-boundary tests.

**Evidence:** Pending.

### ENT-04 — Implement scoped project operations

**Status:** Todo · **Priority:** P1 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ENT-02](#ent-02--implement-account-scoped-permissions-and-resource-isolation), [ENT-03](#ent-03--resolve-current-subscription-entitlements-from-postgresql)

**Outcome:** Create the small project domain and active-project capacity enforcement.

**Acceptance criteria:**

- [ ] Users can create/read/edit/archive/delete their account's projects; archived projects do not count against the active cap.
- [ ] Concurrent creations cannot exceed the current cap; lowering the cap preserves existing data and permits cleanup.

**Verification:** CRUD browser smoke and concurrent last-slot PostgreSQL test.

**Evidence:** Pending.

### ENT-05 — Implement transactional usage accounting

**Status:** Todo · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Opus / high

**Dependencies:** [ENT-03](#ent-03--resolve-current-subscription-entitlements-from-postgresql), [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Create quota windows, counters, reservations, and idempotent consumption.

**Acceptance criteria:**

- [ ] Monthly windows use UTC half-open intervals; consumption checks use the effective limit in the same serialized transaction.
- [ ] Retries consume once; terminal failure releases a reservation; stale reservations are recoverable; IDistributedCache is not used as an atomic counter.

**Verification:** Concurrent last-unit, retry, boundary, failure, and recovery tests.

**Evidence:** Pending.

### ENT-06 — Implement basic and advanced reports

**Status:** Todo · **Priority:** P1 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ENT-04](#ent-04--implement-scoped-project-operations), [ENT-05](#ent-05--implement-transactional-usage-accounting)

**Outcome:** Generate basic CSV and simple advanced summaries to exercise paid features.

**Acceptance criteria:**

- [ ] Pro can generate basic reports; Max can also generate advanced reports; both types share the account report quota.
- [ ] Persist report state, protect delayed execution and downloads by account/current authorization, retain temporary results for 24 hours, and release failed reservations.

**Verification:** Pro/Max matrix, failed generation, duplicate execution, and foreign-download tests.

**Evidence:** Pending.

### ENT-07 — Build plan, usage, and capability-aware React screens

**Status:** Todo · **Priority:** P1 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ENT-01](#ent-01--create-the-versioned-plan-and-price-catalog), [ENT-03](#ent-03--resolve-current-subscription-entitlements-from-postgresql), [ENT-04](#ent-04--implement-scoped-project-operations), [ENT-06](#ent-06--implement-basic-and-advanced-reports)

**Outcome:** Show available plans, current capabilities, projects, reports, and usage.

**Acceptance criteria:**

- [ ] UI reads API capabilities and displays limits/upgrade explanations rather than decoding a JWT plan as authority.
- [ ] Loading/denial/over-quota/empty/error states work; the API still rejects manually crafted forbidden requests.

**Verification:** Browser capability matrix and API bypass attempts.

**Evidence:** Pending.

### ENT-08 — Verify the personal plan checkpoint

**Status:** Todo · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ENT-07](#ent-07--build-plan-usage-and-capability-aware-react-screens), [FND-06](P00-foundation.md#fnd-06--create-the-automated-verification-foundation)

**Outcome:** Complete a provider-free demonstration of personal Free/Pro/Max behavior.

**Acceptance criteria:**

- [ ] Test-only plan fixtures can demonstrate each tier without exposing arbitrary plan assignment to ordinary users.
- [ ] Isolation, feature denial, limits, transactions, and current-state behavior pass together and are explained in the learning note.

**Verification:** Run the personal account/entitlement regression suite.

**Evidence:** Pending.
