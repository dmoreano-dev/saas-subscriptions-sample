import { expect, test, type Page } from '@playwright/test'
import { randomUUID } from 'node:crypto'

// JWT-04: registration, login, protected /me, logout, reload and expiry against the real API + PostgreSQL.
const PASSWORD = 'correct horse 42'
const WRONG_PASSWORD = 'definitely wrong 99'
const ELEVEN_MINUTES = '11:00'

const uniqueEmail = () => `browser-${randomUUID()}@example.test`

async function register(page: Page, email: string) {
  await page.goto('/register')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(PASSWORD)
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page).toHaveURL(/\/login$/)
}

async function signIn(page: Page, email: string, password = PASSWORD) {
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()
}

async function registerAndSignIn(page: Page, email: string) {
  await register(page, email)
  await signIn(page, email)
  await expect(page.getByRole('heading', { name: 'Your profile' })).toBeVisible()
}

const clientSideStorage = (page: Page) =>
  page.evaluate(() => ({
    local: window.localStorage.length,
    session: window.sessionStorage.length,
    cookie: document.cookie,
  }))

test('register_validNewUser_showsCreatedNoticeOnLogin', async ({ page }) => {
  // Arrange
  const email = uniqueEmail()

  // Act
  await register(page, email)

  // Assert
  await expect(page.getByRole('status')).toHaveText('Account created. Sign in to continue.')
})

test('login_validCredentials_showsProfileFromProtectedMe', async ({ page }) => {
  // Arrange
  const email = uniqueEmail()
  await register(page, email)

  // Act
  await signIn(page, email)

  // Assert
  await expect(page).toHaveURL(/\/me$/)
  await expect(page.getByTestId('me-email')).toHaveText(email)
})

test('login_wrongPassword_showsGenericErrorAndStaysOnLogin', async ({ page }) => {
  // Arrange
  const email = uniqueEmail()
  await register(page, email)

  // Act
  await signIn(page, email, WRONG_PASSWORD)

  // Assert
  await expect(page.getByRole('alert')).toHaveText('Email or password is incorrect.')
  await expect(page).toHaveURL(/\/login$/)
})

test('me_withoutSession_redirectsToLogin', async ({ page }) => {
  // Arrange / Act
  await page.goto('/me')

  // Assert
  await expect(page).toHaveURL(/\/login$/)
})

test('login_success_keepsTokenOutOfBrowserStorageAndCookies', async ({ page, context }) => {
  // Arrange
  await registerAndSignIn(page, uniqueEmail())

  // Act
  const storage = await clientSideStorage(page)

  // Assert
  expect(storage).toEqual({ local: 0, session: 0, cookie: '' })
  expect(await context.cookies()).toEqual([])
})

test('logout_afterLogin_returnsToLoginWithSignedOutNotice', async ({ page }) => {
  // Arrange
  await registerAndSignIn(page, uniqueEmail())

  // Act
  await page.getByRole('button', { name: 'Sign out' }).click()

  // Assert
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('status')).toContainText('You signed out of this browser')
})

test('logout_copiedToken_staysValidUntilExpiry', async ({ page }) => {
  // Arrange
  const email = uniqueEmail()
  await register(page, email)
  const loginResponse = page.waitForResponse((r) => r.url().endsWith('/api/auth/login'))
  await signIn(page, email)
  const { accessToken } = (await (await loginResponse).json()) as { accessToken: string }
  await expect(page.getByRole('heading', { name: 'Your profile' })).toBeVisible()
  await page.getByRole('button', { name: 'Sign out' }).click()

  // Act
  const copied = await page.request.get('/api/me', { headers: { Authorization: `Bearer ${accessToken}` } })

  // Assert
  expect(copied.status()).toBe(200)
})

test('reload_onProtectedPage_returnsToLoginWithoutPersistedSession', async ({ page }) => {
  // Arrange
  await registerAndSignIn(page, uniqueEmail())

  // Act
  await page.reload()

  // Assert
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible()
})

test('expiry_clockPassesExpiresAt_closesSessionWithExpiredNotice', async ({ page }) => {
  // Arrange
  await page.clock.install()
  await registerAndSignIn(page, uniqueEmail())

  // Act
  await page.clock.fastForward(ELEVEN_MINUTES)

  // Assert
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('status')).toHaveText('Your session expired. Sign in again.')
})

test('me_serverRejectsToken_closesSessionWithExpiredNotice', async ({ page }) => {
  // Arrange
  const email = uniqueEmail()
  await register(page, email)
  await page.route('**/api/me', (route) =>
    route.continue({ headers: { ...route.request().headers(), authorization: 'Bearer not-a-valid-token' } }),
  )

  // Act
  await signIn(page, email)

  // Assert
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('status')).toHaveText('Your session expired. Sign in again.')
})
