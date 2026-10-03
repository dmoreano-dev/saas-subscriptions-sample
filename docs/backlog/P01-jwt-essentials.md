# P01 — JWT essentials

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Own users and short-lived access JWTs only. Validate tokens correctly from day one; persisted sessions, refresh tokens, and auth cookies belong to later phases.

### JWT-01 — Implement custom users and password credentials

**Status:** Done · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Create users/password_credentials and a reviewed password-hashing integration without ASP.NET Core Identity.

**Acceptance criteria:**

- [x] Store encoded hashes and algorithm parameters; support future verifier versions without storing recoverable passwords. *(Met by the algorithm name stored per row and the format/version/parameters inside the encoded hash; a separate `verifier_version` column was deliberately left out, see ADR 0007.)*
- [x] Define normalized-email uniqueness, user status, constant-time verification, secure randomness, and bounded credential input.

**Verification:** Hash/verify/rehash tests, duplicate-email concurrency test, and schema inspection.

**Evidence:** 2026-10-03. Commit: `git log --grep JWT-01`. Decisions: [ADR 0007](../decisions/0007-password-hashing-and-credential-storage.md).

- Verification: `dotnet build Saas.Subscription.Sample.slnx` → 0 warnings, 0 errors; `dotnet test` → 104/104 unit and 172/172 integration passed (run against real PostgreSQL 17 through Testcontainers, one database per test); `scripts/verify.sh all` → exit 0 (format, build, tests, NuGet and npm audit).
- Hashing: Argon2id through Geralt 4.4.0 (libsodium 1.0.22) behind `IPasswordHasher`, a single `Argon2idPasswordHasher` (no scheme abstraction until a second algorithm exists); PHC-encoded hash carries algorithm and parameters; salt from the libsodium CSPRNG; constant-time comparison inside libsodium; every hash is stored with its `algorithm` (no separate version column: the PHC string carries `v=19` and the parameters) and an unknown algorithm or a corrupt encoding fails closed; work factor from `PasswordHashing` options (default 64 MiB, t=3, ≈88 ms on the author's Apple arm64 machine; validated against the OWASP floor at startup).
- Schema (migration `AddPasswordCredentials`): `password_credentials` (PK/FK `user_id`, `algorithm`, `encoded_hash` carrying the work-factor parameters and salt, timestamps, `xmin`) with CHECKs for algorithm and non-empty hash; `users.status` (`Active`/`Disabled`, default `Active`, CHECK). Normalized-email uniqueness is the existing `ux_users_normalized_email`.
- Unit tests: policy bounds (including surrogate pairs and oversized input); real Argon2id (PHC format, unique salts, wrong/right/non-ASCII passwords, corrupt and foreign encodings, unknown algorithm, oversized and empty input, a password shorter than the current minimum still verifies, rehash requested when memory or iterations rise); options validator; `StoredPasswordHash.ToString` hides the hash.
- PostgreSQL tests: schema inspection (columns, key/FK/CHECK names, status default, no password/hash column elsewhere), each CHECK/FK/unique rejection with its SQLSTATE, save and reload then verify through the real hasher, the password never appears in a stored row, rehash persists, and 8 concurrent saves of one email in different case/whitespace persist exactly one row while the others fail with 23505 on `ux_users_normalized_email`.
- Mutation checks (reverted): making `Verify` always succeed, never asking for a rehash, rejecting passwords below the minimum in `Verify`, and ignoring the stored algorithm each failed 2, 2, 1 and 1 tests.
- Finding (not fixed here, by decision): FND-02's `MigrateAsync_ConcurrentRuns_AppliesEachMigrationOnce` failed once a second migration existed (`42701 column "status" already exists`; 6 of 6 runs), so EF Core's lock does not serialize concurrent migrators. Local (Aspire) and the planned deployment step run one Migrator at a time, so no lock was added; the test and the claim in ADR 0003 and the runbook were removed, and the P05 migration item must guarantee a single run.
- Spike findings that shaped the code: Geralt takes memory in bytes, writes the hash into a 128-char zero-padded buffer, and throws `FormatException` (not a failed result) on a malformed hash; both are handled and tested.
- Limitations: nothing calls the hasher yet (registration/login, constant-time handling of unknown accounts, throttling and rehash-on-login are JWT-02); only one algorithm exists, so multi-algorithm support is not built or tested, only enabled by the stored `algorithm` and the rehash result (the scheme abstraction is for P11); the libsodium native binary was exercised on macOS arm64 only, Linux x64 is covered by the NuGet runtimes but not yet run (CI runs the tests on `ubuntu-latest`, the Render image is DEP-01); the hash cost was calibrated on one developer machine; minimum password length 5 is a lab value (NIST: 8); concurrent Migrator runs are unsupported until the deployment step guarantees one; the `Down` migration was not exercised; Unicode normalization of passwords is not applied.

### JWT-02 — Implement personal registration and credential login

**Status:** Done · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-01](#jwt-01--implement-custom-users-and-password-credentials), [FND-03](P00-foundation.md#fnd-03--define-api-and-frontend-contracts)

**Outcome:** Implement registration and authentication services using own tables.

**Acceptance criteria:**

- [x] Registration atomically creates user, personal account, and owner membership; retries/conflicts cannot leave partial accounts. *(Also creates the password credential in the same transaction.)*
- [x] Login returns an authenticated application identity only after credential verification; errors do not expose hashes or distinguish unknown accounts unnecessarily. *(Service level: `ILoginService` returns an `AuthenticatedIdentity`; the HTTP endpoint is wired with the token in JWT-03.)*

**Verification:** Registration rollback, duplicate requests, and valid/invalid credential integration tests.

**Evidence:** 2026-10-03. Commit: `git log --grep JWT-02`. Decisions: [ADR 0008](../decisions/0008-registration-enumeration-and-login-throttling.md).

- Verification: `scripts/verify.sh all` → exit 0 (format, build with warnings as errors, 130 unit and 203 integration tests passed against real PostgreSQL 17 through Testcontainers, contract check unchanged, gitleaks, NuGet and npm audit).
- Registration: `RegistrationService` saves user, `password_credentials`, personal account and owner membership in one `SaveChanges` (one transaction) after hashing; no existence check, the unique index on the normalized email decides. `POST /api/auth/register` validates with `EmailPolicy` (≤254, plain shape) and `PasswordPolicy` and answers 201 with no token.
- Duplicates: a unique violation on `ux_users_normalized_email` rolls back and returns a decoy (new ids, same shape), so the endpoint answers the same 201 for a new and an existing email (decision A in ADR 0008); the password is hashed exactly once in both paths.
- Login: `LoginService` returns `Succeeded(AuthenticatedIdentity)`, `InvalidCredentials` or `Throttled`; unknown email, wrong password, no credential row and a `Disabled` user are the same result and each performs exactly one `Verify` (dummy hash when there is no credential); `SucceededRehashNeeded` replaces only the verified hash and never fails the login.
- Throttle: `InMemoryLoginThrottle`, per normalized email (known or not), 5 attempts per fixed 15-minute window taken before the check, released by a success, passive expiry (no worker), capped at 10 000 keys.
- Persistence: `AddPersistence()` registers `AppDbContext` from the `Database` options; the API connects to PostgreSQL for the first time here.
- PostgreSQL tests (`tests/integration/Identity`): complete rows and links, the hash is stored and the password is not, rollback of all four tables when a trigger makes the membership insert fail (and the failure is not mistaken for a duplicate), duplicate registrations (case/whitespace variants) leave the original untouched, 8 concurrent registrations of one email create exactly one complete account and one winner who can log in, valid and invalid logins, equal generic result for the four failure kinds, exactly one verification per attempt, rehash from a weaker work factor (and no rewrite when current), throttling (including unknown emails, window expiry and reset on success), and the HTTP endpoint (201 shape without token, same status/shape for duplicates, field validation errors). Unit tests: email policy and the throttle with `FakeTimeProvider`.
- Mutation checks (reverted): skipping the dummy verification failed 2 tests, accepting a `Disabled` user 1, not releasing the throttle on success 1, treating every `DbUpdateException` as a duplicate 1 (rollback test), and a separate first save of the user 7.
- Limitations: `POST /api/auth/login` still answers 501 (no token until JWT-03); the throttle is per process and per email only (no per-IP limit, no request-size limit, no lock-out policy, no log redaction: JWT-05) and can be used to deny one email for 15 minutes; response-time equality is argued from the code path (one hash per attempt), not measured; the decoy 201 leaves a person who registers an existing email without feedback until P06; the personal account name is the constant `Personal` (users have no display name); the rehash concurrency guard (`WHERE encoded_hash = <verified>`) has no dedicated concurrent test.

### JWT-03 — Issue and validate short-lived bearer JWTs

**Status:** Done · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Opus / high

**Dependencies:** [JWT-02](#jwt-02--implement-personal-registration-and-credential-login), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Connect successful login to maintained JWT issuance and validation libraries.

**Acceptance criteria:**

- [x] Tokens have sub, jti, issuer, audience, issued/expiry times, a 10-minute baseline lifetime, and a persistent configured signing key.
- [x] Reject unsigned/tampered tokens, invalid algorithms/issuer/audience/time claims, and missing subject; no session or refresh tables are introduced.

**Verification:** Negative token matrix plus restart test with stable signing material.

**Evidence:** 2026-10-03. Commit: `git log --grep JWT-03`. Decisions: [ADR 0009](../decisions/0009-access-token-signing-and-validation.md).

- Verification: `scripts/verify.sh all` → exit 0 (format, build with warnings as errors, 168 unit and 265 integration tests passed against real PostgreSQL 17 through Testcontainers, contract check unchanged, gitleaks, NuGet and npm audit).
- Signing key: ES256, `Authentication:SigningKey` is an unencrypted P-256 private key in PEM (`\n` accepted as line breaks), `SigningKeyId` is the `kid`. Missing or unusable material stops the API at startup in every environment (not only hosted) and is never generated; the failure names the setting, not the value. Validation uses a public-only copy of the key.
- Issuance: `JwtAccessTokenIssuer` (`JsonWebTokenHandler`, IdentityModel 8.19.2) writes exactly `iss`, `aud`, `sub`, `jti`, `iat`, `nbf`, `exp` from the injected `TimeProvider`; `exp = iat + AccessTokenLifetime` (10 minutes).
- Validation: `AddJwtBearer` with allow-list `[ES256]`, signed tokens required, key matched by `kid` only, exact issuer and audience, expiry required, explicit 30-second clock skew evaluated on the injected clock, then `sub` (a user id) and `jti` required. Refusals answer 401 `unauthenticated` with a bare `WWW-Authenticate: Bearer`.
- Endpoints: every `/api` route outside `/api/auth/*` requires the token (OpenAPI security derived from that metadata; the committed contract did not change). `POST /api/auth/login` returns the token (`Cache-Control: no-store`), 401 `invalid_credentials` for any credential failure and 429 with `Retry-After` when throttled. `GET /api/me` returns the profile read by `sub`, 401 if the user does not exist or is disabled. Account and project routes stay 501 behind the token.
- Negative matrix over HTTP through the real pipeline (`tests/integration/Identity/AccessTokenValidationTests`, tokens built by hand, each differing from an accepted control in one way): `alg: none`, signature removed, signature tampered, payload tampered, signed with another key, unknown `kid`, no `kid`, HS256 with the public key as secret, ES384 from another curve, wrong/missing issuer, wrong/missing audience, expired, no expiry, not yet valid, `nbf` after `exp`, missing subject, subject that is not a user id, missing `jti`, garbage tokens and a valid token under another scheme. Controls: the same forge without changes, and tokens exactly at `exp + skew` / `nbf - skew`, are accepted. An issued token is accepted at +630 s and refused at +631 s (fake clock).
- Restart (`MeEndpointTests`, real PostgreSQL): a token from a login on one host is accepted by a new host with the same key, and refused by a new host with a different key. Login over HTTP against PostgreSQL: token shape and claims, different `jti` per login, identical generic failure for wrong password / unknown email / malformed email / disabled user, 429 after five failures even with the right password, field validation.
- No session or refresh tables: this item adds no migration; `MigrationTests` still pins the table list.
- Mutation checks (reverted), each failed the listed tests: no audience validation 2, no issuer validation 2, try all keys 2, 5-minute skew 1, unsigned allowed 2, no subject/`jti` check 3, `jti` not required 1, no `RequireAuthorization` 40 (including the OpenAPI document test), `/me` ignoring user status 1, throttle answered as invalid credentials 1, lifetime ignoring the option 2. Removing the algorithm allow-list failed **no** HTTP test (the EC key itself refuses HS256/ES384), so the allow-list is pinned only by a unit test of the parameters; `ValidateLifetime = false` is not observable because the lifetime delegate takes precedence.
- Limitations: one active key, no rotation (changing it signs everyone out); no revocation: a token stays valid until `exp` after logout or after the user is disabled, except on `GET /api/me` (P06); `jti` is not stored; the first local run needs the key created by hand (README "Signing key"); `npm run contract:generate` fails on a machine without full API configuration (it starts the API as a hosted environment; already the case before this item for the database, frontend and email settings) — not needed here because the contract is unchanged, but it must be fixed before the next contract change; request-size limits and log redaction are JWT-05; no hosted/Render evidence yet (the "not regenerated on restart" claim is proven in-process, the hosted check belongs to P05).

### JWT-04 — Build the React JWT-only login experience

**Status:** Done · **Priority:** P1 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-03](#jwt-03--issue-and-validate-short-lived-bearer-jwts), [FND-03](P00-foundation.md#fnd-03--define-api-and-frontend-contracts)

**Outcome:** Create registration/login, protected navigation, and local logout using an in-memory access token.

**Acceptance criteria:**

- [x] Authenticated API calls use the bearer header; reload or expiry returns the user to login with a clear state.
- [x] No access-token persistence in localStorage and no refresh-cookie flow; logout behavior explicitly documents copied-token validity until expiry.

**Verification:** Browser registration/login/logout/reload/expiry scenarios.

**Evidence:** 2026-10-03. Commit: `git log --grep JWT-04`.

- Verification: `scripts/verify.sh frontend` (build, lint, contract check) and `scripts/verify.sh browser` → 11 Playwright tests passed against the real API and PostgreSQL 17 (Docker). Browser tests stay **local only**; whether they join CI is decided after the frontend work (owner decision).
- Token: `AuthProvider` keeps `{ accessToken, expiresAt, user }` in React state only. No localStorage, sessionStorage, cookies or IndexedDB; `fetch` uses `credentials: 'omit'`. A reload loses the session and lands on `/login` (the login page states the token is memory-only).
- API client: `src/api/client.ts` returns `ok | problem` using the generated types through `contract.ts`; it adds `Authorization: Bearer` and branches on `code`. 401 `unauthenticated` with a session ends it with the "expired" notice; 401 `invalid_credentials` is a generic form error; 429 reads `Retry-After` and disables the button for that countdown; 400/`registration_failed` give a generic validation message (ProblemDetails has no per-field errors); 503/network give a retryable message.
- Expiry: a client timer closes the session at `expiresAt` and the server 401 stays the authority (clock drift, 30 s skew).
- Screens (`react-router-dom`): `/register` (201 has no token, goes to `/login` with a notice), `/login`, protected `/me` (calls `GET /api/me`), local logout. `/me` and the docs state that a copied token stays valid until `exp` (~10 min) and there is no revocation until P06.
- Browser scenarios (`tests/browser/tests/auth.spec.ts`): register, login + profile, wrong password, `/me` without session, no token in storage/cookies, logout, copied token still `200` after logout, reload, expiry (`page.clock`), server-rejected token. `support/global-setup.ts` starts `postgres:17`, runs the Migrator and the API with a throwaway ES256 key generated in memory (nothing committed); `vite preview` proxies `/api` to it.
- Limitations: not in CI; needs Docker and the .NET SDK. Inherited issue: `npm run contract:generate` still fails without full API configuration because OpenAPI generation boots the host and startup option validation rejects it (reproduced; the contract did not change in this item, so it was not fixed). Follow-up: supply throwaway configuration to that invocation.


### JWT-05 — Add baseline authentication abuse controls

**Status:** Done · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-02](#jwt-02--implement-personal-registration-and-credential-login), [JWT-03](#jwt-03--issue-and-validate-short-lived-bearer-jwts)

**Outcome:** Protect authentication endpoints before the first learning demo.

**Acceptance criteria:**

- [x] Apply bounded (scope reduced by the owner: login throttling with generic errors and no permanent lock-out already existed from JWT-02/ADR 0008; request-size limits are deferred, see Limitations) request sizes and login throttling with generic errors; account lockout policy avoids easy permanent denial of service.
- [x] Secrets and raw credentials/tokens are excluded from logs; signing-key and HTTPS configuration are checked.

**Verification:** Throttling, oversized payload, logging-redaction, and configuration tests.

**Evidence:** 2026-10-03. Commit: `git log --grep JWT-05`.

- Verification: `scripts/verify.sh all` → exit 0 (168 unit and 268 integration tests against real PostgreSQL 17, contract unchanged, gitleaks, dependency audit).
- Scope (owner decision, learning focus): this item only adds what was missing for the second criterion. Already in place and re-confirmed: login throttle per email (5 attempts / 15 min, fixed bounded window, no persistent lock-out, ADR 0008); signing key required and checked at startup in every environment, failures name the setting and never the value (ADR 0009); hosted startup rejects a database connection string without `SSL Mode=VerifyFull` and a non-HTTPS or loopback `Frontend:BaseUrl` (`HostedStartupTests`).
- Log redaction: `LogRedactionTests` runs register, login (right, wrong, unknown email, throttled), `/api/me` with a valid, tampered and garbage token and an invalid registration through the real host over PostgreSQL, capturing every log entry at Trace level (message, structured state, exception, scopes). Sentinels: the password, the wrong password, the access token and its signature, the signing key body, the stored Argon2id hash and the database password. None may appear in the logs, and none of the failure responses echoes the wrong password, key, hash or database password. Mutation check (reverted): logging `{Password}` in `LoginService` failed the test; the build analyzers (CA1848/CA1873) also reject ad-hoc `LogInformation` calls.
- HSTS: `UseHsts` outside Development (`HostedStartupTests`: header present when hosted over HTTPS, absent in Development). No HTTPS redirection: behind Render TLS ends at the proxy, so it belongs to P05 with trusted forwarded headers.
- Limitations (deferred by the owner, not done): no request-size limits (Kestrel default ~30 MB, so 413/`payload_too_large` and the OpenAPI note do not exist); no per-IP or global rate limiting (the per-email throttle can still deny one email for 15 minutes and does not see one source spraying many emails); throttle state is per process and lost on restart; HTTP request logging is not enabled, so `Authorization` header redaction is not exercised; redaction is proven for the authentication flows only, not as a general log filter.

### JWT-06 — Verify and explain the JWT checkpoint

**Status:** Todo · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-04](#jwt-04--build-the-react-jwt-only-login-experience), [JWT-05](#jwt-05--add-baseline-authentication-abuse-controls), [FND-06](P00-foundation.md#fnd-06--create-the-automated-verification-foundation)

**Outcome:** Complete the first demonstrable lesson without later identity machinery.

**Acceptance criteria:**

- [ ] Tests prove unauthorized requests fail and valid tokens reach only protected operations allowed at this stage.
- [ ] Document the distinction between token validation, user persistence, client logout, and future server revocation; record the P01 demonstration.

**Verification:** Run the JWT regression suite and capture a reproducible demo checklist.

**Evidence:** Pending.
