# Optional — Conditional and future extensions

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

These items are visible extensions. They are not required for baseline lab completion and require a later scope/provider decision.

### ADV-01 — Add hosted shared Redis and multi-instance validation

**Status:** Todo · **Priority:** P2 · **Phase:** Optional · **Suggested model:** Sonnet 5.5

**Dependencies:** [CAC-06](P11-comparison-exercises.md#cac-06--add-and-compare-local-redis-storage), [DEP-06](P05-memory-cache-and-hosted-demo.md#dep-06--accept-the-first-hosted-learning-release-r1)

**Outcome:** Extend deployment when an external cache and multiple API instances are desired.

**Acceptance criteria:**

- [ ] Choose and configure a provider only when this extension is selected; verify shared version/invalidation semantics and recovery.
- [ ] Do not assume process-local eviction, rate limits, session caches, or worker locks coordinate multiple instances.

**Verification:** Two-instance hosted fault/concurrency tests.

**Evidence:** Pending.

### ADV-02 — Validate against a real Microsoft Entra test directory

**Status:** Todo · **Priority:** P2 · **Phase:** Optional · **Suggested model:** Sonnet 5.5

**Dependencies:** [SSO-06](P10-corporate-access.md#sso-06--verify-and-explain-the-corporate-login-checkpoint), [SCI-05](P10-corporate-access.md#sci-05--verify-the-provisioning-lifecycle-and-r2-checkpoint)

**Outcome:** Prove actual corporate-provider interoperability beyond local fixtures.

**Acceptance criteria:**

- [ ] Requires a reachable test tenant and permission to register/configure applications and provisioning.
- [ ] Record OIDC/SCIM mapping behavior, credential rotation, provisioning delay, disable/reprovision results, and provider-specific gaps; never claim certification from a simulator.

**Verification:** Recorded real-directory onboarding/offboarding demonstration.

**Evidence:** Pending.

### ADV-03 — Add a SAML corporate-login adapter

**Status:** Todo · **Priority:** P2 · **Phase:** Optional · **Suggested model:** Sonnet 5.5

**Dependencies:** [SSO-06](P10-corporate-access.md#sso-06--verify-and-explain-the-corporate-login-checkpoint), [SCI-05](P10-corporate-access.md#sci-05--verify-the-provisioning-lifecycle-and-r2-checkpoint)

**Outcome:** Support an additional enterprise authentication protocol if selected.

**Acceptance criteria:**

- [ ] Use maintained middleware or a broker and validate metadata, signatures, audience, recipient, replay, and time constraints.
- [ ] Preserve the same safe linking, organization assurance, recovery, and deprovisioning rules.

**Verification:** Real SAML test-IdP flow and negative protocol/security tests.

**Evidence:** Pending.

### ADV-04 — Define a separate production and real-money readiness project

**Status:** Todo · **Priority:** P2 · **Phase:** Optional · **Suggested model:** Sonnet 5.5

**Dependencies:** [FIN-06](P12-full-lab-verification.md#fin-06--accept-r3-and-prepare-the-implementation-handoff)

**Outcome:** Assess work beyond this sandbox learning scope before any live release.

**Acceptance criteria:**

- [ ] Decide provider/region/domain, reliable workers/availability, backup targets, retention/legal/tax requirements, support commitments, and security review.
- [ ] Evaluate any desired trials/coupons/add-ons/metered billing/mobile/API clients separately; live billing activation or resource spending requires its own explicit scope.

**Verification:** A separately reviewed production plan; no live charges implied by this item.

**Evidence:** Pending.
