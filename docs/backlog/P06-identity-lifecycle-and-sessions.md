# P06 — Identity lifecycle and sessions

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Introduce identity recovery, persistent sessions, revocation, and MFA as distinct changes with observable behavior.

### SEC-01 — Add email verification and protected email changes

**Status:** Todo · **Priority:** P0 · **Phase:** P06 · **Suggested model:** Sonnet 5.5

**Dependencies:** [DEP-06](P05-memory-cache-and-hosted-demo.md#dep-06--accept-the-first-hosted-learning-release-r1), [FND-05](P00-foundation.md#fnd-05--set-up-local-email-capture)

**Outcome:** Implement hashed single-use email tokens and verified email lifecycle.

**Acceptance criteria:**

- [ ] Verification/resend uses expiring tokens and throttling; pending users retain the operations needed to verify/recover.
- [ ] Changing email requires recent authentication and verifies the new address; old-address notices are sent where appropriate; uniqueness is enforced under concurrency.

**Verification:** Expired/replayed/wrong-purpose token and concurrent email-change tests.

**Evidence:** Pending.

### SEC-02 — Implement password recovery and password changes

**Status:** Todo · **Priority:** P0 · **Phase:** P06 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SEC-03](#sec-03--persist-sessions-and-bind-new-jwts-to-them), [SEC-01](#sec-01--add-email-verification-and-protected-email-changes)

**Outcome:** Add own reset/change flows with session invalidation.

**Acceptance criteria:**

- [ ] Forgot-password responses are generic; token hashes, purpose, expiry, and one-time consumption are enforced atomically.
- [ ] Reset/change updates the credential and revokes affected sessions/security versions; secrets are never returned in hosted API responses.

**Verification:** Reset replay, token expiry, generic-response, and existing-session denial tests.

**Evidence:** Pending.

### SEC-03 — Persist sessions and bind new JWTs to them

**Status:** Todo · **Priority:** P0 · **Phase:** P06 · **Suggested model:** Opus / high

**Dependencies:** [DEP-06](P05-memory-cache-and-hosted-demo.md#dep-06--accept-the-first-hosted-learning-release-r1), [JWT-03](P01-jwt-essentials.md#jwt-03--issue-and-validate-short-lived-bearer-jwts)

**Outcome:** Introduce sessions with explicit lifecycle and a session identifier in new access tokens.

**Acceptance criteria:**

- [ ] API checks session/user state before sensitive access; sessions have expiry, revocation, and a documented security-version relationship.
- [ ] Define the legacy JWT cutover and reject it after its planned grace/expiry; preserve the original JWT lesson as a historical checkpoint.

**Verification:** Session expiry/revocation, disabled-user, and migration-cutover tests.

**Evidence:** Pending.

### SEC-04 — Implement current/all-session revocation

**Status:** Todo · **Priority:** P0 · **Phase:** P06 · **Suggested model:** Opus / high

**Dependencies:** [SEC-02](#sec-02--implement-password-recovery-and-password-changes), [SEC-03](#sec-03--persist-sessions-and-bind-new-jwts-to-them)

**Outcome:** Add server-enforced logout, session management, and security-change handling.

**Acceptance criteria:**

- [ ] A user can revoke their own current or other sessions; platform/security actions follow separate privileges and audit.
- [ ] Revoked sessions are rejected despite unexpired JWTs; other users' sessions cannot be listed or revoked; state remains durable after restart.

**Verification:** Cross-user session tests and immediate-revocation/restart demonstration.

**Evidence:** Pending.

### SEC-05 — Add MFA enrollment, challenge, and recovery

**Status:** Todo · **Priority:** P0 · **Phase:** P06 · **Suggested model:** Opus / high

**Dependencies:** [SEC-04](#sec-04--implement-currentall-session-revocation)

**Outcome:** Implement MFA using maintained protocol/crypto components and own persistence.

**Acceptance criteria:**

- [ ] Enroll and confirm TOTP before enabling it; encrypt shared secrets and hash single-use recovery codes.
- [ ] Successful password-only authentication cannot obtain full access when MFA is required; challenge tokens have limited purpose/expiry; recovery/reset is protected and audited.

**Verification:** Enrollment, replay, clock-window, recovery-code reuse, and factor-bypass tests.

**Evidence:** Pending.

### SEC-06 — Build security settings and protect privileged actions

**Status:** Todo · **Priority:** P1 · **Phase:** P06 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SEC-01](#sec-01--add-email-verification-and-protected-email-changes), [SEC-04](#sec-04--implement-currentall-session-revocation), [SEC-05](#sec-05--add-mfa-enrollment-challenge-and-recovery)

**Outcome:** Add React verification/recovery/session/MFA flows and recent-auth requirements.

**Acceptance criteria:**

- [ ] Users can understand and manage their security state without exposing secrets in lists/logs.
- [ ] Operator commercial/security changes require the planned MFA/recent-auth assurance; audit records show actor, account, reason, and result.

**Verification:** Browser security journey and unauthorized privileged-action tests.

**Evidence:** Pending.

### SEC-07 — Verify the custom identity lifecycle checkpoint

**Status:** Todo · **Priority:** P0 · **Phase:** P06 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SEC-06](#sec-06--build-security-settings-and-protect-privileged-actions)

**Outcome:** Regression-test identity recovery and revocation before introducing refresh.

**Acceptance criteria:**

- [ ] Subscription and account authorization still work after all identity changes.
- [ ] Document which previous JWT limitations are now resolved and prove durable recovery/revocation without refresh-token cookies.

**Verification:** Identity plus personal billing regression suite and P06 demo.

**Evidence:** Pending.
