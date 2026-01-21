/**
 * Test to validate the Torus organization's org chart rendering
 */

import { test, expect } from '@playwright/test'

const SCREENSHOT_DIR = 'tests/e2e/screenshots'

test.describe('Torus Organization Org Chart', () => {
  test.setTimeout(90000)

  test('switch to Torus org and validate org chart', async ({ page }) => {
    // Go to the app
    await page.goto('/app')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(3000)

    // Take initial screenshot
    await page.screenshot({ path: `${SCREENSHOT_DIR}/torus-01-initial.png`, fullPage: true })

    // Log current localStorage org
    const initialOrg = await page.evaluate(() => localStorage.getItem('currentOrganizationId'))
    console.log('Initial org in localStorage:', initialOrg)

    // Look for organization switcher in sidebar
    const sidebar = page.locator('aside')
    await page.screenshot({ path: `${SCREENSHOT_DIR}/torus-02-sidebar.png` })

    // Find and click on organization dropdown/switcher button
    // Look for elements that might be the org switcher
    const orgSwitcherBtns = await page.locator('aside button').all()
    console.log('Found', orgSwitcherBtns.length, 'buttons in sidebar')

    // Find a button with org-related text or the OrganizationSwitcher component
    const orgDropdown = page.locator('aside').locator('button').filter({
      hasText: /Select|Rugerteks|Torus|Organization|Switch/i
    }).first()

    if (await orgDropdown.isVisible().catch(() => false)) {
      console.log('Found org dropdown button')
      await orgDropdown.click()
      await page.waitForTimeout(500)
      await page.screenshot({ path: `${SCREENSHOT_DIR}/torus-03-dropdown-open.png`, fullPage: true })

      // Look for Torus option
      const torusOption = page.locator('button, div, li, [role="option"]').filter({ hasText: /Torus/i }).first()
      if (await torusOption.isVisible().catch(() => false)) {
        console.log('Found Torus option, clicking...')
        await torusOption.click()
        await page.waitForTimeout(2000)
        await page.screenshot({ path: `${SCREENSHOT_DIR}/torus-04-after-switch.png`, fullPage: true })
      } else {
        console.log('Torus option not found in dropdown')
        // Log what options are visible
        const dropdownOptions = await page.locator('[role="listbox"] button, [role="menu"] button, .dropdown button').allTextContents()
        console.log('Dropdown options:', dropdownOptions)
      }
    } else {
      console.log('Org dropdown not found, checking for other elements')
    }

    // Check localStorage after switch
    const afterSwitchOrg = await page.evaluate(() => localStorage.getItem('currentOrganizationId'))
    console.log('After switch, org in localStorage:', afterSwitchOrg)

    // Navigate to org chart
    await page.goto('/app/people/org-chart')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(3000)

    // Take screenshot of org chart
    await page.screenshot({ path: `${SCREENSHOT_DIR}/torus-05-org-chart.png`, fullPage: true })

    // Check network requests to verify which org is being queried
    const currentOrgInStorage = await page.evaluate(() => localStorage.getItem('currentOrganizationId'))
    console.log('Current org when viewing org chart:', currentOrgInStorage)

    // Analyze what's displayed
    const pageAnalysis = await page.evaluate(() => {
      const main = document.querySelector('main')
      const metrics = main?.querySelectorAll('.orbitos-glass-subtle')
      const metricValues: string[] = []
      metrics?.forEach(m => {
        const value = m.querySelector('.text-2xl, .font-semibold')?.textContent?.trim()
        const label = m.querySelector('.text-xs, .uppercase')?.textContent?.trim()
        if (value && label) {
          metricValues.push(`${label}: ${value}`)
        }
      })

      // Check for empty state
      const hasEmptyState = !!main?.querySelector('[class*="empty"], .text-center')
      const emptyStateText = main?.querySelector('.text-center')?.textContent?.substring(0, 100)

      // Get page title/heading
      const heading = main?.querySelector('h1')?.textContent?.trim()

      return {
        heading,
        metricValues,
        hasEmptyState,
        emptyStateText,
        bodyText: main?.textContent?.substring(0, 500)
      }
    })

    console.log('Page analysis:', JSON.stringify(pageAnalysis, null, 2))

    // Take screenshot of main content
    const mainContent = page.locator('main')
    if (await mainContent.isVisible()) {
      await mainContent.screenshot({ path: `${SCREENSHOT_DIR}/torus-06-org-chart-main.png` })
    }
  })

  test('verify API requests use correct org ID after switch', async ({ page }) => {
    // Track API requests
    const apiRequests: { url: string; orgId?: string }[] = []

    page.on('request', request => {
      const url = request.url()
      if (url.includes('/api/organizations/')) {
        const match = url.match(/\/api\/organizations\/([^/]+)/)
        apiRequests.push({
          url,
          orgId: match?.[1]
        })
      }
    })

    // Go to app and wait for initial load
    await page.goto('/app')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(2000)

    // Try to find organizations in the dropdown
    const orgSwitcher = page.locator('aside button').filter({ hasText: /Select|Organization|Rugerteks|Torus/i }).first()

    if (await orgSwitcher.isVisible().catch(() => false)) {
      await orgSwitcher.click()
      await page.waitForTimeout(500)

      // Find and click Torus
      const torusBtn = page.locator('button').filter({ hasText: /^Torus$/i }).first()
      if (await torusBtn.isVisible().catch(() => false)) {
        await torusBtn.click()
        await page.waitForTimeout(2000)

        console.log('Switched to Torus')
      }
    }

    // Clear tracked requests
    apiRequests.length = 0

    // Navigate to org chart - this should trigger API calls
    await page.goto('/app/people/org-chart')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(3000)

    // Log the API requests made
    console.log('API requests after navigation:')
    apiRequests.forEach(req => {
      console.log(`  ${req.orgId} - ${req.url}`)
    })

    // Get current org from localStorage
    const currentOrg = await page.evaluate(() => localStorage.getItem('currentOrganizationId'))
    console.log('Current org ID in localStorage:', currentOrg)

    // Verify requests are using the correct org ID
    if (currentOrg) {
      const wrongOrgRequests = apiRequests.filter(r => r.orgId && r.orgId !== currentOrg)
      if (wrongOrgRequests.length > 0) {
        console.log('WARNING: Found requests to wrong organization:')
        wrongOrgRequests.forEach(r => console.log(`  ${r.url}`))
      } else {
        console.log('All API requests used the correct organization ID')
      }
    }

    await page.screenshot({ path: `${SCREENSHOT_DIR}/torus-07-api-test.png`, fullPage: true })
  })
})
