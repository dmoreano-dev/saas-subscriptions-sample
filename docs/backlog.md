# Subscription Lab — Implementation Backlog

Version: 1.0  
Prepared: 2026-10-01  
Status: see the progress table below  
Architecture and policies: [Implementation plan](plan.md)

## How to use this backlog

This is the complete baseline implementation inventory for the agreed learning application: personal Free/Pro/Max subscriptions first, an additional Enterprise organization journey, custom identity before ASP.NET Core Identity, React on Vercel, C# on Render, PostgreSQL on Supabase, and local development dependencies.

There are **99 items: 95 required and 4 optional/conditional extensions**. Every item starts as **Todo**. The planning documents are delivered; see the progress table below for what is implemented.

- **P0:** required correctness, security, integration, or release foundation for its phase.
- **P1:** required product experience or learning/comparison work for the full baseline.
- **P2:** optional or externally dependent extension; excluded from baseline completion.
- **States:** Todo → In progress → Done. Use Blocked with a concrete reason and next action. Deferred applies to explicitly postponed optional scope, not to quietly missing required work.
- **Dependencies:** minimum technical prerequisites; respect the phase's learning sequence as well. Dependency links point to the item's per-phase file.
- **External inputs:** credentials/resources are supplied securely when needed. Continue useful local implementation when external verification is pending; do not mark the external acceptance or release checkpoint Done.
- **Evidence:** replace “Pending” with a commit/change reference, test/scenario and result, plus known limitations. A checked box or a mocked provider response alone is not completion evidence.
- **English codebase:** code, schema, endpoints, comments, tests, errors, logs, and documentation are in English. Teaching discussion may remain Spanish.
- **Suggested model:** each item's Status line recommends a model/effort — `Sonnet 5.5` for most work, `Opus / high` for red-zone correctness items (concurrency, crypto/token validation, cache consistency, webhook ordering, OIDC/SCIM, identity migration). Advisory only; the user switches with `/model` and may override.
- **File layout:** this file is the index (how-to, progress, Definition of Done, traceability, change log). The detailed items live in one file per phase under [`backlog/`](backlog/). Open only the phase you are working on; keep each item's **Status** and **Evidence** in its per-phase file as the single source of truth, and update the Done count in the progress table below when a phase advances.

The planning marker **AUTH-COMPARE** maps to CMP-01 through CMP-04. It is a later migration/comparison exercise, not a runtime authentication bypass.

Commercial seed values, effective-date rules, access guarantees, retention, role semantics, and integration limits are defined in plan.md. Change that document and affected acceptance criteria together if a policy changes.

## Progress and navigation

| Phase | Items | Required | Done | Link |
|---|---:|---:|---:|---|
| P00 — Foundation | 6 | 6 | 6 | [Open](backlog/P00-foundation.md) |
| P01 — JWT essentials | 6 | 6 | 6 | [Open](backlog/P01-jwt-essentials.md) |
| P02 — Personal accounts and plans | 8 | 8 | 3 | [Open](backlog/P02-personal-accounts-and-plans.md) |
| P03 — Billing integration | 8 | 8 | 0 | [Open](backlog/P03-billing-integration.md) |
| P04 — Subscription lifecycle | 8 | 8 | 0 | [Open](backlog/P04-subscription-lifecycle.md) |
| P05 — Memory cache and first hosted demo | 11 | 11 | 0 | [Open](backlog/P05-memory-cache-and-hosted-demo.md) |
| P06 — Identity lifecycle and sessions | 7 | 7 | 0 | [Open](backlog/P06-identity-lifecycle-and-sessions.md) |
| P07 — Refresh tokens and browser transport | 6 | 6 | 0 | [Open](backlog/P07-refresh-and-browser-transport.md) |
| P08 — Organizations | 7 | 7 | 0 | [Open](backlog/P08-organizations.md) |
| P09 — Enterprise commerce | 6 | 6 | 0 | [Open](backlog/P09-enterprise-commerce.md) |
| P10 — Corporate access | 11 | 11 | 0 | [Open](backlog/P10-corporate-access.md) |
| P11 — Comparison exercises | 5 | 5 | 0 | [Open](backlog/P11-comparison-exercises.md) |
| P12 — Full lab verification | 6 | 6 | 0 | [Open](backlog/P12-full-lab-verification.md) |
| Optional — Conditional and future extensions | 4 | 0 | 0 | [Open](backlog/optional-extensions.md) |

**Current focus:** [ENT-04](backlog/P02-personal-accounts-and-plans.md#ent-04--implement-scoped-project-operations) — next item, not started (ENT-03 done). Update this line to the active item at the start/end of each session.

## Definition of Done

An item is Done only when its behavior exists, applicable acceptance criteria pass, relevant tests are meaningful and reproducible, and documentation/evidence is updated. Avoid tests that merely mirror implementation. Relational isolation/concurrency claims require real PostgreSQL tests. A provider-backed claim requires actual sandbox/provider evidence in addition to simulator tests.

Phase delivery includes a runnable demonstration and a short explanation of what changed, why, and what limitations remain. Final baseline completion requires all 95 P0/P1 items, including the comparison exercises, plus the R3 acceptance record. Optional items retain their actual state.

## Requirement traceability

| Agreed requirement | Main items |
|---|---|
| Personal-first Free/Pro/Max experience | JWT-02, ENT-01 through ENT-08, BIL-03, LIF-01 through LIF-08 |
| Enterprise is an additional account context | ORG-01 through ORG-07, CON-01 through CON-06 |
| Own users/credentials/roles before Identity | JWT-01 through JWT-06, ENT-02, SEC-01 through SEC-07 |
| JWT first, then sessions, then refresh/cookies | JWT-03, SEC-03, REF-01 through REF-06 |
| Pro/Max capabilities enforced in the API | ENT-03, ENT-05, ENT-08, CAC-02 |
| Effective-date-safe upgrades/downgrades | LIF-01, LIF-02, LIF-04, CAC-02, CAC-03 |
| IDistributedCache with local/hosted memory first | CAC-01 through CAC-05, DEP-02 |
| Redis introduced later | CAC-06; optional ADV-01 |
| Stripe Sandbox and deterministic simulator | BIL-01 through BIL-08, LIF-08 |
| Local email capture and hosted delivery | FND-05, DEP-04, SEC-01, SEC-02, ORG-03 |
| React + Vercel; C# + Render Dockerfile | FND-01, JWT-04, DEP-02, DEP-03, REF-04 |
| Local PostgreSQL + hosted Supabase | FND-02, DEP-01, DEP-05 |
| Enterprise contracts and invoice terms | CON-01 through CON-06 |
| Corporate SSO and SCIM lifecycle | SSO-01 through SSO-06, SCI-01 through SCI-05 |
| Later Identity migration and fair performance comparison | CMP-01 through CMP-04 |
| English codebase and complete documentation | FND-01, FND-03, FIN-05 |
| Recovery, account closure, and testable release | FIN-01 through FIN-06 |

## Change and implementation log

| Date | Change | Evidence / reason |
|---|---|---|
| 2026-10-01 | Established planning baseline and all 99 backlog items | Decisions from the design conversation; implementation remains Todo |
| 2026-10-01 | FND-01 done: solution layout `src/backend` + `src/frontend` + `src/aspire`, Aspire as local orchestrator (D19, ADR 0002) | Replaces the provisional `src/api` / `src/web` layout in plan.md §3 |
| 2026-10-01 | FND-02 done: PostgreSQL 17 in the AppHost with persistent volume, Migrator project, default-schema tables for users, accounts and memberships (ADR 0003, D20) | Adds a fifth backend project (`Migrator`) to the FND-01 layout and decision D20 to plan.md |
| 2026-10-02 | Scope change: FND-02 drops custom schemas (default `public`), the separate runtime/migration database roles (deferred to DEP-01) and `memberships.is_owner` (roles in P02/P08) | Project owner: not needed to learn the SaaS concepts at this stage. Updated plan.md D20/§4/§10, ADR 0003, FND-02 criteria/evidence, DEP-01 |
| 2026-10-02 | FND-03 done: OpenAPI document + generated TypeScript types + ProblemDetails catalog (ADR 0004) | Contract-only endpoints answer 501; register has no duplicate-email 409 (anti-enumeration); `quota_exceeded` is a 409 |

Record future scope changes here. Update dependency links, phase counts, traceability, and plan.md whenever items are added, split, removed, or completed.
| 2026-10-02 | FND-04 done: typed options for seven groups validated at startup, hosted = any environment other than Development, `TimeProvider` registered and replaceable (ADR 0005) | The API now needs a database connection string to start (Aspire supplies it); no instance-count setting (single instance) |
| 2026-10-02 | FND-05 done: `IEmailSender` seam, MailKit SMTP adapter, Mailpit container in the AppHost, Development-only `POST /dev/email/test` | `Email:Provider=Https` resolves a sender that throws until DEP-04; the test endpoint is excluded from the OpenAPI contract |
| 2026-10-03 | FND-06 done (first GitHub CI run green): GitHub Actions workflow, `scripts/verify.sh` entry point, gitleaks, Dependabot, local-only Playwright smoke test (ADR 0006) | P00 complete (6/6); JWT-04 later adds the browser CI job |
| 2026-10-03 | JWT-01 done: Argon2id (Geralt/libsodium) behind `IPasswordHasher`, `password_credentials` table, `users.status`, 5–50 character policy (ADR 0007) | EF's migration lock does not serialize concurrent Migrators (found with a second migration): the FND-02 concurrency test and claim were withdrawn, P05 must guarantee a single run; minimum length 5 is a lab value |
| 2026-10-03 | JWT-02 done: atomic registration (user + credential + personal account + owner membership), decoy 201 for duplicate emails, generic credential login service, dummy-hash verification, rehash on login, in-memory per-email login throttle (ADR 0008) | `POST /api/auth/login` stays 501 until JWT-03 (no token yet); the API now opens a database connection (`AddPersistence`); throttle is per process and per email only, JWT-05 owns the rest |
| 2026-10-03 | JWT-03 done: ES256 access tokens (persistent PEM key + `kid`), bearer validation with a 30-second clock skew, `POST /api/auth/login` and `GET /api/me` implemented, every other `/api` route behind the token (ADR 0009) | The signing key is now required in every environment, not only hosted (ADR 0005 amended): a local run needs it in user-secrets; `contract:generate` cannot start the API without full configuration (pre-existing, to fix before the next contract change) |
| 2026-10-03 | JWT-05 done with criteria narrowed: log-redaction test over the real host and HSTS outside Development; throttle (ADR 0008), key and hosted TLS validators already existed; expensive work is bounded by input validation before hashing | Owner moved the explicit request-size limit (413 + oversized-payload test), per-IP rate limiting and request-log redaction to the end of the lab (revisit near the end or at P05 hosting); the acceptance criterion and verification line were edited accordingly |
| 2026-10-03 | JWT-06 done (P01 complete, 6/6): `ProtectedSurfaceTests` (every `/api` route but register/login needs a token; a valid token only reaches 501 handlers on contract-only routes), JWT checkpoint runbook with the concept split and demo checklist | Demo checklist not walked by hand (Playwright covers the same steps); copied token valid until `exp` is documented, revocation is P06 |
| 2026-10-03 | ENT-01 done: versioned plan/price catalog (`plans`, `plan_versions`, `plan_capabilities`, `plan_prices`, `feature_definitions`), PostgreSQL triggers for published-version immutability, idempotent seed in the Migrator (ADR 0010) | `GET /api/plans` deferred; annual prices not seeded; immutability is by "published" because no subscriptions exist yet |
| 2026-10-03 | ENT-02 done: role/permission tables with separate platform scope, `memberships.status`, central `IAccountAccessService` + `AccountScope`, minimal `projects` table with list/get/delete (ADR 0011) | New problem code `account_suspended` (403); `GET /api/accounts` and project read/delete are implemented, capabilities and project creation stay 501 behind authorization |
| 2026-10-25 | ENT-03 done: local subscription projection (`subscriptions`, `subscription_changes`), trigger allowing only published plan versions, reader (facts) + pure `EntitlementPolicy` (facts + time → answer), `GET /api/accounts/{id}/capabilities` implemented (ADR 0012) | Schema created in ENT-03 instead of P03 because the resolver needs something to read; access version deferred to P05 CAC-01 and organization accounts to P08/P09 (owner: build only what the item needs); transitive security pins and a vite bump landed just before (separate `chore(deps)` commit) |
