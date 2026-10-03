import { expect, test } from '@playwright/test'

const APP_TITLE = 'Subscription Lab'

test('home_withoutSession_redirectsToLoginShell', async ({ page }) => {
  // Arrange
  const consoleErrors: string[] = []
  page.on('pageerror', (error) => consoleErrors.push(error.message))

  // Act
  await page.goto('/')

  // Assert
  await expect(page).toHaveTitle(APP_TITLE)
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
  expect(consoleErrors).toEqual([])
})
