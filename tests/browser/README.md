# tests/browser

Playwright (Chromium) browser tests, **local only** until a flow needs them in CI. Today there is one smoke test
that serves the production build with `vite preview` and checks that the frontend shell renders.

```bash
scripts/verify.sh browser                                  # from the repository root
# or, from this folder:
npm ci && npm run install:browsers && npm test
```

Planned flows (login, checkout return, account switching, cookie/CSRF behavior) are added by the items that
introduce them; see [`docs/runbooks/verification.md`](../../docs/runbooks/verification.md).
