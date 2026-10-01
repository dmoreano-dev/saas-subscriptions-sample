# P07 — Refresh tokens and browser transport

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Test refresh persistence/rotation first. Add browser-cookie transport only after the server lifecycle is understood.

### REF-01 — Create opaque refresh-token persistence

**Status:** Todo · **Priority:** P0 · **Phase:** P07 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SEC-07](P06-identity-lifecycle-and-sessions.md#sec-07--verify-the-custom-identity-lifecycle-checkpoint)

**Outcome:** Add hashed refresh tokens linked to persisted sessions.

**Acceptance criteria:**

- [ ] Token family, predecessor/replacement, issue/expiry/consumed/revoked timestamps, and session association are explicit.
- [ ] Absolute/inactivity limits cannot be extended indefinitely; secrets are generated securely and never stored raw.

**Verification:** Schema/hash/expiry tests without browser-cookie integration.

**Evidence:** Pending.

### REF-02 — Implement atomic refresh rotation

**Status:** Todo · **Priority:** P0 · **Phase:** P07 · **Suggested model:** Opus / high

**Dependencies:** [REF-01](#ref-01--create-opaque-refresh-token-persistence)

**Outcome:** Consume one refresh credential and issue the next generation safely.

**Acceptance criteria:**

- [ ] Concurrent requests cannot create independently valid successor branches; user/session/MFA policy is checked.
- [ ] Idempotency/retry behavior for a lost response is documented and bounded; old-token replay cannot silently mint another access token.

**Verification:** Concurrent rotation, rollback, lost-response, and replay tests.

**Evidence:** Pending.

### REF-03 — Handle refresh-family revocation and abuse

**Status:** Todo · **Priority:** P0 · **Phase:** P07 · **Suggested model:** Opus / high

**Dependencies:** [REF-02](#ref-02--implement-atomic-refresh-rotation)

**Outcome:** Connect replay/expiry/security events to controlled session/family termination.

**Acceptance criteria:**

- [ ] A detected unauthorized reuse invalidates the affected family/session according to documented policy without affecting unrelated users.
- [ ] Logout, password reset, user disable, and later organization access removal are consistent with refresh issuance; errors expose no raw token details.

**Verification:** Replay, reset, logout, and disabled-session refresh scenarios.

**Evidence:** Pending.

### REF-04 — Add secure refresh-cookie transport through Vercel

**Status:** Todo · **Priority:** P0 · **Phase:** P07 · **Suggested model:** Opus / high

**Dependencies:** [REF-03](#ref-03--handle-refresh-family-revocation-and-abuse), [DEP-03](P05-memory-cache-and-hosted-demo.md#dep-03--deploy-react-to-vercel-and-verify-api-routing)

**Outcome:** Integrate the already-tested refresh service with browser transport.

**Acceptance criteria:**

- [ ] Cookie is Secure, HttpOnly, host-only, with explicit SameSite/path/lifetime; no accidental Render-domain cookie is returned through the frontend proxy.
- [ ] Validate CSRF tokens and trusted origins for applicable mutations; verify Set-Cookie, HTTPS/forwarded headers, no-store, and direct-origin behavior.

**Verification:** Hosted browser cookie/CSRF/origin tests; test with third-party cookies restricted.

**Evidence:** Pending.

### REF-05 — Implement React renewal and coherent logout

**Status:** Todo · **Priority:** P1 · **Phase:** P07 · **Suggested model:** Sonnet 5.5

**Dependencies:** [REF-04](#ref-04--add-secure-refresh-cookie-transport-through-vercel)

**Outcome:** Add transparent access-token renewal while retaining the JWT in memory.

**Acceptance criteria:**

- [ ] Coordinate simultaneous API failures/refresh attempts, bound retries, and avoid recursive refresh loops.
- [ ] Logout clears client state and the cookie, revokes server access, and prevents stale in-flight responses from restoring authentication.

**Verification:** Browser expiry, parallel requests, multi-tab, refresh failure, and logout-race tests.

**Evidence:** Pending.

### REF-06 — Verify the complete JWT/session/refresh lesson

**Status:** Todo · **Priority:** P0 · **Phase:** P07 · **Suggested model:** Sonnet 5.5

**Dependencies:** [REF-05](#ref-05--implement-react-renewal-and-coherent-logout)

**Outcome:** Document how token format, server session, and cookie transport work together.

**Acceptance criteria:**

- [ ] Custom authentication passes the same API authorization suite and the deployed browser suite.
- [ ] The lesson separates server rotation tests from transport tests and records expiry, revocation, CSRF, and storage guarantees.

**Verification:** Combined auth regression and reproducible P07 demo.

**Evidence:** Pending.
