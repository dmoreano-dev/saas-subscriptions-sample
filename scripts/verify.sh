#!/usr/bin/env bash
# Single verification entry point: CI runs exactly these commands, and so can you locally.
#   scripts/verify.sh backend       format check, build, unit + integration tests (needs Docker)
#   scripts/verify.sh frontend      npm ci, build, lint, OpenAPI contract check
#   scripts/verify.sh secrets       gitleaks scan of the working tree and the full git history
#   scripts/verify.sh dependencies  known-vulnerability audit of NuGet and npm packages
#   scripts/verify.sh browser       Playwright smoke tests (local only, not part of CI yet; needs Chromium)
#   scripts/verify.sh all           backend + frontend + secrets + dependencies (what CI runs)
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

solution="Saas.Subscription.Sample.slnx"

step() { printf '\n==> %s\n' "$*"; }

backend() {
  step "dotnet format (verify only)"
  dotnet format "$solution" --verify-no-changes
  step "dotnet build"
  dotnet build "$solution"
  step "dotnet test (unit + integration; Testcontainers starts PostgreSQL 17)"
  dotnet test "$solution" --no-build
}

frontend() {
  step "frontend: npm ci"
  npm ci --prefix src/frontend
  step "frontend: build"
  npm run build --prefix src/frontend
  step "frontend: lint"
  npm run lint --prefix src/frontend
  step "frontend: contract:check"
  npm run contract:check --prefix src/frontend
}

secrets() {
  step "gitleaks (working tree and full history)"
  command -v gitleaks >/dev/null || { echo "gitleaks is not installed (brew install gitleaks)" >&2; exit 1; }
  gitleaks detect --source . --config .gitleaks.toml --redact --no-banner
}

dependencies() {
  step "NuGet: known vulnerabilities (direct and transitive)"
  # `dotnet list package --vulnerable` exits 0 even when it finds advisories, so inspect its output.
  local report
  report="$(dotnet list "$solution" package --vulnerable --include-transitive 2>&1)"
  printf '%s\n' "$report"
  if grep -q "has the following vulnerable packages" <<<"$report"; then
    echo "Vulnerable NuGet packages found." >&2
    exit 1
  fi
  step "npm: known vulnerabilities (high and above)"
  npm audit --audit-level=high --prefix src/frontend
  npm audit --audit-level=high --prefix tests/browser
}

browser() {
  step "browser: install dependencies"
  npm ci --prefix tests/browser
  npx --prefix tests/browser playwright install chromium
  step "browser: Playwright smoke tests"
  npm test --prefix tests/browser
}

case "${1:-all}" in
  backend) backend ;;
  frontend) frontend ;;
  secrets) secrets ;;
  dependencies) dependencies ;;
  browser) browser ;;
  all) backend; frontend; secrets; dependencies ;;
  *) echo "usage: $0 {backend|frontend|secrets|dependencies|browser|all}" >&2; exit 2 ;;
esac
