# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

A learning SaaS lab built step by step: a C# API + React frontend with personal Free/Pro/Max
subscriptions and an additional Enterprise organization experience (identity, authorization,
entitlements, billing, data isolation, and their failure modes).

Implementation progress lives in the progress table of `docs/backlog.md`. The specification:

- [`docs/plan.md`](docs/plan.md) — architecture, decisions (D01–D19), data model and invariants,
  auth progression, authorization/cache consistency, billing, Enterprise, deployment, test strategy.
  This is the source of truth for *what* and *why*.
- [`docs/backlog.md`](docs/backlog.md) — the implementation **index**: how-to, progress table,
  Definition of Done, traceability, change log, and a **Current focus** pointer.
- [`docs/backlog/`](docs/backlog/) — one file per phase (`P00`…`P12`, `optional`) with the detailed
  items (acceptance criteria, dependencies, status, evidence). **This is the implementation tracker.**

Read the plan and the relevant phase file before writing code. Open only the phase you are working on.

## Working agreement

- **Language split:** all code, schema, migrations, identifiers, comments, tests, logs, error codes,
  and technical docs are in **English**. Explanations to the user in chat are in **Spanish**.
- **Test conventions:** follow Microsoft's [unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices):
  names are `Method_Scenario_ExpectedBehavior`; explicit `// Arrange`, `// Act`, `// Assert` sections with a
  single Act per test (use `[Theory]` instead of loops or repeated calls); minimal inputs; constants instead
  of magic strings; no logic in tests; helper methods instead of setup/teardown; time and other statics through
  seams (`TimeProvider`/parameters). Unit tests (`tests/unit`) have no infrastructure dependencies; anything that
  needs PostgreSQL or the host belongs in `tests/integration`.
- **Sequence:** start at **FND-01** and follow phase order P00→P12, respecting each item's dependency
  IDs. Mark an item *In progress* only when actually started.
- **Teach-the-concept-in-isolation:** do not scaffold later-phase mechanisms into earlier phases.
  Specifically, no persisted sessions, refresh tokens, auth cookies, Redis, or ASP.NET Core Identity
  in the P01 JWT lesson — they belong to P06/P07/P11.
- **Simulator-first:** use the deterministic local billing simulator and local dependencies; reach for
  Stripe Sandbox / Supabase / Render / Vercel / a real IdP only when the phase actually requires them.
  Missing credentials block *external acceptance*, not useful local work — record the gap, keep going,
  and do not mark the external checkpoint Done.

## Per-item model (advisory)

Each backlog item's Status line carries a **Suggested model**: `Sonnet 5.5` for most work, or
`Opus / high` for red-zone correctness items (subtle concurrency, crypto/token validation, cache
consistency, webhook ordering, OIDC callback, SCIM races, identity migration/recovery).

Before starting an item, compare its Suggested model with your active model. **If they differ, pause
and ask the user to switch with `/model` before implementing** — do not silently build an
`Opus / high` item on a weaker model. The tag is advisory: the user may override it (e.g. choose
Sonnet on an Opus item to save cost), and you may flag when an item feels harder/easier than tagged.

## Definition of Done (per item)

An item is Done only when behavior exists, acceptance criteria pass, tests are meaningful and
reproducible, and evidence is recorded. Enforcement details that are easy to get wrong:

- A passing **mocked** test is not completion evidence.
- Isolation/concurrency/transaction/token-rotation claims require tests against **real PostgreSQL**
  (not in-memory EF).
- A provider-backed claim requires **actual sandbox/provider evidence** in addition to simulator tests.

When you finish an item: update its **Status** and **Evidence** (commit/scenario/result + limitations)
in its per-phase file, and update the **Done count** and **Current focus** line in `docs/backlog.md`.
If a policy changes, update `docs/plan.md` and the affected acceptance criteria together. Record
non-obvious, hard-to-reverse, or cross-module decisions as an ADR in `docs/decisions/` (see its README).

## Item completion protocol

After finishing a backlog item, end your message with exactly two fenced blocks so the user can commit
and launch the next session. Two items are involved — keep them separate:
- **Current item** = the one you just worked on. Mark its **Status: Done** and fill its **Evidence**.
  The **commit message** is for THIS item.
- **Next item** = the one *after* the current item in phase/dependency order. Update the index's **Done
  count** and move **Current focus** to it. Only the **next-item prompt** is about it.

1. **Commit message** (for the CURRENT item) — Conventional-Commits style, referencing its ID and evidence:

   ```
   feat(<module>): <subject> (<ITEM-ID>)

   - <what changed>
   - Acceptance: <criteria satisfied>
   - Evidence: <build/test command + result; limitations>
   ```

   If you run the commit yourself, also append the `Co-Authored-By` line this session is instructed to
   use. Do not commit unless the user asks.

2. **Next-item prompt** (for the NEXT item) — a ready-to-paste, **detailed** prompt for the next `Todo`
   after the current item (the first item of the next phase if the current phase is now finished). Fill
   the decision bullets from that item's own acceptance criteria — give the user specifics to decide on,
   do not leave them generic. Shape:

   ```
   Implementemos <NEXT-ID> — <title>. Lee CLAUDE.md y docs/backlog/<phase-file>.md
   (ítem <NEXT-ID> y sus dependencias); si necesitas arquitectura, docs/plan.md <relevant §>.

   Antes de escribir código, muéstrame un plan corto y espera mi OK:
   - <item-specific decision 1>
   - <item-specific decision 2>
   - <tests: PostgreSQL real / proveedor real donde el ítem lo exija>
   - Suggested model del ítem: <X> — si no coincide con mi modelo activo, recuérdamelo antes de empezar.

   Luego implementa SOLO <NEXT-ID> con sus criterios de aceptación, código en inglés, sin
   scaffoldear mecanismos de fases posteriores. Al terminar, aplica este mismo protocolo.
   ```

If the next item's dependencies are not all Done, say so and propose the correct next item instead.

## Target stack (so structure/choices match the plan)

- API: ASP.NET Core on **.NET 10**, EF Core + Npgsql, PostgreSQL. Modular monolith (Identity, Accounts,
  Authorization, Subscriptions, Billing, Usage, Projects/Reports, Audit) — not microservices, no event
  broker/CQRS/generic-repository required. Use `TimeProvider` for business time.
- Web: React + TypeScript + Vite.
- Deploy targets: API → Render (Docker), Web → Vercel, DB → Supabase PostgreSQL.
- Layout (created in FND-01; see [ADR 0002](docs/decisions/0002-solution-layout-and-aspire-orchestration.md)):
  solution `Saas.Subscription.Sample.slnx` at the root; `src/backend/Saas.Subscription.Sample.{Domain,Application,Infrastructure,Api,Migrator}`
  (all provider integrations live in `Infrastructure`; the `Migrator` console applies EF migrations, see [ADR 0003](docs/decisions/0003-postgresql-migrations-and-database-layout.md)), `src/frontend/`, `src/aspire/Saas.Subscription.Sample.AppHost`
  (the single local orchestrator), `tests/{unit,integration,browser}/`, `docs/`.
- **Toolchain versions are pinned in FND-01** (see README "Pinned toolchain"): .NET SDK 10.0.401, Aspire 13.6.0,
  Node 24.16.0, npm 11.13.0. PostgreSQL major **17** is pinned (assumed Supabase major; confirm against the Supabase
  project when it exists and keep the local container matching it).

## Commands

```
dotnet build Saas.Subscription.Sample.slnx
dotnet test  Saas.Subscription.Sample.slnx
dotnet run --project src/aspire/Saas.Subscription.Sample.AppHost --launch-profile https   # API + frontend
(cd src/frontend && npm ci && npm run build && npm run lint)
(cd src/frontend && npm run contract:generate)   # after changing an endpoint/DTO: regenerates docs/contracts/openapi.json + TS types
(cd src/frontend && npm run contract:check)      # fails if the TS types drift from the committed OpenAPI document
dotnet tool run dotnet-ef migrations add <Name> --project src/backend/Saas.Subscription.Sample.Infrastructure --startup-project src/backend/Saas.Subscription.Sample.Migrator --output-dir Persistence/Migrations
```

`dotnet test` needs Docker running (Testcontainers starts a real PostgreSQL 17).

## Secrets

Never commit a real `.env`, JWT signing private key, provider API key, or connection string. Keep one
checked-in example config with placeholders. Keep raw tokens/hashes/secrets out of logs and DTOs.
