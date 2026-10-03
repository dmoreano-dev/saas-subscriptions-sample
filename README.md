# Subscription Lab

A learning SaaS lab: a C# API and a React frontend with personal Free/Pro/Max subscriptions plus an
additional Enterprise organization experience. It is built step by step to study identity,
authorization, entitlements, billing, data isolation and their failure modes.

Status: **P00 Foundation in progress.** Only the skeleton exists; the product arrives item by item.

- Architecture, decisions and policies: [`docs/plan.md`](docs/plan.md)
- Implementation tracker: [`docs/backlog.md`](docs/backlog.md) (detail per phase in [`docs/backlog/`](docs/backlog/))
- Architecture decision records: [`docs/decisions/`](docs/decisions/)
- Working agreement for AI-assisted sessions: [`CLAUDE.md`](CLAUDE.md)

All code, identifiers, comments, tests and technical documentation are in English.

## Pinned toolchain

| Tool | Version | Where it is pinned |
|---|---|---|
| .NET SDK | 10.0.401 (`rollForward: latestPatch`) | `global.json` |
| Target framework | net10.0 | `Directory.Build.props` |
| .NET Aspire (AppHost SDK + hosting packages) | 13.6.0 | AppHost `.csproj`, `Directory.Packages.props` |
| OpenAPI / contract | Microsoft.AspNetCore.OpenApi 10.0.12, Scalar.AspNetCore 2.17.13, openapi-typescript 7.13.0 | `Directory.Packages.props`, `src/frontend/package.json` |
| Test stack | xUnit 2.9.3, Microsoft.NET.Test.Sdk 18.10.1, Mvc.Testing 10.0.12 | `Directory.Packages.props` |
| Node.js | 24.16.0 | `src/frontend/.nvmrc`, `engines` in `package.json` |
| npm | 11.13.0 | `packageManager` in `package.json` |
| Frontend dependencies | React, Vite, TypeScript as locked | `src/frontend/package-lock.json` (use `npm ci`) |
| Docker | Docker Engine/Desktop, any current version (verified with 29.7.2) | required for local PostgreSQL and the relational tests (FND-02) |
| PostgreSQL | major **17** (assumed Supabase major; confirm when the Supabase project exists) | `WithImageTag("17")` in the AppHost, `postgres:17` in `tests/integration` |
| EF Core / Npgsql | EF Core 10.0.12 (`dotnet-ef` tool), Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, EFCore.NamingConventions 10.0.1 | `Directory.Packages.props`, `.config/dotnet-tools.json` |
| Testcontainers | Testcontainers.PostgreSql / Testcontainers 4.15.0 | `Directory.Packages.props` |
| Email | MailKit 4.18.1 (SMTP adapter); Mailpit `axllent/mailpit:v1.31.3` (local capture) | `Directory.Packages.props`, `AppHost.cs`, `tests/integration/Email/MailpitFixture.cs` |

NuGet package versions are managed centrally in `Directory.Packages.props`; do not put versions in `.csproj` files.

## Repository layout

```
Saas.Subscription.Sample.slnx
src/backend/Saas.Subscription.Sample.Domain/          no dependencies
src/backend/Saas.Subscription.Sample.Application/     -> Domain; use cases and seams (interfaces)
src/backend/Saas.Subscription.Sample.Infrastructure/  -> Application; every provider integration (PostgreSQL/EF Core in Persistence/)
src/backend/Saas.Subscription.Sample.Migrator/        -> Infrastructure; applies EF migrations (never run by the API)
src/backend/Saas.Subscription.Sample.Api/             -> Application + Infrastructure; HTTP host
src/frontend/                                            React + TypeScript + Vite
src/aspire/Saas.Subscription.Sample.AppHost/          local orchestration only
tests/unit/  tests/integration/  tests/browser/
docs/
```

### Architecture boundaries

- References go one way: **Domain → nothing; Application → Domain; Infrastructure → Application;
  Api → Application + Infrastructure; Migrator → Infrastructure.** This is enforced by review for now; an automated check is
  deferred to FIN-04.
- Seams such as `IBillingGateway` and `IEmailSender` are declared in `Application`; their
  PostgreSQL, SMTP, Stripe, cache and identity implementations live in `Infrastructure`, one folder per
  integration.
- Modules (Identity, Accounts, Authorization, Subscriptions, Billing, Usage, Projects/Reports, Audit) are
  folders inside each layer, not separate projects. This is a modular monolith: one API, one database.
- Aspire is **local orchestration only**. The hosted API image builds `Api` alone. See
  [ADR 0002](docs/decisions/0002-solution-layout-and-aspire-orchestration.md).

## Reproducing a clean build

From a fresh clone, with the pinned .NET SDK and Node installed:

```bash
# Backend: restore, build, run all tests
dotnet build Saas.Subscription.Sample.slnx
dotnet test  Saas.Subscription.Sample.slnx

# Frontend: reproducible install from the lockfile, production build, lint
cd src/frontend
npm ci
npm run build
npm run lint
```

Warnings are treated as errors for the whole solution. The relational integration tests start a real
PostgreSQL 17 container through Testcontainers, so **Docker must be running** (no other credentials are
needed); they fail with an explicit message, and are never skipped, when Docker is unavailable.

## API contract

The OpenAPI document `docs/contracts/openapi.json` and the frontend types `src/frontend/src/api/schema.d.ts`
are generated and committed. After changing an endpoint or DTO run `cd src/frontend && npm run contract:generate`;
`npm run contract:check` and the `Contracts` integration tests fail when they drift. Conventions, endpoint list
and the error-code catalog: [`docs/contracts/README.md`](docs/contracts/README.md). In Development the API
serves `/openapi/v1.json` and a Scalar UI at `/scalar` (404 in every other environment).

## Running locally

One documented path: the Aspire AppHost starts the API and the Vite dev server together.

```bash
dotnet dev-certs https --trust          # once per machine; the AppHost uses the https profile
cd src/frontend && npm ci && cd ../..   # once, so Aspire can launch Vite
dotnet run --project src/aspire/Saas.Subscription.Sample.AppHost --launch-profile https
```

- Aspire dashboard: `https://localhost:17085` (the login URL with a one-time token is printed in the console).
- API: `http://localhost:5282` or `https://localhost:7101`; liveness check at `/health/live`.
- Frontend: the dashboard lists the dynamically assigned Vite URL. Requests to `/api` are proxied to the API.
- PostgreSQL 17 runs in a container with the persistent volume `saas-sample-pgdata`; the Migrator runs to
  completion before the API starts. The database password is generated once into the AppHost's user secrets. Docker must be running. See [`docs/runbooks/database-migrations.md`](docs/runbooks/database-migrations.md)
  (reset, inspect, add a migration, run the Migrator by hand).
- Stop with Ctrl+C; the data survives in the volume. The AppHost also runs a Mailpit container that captures local email (nothing is delivered); see [`docs/runbooks/local-email-capture.md`](docs/runbooks/local-email-capture.md).

You can also run pieces alone: `dotnet run --project src/backend/Saas.Subscription.Sample.Api`
and `npm run dev` in `src/frontend` (the proxy falls back to `http://localhost:5282`). The API alone needs a
database connection string even though it does not connect yet:
`dotnet user-secrets set "ConnectionStrings:appdb" "<connection string>" --project src/backend/Saas.Subscription.Sample.Api`.

## Configuration

Seven typed option groups (`Database`, `Authentication`, `Billing`, `Email`, `Cache`, `Frontend`,
`BackgroundWork`) live in `Application/Configuration` and are validated when the API starts; an incomplete or
unsafe configuration stops it with a message that names the key (never the secret value). Only
`ASPNETCORE_ENVIRONMENT=Development` is local; every other environment is **hosted** and gets the strict rules
(simulator controls off, signing key present, TLS-verified database, HTTPS email provider and frontend URL).
Rules and rationale: [ADR 0005](docs/decisions/0005-typed-configuration-and-hosted-startup-validation.md).

- `appsettings.json` holds safe non-secret defaults; `appsettings.Development.json` the local ones.
- [`appsettings.example.json`](src/backend/Saas.Subscription.Sample.Api/appsettings.example.json) lists every key
  with placeholders (a test keeps it in step with the options). Hosted values go in environment variables
  (`Authentication__SigningKey`); local secrets in `dotnet user-secrets` (the API has a `UserSecretsId`).
- Business time comes from the injected `TimeProvider` (system clock by default, `FakeTimeProvider` in tests).
- Seams are declared by the item that needs them: `IEmailSender` (FND-05, done), `IBillingGateway` (BIL-01), cache
  (P05), password hashing (JWT-01).

## Secrets

Never commit a real `.env`, JWT signing private key, provider API key or connection string. The single
checked-in example configuration with placeholders is `appsettings.example.json` (see Configuration).
Keep raw tokens, hashes and secrets out of logs and DTOs.
