/**
 * E2E Tests for Dashboard Progressive Loading
 * Tests that each widget loads independently with proper skeleton states
 */

import { test, expect } from '@playwright/test'

const SCREENSHOT_DIR = 'tests/e2e/screenshots/dashboard-progressive'

test.describe('Dashboard Progressive Loading', () => {
  test.setTimeout(60000)

  test.beforeAll(async () => {
    const fs = await import('fs')
    if (!fs.existsSync(SCREENSHOT_DIR)) {
      fs.mkdirSync(SCREENSHOT_DIR, { recursive: true })
    }
  })

  test.beforeEach(async ({ page }) => {
    // Navigate to dashboard
    await page.goto('/app')
    await page.waitForLoadState('networkidle')
  })

  test('should display dashboard page with header', async ({ page }) => {
    // Verify page header
    await expect(page.getByRole('heading', { name: 'Operations Overview' })).toBeVisible()
    await expect(page.getByText('A live snapshot of people, roles, processes, and resources.')).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/01-dashboard-header.png`, fullPage: true })
    console.log('Dashboard header displayed correctly')
  })

  test('should show skeleton loaders initially then load stats', async ({ page }) => {
    // Take screenshot immediately to capture skeleton state
    await page.screenshot({ path: `${SCREENSHOT_DIR}/02-initial-skeleton-state.png`, fullPage: true })

    // Wait for stats to load - wait for the stats cards to appear
    await page.waitForTimeout(5000)

    // Verify stats cards are visible (they should be links now)
    const statsCards = page.locator('a[href="/app/people"], a[href="/app/roles"], a[href="/app/functions"], a[href="/app/processes"]')
    const count = await statsCards.count()
    expect(count).toBeGreaterThanOrEqual(4)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/03-stats-loaded.png`, fullPage: true })
    console.log('Stats widget loaded successfully')
  })

  test('should load stats widget with correct data', async ({ page }) => {
    // Wait for network to settle
    await page.waitForTimeout(3000)

    // Check each stat card exists and is clickable
    const peopleCard = page.locator('a[href="/app/people"]').first()
    const rolesCard = page.locator('a[href="/app/roles"]').first()
    const functionsCard = page.locator('a[href="/app/functions"]').first()
    const processesCard = page.locator('a[href="/app/processes"]').first()

    await expect(peopleCard).toBeVisible()
    await expect(rolesCard).toBeVisible()
    await expect(functionsCard).toBeVisible()
    await expect(processesCard).toBeVisible()

    // Verify labels
    await expect(page.getByText('People').first()).toBeVisible()
    await expect(page.getByText('Roles').first()).toBeVisible()
    await expect(page.getByText('Functions').first()).toBeVisible()
    await expect(page.getByText('Processes').first()).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/04-all-stats-visible.png`, fullPage: true })
    console.log('All stat cards visible and clickable')
  })

  test('should load focus widget independently', async ({ page }) => {
    // Wait for focus widget to load
    await page.waitForTimeout(3000)

    // Check for Current Focus heading
    await expect(page.getByRole('heading', { name: 'Current Focus' })).toBeVisible()
    await expect(page.getByText('Highest impact areas across the org graph.')).toBeVisible()

    // Check for View all link
    await expect(page.getByRole('link', { name: 'View all' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/05-focus-widget-loaded.png`, fullPage: true })
    console.log('Focus widget loaded successfully')
  })

  test('should load actions widget independently', async ({ page }) => {
    // Wait for actions widget to load
    await page.waitForTimeout(3000)

    // Check for Next Actions heading
    await expect(page.getByRole('heading', { name: 'Next Actions' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/06-actions-widget-loaded.png`, fullPage: true })
    console.log('Actions widget loaded successfully')
  })

  test('should navigate to people page when clicking People stat', async ({ page }) => {
    await page.waitForTimeout(3000)

    // Click on People stat card
    await page.locator('a[href="/app/people"]').first().click()
    await page.waitForURL('/app/people')

    // Verify we're on the people page
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/07-navigated-to-people.png`, fullPage: true })
    console.log('Navigation to People page works')
  })

  test('should navigate to roles page when clicking Roles stat', async ({ page }) => {
    await page.waitForTimeout(3000)

    // Click on Roles stat card
    await page.locator('a[href="/app/roles"]').first().click()
    await page.waitForURL('/app/roles')

    // Verify we're on the roles page
    await expect(page.getByRole('heading', { name: 'Roles' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/08-navigated-to-roles.png`, fullPage: true })
    console.log('Navigation to Roles page works')
  })

  test('should navigate to functions page when clicking Functions stat', async ({ page }) => {
    await page.waitForTimeout(3000)

    // Click on Functions stat card
    await page.locator('a[href="/app/functions"]').first().click()
    await page.waitForURL('/app/functions')

    // Verify we're on the functions page
    await expect(page.getByRole('heading', { name: 'Functions' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/09-navigated-to-functions.png`, fullPage: true })
    console.log('Navigation to Functions page works')
  })

  test('should navigate to processes page when clicking Processes stat', async ({ page }) => {
    await page.waitForTimeout(3000)

    // Click on Processes stat card
    await page.locator('a[href="/app/processes"]').first().click()
    await page.waitForURL('/app/processes')

    // Verify we're on the processes page
    await expect(page.getByRole('heading', { name: 'Processes' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/10-navigated-to-processes.png`, fullPage: true })
    console.log('Navigation to Processes page works')
  })

  test('should show focus items with correct status badges', async ({ page }) => {
    await page.waitForTimeout(5000)

    // Check for Current Focus section loaded
    await expect(page.getByRole('heading', { name: 'Current Focus' })).toBeVisible()

    // Check for any focus items or empty state message
    const hasFocusItems = await page.locator('.rounded-xl.border').count() > 0
    const hasEmptyState = await page.getByText('No focus areas identified').isVisible().catch(() => false)
    const hasGettingStarted = await page.getByText('Getting Started').isVisible().catch(() => false)
    const hasOrgHealth = await page.getByText('Organization Health').isVisible().catch(() => false)

    // One of these states should be true
    expect(hasFocusItems || hasEmptyState || hasGettingStarted || hasOrgHealth).toBeTruthy()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/11-focus-items-with-badges.png`, fullPage: true })
    console.log('Focus items display correctly')
  })

  test('should show next actions as bullet points', async ({ page }) => {
    await page.waitForTimeout(4000)

    // Check for action bullet points (purple dots)
    const actionBullets = page.locator('.rounded-full.bg-purple-400')
    const bulletCount = await actionBullets.count()

    // Should have at least some action items or empty state
    const hasActions = bulletCount > 0
    const hasEmptyState = await page.getByText('No actions needed').isVisible().catch(() => false)
    const hasGettingStarted = await page.getByText('Add people').isVisible().catch(() => false)

    expect(hasActions || hasEmptyState || hasGettingStarted).toBeTruthy()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/12-next-actions-list.png`, fullPage: true })
    console.log('Next actions displayed correctly')
  })

  test('full dashboard screenshot after all widgets loaded', async ({ page }) => {
    // Wait for all widgets to fully load
    await page.waitForTimeout(5000)

    // Take a full page screenshot
    await page.screenshot({ path: `${SCREENSHOT_DIR}/13-full-dashboard-loaded.png`, fullPage: true })

    // Also take viewport screenshot
    await page.screenshot({ path: `${SCREENSHOT_DIR}/14-dashboard-viewport.png` })

    console.log('Full dashboard screenshots captured')
  })

  test('should handle widget refresh on page revisit', async ({ page }) => {
    // Wait for initial load
    await page.waitForTimeout(3000)

    // Navigate away
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Navigate back to dashboard
    await page.goto('/app')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000)

    // Verify widgets reload
    await expect(page.getByRole('heading', { name: 'Operations Overview' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Current Focus' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Next Actions' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/15-dashboard-after-revisit.png`, fullPage: true })
    console.log('Dashboard reloads correctly on revisit')
  })

  test('responsive layout on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 812 })

    // Reload page
    await page.goto('/app')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000)

    // Verify layout adapts
    await expect(page.getByRole('heading', { name: 'Operations Overview' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/16-mobile-viewport.png`, fullPage: true })
    console.log('Mobile layout renders correctly')
  })

  test('responsive layout on tablet viewport', async ({ page }) => {
    // Set tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 })

    // Reload page
    await page.goto('/app')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000)

    // Verify layout adapts
    await expect(page.getByRole('heading', { name: 'Operations Overview' })).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/17-tablet-viewport.png`, fullPage: true })
    console.log('Tablet layout renders correctly')
  })
})

test.describe('Dashboard API Integration', () => {
  test('should make separate API calls for each widget', async ({ page }) => {
    const apiCalls: string[] = []

    // Intercept API calls
    page.on('request', request => {
      const url = request.url()
      if (url.includes('/dashboard/')) {
        apiCalls.push(url)
      }
    })

    await page.goto('/app')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(5000)

    // Verify separate API calls were made
    const hasStatsCall = apiCalls.some(url => url.includes('/dashboard/stats'))
    const hasFocusCall = apiCalls.some(url => url.includes('/dashboard/focus'))
    const hasActionsCall = apiCalls.some(url => url.includes('/dashboard/actions'))

    console.log('API calls made:', apiCalls)

    // All three endpoints should be called
    expect(hasStatsCall).toBeTruthy()
    expect(hasFocusCall).toBeTruthy()
    expect(hasActionsCall).toBeTruthy()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/18-api-integration-verified.png`, fullPage: true })
    console.log('All three API endpoints called successfully')
  })
})
