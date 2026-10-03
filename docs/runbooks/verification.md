# Runbook: automated verification (local and CI)

One entry point, [`scripts/verify.sh`](../../scripts/verify.sh), holds every check. CI
([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)) calls the same subcommands you run locally, so a
green local run means the same commands pass in CI. Rationale: [ADR 0006](../decisions/0006-ci-and-verification-foundation.md).

| Command | What it runs | Needs |
|---|---|---|
| `scripts/verify.sh backend` | `dotnet format --verify-no-changes`, `dotnet build` (warnings are errors), `dotnet test` (unit + integration) | .NET SDK from `global.json`, **Docker** |
| `scripts/verify.sh frontend` | `npm ci`, `npm run build` (`tsc -b` + Vite), `npm run lint` (oxlint), `npm run contract:check` | Node 24.16.0 |
| `scripts/verify.sh secrets` | `gitleaks detect` over the working tree **and the full git history** | `brew install gitleaks` |
| `scripts/verify.sh dependencies` | NuGet vulnerability report (direct + transitive) and `npm audit --audit-level=high` | network |
| `scripts/verify.sh browser` | Playwright smoke test against `vite preview` | Chromium (installed by the script); **local only, not in CI** |
| `scripts/verify.sh all` | backend + frontend + secrets + dependencies (what CI runs) | all of the above |

Each subcommand stops at the first failing step and exits non-zero; CI turns that into a red check. Provider
credentials are never needed: the workflow declares no `secrets.*`.

## CI jobs

`backend`, `frontend`, `secrets` and `dependencies` run in parallel on `ubuntu-latest` for every push to `main`
and every pull request. They are separate checks on purpose: a new advisory should not hide a test failure.
To make them gate merges, require the four checks in the repository's branch protection (a GitHub setting, not code).

## Fixture isolation (PostgreSQL and Mailpit)

- Relational tests never use in-memory EF. `PostgresFixture` starts **one** real `postgres:17` container per test
  collection through Testcontainers (the Docker daemon of your machine or of the CI runner; no service container
  and no connection string from the environment, so there is a single code path).
- **Every test gets its own database**: `CREATE DATABASE test_<guid>`, cloned `TEMPLATE migrated_template` when
  migrations are needed (the migrations run once per container). Tests therefore cannot see each other's rows and
  can run in parallel within the collection.
- If Docker is missing the fixture throws an explicit error; relational tests are **never skipped**, because a
  skipped test would look like passing evidence.
- The email tests start a pinned Mailpit container the same way. The image tags in the fixtures and the AppHost
  must be kept in sync.
- Unit tests (`tests/unit`) have no infrastructure dependency; anything needing PostgreSQL or the host belongs in
  `tests/integration`.

## Secret exclusion

- Never commit a real `.env`, JWT signing private key, provider API key or connection string. `.gitignore` already
  excludes `.env*` (except `.env.example`), `*.pem`, `*.pfx`, `*.key`, `appsettings.*.local.json` and `secrets.json`.
  The only checked-in configuration with placeholders is `appsettings.example.json`; local secrets live in
  `dotnet user-secrets`, hosted ones in environment variables.
- `scripts/verify.sh secrets` runs gitleaks with the default ruleset ([`.gitleaks.toml`](../../.gitleaks.toml))
  over the whole history. To allow a value, add an `[allowlist]` entry to that file only if it is provably not a
  secret, with a comment saying why. If a real secret is ever found, rotate it first; rewriting history is not a fix.
- Also enable **secret scanning and push protection** in the GitHub repository settings (manual; free for public
  repositories). gitleaks is a safety net, not a replacement.
- CI installs gitleaks from a pinned release and verifies its SHA-256 before running it.

## Formatting and static analysis

- `.editorconfig` defines style; `EnforceCodeStyleInBuild`, `AnalysisLevel=latest-recommended` and
  `TreatWarningsAsErrors` make analyzer and style violations fail `dotnet build`. `dotnet format --verify-no-changes`
  additionally fails on whitespace/layout drift. Fix locally with `dotnet format Saas.Subscription.Sample.slnx`.
- Frontend: `oxlint` and `tsc -b` (through `npm run build`). There is no Prettier by design.
- `contract:check` fails when the generated TypeScript types drift from `docs/contracts/openapi.json`.

## Dependency review

- `scripts/verify.sh dependencies` fails on a known-vulnerable NuGet package (direct or transitive) or an npm
  advisory of high severity or above.
- Dependabot ([`.github/dependabot.yml`](../../.github/dependabot.yml)) opens weekly update PRs for NuGet, npm
  (frontend and browser tests) and GitHub Actions. NuGet versions are centralized in `Directory.Packages.props`
  and npm versions are locked in `package-lock.json`; review the diff of both in every update PR.
- A vulnerability check only knows published advisories; it does not judge whether a new package is trustworthy.

## Browser tests (local only)

`tests/browser` is a minimal Playwright project (Chromium only) with one smoke test that builds the frontend, serves
it with `vite preview` and checks that the shell renders. Run it with `scripts/verify.sh browser` or, from
`tests/browser`, `npm ci && npm run install:browsers && npm test`.

It is intentionally **not** a CI job yet: there are no user flows to protect. Add the job when the first flow that
needs the API arrives (login in JWT-04; checkout return, account switching and cookie/CSRF behaviour later) and
extend the project then. The config already honours `CI=true` (no server reuse, `forbidOnly`).

## What cannot be proven locally, and how it is validated

Local runs prove the commands; they do not prove the GitHub runner environment. Not exercised locally:

- the workflow executing on `ubuntu-latest` (Docker available to Testcontainers, `setup-dotnet`/`setup-node`
  versions, the gitleaks download and checksum, `fetch-depth: 0`);
- branch protection, secret scanning/push protection and Dependabot (repository settings).

Mitigations: `actionlint` validates the workflow syntax and expressions locally (`brew install actionlint`), and the
commands themselves are the local ones. `act` is not used: Testcontainers inside `act`'s containers is unreliable
and would give false negatives. The real validation is the **first CI run on GitHub**; check that all four jobs are
green and that the `backend` job reports the same test totals as the local run.

## Proving a check can fail

A gate that cannot fail is not evidence. Two quick demonstrations (do not commit them): add a badly formatted `.cs`
file and run `scripts/verify.sh backend` (exit 2 from `dotnet format`); commit a throwaway file containing a fake
token in a scratch repository and run `gitleaks detect` (exit 1).
