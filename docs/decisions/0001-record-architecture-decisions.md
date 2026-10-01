# 0001 — Record architecture decisions in ADRs

- **Status:** Accepted
- **Date:** 2026-10-01
- **Backlog item(s):** FND-01
- **Deciders:** Project owner

## Context

The lab is implemented step by step across many sessions, possibly on different models. Detailed design
(concrete types, DTOs, schemas, library choices) is derived just-in-time per phase rather than written
up front, so it stays accurate. We still need a durable, low-drift place to capture the *why* behind
non-obvious choices so future sessions don't re-litigate them.

`plan.md` §2 already holds the baseline product/architecture decisions **D01–D18**. We do not want to
duplicate those, but we do need somewhere for decisions made *during implementation*.

## Decision

We will keep lightweight ADRs in `docs/decisions/`, one file per decision, following the format in
`0000-template.md` and the conventions in this folder's `README.md`.

- Baseline decisions D01–D18 remain in `plan.md`; ADRs cover implementation-time decisions and any
  change to a baseline decision.
- An ADR is written when a choice is non-obvious, hard to reverse, or crosses module boundaries.
- Accepted ADRs are immutable in their decision; changes are made by a new superseding ADR.

## Consequences

- Positive: durable rationale, cheap to read, no up-front architecture that would drift from code.
- Negative / trade-offs: requires the small discipline of writing an ADR at decision time and keeping
  the README index current.
- Follow-ups: implementation sessions link the driving ADR from the relevant backlog item's Evidence.
