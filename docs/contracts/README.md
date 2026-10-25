# API contract

`openapi.json` is the machine-readable contract of the API (OpenAPI 3.1, generated from the code; see
[ADR 0004](../decisions/0004-api-contract-openapi-and-problem-details.md)). Do not edit it by hand.

## Commands

| Goal | Command |
|---|---|
| Regenerate the document **and** the TypeScript types | `cd src/frontend && npm run contract:generate` |
| Regenerate only the document | `dotnet build src/backend/Saas.Subscription.Sample.Api -p:GenerateOpenApiContract=true` |
| Regenerate only the types from the document | `cd src/frontend && npm run contract:types` |
| Check that the types match the document | `cd src/frontend && npm run contract:check` |
| Check that the document matches the code | `dotnet test tests/integration --filter "FullyQualifiedName~Contracts"` |

After changing an endpoint or DTO, run `npm run contract:generate` and commit `openapi.json` and
`src/frontend/src/api/schema.d.ts` together. Import types from `src/frontend/src/api/contract.ts`.

During local development the document is served at `/openapi/v1.json` and a browsable UI at `/scalar`
(Development only; both are 404 elsewhere).

## Endpoints (FND-03)

An operation is contract-only until its item and answers `501 not_implemented`. Still contract-only:
`GetAccountCapabilities` (ENT-03) and `CreateProject` (ENT-04); both are authorized first, so only a member reaches the 501.

| Operation | Route | Notes |
|---|---|---|
| `Register` | `POST /api/auth/register` | Creates user + personal account; no token. 400 only (no duplicate-email 409): an existing email gets the same 201 shape with ids that refer to nothing, so clients must not use them (ADR 0008). Implemented in JWT-02. |
| `Login` | `POST /api/auth/login` | Returns a bearer access token; failures are the generic `invalid_credentials`. |
| `GetMe` | `GET /api/me` | Caller's profile. |
| `ListAccounts` | `GET /api/accounts` | Only accounts the caller is an active member of; closed accounts are not listed. |
| `GetAccountCapabilities` | `GET /api/accounts/{accountId}/capabilities` | Effective capabilities; unlimited is explicit (`isUnlimited`). |
| `ListProjects` / `CreateProject` / `GetProject` / `DeleteProject` | `/api/accounts/{accountId}/projects[/{projectId}]` | `accountId` is a selector, not proof of membership. Reading needs `projects.read`, deleting `projects.write`. |

## Conventions

- JSON in camelCase; ids are UUID strings; timestamps are UTC ISO-8601; enums are camelCase strings.
- Every property is required in the schema; optional values are explicitly nullable.
- Responses never contain password hashes, tokens other than the issued access token, provider secrets,
  internal versions or data of accounts the caller cannot access (guarded by `ContractGuardTests`).
- A foreign or unknown account/resource is `404`, never `403` (no existence leak). `403` is only for a member:
  `forbidden` when their role lacks the permission, `account_suspended` when the account is suspended.

## Errors

Every error is `application/problem+json` (RFC 9457) with `status`, `code`, `correlationId` and `traceId`.
Clients branch on `code`, never on `title` or `detail`. The `correlationId` is also returned in the
`X-Correlation-ID` header; send your own (≤ 64 chars of `A-Z a-z 0-9 . _ -`) to link a browser request to the
server logs, otherwise one is generated. Quote it when reporting a problem.

| Status | Codes | Meaning |
|---|---|---|
| 400 | `bad_request`, `validation_failed` (with `errors`), `registration_failed` | Malformed or invalid input |
| 401 | `unauthenticated`, `invalid_credentials` | Missing/invalid credentials |
| 403 | `forbidden`, `capability_not_granted`, `account_suspended` | Caller is a member; permission or capability denied, or the account is suspended |
| 404 | `not_found` | Missing or hidden resource |
| 409 | `conflict`, `quota_exceeded` | Business/concurrency conflict; contractual quota used up |
| 429 | `rate_limited` (+ `Retry-After`) | Throttling (not implemented yet) |
| 500 | `internal_error` | Unexpected failure; no details are returned |
| 501 | `not_implemented` | Contract-only endpoint (temporary) |
| 503 | `service_unavailable` | Transient dependency failure; retry |

The catalog lives in `Api/Problems/ProblemCodes.cs` and is published as the enum of `code` in the document.
