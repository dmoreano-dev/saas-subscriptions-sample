# 0012 — Local subscription projection and entitlement resolution

- **Status:** Accepted
- **Date:** 2026-10-25
- **Backlog item(s):** ENT-03
- **Deciders:** Project owner

## Context

Plan §3, §6 and §7: effective entitlements are decided from PostgreSQL at request time, never by calling the payment
provider, and a stale plan label in a JWT must not override them. The resolver must evaluate persisted schedules
against the current time even if no worker has run, never extend paid access past a bounded deadline, and report
version and validity metadata. Billing (P03) and the lifecycle (P04) will write subscription state, but no table for
it existed yet, so ENT-03 had nothing to read.

## Decision

- **ENT-03 creates the local projection, with only what resolution needs.** `subscriptions` (purchased
  `plan_version_id`, `status` `Active` | `PastDue` | `Ended`, `current_period_start`/`end`, `access_until`,
  `started_at`, `ended_at`) and `subscription_changes` (`kind` `PlanChange` | `Cancel`, `target_plan_version_id`,
  `effective_at`, `status` `Pending` | `Applied` | `Cancelled`). Nothing in the API writes these rows yet; tests
  insert them.
- **Free is not a row.** An account without a subscription that has not ended uses the latest published Free version.
  If no Free version is published the reader throws (fail closed); the Migrator seed provides it.
- **The access deadline is its own column.** `access_until` equals `current_period_end` while active and the end of
  the grace while past due. Paid capabilities apply in `[current_period_start, access_until)`; a missed renewal or a
  provider outage can never extend them. `current_period_end` (paid-through) stays separate so grace does not move
  the paid period.
- **Invariants in PostgreSQL.** A partial unique index keeps at most one non-ended subscription per account (plan §4
  invariant 6) and another keeps one pending change per subscription. Changes reference their subscription through
  the composite key `(account_id, subscription_id)`, so a change cannot cross accounts. CHECKs bind `ended_at` to
  `Ended`, the target to `PlanChange`, `resolved_at` to non-pending. A trigger rejects a subscription or a change that
  references a plan version that is not published (SQLSTATE 55000, like the catalog triggers); this closes the ENT-01
  limitation where "referenced" could only be enforced as "published".
- **Facts are stored, the answer is computed.** The same rows give different answers at different times (Pro until
  `access_until`, Free from then on, with no row changing), so the answer is never stored; it is recomputed on every
  request from the rows and `TimeProvider` time:
  - `IEntitlementReader` reads the facts with one small query per piece: the latest published Free version, the
    current subscription, its pending change, and each plan version they reference with its capabilities (a `Select`
    projection through the `PlanVersion.Plan` and `PlanCapability.Definition` navigations, added without a schema
    change: EF writes the joins and reads only the needed columns, no entities are loaded). The reader does not know the time, which is why it loads every version that could apply (at most three).
    Readability was preferred over fewer round trips; the P05 cache removes them from the hot path.
  - `EntitlementPolicy` (a pure function: facts + time → answer) applies the rules in order: no current subscription →
    Free; a cancellation whose instant passed → Free; at or after `access_until` → Free with state `restricted` and
    restriction `paymentOverdue` (was past due) or `accessExpired` (active, not renewed); a plan change whose instant
    passed → its target; otherwise the paid plan, `active` or `grace` (past due). A change governs access from its
    instant even if no worker marked it applied. `valid_until` is the earliest known future transition.
  - `EntitlementSnapshot` is that answer at one instant (`calculated_at`), with `IsGranted(key)`.
- **Absence denies.** The snapshot lists only what the effective version declares; `IsGranted(key)` is false for any
  other key, including keys nobody defined. A limit is granted when unlimited or above zero.
- **`IEntitlementService` takes an `AccountScope`** (ADR 0011), so it never answers for an unauthorized account.
  `GET /api/accounts/{id}/capabilities` (any active member) returns only what ENT-03 needs: `plan` {key, version},
  `calculatedAt`, `validUntil` and `capabilities`. The snapshot's state, restriction, effective-from and pending change
  stay internal until a screen needs them (LIF-02, LIF-04); adding response fields is non-breaking.

## Consequences

- Positive: the JWT carries only the user id, and capabilities are read per request, so a plan change applies to
  tokens already issued; a forged `plan` claim changes nothing.
- Positive: a scheduled downgrade or cancellation takes effect at its instant without a worker; P04 workers only
  tidy rows (`Pending` → `Applied`), they do not decide access.
- Trade-off: `access_until` equal to period end means paid access ends exactly at the end of the period until the
  renewal is confirmed. Writers that want leeway for webhook latency must set `access_until` later; the resolver does
  not add any.
- Deferred to the items that need them: an account access version for cache invalidation (P05 CAC-01), organization
  accounts without a free tier (P08/P09; today every account falls back to Free, and organization accounts cannot be
  created through the API), provider ids and `billing_customers` (P03), prices/intervals and quota anchors (ENT-05,
  LIF-05). That a subscription's plan matches the account type is not enforced; P03/P04 must check it.
- No cache yet: a capability read is 3 small queries for a Free account and up to 6 with a subscription and a pending
  change, after the two authorization queries.
