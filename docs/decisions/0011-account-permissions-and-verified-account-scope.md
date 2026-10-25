# 0011 — Account permissions in role tables and a verified account scope

- **Status:** Accepted
- **Date:** 2026-10-03
- **Backlog item(s):** ENT-02
- **Deciders:** Project owner

## Context

Plan §4 invariants 3–5 and §6: business resources carry `account_id`, a user-supplied account id is only a
selector, roles are account-scoped, platform operator roles are a separate scope, and Pro/Max are never roles.
Until now a membership had neither a status nor a role, and the account and project routes answered 501 to any
valid token.

## Decision

- **Permissions come from role rows, also for personal owners.** `permissions`, `roles`, `role_permissions` and
  `membership_roles` (plan §4). Registration writes the `personal_owner` role for the owner membership in the same
  transaction; the migration backfills owners that already existed. `accounts.personal_owner_user_id` stays the
  ownership invariant (ADR 0003), not the source of permissions, so personal and organization accounts are evaluated
  by one path and P08 only adds roles.
- **Roles and permissions are migration data** (`HasData`), not Migrator fixtures: handlers name them in code, so a
  database at a given migration must hold exactly those rows. Keys are text (`projects.read`), as in the catalog.
- **Platform scope is separated by the schema.** Roles and permissions carry a `scope` (`Account` | `Platform`).
  Composite foreign keys on `(key, scope)` plus a constant-scope CHECK make it impossible to attach a platform role to
  a membership, an account role to `platform_role_assignments`, or a permission of one scope to a role of the other.
  Account authorization never reads `platform_role_assignments`: an operator who is not a member gets 404.
- **One decision point.** `IAccountAccessService.AuthorizeAsync(userId, accountId, permission)` reads the current
  rows from PostgreSQL on every call and applies `AccountAccessPolicy`: active user, active membership, account
  status, permission. Only the user id is taken from the token.
- **`AccountScope` is the proof of that decision.** Its constructor is internal to the Application assembly and only
  the service calls it. Account-scoped data access (`IProjectStore`: EF queries and the raw SQL delete) takes an
  `AccountScope`, never a `Guid`, and puts `account_id` in every predicate. A future job stores the account id and
  must obtain a scope through the Application layer before touching data.
- **API wiring.** `RequireAccountAccess(permission)` adds the endpoint filter and an `AccountAccessRequirement`
  metadata. A test reads the endpoint table and fails if a route with `{accountId}` lacks the requirement or declares
  another permission. Handlers read `HttpContext.GetAccountScope()`, which throws when the filter did not run.
- **Responses.** Unknown account, no membership, suspended membership, closed account, or a resource of another
  account: `404 not_found`, indistinguishable from each other. Active member without the permission: `403 forbidden`.
  Active member of a suspended account: `403 account_suspended` (new code, so the UI can explain it). User missing or
  disabled: `401`.
- **Cross-account references.** `projects` has the alternate key `(account_id, id)`; any table that references a
  project must use the composite foreign key against it.
- **No EF global query filter.** The scope parameter is explicit in every query (plan §4: filters do not replace
  write checks).

## Consequences

- Positive: a stale or forged role/plan/account claim in a token grants nothing; a role or status change applies to
  tokens already issued, without re-login.
- Trade-off: two queries per account-scoped request (membership, then permissions), not in one transaction. A role
  removed between them could still be seen for that request. Caching is P05.
- Trade-off: the endpoint filter runs after parameter binding, so a non-member who sends a malformed body gets 400
  before 404. It reveals nothing about the account.
- A suspended account refuses every account route, including capabilities. Billing recovery for a suspended account
  (P03/P04) will need an explicit exception in the policy.
- The database does not force a personal owner membership to hold `personal_owner`; a membership without roles is
  denied (403), never granted.
- `projects` exists only with the columns needed here; creation, archiving and the project limit are ENT-04.
- Organization roles and the permission matrix are P08; operator endpoints and the use of `platform.operate` are P09.
