import { test, expect, Page } from '@playwright/test'

const BASE_URL = 'http://localhost:3000'

// Test credentials (use your real test credentials)
const TEST_EMAIL = 'rodrigo@rugertek.com'
const TEST_PASSWORD = '123456'

/**
 * Help Sidebar Tests
 * Tests the contextual help sidebar functionality
 */

async function loginAndNavigate(page: Page) {
  // Navigate to login page
  await page.goto(`${BASE_URL}/`)
  await page.waitForLoadState('networkidle')
  await page.waitForTimeout(500)

  // Fill login form
  const emailInput = page.locator('input[type="email"]')
  const passwordInput = page.locator('input[type="password"]')

  await emailInput.fill(TEST_EMAIL)
  await passwordInput.fill(TEST_PASSWORD)

  // Click sign in button (exact text "Sign In")
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()

  // Wait for the app selection to appear
  await page.waitForTimeout(2000)

  // Click "Operations App" link
  await page.locator('a:has-text("Operations App")').click()

  // Wait for navigation to /app
  await page.waitForURL(/\/app/, { timeout: 15000 })
  await page.waitForLoadState('networkidle')
  await page.waitForTimeout(1000)
}

test.describe('Help Sidebar', () => {
  test('should show help toggle button on right edge', async ({ page }) => {
    await loginAndNavigate(page)

    // Wait for the page to fully load
    await page.waitForTimeout(2000)

    // Take screenshot of the full page to see what's rendered
    await page.screenshot({
      path: 'tests/e2e/screenshots/help-sidebar-01-full-page.png',
      fullPage: true
    })

    // Look for the help toggle button (? button on right edge)
    const helpToggleButton = page.locator('button:has-text("?")').first()

    // Check if it exists
    const count = await helpToggleButton.count()
    console.log(`Found ${count} help buttons with "?" text`)

    // Take screenshot showing the button location
    if (count > 0) {
      await helpToggleButton.screenshot({
        path: 'tests/e2e/screenshots/help-sidebar-02-button.png'
      })

      // Verify button is visible
      await expect(helpToggleButton).toBeVisible()
    }

    // Also check for any button with title containing "Help"
    const helpTitleButton = page.locator('button[title*="Help"]').first()
    const helpTitleCount = await helpTitleButton.count()
    console.log(`Found ${helpTitleCount} buttons with title containing "Help"`)

    if (helpTitleCount > 0) {
      await helpTitleButton.screenshot({
        path: 'tests/e2e/screenshots/help-sidebar-03-title-button.png'
      })
    }

    // Ensure at least one help button was found
    expect(count + helpTitleCount).toBeGreaterThan(0)
  })

  test('should open help sidebar when clicking toggle button', async ({ page }) => {
    await loginAndNavigate(page)
    await page.waitForTimeout(2000)

    // Find the help button (try by title first, then by text)
    let helpButton = page.locator('button[title*="Help"]').first()
    if (await helpButton.count() === 0) {
      helpButton = page.locator('button:has-text("?")').first()
    }

    // Take screenshot before clicking
    await page.screenshot({
      path: 'tests/e2e/screenshots/help-sidebar-04-before-click.png',
      fullPage: true
    })

    // Ensure help button exists
    const buttonCount = await helpButton.count()
    expect(buttonCount).toBeGreaterThan(0)

    // Click the help button
    await helpButton.click()
    await page.waitForTimeout(500)

    // Take screenshot after clicking
    await page.screenshot({
      path: 'tests/e2e/screenshots/help-sidebar-05-after-click.png',
      fullPage: true
    })

    // Check if sidebar appeared - look for the header title
    const sidebar = page.locator('text="Dashboard Overview"')
    await expect(sidebar).toBeVisible({ timeout: 5000 })

    // Take screenshot of the sidebar open
    await page.screenshot({
      path: 'tests/e2e/screenshots/help-sidebar-06-sidebar-open.png',
      fullPage: true
    })
  })

  test('should show contextual help content based on current page', async ({ page }) => {
    await loginAndNavigate(page)

    // Navigate to People page
    await page.goto(`${BASE_URL}/app/people`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Find and click help button
    let helpButton = page.locator('button[title*="Help"]').first()
    if (await helpButton.count() === 0) {
      helpButton = page.locator('button:has-text("?")').first()
    }

    const buttonCount = await helpButton.count()
    expect(buttonCount).toBeGreaterThan(0)

    await helpButton.click()
    await page.waitForTimeout(500)

    // Check if People-specific help content is shown
    const peopleHelp = page.locator('text="People Management"')
    await expect(peopleHelp).toBeVisible({ timeout: 5000 })

    await page.screenshot({
      path: 'tests/e2e/screenshots/help-sidebar-07-people-page.png',
      fullPage: true
    })
  })

  test('should close sidebar when clicking close button', async ({ page }) => {
    await loginAndNavigate(page)
    await page.waitForTimeout(2000)

    // Open sidebar first
    let helpButton = page.locator('button[title*="Help"]').first()
    if (await helpButton.count() === 0) {
      helpButton = page.locator('button:has-text("?")').first()
    }

    expect(await helpButton.count()).toBeGreaterThan(0)

    // Open sidebar
    await helpButton.click()
    await page.waitForTimeout(500)

    // Find close button (X icon)
    const closeButton = page.locator('button[title*="Close"]').first()

    if (await closeButton.count() > 0) {
      await closeButton.click()
      await page.waitForTimeout(500)

      // Verify sidebar is closed (Dashboard Overview should not be visible)
      const sidebarContent = page.locator('text="Dashboard Overview"')
      await expect(sidebarContent).not.toBeVisible()

      await page.screenshot({
        path: 'tests/e2e/screenshots/help-sidebar-08-closed.png',
        fullPage: true
      })
    }
  })

  test('should check all help-related elements in DOM', async ({ page }) => {
    await loginAndNavigate(page)
    await page.waitForTimeout(3000)

    // Get all buttons and their text content
    const buttonCount = await page.locator('button').count()
    console.log(`Total buttons on page: ${buttonCount}`)

    // Check for elements with z-index
    const highZIndex = await page.evaluate(() => {
      const elements = document.querySelectorAll('*')
      const highZ: string[] = []
      elements.forEach(el => {
        const style = window.getComputedStyle(el)
        const zIndex = parseInt(style.zIndex || '0')
        if (zIndex > 100) {
          const className = typeof (el as HTMLElement).className === 'string'
            ? (el as HTMLElement).className
            : ''
          highZ.push(`${el.tagName}.${className.substring(0, 50)} - z-index: ${zIndex}`)
        }
      })
      return highZ
    })
    console.log('Elements with high z-index:', highZIndex)

    // Check for elements on the right edge
    const rightElements = await page.evaluate(() => {
      const elements = document.querySelectorAll('*')
      const rightEdge: string[] = []
      elements.forEach(el => {
        const rect = el.getBoundingClientRect()
        if (rect.right >= window.innerWidth - 10 && rect.width > 0 && rect.height > 0) {
          const className = typeof (el as HTMLElement).className === 'string'
            ? (el as HTMLElement).className
            : ''
          rightEdge.push(`${el.tagName}.${className.substring(0, 50)}`)
        }
      })
      return rightEdge.slice(0, 20)
    })
    console.log('Elements on right edge:', rightElements)

    await page.screenshot({
      path: 'tests/e2e/screenshots/help-sidebar-09-dom-check.png',
      fullPage: true
    })
  })
})
