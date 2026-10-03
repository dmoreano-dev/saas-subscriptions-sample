# JWT checkpoint (P01): what a token proves and what it does not

Written in JWT-06. It explains the four things that are easy to confuse in the first lesson, then gives a
checklist to reproduce the P01 demonstration. Decisions: [ADR 0008](../decisions/0008-registration-enumeration-and-login-throttling.md)
(registration and login), [ADR 0009](../decisions/0009-access-token-signing-and-validation.md) (token signing and validation).

## Four different things

| Concept | Where it lives | What it answers | What it does **not** do |
|---|---|---|---|
| **Token validation** | The API, on every protected request. Stateless: ES256 signature with the configured key, `kid`, exact issuer and audience, `sub` and `jti` present, `exp` with a 30-second skew. | "Did this API sign this token, and has it not expired?" | It does not read the database. A valid signature says nothing about whether the user still exists or is still active. |
| **User persistence** | PostgreSQL: `users`, `password_credentials`, personal account and membership. | "Who is this user and what is their state?" The source of truth. | It is not consulted per request. Today only `GET /api/me` loads the user (so a disabled or deleted user gets 401 there); the other protected routes are contract-only (501). |
| **Client logout** | The browser: React state drops `{ accessToken, expiresAt, user }`. | "The UI no longer has the token." | It tells the server nothing and revokes nothing. |
| **Server revocation** | **Does not exist in P01.** Sessions, refresh tokens and revocation arrive in P06/P07. | (future) "This token or session must stop working now." | — |

### The copied token

A token that was copied before logout (a header in a terminal, a log, a proxy) **keeps working until its `exp`**
(10 minutes after login, `Authentication:AccessTokenLifetime`), because validation is stateless and nothing is
stored to revoke. The same holds if the user is disabled afterwards, except on `GET /api/me`, which checks the user.
The only controls in P01 are the short lifetime and the signing key: changing the key invalidates every token
at once (and signs everyone out). Closing that gap is P06 (server-side sessions/revocation); do not model it in
the access token itself.

## Regression suite

| What is proved | Test |
|---|---|
| Forged, tampered, expired, wrong issuer/audience/algorithm and garbage tokens get 401; acceptable-within-skew tokens pass | `AccessTokenValidationTests` |
| Expiry with a controllable clock | `AccessTokenExpiryTests` |
| `/api/me` returns only the caller; disabled/unknown user is 401; same key survives restart, a new key does not | `MeEndpointTests` |
| Login issues the token, generic errors, throttle | `LoginEndpointTests`, `LoginServiceTests` |
| Every `/api` route except register/login requires a token; a valid token only meets 501 on contract-only routes; the lists drift-check against the real endpoint table | `ProtectedSurfaceTests` |
| No secret in logs | `LogRedactionTests` |
| Browser: no token in storage/cookies, logout, copied token still 200, reload, expiry | `tests/browser/tests/auth.spec.ts` |

Run: `scripts/verify.sh all` (backend, frontend, secrets, audit; needs Docker) and `scripts/verify.sh browser`
(Playwright, local only).

## Demo checklist

Prerequisites: Docker running, signing key in user-secrets (README "Signing key"), then start the AppHost
(README "Running locally"). Use the API URL from the dashboard (`$API`, e.g. `https://localhost:7101`) and the
frontend URL from the dashboard.

1. **Register.** In the web app open `/register`, create `demo@example.com` with a 5–50 character password.
   Expect a notice on `/login`; no token is issued by registration.
   `curl -k -X POST $API/api/auth/register -H 'content-type: application/json' -d '{"email":"demo@example.com","password":"demo-pass"}'` → 201.
2. **Login.** Sign in. Expect the profile page. Copy the token from the response of
   `curl -k -X POST $API/api/auth/login -H 'content-type: application/json' -d '{"email":"demo@example.com","password":"demo-pass"}'`
   (`accessToken`, `expiresAt`); keep it as `$TOKEN`.
3. **Protected call.** `curl -k $API/api/me -H "authorization: Bearer $TOKEN"` → 200 with the profile.
   Without the header → 401 (`code: unauthenticated`). `curl -k $API/api/accounts -H "authorization: Bearer $TOKEN"` → 501: valid token, but the feature does not exist yet.
4. **No persistence in the browser.** DevTools → Application: local storage, session storage, cookies and IndexedDB hold no token.
5. **Logout (local).** Click logout → `/login` with a signed-out notice.
6. **Copied token.** Repeat the `/api/me` call with `$TOKEN` → **still 200**. This is the documented limitation, not a bug.
7. **Reload.** Log in again, reload on `/me` → back to `/login`: the memory-only session is gone.
8. **Expiry.** After 10 minutes (or `AccessTokenLifetime` lowered locally) the page closes the session with the "expired" notice, and the `curl` from step 3 returns 401.

Automated equivalents: steps 1–2, 4–8 in `auth.spec.ts`; step 3 in `ProtectedSurfaceTests` and `MeEndpointTests`.

## Known gaps in this lesson (not done, on purpose)

- **JWT-05 extras moved to the end of the lab:** no explicit request-size limit (Kestrel's default applies; hashing is
  still bounded by email/password length validation), no per-IP rate limit, no request-log redaction.
- The login throttle is in memory, per process and per email: it resets on restart and does not see one source trying many emails.
- One signing key, no rotation; no revocation (P06); no refresh tokens, cookies or persisted sessions (P06/P07).
- Hosted (Render) behavior is not exercised; it belongs to P05.
