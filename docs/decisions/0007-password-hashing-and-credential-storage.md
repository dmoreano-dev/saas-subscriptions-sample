# 0007 — Password hashing and credential storage

- **Status:** Accepted
- **Date:** 2026-10-03
- **Backlog item(s):** JWT-01
- **Deciders:** Project owner

## Context

P01 teaches own registration/login without ASP.NET Core Identity (plan §5A). Passwords need a memory-hard hash, a
stored format that survives a later algorithm change, and a table that is hard to leak by accident. The plan prefers
Argon2id through a maintained library, or a documented equivalent.

## Decision

- **Argon2id through `Geralt` 4.4.0** (a .NET wrapper of libsodium 1.0.22, which ships native binaries for
  osx-arm64/x64 and linux-x64/arm64/musl in its NuGet). libsodium draws the salt from the OS CSPRNG, writes the PHC
  string (`$argon2id$v=19$m=…,t=…,p=1$salt$hash`, 97 chars) and compares in constant time. Parallelism is fixed at 1.
  Not chosen: `SHA256` (fast hash, unusable for passwords); `Rfc2898DeriveBytes.Pbkdf2` (native and acceptable per
  OWASP at ~600k iterations, but not memory-hard); Konscious (managed, but we would write the encoding and the
  comparison ourselves). Fallback if the native library ever fails to load on a target: Konscious behind the same seam.
- **Work factor from configuration** (`PasswordHashing:MemoryMebibytes`, `Iterations`), default **64 MiB, t=3**,
  validated at startup against the OWASP floor (19 MiB, t=2) and a sanity ceiling. Calibration on the author's machine
  (Apple arm64, 5 runs each): 19 MiB/t=2 16 ms · 64 MiB/t=2 61 ms · **64 MiB/t=3 88 ms** · 128 MiB/t=2 133 ms ·
  256 MiB/t=2 287 ms. Hosted CPUs are slower, so re-measure on Render (DEP-01) before treating 88 ms as the hosted
  figure. 64 MiB is also the per-login memory cost: concurrent logins multiply it, which is why throttling (JWT-02)
  matters.
- **Seam:** `IPasswordHasher` (`Hash`, `Verify` → `Failed | Succeeded | SucceededRehashNeeded`) with one
  implementation, `Argon2idPasswordHasher`. There is deliberately no scheme/strategy abstraction yet: it would have
  no second implementation to justify it. What is kept is the data that makes a second algorithm possible later
  without a rewrite: every hash is stored with its `algorithm`, and `Verify` returns `SucceededRehashNeeded` when
  the stored parameters are weaker than the configured ones. A stored algorithm this class does not know, a
  corrupt encoding or an oversized input is `Failed`, never an exception. When a second algorithm arrives (P11),
  extract the interface and dispatch on the stored algorithm then. There is no `verifier_version` column: the PHC
  string already carries the Argon2 version (`v=19`) and parameters, and a separate number would only matter if
  our own procedure around the hash changed with an identical-looking result (a pepper, a password
  pre-processing step). That is hypothetical; if it happens, add the column with `DEFAULT 1` in a migration.
- **Storage:** table `password_credentials`, one row per user (`user_id` is PK and FK, `Restrict`), separate from
  `users` so the hash is not loaded with the user, users without a password (SSO later) have no row and a rehash does
  not touch `users`. Columns: `algorithm`, `encoded_hash` (≤255), timestamps. There is no separate
  parameters column: memory, iterations, parallelism and salt are already inside the encoded hash, and a copy could
  drift from it; stale hashes can be found by matching `t=`/`m=` in `encoded_hash` if that is ever needed. CHECKs: algorithm list,
  non-empty hash. `users.status` (`Active | Disabled`, default `Active`) is added now; `Disabled` is used from P06.
- **Input bounds:** `PasswordPolicy` 5–50 Unicode characters, no composition rules, no Unicode normalization.
  `Verify` enforces only the upper bound, so raising the minimum later never locks out existing people.
- **Email uniqueness** is the existing `ux_users_normalized_email` over `EmailNormalizer` output (FND-02).

## Consequences

- Positive: a stored hash is self-describing and upgradeable on login; parameters are tunable without a deploy; the
  hash lives in one table.
- Negative / trade-offs: a native dependency (libsodium) must load in the Render Docker image (verify in DEP-01); a
  minimum of 5 characters is a lab value, below NIST's 8, and must rise before any real use; Argon2id memory cost
  needs login throttling to be safe against abuse.
- Follow-ups: constant-time handling of unknown accounts, throttling and generic errors (JWT-02); rehash-on-login
  wiring (JWT-02); extract a scheme abstraction with the second verifier and staged migration (P11); revisit the minimum length.
