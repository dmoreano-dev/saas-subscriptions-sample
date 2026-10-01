# P08 — Organizations

[← Índice del backlog](../backlog.md) · [Plan](../plan.md)

Add organizations without changing the personal subscription journey or data ownership.

### ORG-01 — Create organizations and explicit account switching

**Status:** Todo · **Priority:** P0 · **Phase:** P08 · **Suggested model:** Sonnet 5.5

**Dependencies:** [REF-06](P07-refresh-and-browser-transport.md#ref-06--verify-the-complete-jwtsessionrefresh-lesson), [ENT-02](P02-personal-accounts-and-plans.md#ent-02--implement-account-scoped-permissions-and-resource-isolation)

**Outcome:** Add organization accounts while preserving the personal-first experience.

**Acceptance criteria:**

- [ ] Organization creation does not convert/cancel the creator's personal account or auto-grant an Enterprise contract.
- [ ] Only active memberships appear in account selection; all requests/jobs use verified account context and late responses cannot cross account views.

**Verification:** Personal-plus-two-organizations browser and API isolation tests.

**Evidence:** Pending.

### ORG-02 — Implement the organization permission matrix

**Status:** Todo · **Priority:** P0 · **Phase:** P08 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ORG-01](#org-01--create-organizations-and-explicit-account-switching)

**Outcome:** Add Owner, Admin, BillingAdmin, and Member with scoped assignments.

**Acceptance criteria:**

- [ ] Billing-only roles cannot read projects by default; account admins cannot assign platform permissions or cross-account roles.
- [ ] Owner transfer requires recent authentication; transactions prevent removal of the last active owner and audit role changes.

**Verification:** Privilege-escalation, concurrent owner-removal, and transfer tests.

**Evidence:** Pending.

### ORG-03 — Implement verified invitations and acceptance

**Status:** Todo · **Priority:** P0 · **Phase:** P08 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ORG-02](#org-02--implement-the-organization-permission-matrix), [SEC-01](P06-identity-lifecycle-and-sessions.md#sec-01--add-email-verification-and-protected-email-changes), [FND-05](P00-foundation.md#fnd-05--set-up-local-email-capture)

**Outcome:** Add expiring, hashed, single-use invitations with explicit recipient binding.

**Acceptance criteria:**

- [ ] Invite/resend/revoke are authorized and throttled; acceptance verifies recipient identity and active invitation/account state.
- [ ] Retries/concurrent acceptance cannot duplicate membership or exceed seats; an email domain by itself grants no access.

**Verification:** Wrong-recipient, expired/revoked/replayed invitation and seat-race tests.

**Evidence:** Pending.

### ORG-04 — Implement licenses and atomic seat assignment

**Status:** Todo · **Priority:** P0 · **Phase:** P08 · **Suggested model:** Opus / high

**Dependencies:** [ORG-02](#org-02--implement-the-organization-permission-matrix), [ENT-05](P02-personal-accounts-and-plans.md#ent-05--implement-transactional-usage-accounting)

**Outcome:** Enforce purchased capacity independently of membership existence.

**Acceptance criteria:**

- [ ] Only licensed active product members count; pending invitations/provisioned unlicensed users do not; no action silently increases billing.
- [ ] Capacity changes and assignment/reassignment are atomic; a billing/security-only exception never grants content access.

**Verification:** Last-seat concurrency, reassignment, over-capacity, and unlicensed-access tests.

**Evidence:** Pending.

### ORG-05 — Build membership, role, license, and audit screens

**Status:** Todo · **Priority:** P1 · **Phase:** P08 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ORG-03](#org-03--implement-verified-invitations-and-acceptance), [ORG-04](#org-04--implement-licenses-and-atomic-seat-assignment)

**Outcome:** Provide the minimum usable Enterprise administration UI.

**Acceptance criteria:**

- [ ] Admins can see invitation/member/license state and authorized audit entries without accessing another organization's data.
- [ ] UI explains available seats and denied actions; destructive role/member changes have clear confirmation and server authorization.

**Verification:** Role-by-role browser journeys and direct API bypass attempts.

**Evidence:** Pending.

### ORG-06 — Implement organization member offboarding

**Status:** Todo · **Priority:** P0 · **Phase:** P08 · **Suggested model:** Opus / high

**Dependencies:** [ORG-02](#org-02--implement-the-organization-permission-matrix), [ORG-04](#org-04--implement-licenses-and-atomic-seat-assignment), [SEC-04](P06-identity-lifecycle-and-sessions.md#sec-04--implement-currentall-session-revocation)

**Outcome:** Remove corporate access while preserving account-owned resources.

**Acceptance criteria:**

- [ ] Offboarding revokes the membership's access before cleanup, releases assigned seats, and handles owned tasks/resources explicitly.
- [ ] The global user, personal subscription, other organization memberships, and organization-owned content remain intact.

**Verification:** Active-session removal, in-flight job, personal-account preservation, and audit tests.

**Evidence:** Pending.

### ORG-07 — Verify the organization isolation checkpoint

**Status:** Todo · **Priority:** P0 · **Phase:** P08 · **Suggested model:** Sonnet 5.5

**Dependencies:** [ORG-05](#org-05--build-membership-role-license-and-audit-screens), [ORG-06](#org-06--implement-organization-member-offboarding)

**Outcome:** Prove that Enterprise additions do not break personal subscriptions.

**Acceptance criteria:**

- [ ] Run a fixture with a personal Pro user who is Admin in organization A and Member in B.
- [ ] Test cross-account reports/files, roles, billing portal, caches, quotas, background jobs, and owner transfer.

**Verification:** Full personal/organization regression and P08 demonstration.

**Evidence:** Pending.
