# P01 — JWT essentials

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Own users and short-lived access JWTs only. Validate tokens correctly from day one; persisted sessions, refresh tokens, and auth cookies belong to later phases.

### JWT-01 — Implement custom users and password credentials

**Status:** Todo · **Priority:** P0 · **Phase:** P01 · **Suggested model:** Sonnet 5.5

**Dependencies:** [FND-02](P00-foundation.md#fnd-02--establish-postgresql-migrations-and-account-foundations), [FND-04](P00-foundation.md#fnd-04--add-typed-configuration-provider-seams-and-controllable-time)

**Outcome:** Create users/password_credentials and a reviewed password-hashing integration without ASP.NET Core Identity.

**Acceptance criteria:**

- [ ] Store encoded hashes and algorithm parameters; support future verifier versions without storing recoverable passwords.
- [ ] Define normalized-email uniqueness, user status, constant-time verification, secure randomness, and bounded credential input.

**Verification:** Hash/verify/rehash tests, duplicate-email concurrency test, and schema inspection.

**Evidence:** Pending.

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
