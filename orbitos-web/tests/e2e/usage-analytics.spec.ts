import { test, expect } from '@playwright/test'
import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

// Screenshot directory
const screenshotDir = path.join(__dirname, 'screenshots', 'usage-analytics')

// Ensure screenshot directory exists
test.beforeAll(() => {
  if (!fs.existsSync(screenshotDir)) {
    fs.mkdirSync(screenshotDir, { recursive: true })
  }
})

test.describe('Usage Analytics Feature', () => {
  // Login before each test
  test.beforeEach(async ({ page }) => {
    await page.goto('/login')
    await page.fill('input[type="email"]', 'rodrigo@rugertek.com')
    await page.fill('input[type="password"]', '123456')
    await page.click('button[type="submit"]')
    await page.waitForURL(/\/app/, { timeout: 15000 })
  })

  test.describe('Organization Usage Analytics Page', () => {
    test('should navigate to usage analytics from settings', async ({ page }) => {
      // Go to settings
      await page.goto('/app/settings')
      await page.waitForLoadState('networkidle')

      // Take screenshot of settings page
      await page.screenshot({ path: path.join(screenshotDir, '01-settings-page.png'), fullPage: true })

      // Click on AI Usage Analytics link
      const usageLink = page.locator('a[href="/app/settings/usage"]')
      await expect(usageLink).toBeVisible()
      await usageLink.click()

      await page.waitForURL('/app/settings/usage')
      await page.waitForLoadState('networkidle')
    })

    test('should display organization usage analytics correctly', async ({ page }) => {
      await page.goto('/app/settings/usage')
      await page.waitForLoadState('networkidle')

      // Wait for data to load
      await page.waitForTimeout(2000)

      // Take screenshot of usage page
      await page.screenshot({ path: path.join(screenshotDir, '02-org-usage-page.png'), fullPage: true })

      // Verify page title
      await expect(page.locator('h1')).toContainText('AI Usage Analytics')

      // Verify summary cards are visible
      await expect(page.locator('text=Total Tokens')).toBeVisible()
      await expect(page.locator('text=Total Cost')).toBeVisible()
      await expect(page.locator('text=AI Responses')).toBeVisible()
      await expect(page.locator('text=Conversations')).toBeVisible()
    })

    test('should display usage by model section', async ({ page }) => {
      await page.goto('/app/settings/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Check Usage by Model section
      await expect(page.locator('text=Usage by Model')).toBeVisible()

      // Take screenshot showing model breakdown
      await page.screenshot({ path: path.join(screenshotDir, '03-usage-by-model.png'), fullPage: true })
    })

    test('should display usage by agent table', async ({ page }) => {
      await page.goto('/app/settings/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Check Usage by Agent section
      await expect(page.locator('text=Usage by Agent')).toBeVisible()

      // Verify table headers
      await expect(page.locator('th:has-text("Agent")')).toBeVisible()
      await expect(page.locator('th:has-text("Responses")')).toBeVisible()
      await expect(page.locator('th:has-text("Tokens")')).toBeVisible()
      await expect(page.locator('th:has-text("Cost")')).toBeVisible()

      // Take screenshot of agent table
      await page.screenshot({ path: path.join(screenshotDir, '04-usage-by-agent.png'), fullPage: true })
    })

    test('should allow date range filtering', async ({ page }) => {
      await page.goto('/app/settings/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Check date range buttons are visible
      await expect(page.locator('button:has-text("Last 7 days")')).toBeVisible()
      await expect(page.locator('button:has-text("Last 30 days")')).toBeVisible()
      await expect(page.locator('button:has-text("Last 90 days")')).toBeVisible()
      await expect(page.locator('button:has-text("All time")')).toBeVisible()

      // Click 7 days filter
      await page.click('button:has-text("Last 7 days")')
      await page.waitForTimeout(1500)

      await page.screenshot({ path: path.join(screenshotDir, '05-7-day-filter.png'), fullPage: true })

      // Click 90 days filter
      await page.click('button:has-text("Last 90 days")')
      await page.waitForTimeout(1500)

      await page.screenshot({ path: path.join(screenshotDir, '06-90-day-filter.png'), fullPage: true })
    })

    test('should display daily usage chart', async ({ page }) => {
      await page.goto('/app/settings/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Verify daily usage chart section
      await expect(page.locator('text=Daily Token Usage')).toBeVisible()

      // Take screenshot focusing on chart area
      await page.screenshot({ path: path.join(screenshotDir, '07-daily-usage-chart.png'), fullPage: true })
    })
  })

  test.describe('Super Admin Global Usage Dashboard', () => {
    // Note: This requires super admin access
    test('should display global usage dashboard', async ({ page }) => {
      // Navigate directly to admin usage page
      await page.goto('/admin/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Take screenshot of global usage dashboard
      await page.screenshot({ path: path.join(screenshotDir, '08-admin-global-usage.png'), fullPage: true })

      // Verify page title
      await expect(page.locator('h1')).toContainText('Global AI Usage Analytics')

      // Verify global summary cards
      await expect(page.locator('text=Total Tokens')).toBeVisible()
      await expect(page.locator('text=Total Cost')).toBeVisible()
      await expect(page.locator('text=AI Responses')).toBeVisible()
      await expect(page.locator('text=Active Orgs')).toBeVisible()
    })

    test('should display usage by provider', async ({ page }) => {
      await page.goto('/admin/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Check Usage by Provider section
      await expect(page.locator('text=Usage by Provider')).toBeVisible()

      // Take screenshot showing provider breakdown
      await page.screenshot({ path: path.join(screenshotDir, '09-usage-by-provider.png'), fullPage: true })
    })

    test('should display usage by organization', async ({ page }) => {
      await page.goto('/admin/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Check Usage by Organization section
      await expect(page.locator('text=Usage by Organization')).toBeVisible()

      // Verify org table headers
      await expect(page.locator('th:has-text("Organization")')).toBeVisible()
      await expect(page.locator('th:has-text("Share")')).toBeVisible()

      // Take screenshot of organization breakdown
      await page.screenshot({ path: path.join(screenshotDir, '10-usage-by-organization.png'), fullPage: true })
    })

    test('should display usage by model grid', async ({ page }) => {
      await page.goto('/admin/usage')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      // Check Usage by Model section
      await expect(page.locator('h3:has-text("Usage by Model")')).toBeVisible()

      // Take screenshot of model grid
      await page.screenshot({ path: path.join(screenshotDir, '11-admin-usage-by-model.png'), fullPage: true })
    })
  })
})
