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

**Status:** Todo · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-01](#jwt-01--implement-custom-users-and-password-credentials), [FND-03](P00-foundation.md#fnd-03--define-api-and-frontend-contracts)

**Outcome:** Implement registration and authentication services using own tables.

**Acceptance criteria:**

- [ ] Registration atomically creates user, personal account, and owner membership; retries/conflicts cannot leave partial accounts.
- [ ] Login returns an authenticated application identity only after credential verification; errors do not expose hashes or distinguish unknown accounts unnecessarily.

**Verification:** Registration rollback, duplicate requests, and valid/invalid credential integration tests.

**Evidence:** Pending.

### JWT-03 — Issue and validate short-lived bearer JWTs

**Status:** Todo · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Opus / high

**Dependencies:** [JWT-02](#jwt-02--implement-personal-registration-and-credential-login), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Connect successful login to maintained JWT issuance and validation libraries.

**Acceptance criteria:**

- [ ] Tokens have sub, jti, issuer, audience, issued/expiry times, a 10-minute baseline lifetime, and a persistent configured signing key.
- [ ] Reject unsigned/tampered tokens, invalid algorithms/issuer/audience/time claims, and missing subject; no session or refresh tables are introduced.

**Verification:** Negative token matrix plus restart test with stable signing material.

**Evidence:** Pending.

### JWT-04 — Build the React JWT-only login experience

**Status:** Todo · **Priority:** P1 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-03](#jwt-03--issue-and-validate-short-lived-bearer-jwts), [FND-03](P00-foundation.md#fnd-03--define-api-and-frontend-contracts)

**Outcome:** Create registration/login, protected navigation, and local logout using an in-memory access token.

**Acceptance criteria:**

- [ ] Authenticated API calls use the bearer header; reload or expiry returns the user to login with a clear state.
- [ ] No access-token persistence in localStorage and no refresh-cookie flow; logout behavior explicitly documents copied-token validity until expiry.

**Verification:** Browser registration/login/logout/reload/expiry scenarios.

**Evidence:** Pending.

### JWT-05 — Add baseline authentication abuse controls

**Status:** Todo · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-02](#jwt-02--implement-personal-registration-and-credential-login), [JWT-03](#jwt-03--issue-and-validate-short-lived-bearer-jwts)

**Outcome:** Protect authentication endpoints before the first learning demo.

**Acceptance criteria:**

- [ ] Apply bounded request sizes and login throttling with generic errors; account lockout policy avoids easy permanent denial of service.
- [ ] Secrets and raw credentials/tokens are excluded from logs; signing-key and HTTPS configuration are checked.

**Verification:** Throttling, oversized payload, logging-redaction, and configuration tests.

**Evidence:** Pending.

### JWT-06 — Verify and explain the JWT checkpoint

**Status:** Todo · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [JWT-04](#jwt-04--build-the-react-jwt-only-login-experience), [JWT-05](#jwt-05--add-baseline-authentication-abuse-controls), [FND-06](P00-foundation.md#fnd-06--create-the-automated-verification-foundation)

**Outcome:** Complete the first demonstrable lesson without later identity machinery.

**Acceptance criteria:**

- [ ] Tests prove unauthorized requests fail and valid tokens reach only protected operations allowed at this stage.
- [ ] Document the distinction between token validation, user persistence, client logout, and future server revocation; record the P01 demonstration.

**Verification:** Run the JWT regression suite and capture a reproducible demo checklist.

**Evidence:** Pending.
