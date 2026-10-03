import { defineConfig, devices } from '@playwright/test'

// Local-only entry point (FND-06): it is not part of CI yet. It serves the production build of the
// frontend with `vite preview`; flows that need the API (login, checkout return, account switching,
// cookie/CSRF) arrive with the items that introduce them.
const baseURL = 'http://localhost:4173'

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  reporter: 'list',
  use: { baseURL, trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm run build && npm run preview -- --port 4173 --strictPort',
    cwd: '../../src/frontend',
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
})
