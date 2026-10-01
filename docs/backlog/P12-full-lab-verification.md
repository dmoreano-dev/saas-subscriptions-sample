# P12 — Full lab verification

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Verify the complete agreed scope, including failure recovery and reproducibility. Optional provider extensions must not be represented as completed.

### FIN-01 — Complete operational observability and diagnostics

**Status:** Todo · **Priority:** P1 · **Phase:** P12 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SCI-05](P10-corporate-access.md#sci-05--verify-the-provisioning-lifecycle-and-r2-checkpoint), [BIL-07](P03-billing-integration.md#bil-07--add-reconciliation-and-protected-billing-diagnostics)

**Outcome:** Make identity, access, integration work, and usage failures diagnosable.

**Acceptance criteria:**

- [ ] Structured logs include correlation and verified account IDs; metrics cover auth failures, denials, work age, reconciliation drift, cache behavior, and report failure.
- [ ] Diagnostics require appropriate operator access, redact secrets, and distinguish expected business denials from service failures.

**Verification:** Synthetic failure traces and redaction/authorization tests.

**Evidence:** Pending.

### FIN-02 — Rehearse recovery, key rotation, and database restore

**Status:** Todo · **Priority:** P0 · **Phase:** P12 · **Suggested model:** Opus / high

**Dependencies:** [FIN-01](#fin-01--complete-operational-observability-and-diagnostics), [DEP-06](P05-memory-cache-and-hosted-demo.md#dep-06--accept-the-first-hosted-learning-release-r1), [CAC-06](P11-comparison-exercises.md#cac-06--add-and-compare-local-redis-storage), [CMP-03](P11-comparison-exercises.md#cmp-03--implement-and-rehearse-the-identity-migration)

**Outcome:** Prove durable behavior across infrastructure and security maintenance.

**Acceptance criteria:**

- [ ] Exercise API/cache/worker failure, signing/integration credential rotation, and a test database backup/restore.
- [ ] Restarted processing does not duplicate grants/usage; restored tenant data stays scoped; keys and database state do not depend on container files.

**Verification:** Recorded fault/restore/rotation runbook executions.

**Evidence:** Pending.

### FIN-03 — Finalize retention and account offboarding policies

**Status:** Todo · **Priority:** P0 · **Phase:** P12 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CON-05](P09-enterprise-commerce.md#con-05--implement-termination-export-retention-and-purge), [SEC-04](P06-identity-lifecycle-and-sessions.md#sec-04--implement-currentall-session-revocation), [SCI-04](P10-corporate-access.md#sci-04--make-deprovisioning-win-over-concurrent-login)

**Outcome:** Document and enforce data lifecycle for reports, invitations, tokens, personal accounts, and organizations.

**Acceptance criteria:**

- [ ] Expired secret/report cleanup is bounded and retryable; account closure revokes access and addresses active billing and final-owner responsibilities.
- [ ] Exports/purge are authorized and audited; separate product, identity, audit, and financial retention classes; preserve other accounts and required reference integrity.

**Verification:** Cleanup, personal closure, organization retention, export, and purge regression.

**Evidence:** Pending.

### FIN-04 — Run the complete scenario regression matrix

**Status:** Todo · **Priority:** P0 · **Phase:** P12 · **Suggested model:** Opus / high

**Dependencies:** [FIN-02](#fin-02--rehearse-recovery-key-rotation-and-database-restore), [FIN-03](#fin-03--finalize-retention-and-account-offboarding-policies), [CMP-04](P11-comparison-exercises.md#cmp-04--publish-the-identity-comparison-report)

**Outcome:** Verify the finished lab against the plan's acceptance scenarios.

**Acceptance criteria:**

- [ ] Cover personal Free/Pro/Max, Enterprise, JWT/session/refresh, roles, isolation, quotas, billing, effective dates, cache races, and corporate offboarding.
- [ ] Run provider-independent tests plus separately evidenced Stripe/browser/provider tests; optional integrations show their actual status.
- [ ] Add an automated architecture check that each backend layer declares only its allowed project references (Domain → none; Application → Domain; Infrastructure → Application; Api → Application + Infrastructure), deferred from FND-01.

**Verification:** Full test report with failed/skipped/external-blocked cases explicitly identified.

**Evidence:** Pending.

### FIN-05 — Complete runbooks and learning documentation

**Status:** Todo · **Priority:** P1 · **Phase:** P12 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FIN-04](#fin-04--run-the-complete-scenario-regression-matrix)

**Outcome:** Make the project understandable without the original conversation.

**Acceptance criteria:**

- [ ] English docs cover startup, configuration, migrations, API contracts, auth lessons, provider setup, cache guarantees, identity comparison, and recovery procedures.
- [ ] Each phase has a runnable demo; all links, example values, and backlog statuses match the actual implementation.

**Verification:** Clean-reader documentation review and fresh-environment walkthrough.

**Evidence:** Pending.

### FIN-06 — Accept R3 and prepare the implementation handoff

**Status:** Todo · **Priority:** P0 · **Phase:** P12 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FIN-05](#fin-05--complete-runbooks-and-learning-documentation)

**Outcome:** Close the baseline learning project only when required work has evidence.

**Acceptance criteria:**

- [ ] Every other required P00–P12 item is Done before accepting this release item; optional/external extensions remain separately visible.
- [ ] Record deployed revisions/configuration profile, reproducible local checks, known limitations, and final demo of personal plus Enterprise journeys.

**Verification:** Signed-off R3 checklist and zero unexplained required-item gaps.

**Evidence:** Pending.
