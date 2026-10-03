# 0008 — Registration without enumeration, generic login and in-memory login throttle

- **Status:** Accepted
- **Date:** 2026-10-03
- **Backlog item(s):** JWT-02
- **Deciders:** Project owner

## Context

Registration and login are the first public surface. Two leaks matter: telling a caller which emails have an
account (plan §9: "Registration never reveals whether an email already exists"; ADR 0004 documents no 409), and
letting guessing attacks burn the memory-hard hash (ADR 0007: 64 MiB per login). The documented registration
failure `registration_failed` (400) would itself be an oracle, because a duplicate would be the only input that
produces it.

## Decision

- **Registration:** one `SaveChanges` inserts user, password credential, personal account and owner membership
  in a single transaction. There is no existence check (it would race); the unique index
  `ux_users_normalized_email` decides. A violation of that index (SQLSTATE 23505) rolls the whole transaction
  back and the service returns a **decoy** (`Created = false`): fresh version-7 ids, the submitted email and the
  current time. The endpoint answers the same `201` with the same shape for a new and for an existing email.
  Any other failure propagates (500/503) and leaves no partial rows. The password is hashed before any database
  work in both paths, so time is dominated by Argon2id either way.
  Chosen over: a 400 `registration_failed` for duplicates (better message for the person, but enumerable).
  `registration_failed` stays in the catalog (removing a code is a breaking change) but nothing returns it today.
  The decoy ids refer to nothing; a client must not use them (it goes to login). P06 replaces the silent case
  with the "we emailed you" pattern.
- **Login result, no token:** `ILoginService` returns `Succeeded(AuthenticatedIdentity)`,
  `InvalidCredentials` or `Throttled(retryAfter)`. `AuthenticatedIdentity` is `(UserId, Email, CreatedAt)`: what
  JWT-03 needs, no hash, status or account. `POST /api/auth/login` stays 501 until JWT-03: answering 501 only for
  a correct password would reveal the password check.
- **Generic failure and equal cost:** unknown email, wrong password, a user without a password row and a
  `Disabled` user are the same `InvalidCredentials`. Every attempt that reaches the lookup performs exactly one
  `Verify`: against the stored hash, or against a `DummyPasswordHash` (random password hashed once with the
  current work factor) when there is none. A disabled user is verified against the real hash and then refused.
- **Rehash:** `SucceededRehashNeeded` re-hashes the verified password and replaces the stored hash with
  `UPDATE … WHERE user_id = @id AND encoded_hash = <verified hash>`, so a concurrent password change is never
  overwritten. A failure to rehash is logged (user id and exception type only) and never fails the login.
- **Throttle:** `ILoginThrottle` / `InMemoryLoginThrottle`, keyed by the normalized email as typed (existing or
  not), fixed window of 15 minutes, 5 attempts. An attempt takes its slot **before** the password is checked
  (atomic, so parallel attempts cannot all pass), a correct password releases the key, a blocked attempt does not
  extend the window and the window "unlocks" passively when the next attempt finds it expired (no background
  worker). The table is capped at 10 000 keys (expired windows are dropped first, then the oldest) because the key
  is attacker-controlled. The check runs before the database and the hash. The values are `LoginThrottlePolicy`
  constants, not configuration.
- **Persistence wiring:** `AddPersistence()` registers `AppDbContext` from the validated `Database` options
  (pool size and command timeout included); the API opens a database connection for the first time here.

## Consequences

- Positive: registration reveals nothing about existing emails; login failures are indistinguishable in result
  and comparable in cost; a guessing run is bounded without a lock-out row in the database.
- Negative / trade-offs: a person who registers an existing email sees success and then cannot sign in (until
  P06); the throttle lets anyone make a known email unusable for up to 15 minutes by failing 5 times (temporary,
  no permanent lock-out); state is per process, lost on restart and not shared between instances; flooding more
  than 10 000 distinct emails can evict an older window; response-time equality is argued from the code path
  (one hash everywhere), not asserted by a timing test, because such tests are unreliable.
- Follow-ups: per-IP limits, request-size bounds, lock-out policy and log redaction (JWT-05); wire `/login` and
  issue the token (JWT-03); email-based handling of duplicate registrations (P06); revisit the throttle store if
  more than one API instance exists.
