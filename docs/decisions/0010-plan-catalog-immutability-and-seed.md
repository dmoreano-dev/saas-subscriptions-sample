# 0010 — Plan catalog: trigger-enforced immutability and Migrator seed

- **Status:** Accepted
- **Date:** 2026-10-03
- **Backlog item(s):** ENT-01
- **Deciders:** Project owner

## Context

Plan §4 invariants 7–8: capabilities are separate from prices and provider IDs; published plan versions are
immutable and changes create new versions. No subscription table exists yet, so "referenced by a subscription"
cannot be the immutability criterion.

## Decision

- **Immutable once published, enforced in PostgreSQL.** `BEFORE UPDATE/DELETE` triggers reject any change to a
  `Published` plan version (the only allowed transition is Draft → Published) and any insert, update, move or delete
  of its `plan_capabilities` and `plan_prices` (SQLSTATE `55000`). The domain (`PlanVersion`) also refuses edits after
  `Publish`, but the database is the guarantee: no code path, raw SQL or future job can bypass it. This is stricter than
  "referenced"; subscriptions will later point at published versions only.
- **Value shapes use plain CHECK constraints only:** exactly one of bool / limit / unlimited per capability, a limit
  needs a unit and a boolean has none, price amount/interval/currency. Rules that would need cross-table triggers (the
  value matches the feature's declared type, feature type/unit never change, a non-billable plan has no prices) were
  tried and dropped as too heavy for the lab; the domain factories and the seed keep them. Unlimited is an explicit
  flag, never a large number.
- **Free needs no provider:** `plans.is_billable = false` and no price rows in the seed; the catalog has no customer or
  subscription column. `plan_prices.external_price_id` is nullable, separate and optional.
- **Seed runs in the Migrator, not in a migration.** `DatabaseMigrator.SeedCatalogAsync` runs after `MigrateAsync`
 ; it inserts only what is missing, publishes version 1 after writing its rows,
  and reports (never rewrites) a published version that differs from the fixtures. Fixtures change over time and a
  migration runs once; a new fixture version is a new `plan_versions` row, not an edit.

## Consequences

- A superuser can still disable a trigger; the divergence report in the seed is the second line of defence.
- No lock: concurrent seed runs are not supported (one Migrator at a time, as for migrations, ADR 0003); a race would
  fail loudly on the unique indexes instead of corrupting data.
- Annual prices, `GET /api/plans` and subscription references arrive in later items.
