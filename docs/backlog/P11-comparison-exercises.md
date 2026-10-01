# P11 — Comparison exercises

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Compare implementations after behavior is understood. Redis and Identity are independent exercises, not prerequisites for the first release.

### CAC-06 — Add and compare local Redis storage

**Status:** Todo · **Priority:** P1 · **Phase:** P11 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CAC-05](P05-memory-cache-and-hosted-demo.md#cac-05--verify-memory-cache-consistency-guarantees), [DEP-06](P05-memory-cache-and-hosted-demo.md#dep-06--accept-the-first-hosted-learning-release-r1)

**Outcome:** Replace the memory provider with a Redis container for a later learning exercise.

**Acceptance criteria:**

- [ ] The same resolver contract works with the Redis adapter; serialization/version/expiry behavior is equivalent.
- [ ] Test latency, availability failure, cold cache, and two-instance invalidation; keep PostgreSQL quota transactions and document limitations of the shared-cache abstraction.

**Verification:** Run the cache contract suite against both providers and record measurements.

**Evidence:** Pending.

### CMP-01 — Freeze the custom-authentication behavior and baseline measurements

**Status:** Todo · **Priority:** P1 · **Phase:** P11 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SCI-05](P10-corporate-access.md#sci-05--verify-the-provisioning-lifecycle-and-r2-checkpoint), [REF-06](P07-refresh-and-browser-transport.md#ref-06--verify-the-complete-jwtsessionrefresh-lesson)

**Outcome:** Prepare the AUTH-COMPARE exercise with reproducible fixtures and contracts.

**Acceptance criteria:**

- [ ] Record custom registration/login/recovery/MFA/session behavior, schema, hashing work factor, token contract, and applicable security tests.
- [ ] Benchmark representative login/refresh and authenticated-request workloads in a controlled environment with query/CPU/allocation metrics.

**Verification:** Saved baseline results, exact commands, dataset, and environment description.

**Evidence:** Pending.

### CMP-02 — Implement an ASP.NET Core Identity-backed variation

**Status:** Todo · **Priority:** P1 · **Phase:** P11 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CMP-01](#cmp-01--freeze-the-custom-authentication-behavior-and-baseline-measurements)

**Outcome:** Replace selected user-management internals while preserving application-facing behavior.

**Acceptance criteria:**

- [ ] Map or adapt Identity stores to stable users/accounts; subscription, membership, quota, and billing logic remains independently owned.
- [ ] Run the same behavior suite; document what Identity replaces and what custom session/token/MFA/integration work remains.

**Verification:** Contract/security suite against both implementations.

**Evidence:** Pending.

### CMP-03 — Implement and rehearse the identity migration

**Status:** Todo · **Priority:** P0 · **Phase:** P11 · **Suggested model:** Opus / high

**Dependencies:** [CMP-02](#cmp-02--implement-an-aspnet-core-identity-backed-variation)

**Outcome:** Provide a safe path for existing users and credentials.

**Acceptance criteria:**

- [ ] Preserve user/account IDs; support legacy hash verification and rehash-on-success or a documented staged reset when required.
- [ ] Session/token cutover, MFA compatibility, rollback limits, and interrupted migration are handled; no password hash is treated as decryptable.

**Verification:** Snapshot migration/restore, existing-user login, partial-failure, and rollback rehearsal.

**Evidence:** Pending.

### CMP-04 — Publish the Identity comparison report

**Status:** Todo · **Priority:** P1 · **Phase:** P11 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CMP-03](#cmp-03--implement-and-rehearse-the-identity-migration)

**Outcome:** Explain the learning outcome using measured behavior and effort.

**Acceptance criteria:**

- [ ] Compare code removed/retained, features, maintenance, security cases, database queries, latency percentiles, CPU, and memory.
- [ ] Use equivalent security parameters and environments; report uncertainty and no speedup if none exists; identify the chosen final default while preserving reproducible checkpoints.

**Verification:** Reviewed report and rerunnable benchmark commands.

**Evidence:** Pending.
