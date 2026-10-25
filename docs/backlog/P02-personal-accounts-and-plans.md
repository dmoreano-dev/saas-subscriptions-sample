# P02 — Personal accounts and plans

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Deliver the complete personal-account model first, using PostgreSQL-backed authorization and simulated commercial fixtures.

### ENT-01 — Create the versioned plan and price catalog

**Status:** Done · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-06](P01-jwt-essentials.md#jwt-06--verify-and-explain-the-jwt-checkpoint), [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations)

**Outcome:** Seed the Free/Pro/Max/Enterprise capability fixtures and separate prices.

**Acceptance criteria:**

- [x] Published versions are immutable once referenced; monthly/annual prices are distinct from capabilities and external price IDs.
- [x] Free works without a Stripe customer or subscription; capability keys, value types, limits, and quota units are explicit.

**Verification:** Seed idempotency, version immutability, and catalog query tests.

**Evidence:** 2026-10-03. Commit: `git log --grep ENT-01`. Decision: [ADR 0010](../decisions/0010-plan-catalog-immutability-and-seed.md).

- Verification: `dotnet build Saas.Subscription.Sample.slnx` → 0 warnings, 0 errors; `dotnet test` → 177/177 unit and 320/320 integration passed (real PostgreSQL 17 through Testcontainers); `has-pending-model-changes` test passes (no drift).
- Schema (migration `AddPlanCatalog`): `feature_definitions`, `plans`, `plan_versions`, `plan_capabilities`, `plan_prices`; immutability of published versions is enforced by two PostgreSQL triggers; value shapes by CHECK constraints.
- Seed: `DatabaseMigrator.SeedCatalogAsync` (called by the Migrator after migrations): 7 features, 4 plans (v1 published), 18 capabilities, 3 monthly prices (Pro 1000, Max 3000, Enterprise 20000, USD minor units). Annual prices are not seeded (plan §2: added later).
- Tests (`tests/integration/Persistence/PlanCatalogTests.cs`, `tests/unit/Catalog`): first seed inserts the fixtures; second run inserts nothing and leaves an md5 fingerprint of versions/capabilities/prices unchanged even with a different clock; a tampered published version is reported as a divergence and not rewritten; UPDATE/DELETE of published versions, capabilities and prices, and inserts into or moves out of a published version are rejected (`55000`); drafts are editable until published; a new version leaves v1 untouched; duplicate version number, Published without timestamp, not-exactly-one value, negative limit, limit without unit, bad interval/currency/amount, duplicate interval+currency and empty external id are rejected; unlimited is accepted; monthly and annual prices share one capability package;  Free has no prices and no provider/customer/subscription column exists; `CatalogReader` returns published plans with limits, units and prices, ignoring drafts.
- Limitations: value/type matching, immutable feature type/unit and Free-has-no-prices are not enforced by the database (trigger versions were tried and dropped as too heavy; the seed and domain keep them); concurrent seeds are not supported (no lock; single Migrator run); no mutation check was run on the triggers beyond the tests that disable/replace them; `GET /api/plans` and the OpenAPI contract are unchanged (deferred to ENT-08/ENT-03, so `contract:generate` was not needed); a superuser can disable a trigger (the seed divergence report is the second defence); `external_price_id` is never populated (no provider yet); no subscription references exist yet, so "referenced" is enforced as "published"; the Migrator seed was exercised through tests, not by a manual Aspire run.

### ENT-02 — Implement account-scoped permissions and resource isolation

**Status:** Done · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Opus / high

**Dependencies:** [JWT-06](P01-jwt-essentials.md#jwt-06--verify-and-explain-the-jwt-checkpoint), [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations)

**Outcome:** Centralize active membership, permission, and resource-ownership checks.

**Acceptance criteria:**

- [x] Personal owners receive scoped permissions; platform operator privileges are distinct; Pro/Max are never roles.
- [x] Reads, writes, raw SQL paths, relationships, and future job contexts reject unverified account IDs and cross-account resource references.

**Verification:** Two-user/two-account BOLA and cross-account write/FK tests.

**Evidence:** 2026-10-03. Commit: `git log --grep ENT-02`. Decision: [ADR 0011](../decisions/0011-account-permissions-and-verified-account-scope.md).

- Verification: `scripts/verify.sh all` → exit 0; `dotnet test` → 201/201 unit and 372/372 integration passed (real PostgreSQL 17 through Testcontainers); `contract:check` and the committed-contract test pass.
- Schema (migration `AddPermissionsAndProjects`): `permissions`, `roles`, `role_permissions`, `membership_roles`, `platform_role_assignments`, `memberships.status`, minimal `projects` with the alternate key `(account_id, id)`. Seeded as migration data: role `personal_owner` (Account: `projects.read`, `projects.write`, `reports.generate`, `billing.manage`) and `platform_operator` (Platform: `platform.operate`). Existing owners are backfilled.
- Behavior: `IAccountAccessService` decides from PostgreSQL on every request; `AccountScope` (not constructible outside the Application assembly) is the only way into `IProjectStore`. Implemented `GET /api/accounts`, `GET`/`DELETE` of projects (delete is raw SQL with the scope in the predicate). `GET capabilities` and `POST projects` are authorized first and then still answer 501 (ENT-03/ENT-04).
- Two-user/two-account tests over HTTP (`tests/integration/Authorization`): Ana with her own valid token gets 404 for Bob's project list, project (through his account and through hers), delete (both ways, row stays), create, capabilities, and for an unknown account; her own list returns only her project and `GET /api/accounts` only her account. State read per request with the same token: suspended membership 404, suspended account 403 `account_suspended`, closed account 404, disabled user 401, roles removed 403, role restored 200, read-only role reads but cannot delete. A correctly signed token claiming `plan=max`, roles, permissions and Bob's `account_id` grants nothing (404 on Bob's project, 403 on a write the role lacks), with a control proving that token is accepted. A platform operator without membership gets 404.
- Database tests (`PermissionSchemaTests`): a child row pointing at another account's project and moving a referenced project to another account are rejected (23503); a platform role on a membership, an account role as a platform assignment and a permission of the other scope on a role are rejected (23503/23514); the seeded roles are exactly the two system roles and no plan key is a role or permission; an owner created before the migration receives `personal_owner`.
- Guard: `ProtectedSurfaceTests` reads the endpoint table: the routes with `{accountId}` are exactly the listed ones and each declares its expected permission.
- Mutation checks (run and reverted): removing `account_id` from the project predicates failed 3 isolation tests; ignoring membership status and the specific permission in the policy failed 5 unit and 3 integration tests.
- Contract: one new problem code `account_suspended` and a documented 403 on `GetAccountCapabilities`; `openapi.json` and `schema.d.ts` regenerated.
- Limitations: the cross-account foreign key is proven with a table created inside the test, because no real table references `projects` until reports (ENT-06); no organization roles or permission matrix (P08) and no operator endpoint uses `platform.operate` yet (P09), so "platform privileges are distinct" is proven as schema separation plus no account access; the two authorization queries are not one transaction; the filter runs after body binding (a malformed body is 400 before 404); a suspended account also blocks capabilities and, later, billing until P03/P04 add an exception; the database does not force an owner to hold `personal_owner`; no browser or frontend change (the React app does not call these routes yet); `contract:generate` needed Development placeholders for the connection string and signing key (see README); the `Down` migration was not exercised; the token-validation tests now replace the access reader with a caller who has no accounts, since no protected route is database-free any more.

### ENT-03 — Resolve current subscription entitlements from PostgreSQL

**Status:** Done · **Priority:** P0 · **Phase:** P02 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ENT-01](#ent-01--create-the-versioned-plan-and-price-catalog), [ENT-02](#ent-02--implement-account-scoped-permissions-and-resource-isolation), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Build effective capability resolution with explicit time and account scope.

**Acceptance criteria:**

- [x] Resolve Free defaults or a subscription's effective version, access deadlines, pending changes, and restrictions without calling Stripe.
- [x] A stale plan label in a JWT cannot override current state; unknown capabilities deny access; response includes version and validity metadata.

**Verification:** Capability matrix, missing-capability, and effective-boundary tests.

**Evidence:** 2026-10-25. Commit: `git log --grep ENT-03`. Decision: [ADR 0012](../decisions/0012-subscription-projection-and-entitlement-resolution.md).

- Verification: `scripts/verify.sh all` → exit 0; `dotnet test` → 233/233 unit and 406/406 integration passed (real PostgreSQL 17 through Testcontainers); `contract:check` and the committed-contract test pass.
- Schema (migration `AddSubscriptionProjection`): `subscriptions` (purchased `plan_version_id`, `status` `Active`/`PastDue`/`Ended`, paid period, `access_until`, `ended_at`) and `subscription_changes` (`PlanChange`/`Cancel`, target version, `effective_at`, `Pending`/`Applied`/`Cancelled`). Partial unique indexes: one non-ended subscription per account, one pending change per subscription; composite FK `(account_id, subscription_id)`; CHECKs for status/ended_at, kind/target, pending/resolved_at, period and deadline order; a trigger rejects references to unpublished plan versions (55000), closing the ENT-01 "referenced = published" limitation. No provider ids.
- Behavior: `IEntitlementReader` reads the latest Free version, the current subscription, its pending change and each referenced plan version with its capabilities (one small query each, with a `Select` projection through the new `PlanVersion.Plan` / `PlanCapability.Definition` navigations; 3 queries for a Free account, up to 6). The pure `EntitlementPolicy` resolves them with `TimeProvider` time into an `EntitlementSnapshot`: Free default; paid plan in `[current_period_start, access_until)`; after the deadline Free with `restricted` + `paymentOverdue`/`accessExpired`; a change whose instant passed governs access without a worker; `valid_until` = next known transition. No published Free version → exception (fail closed). `IEntitlementService` takes an `AccountScope`. `GET /api/accounts/{id}/capabilities` is implemented and returns only `plan` {key, version}, `calculatedAt`, `validUntil` and the declared capabilities (state, restriction and pending change stay internal until LIF-02/LIF-04 screens need them).
- Capability matrix and boundaries (unit, `tests/unit/Entitlements`): Free/ended/active/past-due/restricted, each boundary at −1 s / exactly / +1 s (deadline, grace end, downgrade instant, cancellation instant), validity ends at the earliest transition, a subscription before its start is Free until the start; absence denies: undeclared and never-defined keys are not granted, limit 0 is not granted, unlimited is.
- PostgreSQL (`tests/integration/Entitlements`, `SubscriptionSchemaTests`): the latest published Free is used (a newer Free version wins); a subscriber keeps the purchased version after a newer one is published; ended subscriptions and applied/cancelled changes are ignored; persisted grace and an unapplied downgrade switch exactly at their instants; a database without a published Free version throws; every constraint above rejects bad rows with the expected SQLSTATE.
- HTTP over real PostgreSQL: Free defaults with plan version and no boundary; only declared capabilities, with kind, granted and explicit limit; a correctly signed token claiming `plan=max` and extra capabilities still gets Free; active Pro is valid until its deadline; past-due after grace gets Free; a downgrade scheduled before the deadline cuts `validUntil` at the downgrade; Bob's Max does not change Ana's answer; every token was issued before the rows were written.
- Mutation checks (run and reverted): `>` instead of `>=` at the deadline failed 4 unit + 1 integration tests; ignoring an effective pending change failed 6 + 1; granting a zero limit failed 1 unit; reading ended subscriptions failed 1 integration; reading non-pending changes failed 2 integration; picking the oldest Free version failed 1 integration.
- Contract: `CapabilitiesResponse` gains `plan` {key, version}; `openapi.json` and `schema.d.ts` regenerated. Guard tests updated: the expected table list, and the "no provider columns" query allows only `subscription_changes.subscription_id`.
- Scope trimmed on review: an account access version (for the P05 cache), an organization "no free tier" state and extra response fields (state, restriction, effective-from, pending change) were built and then removed because no ENT-03 criterion needs them; they belong to CAC-01, P08/P09 and LIF-02/LIF-04.
- Limitations: no writer for subscription rows exists yet (P03/P04), so all states are inserted by tests; no cache (P05 CAC-01) and therefore no 30-second bound; every account, including an organization account, falls back to Free (organization accounts cannot be created through the API yet); the plan/account-type match is not enforced by the database; `access_until` equal to period end means paid access stops at period end until a renewal is confirmed (writers may add leeway); the enforcement points that consume `IsGranted` (project cap, reports) are ENT-04/ENT-06; no frontend change (ENT-07); the `Down` migration was not exercised; suspended accounts still get 403 on capabilities (ADR 0011).

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
