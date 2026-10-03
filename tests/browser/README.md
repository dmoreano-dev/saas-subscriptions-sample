# tests/browser

Playwright (Chromium) scenarios, **local only** (not in CI). They run against the real stack: `support/global-setup.ts`
starts `postgres:17` in Docker, applies the migrations, and starts the API with a throwaway ES256 key generated in
memory; `vite preview` serves the production build and proxies `/api` to it. Requires Docker and the .NET SDK.

```bash
scripts/verify.sh browser                                  # from the repository root
# or, from this folder:
npm ci && npm run install:browsers && npm test
```

- `tests/shell.spec.ts` — the app shell redirects to login.
- `tests/auth.spec.ts` — JWT-04: register, login, protected `/me`, logout, reload, expiry; the access token stays out of
  storage and cookies, and a copied token still works after local logout until it expires (no revocation until P06).

Later flows (checkout return, account switching, cookie/CSRF) are added by the items that introduce them; see
[`docs/runbooks/verification.md`](../../docs/runbooks/verification.md).
