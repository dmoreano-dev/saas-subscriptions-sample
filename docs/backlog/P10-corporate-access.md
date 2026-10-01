# P10 — Corporate access

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Exercise corporate authentication and provisioning against protocol-aware local integrations. Real Entra validation is separately tracked.

### SSO-01 — Model and configure trusted organization identity connections

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [CON-06](P09-enterprise-commerce.md#con-06--verify-the-enterprise-commercial-journey), [SEC-03](P06-identity-lifecycle-and-sessions.md#sec-03--persist-sessions-and-bind-new-jwts-to-them)

**Outcome:** Add protected OIDC configuration and a local corporate test IdP.

**Acceptance criteria:**

- [ ] Connection belongs to one organization and stores secret references, trusted issuer/client/redirect settings, and activation state.
- [ ] Reject arbitrary user-supplied issuers/metadata destinations; document local versus hosted reachability and secret rotation.

**Verification:** Configuration validation, tenant ownership, and test-provider connectivity checks.

**Evidence:** Pending.

### SSO-02 — Implement corporate OIDC challenge and callback

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Opus / high

**Dependencies:** [SSO-01](#sso-01--model-and-configure-trusted-organization-identity-connections), [REF-06](P07-refresh-and-browser-transport.md#ref-06--verify-the-complete-jwtsessionrefresh-lesson)

**Outcome:** Use standards middleware to authenticate with the test corporation.

**Acceptance criteria:**

- [ ] Validate state, nonce, issuer, audience, redirect URI, and PKCE where applicable; callback returns only controlled frontend destinations.
- [ ] The application still owns local users and its own sessions/JWTs; external tokens are not accepted indiscriminately as API access tokens.
- [ ] Establish the application session and refresh cookie through the trusted callback/proxy, then let React obtain the access JWT through refresh; never expose application access/refresh tokens in redirect URLs.

**Verification:** Real local IdP round trip, forged-state/issuer/callback tests, and browser checks for correlation cookies and safe session handoff.

**Evidence:** Pending.

### SSO-03 — Implement safe external identity linking

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SSO-02](#sso-02--implement-corporate-oidc-challenge-and-callback), [ORG-03](P08-organizations.md#org-03--implement-verified-invitations-and-acceptance)

**Outcome:** Map trusted external subjects to local users and memberships.

**Acceptance criteria:**

- [ ] Unique provider/issuer/subject keys and explicit authenticated linking/invitation flows prevent email-only account takeover.
- [ ] Two corporations or an email change cannot overwrite an existing external identity; links and unlinking are audited.

**Verification:** Same-email/different-issuer, duplicate-subject, invitation, and relinking tests.

**Evidence:** Pending.

### SSO-04 — Enforce account-specific SSO assurance

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Opus / high

**Dependencies:** [SSO-03](#sso-03--implement-safe-external-identity-linking), [ENT-02](P02-personal-accounts-and-plans.md#ent-02--implement-account-scoped-permissions-and-resource-isolation)

**Outcome:** Require the selected organization's approved authentication provenance.

**Acceptance criteria:**

- [ ] Local-password sessions and another organization's SSO do not satisfy the target account's requirement; personal access continues normally.
- [ ] Enabling SSO or changing policy rechecks active access; payment restriction/downgrade cannot create a local-login bypass.

**Verification:** Personal-login bypass, wrong-corporation, policy-change, and unpaid-account tests.

**Evidence:** Pending.

### SSO-05 — Provide connection verification and controlled recovery

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SSO-04](#sso-04--enforce-account-specific-sso-assurance), [SEC-06](P06-identity-lifecycle-and-sessions.md#sec-06--build-security-settings-and-protect-privileged-actions)

**Outcome:** Prevent accidental organization lockout without introducing a hidden bypass.

**Acceptance criteria:**

- [ ] Admins test a connection before enforcement; metadata/secret rotation has a safe documented path.
- [ ] Emergency recovery requires a narrow privileged workflow, explicit expiry, identity verification, and audit; ordinary users cannot invoke it.

**Verification:** Misconfiguration, expired secret, recovery expiry, and privilege tests.

**Evidence:** Pending.

### SSO-06 — Verify and explain the corporate login checkpoint

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SSO-05](#sso-05--provide-connection-verification-and-controlled-recovery)

**Outcome:** Complete real OIDC local acceptance and document hosted prerequisites.

**Acceptance criteria:**

- [ ] Record a successful standards-based flow against the local test IdP and the full negative authorization matrix.
- [ ] Hosted SSO readiness is reported separately and requires a reachable IdP; simulator/local success is never labeled verified Entra compatibility.

**Verification:** SSO browser/API suite and reproducible setup/recovery guide.

**Evidence:** Pending.

### SCI-01 — Implement the supported SCIM resource profile

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SSO-06](#sso-06--verify-and-explain-the-corporate-login-checkpoint), [ORG-02](P08-organizations.md#org-02--implement-the-organization-permission-matrix)

**Outcome:** Add SCIM discovery and Users operations with stable external mappings.

**Acceptance criteria:**

- [ ] ServiceProviderConfig/Schemas/ResourceTypes and supported Users list/filter/pagination/create/get/update/PATCH/deactivate operations have compliant shapes/errors.
- [ ] Retries and externalId uniqueness are idempotent; internal API DTOs are not exposed as a substitute for SCIM resources.

**Verification:** Protocol fixture and duplicate-resource integration tests.

**Evidence:** Pending.

### SCI-02 — Secure provisioning connections per organization

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SCI-01](#sci-01--implement-the-supported-scim-resource-profile), [SEC-06](P06-identity-lifecycle-and-sessions.md#sec-06--build-security-settings-and-protect-privileged-actions)

**Outcome:** Issue and rotate narrowly scoped SCIM integration credentials.

**Acceptance criteria:**

- [ ] Every call resolves its organization from authenticated integration context and enforces provisioning-only permissions.
- [ ] Store credential hashes where applicable, support revocation/rotation, throttle requests, and reject cross-organization resource manipulation.

**Verification:** Wrong-token/wrong-account/revoked-token and credential rotation tests.

**Evidence:** Pending.

### SCI-03 — Add group mappings and licensing behavior

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SCI-02](#sci-02--secure-provisioning-connections-per-organization), [ORG-04](P08-organizations.md#org-04--implement-licenses-and-atomic-seat-assignment)

**Outcome:** Map supported external groups into allowlisted membership roles and seat policy.

**Acceptance criteria:**

- [ ] Group changes cannot assign platform roles or unauthorized ownership; mapping is organization-scoped and audited.
- [ ] Provisioned users may remain unlicensed when capacity is full; no silent paid seat purchase occurs; removal reconciles role/seat state.

**Verification:** Group PATCH/retry, disallowed-role, and exhausted-license tests.

**Evidence:** Pending.

### SCI-04 — Make deprovisioning win over concurrent login

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Opus / high

**Dependencies:** [SCI-03](#sci-03--add-group-mappings-and-licensing-behavior), [ORG-06](P08-organizations.md#org-06--implement-organization-member-offboarding), [SSO-04](#sso-04--enforce-account-specific-sso-assurance)

**Outcome:** Apply corporate offboarding consistently across provisioning, SSO, and sessions.

**Acceptance criteria:**

- [ ] A received deactivation blocks organization access even for unexpired JWTs and existing application sessions.
- [ ] Concurrent JIT/SSO cannot reactivate an explicitly deactivated membership; personal and other-company access survive.

**Verification:** Concurrent SCIM/SSO/offboarding and cross-account survival tests.

**Evidence:** Pending.

### SCI-05 — Verify the provisioning lifecycle and R2 checkpoint

**Status:** Todo · **Priority:** P0 · **Phase:** P10 · **Suggested model:** Sonnet 5.5

**Dependencies:** [SCI-04](#sci-04--make-deprovisioning-win-over-concurrent-login), [CON-06](P09-enterprise-commerce.md#con-06--verify-the-enterprise-commercial-journey)

**Outcome:** Create a deterministic SCIM client and full corporate onboarding/offboarding demo.

**Acceptance criteria:**

- [ ] Test provisioning, update, groups, retry, license shortage, deactivation, and explicit reprovisioning with scoped credentials.
- [ ] Document external synchronization latency and distinguish local profile coverage from optional real Entra validation; run personal-plan regressions.

**Verification:** SCIM suite, R2 checklist, and corporate lifecycle demonstration.

**Evidence:** Pending.
