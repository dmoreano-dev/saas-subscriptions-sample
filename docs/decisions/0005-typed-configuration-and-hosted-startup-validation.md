# 0005 — Typed configuration and hosted startup validation

- **Status:** Accepted
- **Date:** 2026-10-02
- **Backlog item(s):** FND-04
- **Deciders:** Project owner

## Context

The API needs a configuration surface (database, authentication, billing, email, cache, frontend, background
work) that later phases fill in. A wrong value must stop the API from starting instead of surfacing at the
first request, and the unsafe-in-hosted cases (`plan.md` §7 and §10: simulator controls, missing signing key,
real-money keys, SMTP capture as delivery) must be impossible to deploy by accident.

## Decision

- **Options live in `Application/Configuration`** (plain classes with DataAnnotations, one
  `IValidateOptions<T>` per group for conditional and environment rules); **registration lives in
  `Api/Configuration`** (`AddAppConfiguration`: `BindConfiguration` + `ValidateDataAnnotations` + `ValidateOnStart`).
  Validators have no host dependency, so they are unit tested directly.
- **"Hosted" is derived from `ASPNETCORE_ENVIRONMENT`**: only `Development` is local; every other name is
  hosted (fail closed). It is exposed to validators as `AppEnvironment(Name, IsHosted)`. No separate switch.
- **Validation checks configuration, not reachability.** Nothing connects to PostgreSQL, Stripe or SMTP at
  startup; readiness checks come later.
- **Rules that fail startup**
  - Hosted only: `Billing:Simulator:ControlsEnabled=true`; missing `Authentication:SigningKey`/`SigningKeyId`;
    `Database:ConnectionString` without `SSL Mode=VerifyFull`; `Email:Provider=Smtp`; non-https or local
    `Frontend:BaseUrl`/`AllowedOrigins`.
  - Every environment: `Cache:Provider=Redis` without a connection string; `Billing:Provider=Stripe` without
    sandbox keys; any `*_live_` Stripe key; `BackgroundWork:LeaseDuration <= PollInterval`; wildcard or
    path-bearing origins; a missing database connection string.
- **Billing provider `Simulator` stays allowed when hosted** as long as its controls are off, so a hosted
  demo is not blocked while Stripe credentials do not exist.
- **Database connection string source:** `Database:ConnectionString`, falling back to the
  `ConnectionStrings:appdb` value Aspire injects (the AppHost now references the database from the API).
  The Migrator keeps reading `ConnectionStrings__appdb` directly.
- **`TimeProvider.System` is registered once** (`TryAddSingleton`); tests replace it with `FakeTimeProvider`.
  Domain methods keep receiving `now` as a parameter.
- **Seams are declared by their own items**, not here: `IEmailSender` (FND-05), `IBillingGateway` (BIL-01),
  cache and password hashing (P05, JWT-01). This item adds options and validation only.
- Failure messages name the configuration key and never echo a secret value.

## Consequences

- Positive: an unsafe or incomplete hosted configuration cannot start; each rule has a test that breaks one
  setting of an otherwise valid hosted profile; the checked-in `appsettings.example.json` is compared with the
  options by a test, so placeholders cannot drift.
- Negative / trade-offs: any environment not named `Development` (including `Staging` or a typo) gets the
  strict rules; running the API alone locally needs a connection string (user-secrets
  `ConnectionStrings:appdb`) even though it does not connect yet; placeholder values such as `<signing-key>`
  satisfy the presence checks (only prefixes and TLS mode are format-checked).
- Follow-ups: P01 chooses the signing algorithm and may tighten `Authentication` rules; BIL-01 gates the
  simulator endpoints on `Billing:Simulator:ControlsEnabled`; a `/health/ready` check arrives with the
  database registration; `CorporateIdentity` options arrive with P10.
