# 0004 — API contract: OpenAPI as source, generated TypeScript types, ProblemDetails

- **Status:** Accepted
- **Date:** 2026-10-02
- **Backlog item(s):** FND-03
- **Deciders:** Project owner

## Context

FND-03 specifies the initial endpoint families, DTO conventions and error handling before any of the
logic exists (`plan.md` §9). The frontend needs types that cannot silently drift from the API, errors must
be uniform and must not leak internals, and the API documentation describes the whole attack surface so it
must not be public.

## Decision

- **Source of truth:** the endpoints in code (minimal APIs under `Api/Endpoints`, DTO records under
  `Api/Contracts`) generate the OpenAPI 3.1 document with `Microsoft.AspNetCore.OpenApi` 10.0.12.
- **Committed document:** `docs/contracts/openapi.json`, regenerated only on request with
  `dotnet build src/backend/Saas.Subscription.Sample.Api -p:GenerateOpenApiContract=true`
  (`Microsoft.Extensions.ApiDescription.Server`). A plain build never rewrites it, so a contract change
  that was not regenerated fails `ContractDocument`-based tests instead of being absorbed silently.
- **TypeScript:** `openapi-typescript` 7.13.0 (types only, no runtime client) writes the committed
  `src/frontend/src/api/schema.d.ts`; hand-written code imports named aliases from `src/api/contract.ts`.
  `npm run contract:generate` regenerates both; `npm run contract:check` fails if the types differ from the
  document. npm `overrides` scopes `typescript` for this package because 7.13 declares a `typescript@^5` peer
  while the project uses TypeScript 6; revisit when the package updates its peer range.
- **Drift chain:** code → document (integration test compares the served document with the committed one)
  → types (`contract:check`). CI wiring is FND-06.
- **Exposure:** `/openapi/v1.json` and the Scalar UI (`/scalar`) exist only in `Development`; every other
  environment answers 404. A protected hosted option, if ever needed, belongs to typed configuration (FND-04).
- **DTO conventions:** camelCase JSON; enums as camelCase strings with contract-owned enums (not the domain
  enums); numbers strictly numeric; UUID ids; UTC `DateTimeOffset`; positional records so every property is
  *required* in the schema and optional values are explicitly nullable; responses carry no hashes, tokens,
  secrets or other accounts' data. `accountId` in a route is only a selector, never proof of membership.
- **Errors:** RFC 9457 ProblemDetails with an extra stable `code` (catalog in `ProblemCodes`), `correlationId`
  and `traceId`. Mapping: 400 invalid input, 401 unauthenticated, 403 forbidden/capability not granted,
  404 hidden or missing resource, 409 business conflict including `quota_exceeded`, 429 throttling, 503
  transient dependency failure (`DependencyUnavailableException`), 500 generic. A contractual quota is a
  409, not a 429 (`plan.md` §9). The exception handler never returns messages, types or stack traces.
- **Correlation id:** accepted from `X-Correlation-ID` only if ≤ 64 chars of `[A-Za-z0-9._-]`, otherwise
  replaced; always echoed in the header and in problem bodies. It is a debugging aid, not a security control.
- **Contract-only endpoints:** until their phase, handlers answer 501 `not_implemented`; the 501 is not part
  of the documented operation responses. Bearer authentication is documented through endpoint metadata
  (`BearerAuthenticationRequirement`) and enforced from P01.
- **Registration and enumeration:** `POST /api/auth/register` documents only 400 (field validation and one
  generic `registration_failed`); there is deliberately no 409 for a duplicate email, so a caller cannot learn
  which emails exist. P01 must return an indistinguishable response (body and timing) and P06 can move to the
  "we emailed you" pattern. Login failures are always the generic `invalid_credentials`.

## Consequences

- Positive: one reviewable contract file; frontend compile errors on incompatible API changes; uniform,
  non-leaking errors; the future auth middleware gets correct 401/403 bodies without changes here.
- Negative / trade-offs: DTOs are duplicated from the domain by design (a mapping layer is needed in P01+);
  the committed generated files add review noise on contract changes; the npm `overrides` is a workaround
  for a stale peer range; the bearer requirement is documentation-only until P01.
- Follow-ups: P01 enforces auth, validation and throttling (429 mapping exists, no rate limiter yet);
  `registration_failed` timing equivalence (P01/P06); `PUT` for projects and the reports/billing families are
  added when their items need them; CI runs the drift checks in FND-06.
