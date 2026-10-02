# 0003 — PostgreSQL migrations and database layout

- **Status:** Accepted
- **Date:** 2026-10-01 (amended 2026-10-02 before the first commit: single database login, default schema)
- **Backlog item(s):** FND-02
- **Deciders:** Project owner

## Context

FND-02 needs a repeatable way to create the schema for personal registration (users, accounts,
memberships) and to enforce the data-model invariants from `plan.md` §4 in the database itself.
`plan.md` §10 requires migrations applied once per deployment by a controlled step (never on API startup)
and the application's tables kept apart from Supabase-managed schemas. It also recommends a limited runtime
role and a separate migration identity. This is a learning lab: neither custom schemas per module nor the
role split are needed to learn the SaaS concepts at this stage, so both are deferred.

## Decision

- **Engine:** PostgreSQL major **17** (assumed Supabase major; confirm when the Supabase project exists).
  Pinned as `WithImageTag("17")` in the AppHost and `postgres:17` in the Testcontainers fixture.
- **EF Core 10 + Npgsql 10.0.3** with `EFCore.NamingConventions` (snake_case). One `AppDbContext` in
  `Infrastructure/Persistence`; entity configurations live in one folder per module (`Identity/`,
  `Accounts/`); one migrations history.
- **Default schema:** all tables (`users`, `accounts`, `memberships`) and EF's `"__EFMigrationsHistory"` (default name) live
  in the database's default `public` schema; no custom schema is created. Modules stay visible as folders in
  the code and as table names, not as database schemas. An earlier iteration used `identity`, `accounts` and
  `schema_migrations` schemas; it was removed as noise for a lab (it gave organization and a hook for
  per-module grants, but nothing in the plan needs them yet).
- **Applying migrations:** the `Saas.Subscription.Sample.Migrator` console project, run to completion by
  the AppHost (the API waits for it) or by CI/an operator. The API never migrates at startup. Concurrent
  runs are safe because EF Core locks the database while migrating.
- **One database login for now.** The Migrator and, later, the API use the same connection string (locally
  the container's `postgres` superuser, injected by the AppHost as `ConnectionStrings__appdb`). A limited
  runtime role, a separate migration identity, least-privilege grants and tests proving the runtime login
  cannot run DDL are deferred to the hosted database (DEP-01). An earlier iteration of FND-02 implemented
  that split (an `app_runtime` role created by the Migrator, with grants and tests); it was removed.
- **Conventions:** UUID v7 keys generated in the application; `timestamptz` UTC timestamps set from the
  application clock (`TimeProvider` arrives in FND-04); explicit `xmin` shadow row-version property for
  optimistic concurrency (no extra column); enums stored as text guarded by `CHECK` constraints; all
  foreign keys `ON DELETE RESTRICT`.
- **Invariants enforced by the database:**
  - Unique `normalized_email` (plus a `CHECK` that it is already trimmed/lowercase); `email` keeps what the
    person typed.
  - One personal account per user: `CHECK ((type='Personal') = (personal_owner_user_id IS NOT NULL))`, a
    partial unique index on `personal_owner_user_id WHERE type='Personal'`, and a deferrable composite foreign
    key `(id, personal_owner_user_id) → memberships (account_id, user_id)` that requires the owner's membership
    at commit (user, account and membership are inserted atomically).
  - Unique `(account_id, user_id)` membership.
- **No owner flag on memberships.** The owner of a personal account is `accounts.personal_owner_user_id` (single
  source of truth, protected by the constraints above); its membership row carries no role. Ownership of
  organizations and any account-scoped permission will be modeled with roles and `membership_roles` (P02/P08).
  An earlier iteration had a placeholder `memberships.is_owner`; it was removed because it duplicated
  `personal_owner_user_id` for personal accounts and organizations do not exist until P08.
- **Tests:** Testcontainers (`postgres:17`) in `tests/integration`; Docker is the only requirement and the
  tests fail loudly (never skip) when it is missing.

## Consequences

- Positive: invariants hold even if application code is wrong or races; migrations are reproducible and
  reviewable; fewer moving parts (no role provisioning, no extra secret).
- Negative / trade-offs: the application runs with a privileged login, so a bug or injection in the API could
  alter the schema; acceptable only for a local learning lab and it must not reach the hosted demo unchanged.
  Supabase exposes `public` through its Data API by default, so before the hosted database holds data the Data
  API must be disabled (or `anon`/`authenticated` privileges revoked) — recorded in DEP-01 and `plan.md` §10.
  The deferrable foreign key is hand-written SQL inside the initial migration (EF cannot model it). EF's
  generated migration lists `xmin` columns, but Npgsql does not emit them (it is a system column). Rolling a
  migration back with `Down` is for local use only.
- Follow-ups: FND-04 adds typed Database options and `TimeProvider`; P01 uses this schema for registration;
  P02 adds account-scoped roles (owner/admin/member) on memberships; DEP-01 introduces the separate migration and runtime
  roles for Supabase and handles the `public` exposure (custom schemas can be reconsidered there); FND-06 adds the CI PostgreSQL service and may revisit the Docker-missing behavior.
  Refines `plan.md` §4/§10.
