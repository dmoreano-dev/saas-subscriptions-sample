import { defineConfig, devices } from '@playwright/test'

// Local-only entry point (FND-06): it is not part of CI. globalSetup starts the real backend (PostgreSQL 17 in
// Docker, Migrator, API with a throwaway signing key; needs Docker and the .NET SDK) and `vite preview` serves the
// production build of the frontend, proxying /api to that API.
import { API_URL } from './support/global-setup'

const baseURL = 'http://localhost:4173'

export default defineConfig({
  testDir: './tests',
  globalSetup: './support/global-setup.ts',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  reporter: 'list',
  use: { baseURL, trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm run build && npm run preview -- --port 4173 --strictPort',
    cwd: '../../src/frontend',
    env: { services__api__http__0: API_URL },
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
})
