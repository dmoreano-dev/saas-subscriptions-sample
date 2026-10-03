# 0009 — Access tokens: ES256 signing key, claims and validation rules

- **Status:** Accepted
- **Date:** 2026-10-03
- **Backlog item(s):** JWT-03
- **Deciders:** Project owner

## Context

Plan §5A: issue signed access JWTs through a maintained library, prefer asymmetric signing with a persistent
configured key and key id, validate algorithm, signature, issuer, audience, expiry and not-before, require
subject and token id, never regenerate the key on a Render restart, and reject unsigned tokens and tokens meant
for another API. No session, refresh token, cookie or revocation exists at this checkpoint. ADR 0005 only
required the signing key when hosted and left its format open; ADR 0008 left `POST /api/auth/login` at 501.

## Decision

- **Algorithm and key:** ES256 (ECDSA, NIST P-256, SHA-256). `Authentication:SigningKey` is the private key as an
  unencrypted PEM (PKCS#8 `PRIVATE KEY` or SEC1 `EC PRIVATE KEY`); the two characters `\n` are accepted as line
  breaks for hosts with single-line variables. `Authentication:SigningKeyId` is the `kid`. The public key is
  derived from the private one, so there is one secret to configure. Chosen over RS256 (larger keys and tokens,
  no benefit here) and over a symmetric HS256 secret (every validator could also issue).
- **Required everywhere, never generated:** a missing or unusable key (not PEM, public-only, encrypted, RSA,
  an EC key that is not 256 bits) or a missing key id stops the API at startup in **every** environment; the message names the
  setting, never the value. This changes ADR 0005 (hosted only). Locally the key lives in user-secrets (README
  "Signing key"); tests generate a throwaway key per run, so no private key is committed. Chosen over generating
  a development key at startup: tokens would die on every restart, which is exactly the failure the plan forbids
  and would hide it until the first hosted deploy.
- **Libraries:** `Microsoft.IdentityModel.JsonWebTokens` 8.19.2 (`JsonWebTokenHandler`) to issue, and
  `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12 to validate. The IdentityModel version is the one the
  JwtBearer package depends on.
- **Claims:** `iss`, `aud`, `sub` (user id), `jti` (random, unique per token), `iat`, `nbf`, `exp`; header `alg`,
  `kid`, `typ`. Nothing else: no email, account, role or plan (plan §5B resolves those on the server). Times are
  whole seconds from the injected `TimeProvider`; `exp = iat + Authentication:AccessTokenLifetime` (10 minutes).
- **Validation (`AccessTokenValidation.CreateParameters`):** algorithm allow-list `[ES256]`, signed tokens
  required, the configured key matched by `kid` only (`TryAllIssuerSigningKeys = false`, so an unknown or missing
  `kid` is refused), exact issuer and audience, expiry required, lifetime checked with an explicit **30-second
  clock skew** (the library default of 5 minutes would make a 10-minute token live 15). The lifetime check is a
  small delegate that applies the library's own rules to the injected clock, because the library has no public
  clock seam. After the library accepts a token, `sub` must be a user id and `jti` must be present, otherwise the
  token is refused. Validation uses a public-only copy of the key. `MapInboundClaims = false` keeps the JWT claim
  names.
- **Signature-provider cache off:** the keys use a `CryptoProviderFactory` with `CacheSignatureProviders = false`.
  The default factory caches providers process-wide per key and outlives the key object; a second host in the
  same process with the same key (the restart test) was handed a provider bound to a disposed key.
- **401 shape:** any missing or refused token gets `401` with the problem code `unauthenticated` and a bare
  `WWW-Authenticate: Bearer` (`IncludeErrorDetails = false`): the response does not say whether the token was
  expired, forged or for another API.
- **Endpoints:** every route under `/api` except `/api/auth/*` requires authorization; the OpenAPI security
  requirement is derived from that metadata. `POST /api/auth/login` returns `LoginResponse` (`accessToken`,
  `tokenType: Bearer`, `expiresAt`, `user`) with `Cache-Control: no-store`, `401 invalid_credentials` for every
  credential failure and `429` with `Retry-After` when throttled; it validates only that both fields were sent.
  `GET /api/me` reads the profile by `sub` from PostgreSQL and answers `401` when the user does not exist or is
  not `Active`. Account and project routes stay 501 behind the token.

## Consequences

- Positive: tokens survive restarts and work across instances; the validator cannot be downgraded to `none` or
  HMAC; a token for another API, another issuer or another key is refused; the token carries no data that can go
  stale.
- Negative / trade-offs: a first local run needs one manual step (create the key); there is one active key and
  no rotation (changing it signs everyone out; a rotation needs a key set, not built here); creating a signature
  provider per operation costs some CPU; an issued token stays valid until `exp` even after logout or after the
  user is disabled, except on `GET /api/me`, the only route that looks the user up (no revocation until P06); the
  `jti` is not stored or checked.
- Follow-ups: request-size limits, log redaction and the key/HTTPS configuration checks (JWT-05); the React
  login (JWT-04); session id and revocation (P06); key rotation if a hosted key ever has to change.
