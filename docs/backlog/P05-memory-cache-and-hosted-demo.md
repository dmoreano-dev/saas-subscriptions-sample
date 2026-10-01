# P05 — Memory cache and first hosted demo

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Add memory cache and prove its consistency limits, then run the first hosted test checkpoint. External release verification may remain pending while later local learning proceeds.

### CAC-01 — Introduce IDistributedCache with process-memory storage

**Status:** Todo · **Priority:** P1 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [LIF-08](P04-subscription-lifecycle.md#lif-08--verify-all-personal-subscription-transitions), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Cache eligible account capability snapshots behind the existing resolver.

**Acceptance criteria:**

- [ ] Cache:Provider=Memory registers AddDistributedMemoryCache; keys include environment, account, and schema namespace.
- [ ] A miss rebuilds from PostgreSQL; restart/cache loss is safe; request-local reuse avoids redundant resolution.

**Verification:** Cache hit/miss/restart tests and before/after database-query measurement.

**Evidence:** Pending.

### CAC-02 — Enforce cache validity and strict-operation rules

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Opus / high

**Dependencies:** [CAC-01](#cac-01--introduce-idistributedcache-with-process-memory-storage)

**Outcome:** Define which reads may be cached and which checks remain authoritative.

**Acceptance criteria:**

- [ ] Absolute lifetime is at most 30 seconds and never exceeds a known effective/access deadline; no sliding extension of expired rights.
- [ ] Session/membership security decisions, billing mutations, seats, and quota-consuming writes follow the plan's authoritative checks; unavailable valid state never grants uncertain rights.

**Verification:** Known-boundary, outage, stale-read, and strict-write tests.

**Evidence:** Pending.

### CAC-03 — Implement durable account-cache invalidation

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Opus / high

**Dependencies:** [CAC-01](#cac-01--introduce-idistributedcache-with-process-memory-storage), [BIL-05](P03-billing-integration.md#bil-05--process-durable-integration-work-safely)

**Outcome:** Connect commercial changes to account versioning and retriable cache invalidation.

**Acceptance criteria:**

- [ ] Subscription updates, access version increments, and invalidation outbox records commit together; eviction happens after commit.
- [ ] Retries recover failed eviction; older concurrent fills cannot overwrite newer state; version discovery is not based only on a stale cache pointer.

**Verification:** Crash-after-commit, stale-refill, duplicate invalidation, and version-race tests.

**Evidence:** Pending.

### CAC-04 — Refresh frontend subscription state safely

**Status:** Todo · **Priority:** P1 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CAC-03](#cac-03--implement-durable-account-cache-invalidation), [LIF-07](P04-subscription-lifecycle.md#lif-07--complete-lifecycle-ui-and-data-over-limit-behavior)

**Outcome:** Keep React capability and billing state aligned with confirmed changes.

**Acceptance criteria:**

- [ ] Bounded polling/refetch handles pending purchases and changes without requiring a new JWT.
- [ ] Responses are scoped to the originating account; switching accounts cannot display another account's capabilities or cached content.

**Verification:** Browser pending-change and rapid account-switch tests.

**Evidence:** Pending.

### CAC-05 — Verify memory-cache consistency guarantees

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Opus / high

**Dependencies:** [CAC-02](#cac-02--enforce-cache-validity-and-strict-operation-rules), [CAC-03](#cac-03--implement-durable-account-cache-invalidation), [CAC-04](#cac-04--refresh-frontend-subscription-state-safely)

**Outcome:** Demonstrate bounded staleness and recovery rather than claiming atomic DB/cache updates.

**Acceptance criteria:**

- [ ] Tests enforce the documented maximum stale-read window and exact known deadlines.
- [ ] Cache failure does not affect durable billing/usage correctness; code does not rely on IDistributedCache for counters, locks, or durable work.

**Verification:** Concurrency/fault suite and documented consistency results.

**Evidence:** Pending.

### DEP-01 — Provision the Supabase test database connection

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations), [LIF-08](P04-subscription-lifecycle.md#lif-08--verify-all-personal-subscription-transitions)

**Outcome:** Configure the application against a dedicated Supabase PostgreSQL test project.

**Acceptance criteria:**

- [ ] Select a verified direct/session-pooler connection with TLS and bounded pooling; use separate migration/runtime roles.
- [ ] Apply the same migrations/seeds as local; application/credential schemas are unexposed or the unused Data API is disabled; no Supabase Auth dependency is introduced.

**Verification:** Connectivity, migration, grants/exposure, and cross-account smoke tests.

**Evidence:** Pending.

### DEP-02 — Package and deploy the API to Render

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [DEP-01](#dep-01--provision-the-supabase-test-database-connection), [CAC-05](#cac-05--verify-memory-cache-consistency-guarantees)

**Outcome:** Create the production-style Dockerfile and single-instance test deployment.

**Acceptance criteria:**

- [ ] Image contains no credentials; process binds the configured port, exposes liveness/readiness, and loads stable signing keys.
- [ ] Memory cache is explicit; restart preserves PostgreSQL state and pending work; simulator endpoints are unavailable.

**Verification:** Image scan/config review, deploy, restart, and signed-token continuity smoke.

**Evidence:** Pending.

### DEP-03 — Deploy React to Vercel and verify API routing

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [DEP-02](#dep-02--package-and-deploy-the-api-to-render), [JWT-04](P01-jwt-essentials.md#jwt-04--build-the-react-jwt-only-login-experience), [CAC-04](#cac-04--refresh-frontend-subscription-state-safely)

**Outcome:** Set up the /api proxy, environment URLs, and frontend deployment.

**Acceptance criteria:**

- [ ] Requests route to the correct Render paths; authenticated/account-specific responses and auth endpoints cannot be cached by the CDN.
- [ ] Direct Render requests remain authenticated; CORS/trusted origins and preview isolation are explicit; later cookies/callbacks have a documented route design.

**Verification:** Two-user deployment smoke, cache-header checks, and unauthorized-origin tests.

**Evidence:** Pending.

### DEP-04 — Connect a hosted email test provider

**Status:** Todo · **Priority:** P1 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-05](P00-foundation.md#fnd-05--set-up-local-email-capture), [DEP-02](#dep-02--package-and-deploy-the-api-to-render)

**Outcome:** Add an HTTPS email adapter and approved test sender/recipient setup.

**Acceptance criteria:**

- [ ] Use provider secrets and a verified sender/domain or documented restricted test-sender rules; do not assume Render Free supports standard SMTP ports.
- [ ] Delivery/capture status is observable; duplicate retries are controlled and raw sensitive links are not logged.

**Verification:** Actual permitted test delivery and transient-provider-failure scenario.

**Evidence:** Pending.

### DEP-05 — Automate controlled migrations and deployment checks

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [DEP-01](#dep-01--provision-the-supabase-test-database-connection), [DEP-02](#dep-02--package-and-deploy-the-api-to-render), [DEP-03](#dep-03--deploy-react-to-vercel-and-verify-api-routing), [FND-06](P00-foundation.md#fnd-06--create-the-automated-verification-foundation)

**Outcome:** Extend CI for repeatable test-environment deployment.

**Acceptance criteria:**

- [ ] Schema migration runs once with locking/evidence through a supported CI/operator path, without requiring unavailable Free-tier predeploy jobs.
- [ ] Secret handling, backwards compatibility, health checks, rollback constraints, and environment-specific Stripe webhook configuration are documented.

**Verification:** Clean migration/deploy and failed-migration rollback/recovery rehearsal.

**Evidence:** Pending.

### DEP-06 — Accept the first hosted learning release R1

**Status:** Todo · **Priority:** P0 · **Phase:** P05 · **Suggested model:** Sonnet 5.5

**Dependencies:** [DEP-04](#dep-04--connect-a-hosted-email-test-provider), [DEP-05](#dep-05--automate-controlled-migrations-and-deployment-checks), [CAC-05](#cac-05--verify-memory-cache-consistency-guarantees)

**Outcome:** Demonstrate personal subscriptions on Vercel/Render/Supabase.

**Acceptance criteria:**

- [ ] Synthetic users can register/login, use plan-limited features, purchase in Sandbox, and observe lifecycle changes.
- [ ] Record JWT-only logout limitations, hosted email readiness, free-service sleep behavior, failed-work recovery, and exact deployed revisions.

**Verification:** R1 checklist with browser/API/Stripe evidence and restart drill.

**Evidence:** Pending.
