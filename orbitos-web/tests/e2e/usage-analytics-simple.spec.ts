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

test.describe('Usage Analytics Screenshots', () => {
  // Set up auth by calling API directly and setting localStorage
  test.beforeEach(async ({ page, context }) => {
    // First call the login API to get a token
    const loginResponse = await page.request.post('http://localhost:5027/api/auth/login', {
      data: {
        email: 'rodrigo@rugertek.com',
        password: '123456'
      }
    })
    const loginData = await loginResponse.json()

    // Set up localStorage with auth data using CORRECT KEYS (orbitos-token and orbitos-user)
    await page.goto('/')
    await page.evaluate((data) => {
      // Use the correct localStorage keys that useAuth.ts expects
      localStorage.setItem('orbitos-token', data.token)
      localStorage.setItem('orbitos-user', JSON.stringify({
        email: data.email,
        displayName: data.displayName,
        token: data.token
      }))
    }, loginData)

    // Now fetch organizations with the auth token
    const orgsResponse = await page.request.get('http://localhost:5027/api/auth/my-organizations', {
      headers: {
        Authorization: `Bearer ${loginData.token}`
      }
    })
    const orgsData = await orgsResponse.json()

    // Find Rugertek org (has actual usage data) and set as current
    const rugertek = orgsData.find((o: { name: string }) => o.name === 'Rugertek')
    const orgToUse = rugertek || orgsData[0]

    if (orgToUse) {
      await page.evaluate((orgId) => {
        localStorage.setItem('currentOrganizationId', orgId)
      }, orgToUse.id)
    }

    // Navigate to app to trigger org loading - the app layout will fetch orgs on mount
    await page.goto('/app')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000) // Let org context initialize and fetch complete
  })

  test('take org usage page screenshot', async ({ page }) => {
    // Navigate to app first to let organizations load
    await page.goto('/app', { waitUntil: 'networkidle' })
    await page.waitForTimeout(2000)

    // Check if org switcher shows an organization name (indicating orgs were loaded)
    const orgSwitcherText = await page.locator('button:has-text("Rugertek"), button:has-text("Select Organization")').first().textContent().catch(() => 'not found')
    console.log('Org switcher text:', orgSwitcherText)

    // Now navigate to usage page
    await page.goto('/app/usage', { waitUntil: 'networkidle' })
    await page.waitForTimeout(3000)

    // Check page content
    const url = page.url()
    console.log('Current URL:', url)

    const h1Text = await page.locator('h1').first().textContent()
    console.log('H1 Text:', h1Text)

    // Check if we have the error state or data
    const errorText = await page.locator('text=No organization selected').count()
    const totalTokensText = await page.locator('text=Total Tokens').count()
    console.log('Error present:', errorText > 0)
    console.log('Data present:', totalTokensText > 0)

    await page.screenshot({
      path: path.join(screenshotDir, '01-org-usage-overview.png'),
      fullPage: true
    })

    console.log('Screenshot saved: 01-org-usage-overview.png')
  })

  test('take settings page with usage link screenshot', async ({ page }) => {
    await page.goto('/app/settings')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    await page.screenshot({
      path: path.join(screenshotDir, '02-settings-with-usage-link.png'),
      fullPage: true
    })

    console.log('Screenshot saved: 02-settings-with-usage-link.png')
  })

  test('take admin usage page screenshot', async ({ page }) => {
    await page.goto('/admin/usage')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000) // Wait for data to load

    await page.screenshot({
      path: path.join(screenshotDir, '03-admin-global-usage.png'),
      fullPage: true
    })

    console.log('Screenshot saved: 03-admin-global-usage.png')
  })

  test('take admin dashboard screenshot', async ({ page }) => {
    await page.goto('/admin')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    await page.screenshot({
      path: path.join(screenshotDir, '04-admin-dashboard.png'),
      fullPage: true
    })

    console.log('Screenshot saved: 04-admin-dashboard.png')
  })
})
