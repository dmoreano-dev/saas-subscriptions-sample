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

**Status:** Done · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton)

**Outcome:** Create the migration workflow and minimal accounts/memberships schema needed by personal registration.

**Acceptance criteria:**

- [x] Local PostgreSQL uses a persistent container volume and migration-managed application schemas.
- [x] UUID keys, UTC timestamps, account ownership, personal-account uniqueness, and foreign keys are documented and enforced. *(Scope change 2026-10-02: separate runtime/migration database roles were removed from this item and deferred to DEP-01; one login is used for now, see ADR 0003.)*

**Verification:** Apply migrations to an empty real PostgreSQL instance and verify constraints.

**Evidence:** 2026-10-01. Commit: `git log --grep FND-02`. Decisions: [ADR 0003](../decisions/0003-postgresql-migrations-and-database-layout.md); runbook: [database-migrations](../runbooks/database-migrations.md).

- Verification after the scope changes and the test refactor (2026-10-02): `dotnet build Saas.Subscription.Sample.slnx` → 0 warnings, 0 errors; `dotnet test` → 10/10 unit and 29/29 integration passed (27 against a real PostgreSQL 17 container through Testcontainers, 2 health-endpoint tests); `dotnet ef migrations has-pending-model-changes` → no drift; `dotnet ef migrations add` verified without a running database. An earlier 90-file clean-checkout simulation (non-ignored files only) passed with the same commands before the scope changes.
- Relational tests (`tests/integration/Persistence`): migrations apply to an empty database (exactly `users`, `accounts`, `memberships` and the EF history table in the default `public` schema, no custom schema); re-running is a no-op; 4 concurrent Migrator runs apply each migration exactly once (EF Core's migration lock); keys are `uuid`, `*_at` are `timestamptz`, identifiers are snake_case; unique normalized email and non-canonical-email CHECK; one personal account per user (partial unique index); personal account without its owner membership rejected at commit (deferrable composite FK), also when the only member is someone else; personal/organization owner CHECK, type/status CHECKs; membership FKs, unique `(account_id, user_id)`, `ON DELETE RESTRICT`; `xmin` optimistic concurrency throws `DbUpdateConcurrencyException`.
- Test sensitivity (manual mutation check on the final tests, reverted): removing the deferrable FK failed the 2 commit-time tests; making the partial index non-unique failed the duplicate-personal-account test; removing the email CHECK failed its 3 cases; removing the owner CHECK failed its 2 tests.
- Test conventions: tests follow Microsoft's [unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) (see CLAUDE.md).
- Local stack (manual, macOS arm64, Docker 29.7.2): `dotnet run` on the AppHost (https profile) started `postgres:17` with volume `saas-sample-pgdata`, ran the Migrator to completion (the API waited for it), schemas/tables/history row present, API `/health/live` Healthy. A row inserted before a stop/restart was still there afterwards, the persisted admin password still worked and the Migrator was a no-op. After the scope change the volume was recreated and the same startup re-verified.
- Scope change (2026-10-02): custom schemas (`identity`, `accounts`, `schema_migrations`) were removed; everything lives in `public`. The initial migration was regenerated, the local volume recreated and the stack re-verified (`\dt` shows the 4 tables; migration applied; API Healthy).
- Scope change (2026-10-02): `memberships.is_owner` was removed (the personal owner is `accounts.personal_owner_user_id`; roles arrive in P02/P08). Migration regenerated, volume recreated, stack re-verified.
- Limitations: tables are in `public`, which Supabase exposes through its Data API by default, so DEP-01 must disable the Data API or revoke `anon`/`authenticated` privileges before any hosted data; a single database login (the container `postgres` superuser locally) is used for migrations and, later, for the API; separate migration/runtime roles, least-privilege grants and their tests were built and then removed on 2026-10-02 as out of scope for the learning goal, and are deferred to DEP-01; PostgreSQL major 17 is assumed, not yet confirmed against a Supabase project (none exists yet); the API does not connect to the database yet (typed Database options FND-04, registration P01), so there is no `/health/ready` check; no CI yet, so the Docker-required tests run locally only (FND-06); the `Down` migration was not exercised; a membership carries no role until P02 (the owner of a personal account is read from `accounts.personal_owner_user_id`).

### FND-03 — Define API and frontend contracts

**Status:** Done · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton)

**Outcome:** Specify the initial endpoint families, DTO conventions, and consistent error handling.

**Acceptance criteria:**

- [x] OpenAPI describes initial registration, login, account, project, and capability contracts; TypeScript contract generation or equivalence checks are reproducible.
- [x] ProblemDetails has stable English codes and correlation IDs; DTOs exclude secrets and unauthorized fields.

**Verification:** Contract generation/build and representative error-response tests.

**Evidence:** 2026-10-02. Commit: `git log --grep FND-03`. Decisions: [ADR 0004](../decisions/0004-api-contract-openapi-and-problem-details.md); contract reference: [`docs/contracts/README.md`](../contracts/README.md).

- Verification: `dotnet build Saas.Subscription.Sample.slnx` → 0 warnings, 0 errors; `dotnet test` → 10/10 unit and 117/117 integration passed (27 relational tests against real PostgreSQL 17 from FND-02, plus 90 new tests that need no database); `cd src/frontend && npm ci && npm run build && npm run lint && npm run contract:check` → OK.
- Contract: `docs/contracts/openapi.json` (OpenAPI 3.1, 7 paths / 9 operations: register, login, me, accounts, capabilities, projects list/create/get/delete) generated from the code; `src/frontend/src/api/schema.d.ts` generated by openapi-typescript 7.13.0 and consumed through `src/api/contract.ts`. Regenerating the document and the types twice produced identical files (md5 equal, empty `git diff`).
- Drift detection (manually checked): tampering with `schema.d.ts` makes `npm run contract:check` exit 1 and regenerating makes it exit 0; changing a DTO without regenerating fails `GetDocument_InDevelopment_MatchesCommittedContract`; adding a `PasswordHash` property to a response DTO and regenerating fails `ResponseSchema_Properties_DoNotExposeSecretOrInternalFields`.
- Error handling tests (`tests/integration/Problems`, a test host with the same registration as the API): 401, 403, `capability_not_granted`, 404, 409, `quota_exceeded`, 429 (+ `Retry-After`), 503, 400, validation `errors`, bare status-only results turned into problems, 500 and 503 from exceptions, no exception message/type/stack trace in the body, correlation id echoed/replaced/generated and present in header and body. `tests/integration/Contracts` covers the real API through `WebApplicationFactory`: every endpoint answers 501 `not_implemented`, malformed JSON is 400 `bad_request`, an unknown route is 404 `not_found`, `/openapi/v1.json` and `/scalar` are 200 in Development and 404 in Production, the served document equals the committed one, error responses use ProblemDetails with allowed statuses, bearer security matches the anonymous routes, the `code` enum equals `ProblemCodes.All`.
- Mutation checks (reverted): accepting any correlation id, leaking `exception.Message`, mapping OpenAPI outside Development and a leaked DTO field each failed the expected tests.
- Manual run (`dotnet run --no-launch-profile`): Development → 404 problem echoed `X-Correlation-ID`, login stub 501 `not_implemented`, `/openapi/v1.json` and `/scalar/v1` 200; Production → both 404.
- Limitations: all endpoints are contract-only (501); authentication, validation, throttling and the bearer requirement are documentation or catalog entries until P01 (no rate limiter exists, so 429 is only tested through the test host); the TypeScript check is a documented command, not yet in CI (FND-06); npm `overrides` forces `typescript` for openapi-typescript 7.13 (peer range `^5`) while the project uses TypeScript 6; `User` has no display name, so DTOs expose only id, email and createdAt; duplicate-email behaviour (indistinguishable response and timing) is specified here but implemented in P01/P06; `accessVersion` is intentionally not exposed; no frontend runtime client yet.



### FND-04 — Add typed configuration, provider seams, and controllable time

**Status:** Done · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton)

**Outcome:** Introduce narrow integration boundaries and environment-aware startup validation.

**Acceptance criteria:**

- [x] Database, Authentication, Billing, Email, Cache, Frontend, and BackgroundWork options have validation and placeholder examples.
- [x] TimeProvider can be replaced in tests; simulator controls are explicitly gated and invalid hosted combinations fail startup.

**Verification:** Configuration validation tests and deterministic time tests.

**Evidence:** 2026-10-02. Commit: `git log --grep FND-04`. Decisions: [ADR 0005](../decisions/0005-typed-configuration-and-hosted-startup-validation.md).

- Verification: `dotnet build Saas.Subscription.Sample.slnx` → 0 warnings, 0 errors; `dotnet test` → 64/64 unit and 148/148 integration passed (no new test needs PostgreSQL; the 27 relational tests from FND-02 still run against real PostgreSQL 17).
- Options: `Database`, `Authentication`, `Billing`, `Email`, `Cache`, `Frontend`, `BackgroundWork` in `Application/Configuration` with DataAnnotations plus one `IValidateOptions<T>` each; registered in `Api/Configuration` with `ValidateOnStart`. Placeholder example: `src/backend/Saas.Subscription.Sample.Api/appsettings.example.json`, compared with the options by `ExampleConfigurationTests`; secrets via user-secrets (`UserSecretsId`) or environment variables.
- Startup rules (each a test that breaks one setting of a valid hosted profile and asserts the key named in the failure): hosted simulator controls, signing key and key id missing, database without `SSL Mode=VerifyFull`, SMTP email provider, Https email without API key, http or localhost frontend URL, wildcard origin; in every environment: Redis without connection string, Stripe without keys, `sk_live_` key, lease not longer than the poll interval. A valid hosted profile starts and answers `/health/live`; Development defaults start; a malformed connection string failure does not echo the password.
- Time: `TimeProvider.System` registered with `TryAddSingleton`; integration tests resolve the system clock by default and a `FakeTimeProvider` (advanced deterministically) when replaced; unit tests drive `User`/`Account` timestamps from a `FakeTimeProvider` (`now` stays a parameter).
- Mutation checks (reverted): disabling the hosted simulator rule and the hosted signing-key rule each failed the expected unit and integration tests.
- Manual run (`dotnet run --no-launch-profile`): Development without a connection string and Production with an empty configuration both stop at startup (Production lists every missing key); Development with `ConnectionStrings__appdb` starts.
- Limitations: only configuration is validated (no connectivity check, no `/health/ready` yet); the AppHost now passes the database reference to the API but the Aspire run itself was not exercised in this item; hosted = any environment other than `Development`; placeholder values satisfy presence checks; no seams were declared besides `TimeProvider` (IEmailSender → FND-05, IBillingGateway → BIL-01); `CorporateIdentity` options arrive with P10; no CI yet (FND-06).


### FND-05 — Set up local email capture

**Status:** Done · **Priority:** P1 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton), [FND-04](#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Provide IEmailSender and a local SMTP adapter targeting Mailpit.

**Acceptance criteria:**

- [x] A developer can inspect an HTML/text test message locally without sending to a real inbox.
- [x] SMTP host/port are configurable so MailDev can replace Mailpit; no application workflow depends on either product's UI/API.

**Verification:** Send and inspect a synthetic message in the local capture service.

**Evidence:** 2026-10-02. Commit: `git log --grep FND-05`. Runbook: [local-email-capture](../runbooks/local-email-capture.md).

- Verification: `dotnet build Saas.Subscription.Sample.slnx` → 0 warnings, 0 errors; `dotnet test` → 64/64 unit and 155/155 integration passed. `docs/contracts/openapi.json` unchanged (`contract:check` clean).
- Seam and adapter: `IEmailSender.SendAsync(EmailMessage(To, Subject, HtmlBody, TextBody))` in `Application/Email`; `SmtpEmailSender` in `Infrastructure/Email` (MailKit 4.18.1, plain SMTP, `multipart/alternative`, sender from `EmailOptions`, transport failures → `DependencyUnavailableException`); registered by `AddEmail()` according to `Email:Provider`.
- Automated: `SmtpEmailSenderTests` sends through a real Mailpit container (Testcontainers) and reads Mailpit's HTTP API **only in the test** to assert subject, sender, recipient and both the text and HTML parts; a closed port raises `DependencyUnavailableException`. `EmailSenderRegistrationTests` (no Docker): Development resolves the SMTP sender; hosted `Provider=Https` starts and resolves `UnavailableEmailSender`, whose send throws `DependencyUnavailableException`; `POST /dev/email/test` accepts a valid address, rejects an invalid one with 400, and is a 404 when hosted.
- Manual run (AppHost, `--launch-profile https`): the `mailpit` container (`axllent/mailpit:v1.31.3`) started and the API received `Email__Smtp__Host/Port` from the AppHost; `POST /dev/email/test` → 202; Mailpit's API returned the message from `Subscription Lab <no-reply@saas-sample.invalid>` with the HTML and text bodies. This was the first Aspire run of the API with its dependencies; the Mailpit web UI was checked through its API, not visually in a browser.
- MailDev: swapping needs only the image and ports in `AppHost.cs`; not exercised (no MailDev run). No application workflow depends on either product's UI/API.
- Limitations: no hosted adapter (DEP-04); `Provider=Https` starts but sending fails with a 503-class error until then, so a deploy before DEP-04 has broken email (the readiness check for the operator, planned for the hosted demo, should surface it); no outbox, templates, resend limits or real messages (P06+); no TLS/authentication on the SMTP adapter by design; the MIME builder is covered only through the Mailpit test (internal to Infrastructure); image tag is pinned in both the AppHost and the test fixture and must be kept in sync.



### FND-06 — Create the automated verification foundation

**Status:** Todo · **Priority:** P0 · **Phase:** P00 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-01](#fnd-01--create-the-english-repository-and-modular-skeleton), [FND-02](#fnd-02--establish-postgresql-migrations-and-account-foundations), [FND-03](#fnd-03--define-api-and-frontend-contracts)

**Outcome:** Set up backend, frontend, PostgreSQL integration, and browser test entry points.

**Acceptance criteria:**

- [ ] CI builds and runs relevant tests without live provider credentials; real PostgreSQL is available for relational tests.
- [ ] A failing check fails CI; fixture isolation, secret exclusion, formatting, and dependency review are documented.

**Verification:** Run the same verification commands locally and in CI.

**Evidence:** Pending.
