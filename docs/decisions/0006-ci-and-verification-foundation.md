# 0006 — CI and verification foundation

- **Status:** Accepted
- **Date:** 2026-10-03
- **Backlog item(s):** FND-06
- **Deciders:** Project owner

## Context

Until FND-06 the checks were commands in the README run by hand. The relational tests need real PostgreSQL
(plan §12), no provider credentials may be required, and a failing check has to fail the pipeline. Browser tests
have no flow to protect yet.

## Decision

- **GitHub Actions**, workflow `.github/workflows/ci.yml`, four parallel jobs (`backend`, `frontend`, `secrets`,
  `dependencies`) on `ubuntu-latest`, least-privilege (`contents: read`), no `secrets.*`.
- **PostgreSQL through Testcontainers on the runner's Docker**, not a CI service container. The fixture keeps one
  code path locally and in CI, with one database per test cloned from a migrated template. A service container
  would need a second fixture mode that can silently diverge.
- **`scripts/verify.sh` is the single entry point**; CI jobs call the same subcommands used locally. Only
  runner-specific steps (toolchain setup, caching, gitleaks download with pinned SHA-256) live in the workflow.
- **Checks:** `dotnet format --verify-no-changes`, build with warnings as errors, unit + integration tests,
  `npm ci`/build/lint/`contract:check`, gitleaks (full history, default ruleset, pinned 8.30.1), NuGet and npm
  vulnerability audit. Dependabot proposes weekly updates for NuGet, npm and GitHub Actions.
- **Browser tests:** a minimal Playwright project (Chromium, one smoke test over `vite preview`) in `tests/browser`,
  **local only**; the CI job is added with the first flow that needs the API.
- Actions are pinned to major versions (`checkout@v7`, `setup-dotnet@v6`, `setup-node@v7`); Dependabot keeps them current.

## Consequences

- Positive: local and CI cannot drift; a failed step is a red check; secrets and vulnerable dependencies are gated.
- Negative / trade-offs: Docker is required to run the backend job locally; the dependency job can turn red from a
  newly published advisory with no code change; actions are tag-pinned, not SHA-pinned (a supply-chain trade-off
  accepted for a learning lab); the runner environment is only validated by the first CI run.
- Follow-ups: add a browser CI job with JWT-04; add deployment checks in DEP-01; require the four checks in branch protection.
