# P00 — Foundation

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Establish a reproducible foundation. Do not scaffold later authentication mechanisms into the first lesson.

### FND-01 — Create the English repository and modular skeleton

**Status:** Todo · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** None

**Outcome:** Create the API, React, test, infrastructure, and documentation structure described in the plan.

**Acceptance criteria:**

- [ ] The solution and frontend build from documented commands; all authored identifiers and technical documentation are English.
- [ ] Import plan.md/backlog.md into docs, add a README, and record pinned toolchain versions and architecture boundaries.

**Verification:** Clean-checkout build and documentation review.

**Evidence:** Pending.

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
