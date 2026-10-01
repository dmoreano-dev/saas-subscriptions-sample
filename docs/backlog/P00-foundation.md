# P00 — Foundation

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Establish a reproducible foundation. Do not scaffold later authentication mechanisms into the first lesson.

### FND-01 — Create the English repository and modular skeleton

**Status:** Done · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** None

**Outcome:** Create the API, React, test, infrastructure, and documentation structure described in the plan.

**Acceptance criteria:**

- [x] The solution and frontend build from documented commands; all authored identifiers and technical documentation are English.
- [x] Import plan.md/backlog.md into docs, add a README, and record pinned toolchain versions and architecture boundaries.

**Verification:** Clean-checkout build and documentation review.

**Evidence:** 2026-10-01. Commit: `git log --grep FND-01`.

- Clean-checkout simulation: copied only the non-ignored files (`git ls-files -co --exclude-standard`, 62 files, no `bin/obj/node_modules`) to a temp directory and ran the README commands there:
  `dotnet build Saas.Subscription.Sample.slnx` → 0 warnings, 0 errors (warnings are errors);
  `dotnet test Saas.Subscription.Sample.slnx` → 2/2 passed (integration `WebApplicationFactory` tests for `/health/live` and an unknown route; the unit project is an empty skeleton);
  `cd src/frontend && npm ci && npm run build && npm run lint` → OK, 0 vulnerabilities.
- Local startup: `dotnet run --project src/aspire/Saas.Subscription.Sample.AppHost --launch-profile https` started the API and Vite; `/health/live` returned 200 `Healthy` over HTTP and HTTPS, the frontend served the page, and `/api/*` through Vite reached the API (404 from the API, not 502). Shut down cleanly with SIGTERM.
- Docs: layout and Aspire recorded in [ADR 0002](../decisions/0002-solution-layout-and-aspire-orchestration.md); `plan.md` §2 (D19), §3 and §10, `CLAUDE.md` and the ADR index updated; README documents pinned toolchain, boundaries and clean-build steps.
- Pinned: .NET SDK 10.0.401, net10.0, Aspire 13.6.0, xUnit 2.9.3, Node 24.16.0, npm 11.13.0; the Vite template resolved to Vite 8.3.2, TypeScript 6.0.3, React 19.3.0 (locked in `package-lock.json`).
- Limitations: layer-reference rules are enforced by review only (an automated check was removed from FND-01 and deferred to FIN-04); PostgreSQL major version (target 17) is documented but not pinned in code until FND-02; Docker is not exercised yet (first needed in FND-02); no CI and no frontend/browser tests yet (FND-06); "clean checkout" was simulated from the working tree rather than a real clone of a commit; Aspire verification was manual, on macOS arm64 only; the `http` AppHost profile does not start without `ASPIRE_ALLOW_UNSECURED_TRANSPORT` (README documents the `https` profile).

### FND-02 — Establish PostgreSQL migrations and account foundations

**Status:** Todo · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton)

**Outcome:** Create the migration workflow and minimal accounts/memberships schema needed by personal registration.

**Acceptance criteria:**

- [ ] Local PostgreSQL uses a persistent container volume and migration-managed application schemas.
- [ ] UUID keys, UTC timestamps, account ownership, personal-account uniqueness, foreign keys, and runtime/migration database roles are documented and enforced.

**Verification:** Apply migrations to an empty real PostgreSQL instance and verify constraints.

**Evidence:** Pending.

### FND-03 — Define API and frontend contracts

**Status:** Todo · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton)

**Outcome:** Specify the initial endpoint families, DTO conventions, and consistent error handling.

**Acceptance criteria:**

- [ ] OpenAPI describes initial registration, login, account, project, and capability contracts; TypeScript contract generation or equivalence checks are reproducible.
- [ ] ProblemDetails has stable English codes and correlation IDs; DTOs exclude secrets and unauthorized fields.

**Verification:** Contract generation/build and representative error-response tests.

**Evidence:** Pending.

### FND-04 — Add typed configuration, provider seams, and controllable time

**Status:** Todo · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton)

**Outcome:** Introduce narrow integration boundaries and environment-aware startup validation.

**Acceptance criteria:**

- [ ] Database, Authentication, Billing, Email, Cache, Frontend, and BackgroundWork options have validation and placeholder examples.
- [ ] TimeProvider can be replaced in tests; simulator controls are explicitly gated and invalid hosted combinations fail startup.

**Verification:** Configuration validation tests and deterministic time tests.

**Evidence:** Pending.

### FND-05 — Set up local email capture

**Status:** Todo · **Priority:** P1 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton), [FND-04](#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Provide IEmailSender and a local SMTP adapter targeting Mailpit.

**Acceptance criteria:**

- [ ] A developer can inspect an HTML/text test message locally without sending to a real inbox.
- [ ] SMTP host/port are configurable so MailDev can replace Mailpit; no application workflow depends on either product's UI/API.

**Verification:** Send and inspect a synthetic message in the local capture service.

**Evidence:** Pending.

### FND-06 — Create the automated verification foundation

**Status:** Todo · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton), [FND-02](#fnd-02--establish-postgresql-migrations-and-account-foundations), [FND-03](#fnd-03--define-api-and-frontend-contracts)

**Outcome:** Set up backend, frontend, PostgreSQL integration, and browser test entry points.

**Acceptance criteria:**

- [ ] CI builds and runs relevant tests without live provider credentials; real PostgreSQL is available for relational tests.
- [ ] A failing check fails CI; fixture isolation, secret exclusion, formatting, and dependency review are documented.

**Verification:** Run the same verification commands locally and in CI.

**Evidence:** Pending.
