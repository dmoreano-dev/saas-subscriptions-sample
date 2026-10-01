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
| Test stack | xUnit 2.9.3, Microsoft.NET.Test.Sdk 18.10.1, Mvc.Testing 10.0.12 | `Directory.Packages.props` |
| Node.js | 24.16.0 | `src/frontend/.nvmrc`, `engines` in `package.json` |
| npm | 11.13.0 | `packageManager` in `package.json` |
| Frontend dependencies | React, Vite, TypeScript as locked | `src/frontend/package-lock.json` (use `npm ci`) |
| Docker | Docker Engine/Desktop, any current version (verified with 29.7.2) | needed from FND-02 (PostgreSQL container) |
| PostgreSQL | target major **17**, to be confirmed against the Supabase project | decided and pinned in FND-02 |

NuGet package versions are managed centrally in `Directory.Packages.props`; do not put versions in `.csproj` files.

## Repository layout

```
Saas.Subscription.Sample.slnx
src/backend/Saas.Subscription.Sample.Domain/          no dependencies
src/backend/Saas.Subscription.Sample.Application/     -> Domain; use cases and seams (interfaces)
src/backend/Saas.Subscription.Sample.Infrastructure/  -> Application; every provider integration
src/backend/Saas.Subscription.Sample.Api/             -> Application + Infrastructure; HTTP host
src/frontend/                                            React + TypeScript + Vite
src/aspire/Saas.Subscription.Sample.AppHost/          local orchestration only
tests/unit/  tests/integration/  tests/browser/
docs/
```

### Architecture boundaries

- References go one way: **Domain → nothing; Application → Domain; Infrastructure → Application;
  Api → Application + Infrastructure.** This is enforced by review for now; an automated check is
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

Warnings are treated as errors for the whole solution.

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
- Stop with Ctrl+C. PostgreSQL and Mailpit containers are added to the AppHost by FND-02 and FND-05.

You can also run pieces alone: `dotnet run --project src/backend/Saas.Subscription.Sample.Api`
and `npm run dev` in `src/frontend` (the proxy falls back to `http://localhost:5282`).

## Secrets

Never commit a real `.env`, JWT signing private key, provider API key or connection string. A single
checked-in example configuration with placeholders is introduced with typed configuration (FND-04).
Keep raw tokens, hashes and secrets out of logs and DTOs.
