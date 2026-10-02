# Architecture Decision Records (ADRs)

One file per decision, capturing the *why* behind an architectural or technical choice so future
sessions don't re-litigate it. Keep each ADR short.

## Conventions

- **File name:** `NNNN-kebab-title.md`, zero-padded sequential number (e.g. `0002-use-argon2id.md`).
- **Status:** `Proposed` → `Accepted` → (`Superseded by NNNN` | `Deprecated`). Never edit an Accepted
  ADR's decision in place; write a new ADR that supersedes it and update the old one's status line.
- **Scope:** write an ADR when a choice is non-obvious, hard to reverse, or affects multiple modules
  (e.g. password hashing, JWT signing strategy, cache invalidation approach, migration workflow).
  Small, local choices don't need one.
- **Language:** English (codebase documentation). Link the backlog item it came from, e.g. `JWT-01`.

## Relationship to the other docs

- Baseline decisions **D01–D19** already live in [`../plan.md` §2](../plan.md); those stay there. ADRs
  here record decisions made *during implementation* and any change to a baseline decision.
- When an ADR changes a policy, also update `plan.md` and the affected backlog acceptance criteria.

## How to add one

1. Copy [`0000-template.md`](0000-template.md) to the next number.
2. Fill it in; set Status to `Proposed` (or `Accepted` if already agreed).
3. Link it from the relevant backlog item's Evidence when it drives that item's work.

## Index

| # | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions in ADRs | Accepted |
| [0002](0002-solution-layout-and-aspire-orchestration.md) | Solution layout and Aspire as the local orchestrator | Accepted |
| [0003](0003-postgresql-migrations-and-database-layout.md) | PostgreSQL migrations and database layout | Accepted |
