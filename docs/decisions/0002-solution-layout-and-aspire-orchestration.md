# 0002 — Solution layout and Aspire as the local orchestrator

- **Status:** Accepted
- **Date:** 2026-10-01
- **Backlog item(s):** FND-01
- **Deciders:** Project owner

## Context

`plan.md` §3 sketched a provisional layout (`src/api/SubscriptionLab.*`, `src/web/`) and left the local
startup path open (`dotnet run` or Docker). Later items need a local PostgreSQL container with a
persistent volume (FND-02), Mailpit (FND-05), a Keycloak test IdP (P10) and Redis (P11), alongside the
API and the Vite dev server. The backend and its infrastructure are C#, so describing that stack in C#
is attractive.

## Decision

We will use this layout:

- Solution `Saas.Subscription.Sample.slnx` at the repository root; every project and namespace is
  prefixed `Saas.Subscription.Sample.`. "Subscription Lab" remains only the product/documentation name.
- `src/backend/` holds `Domain`, `Application`, `Infrastructure`, `Api`. References are one-directional:
  Domain → nothing; Application → Domain; Infrastructure → Application; Api → Application + Infrastructure.
  `Infrastructure` contains every provider integration (PostgreSQL/EF Core, email, Stripe, cache,
  corporate identity), one folder per integration. Seams (`IBillingGateway`, `IEmailSender`, ...) are
  declared in `Application`. Modules are folders inside each layer, not separate projects.
- `src/frontend/` holds the React + TypeScript + Vite app.
- `src/aspire/Saas.Subscription.Sample.AppHost` is the **single local orchestrator** (.NET Aspire
  13.6.0). It declares the API and Vite app now; PostgreSQL arrives in FND-02, Mailpit in FND-05, and so
  on. The production API Dockerfile is added in P05 (DEP-02), and its location is decided then.
- Tests stay under `tests/{unit,integration,browser}/`. An automated check of the layer references is deferred to FIN-04; until then the rule is enforced by review.
- Aspire is local-only: the Render image builds `Api` alone, and Aspire never ships to hosted environments.
  The AppHost runs with `AspireUseCliBundle=false` (dashboard/DCP delivered through NuGet), so no Aspire
  CLI install is needed; ASPIRE010 is suppressed for that reason.

## Consequences

- Positive: one command starts the whole local stack; resources, ports, volumes and credentials are
  declared in C#; service discovery feeds the Vite `/api` proxy.
- Negative / trade-offs: Aspire is an extra concept to learn and a dependency on Docker from FND-02
  onward; the Aspire dashboard login URL changes every run; the `http` launch profile is unusable without
  `ASPIRE_ALLOW_UNSECURED_TRANSPORT`, so the documented profile is `https` (requires a trusted dev cert).
- Follow-ups: FND-02 adds PostgreSQL (persistent volume) to the AppHost; integration tests that need real
  PostgreSQL may use Aspire testing or Testcontainers (decide in FND-06). Adds baseline decision D19 to plan.md.
