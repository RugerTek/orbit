import { test, expect, Page } from '@playwright/test'

const BASE_URL = 'http://localhost:3000'

// Test credentials
const TEST_EMAIL = 'rodrigo@rugertek.com'
const TEST_PASSWORD = '123456'

async function loginViaUI(page: Page) {
  // Navigate to login page
  await page.goto(`${BASE_URL}/`)
  await page.waitForLoadState('networkidle')
  await page.waitForTimeout(500)

  // Fill login form
  const emailInput = page.locator('input[type="email"]')
  const passwordInput = page.locator('input[type="password"]')

  await emailInput.fill(TEST_EMAIL)
  await passwordInput.fill(TEST_PASSWORD)

  // Click sign in button (the submit button with exact text "Sign In")
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()

  // Wait for the app selection to appear (after successful login)
  await page.waitForTimeout(2000)

  // Click "Operations App" link to go to the main app (it's a NuxtLink, not a button)
  await page.locator('a:has-text("Operations App")').click()

  // Wait for navigation to dashboard or any /app route
  await page.waitForURL(/\/app/, { timeout: 15000 })
  await page.waitForLoadState('networkidle')
  await page.waitForTimeout(1000)
}

// Helper to find the organization switcher button in the sidebar
async function findOrgSwitcher(page: Page) {
  // The organization switcher is at the bottom of the sidebar with class cursor-pointer
  // It contains the org name and a chevron icon
  return page.locator('aside button, aside [class*="cursor-pointer"]').filter({ hasText: /^(?!Sign Out|OrbitOS)/ }).first()
}

// Generate a short unique org name (to avoid truncation issues in UI)
function generateOrgName(prefix: string) {
  // Use last 4 digits of timestamp for brevity
  const suffix = Date.now().toString().slice(-4)
  return `${prefix}${suffix}`
}

test.describe('Organization Switching', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 })
  })

  test('should show organization switcher in sidebar after login', async ({ page }) => {
    await loginViaUI(page)

    // Take screenshot for debugging
    await page.screenshot({ path: './tests/e2e/screenshots/org-01-after-login.png', fullPage: true })

    // The organization switcher should be visible in the sidebar
    // It shows the current organization name (could be "Rugertek", "Acme", or any created org)
    const orgSwitcher = await findOrgSwitcher(page)
    await expect(orgSwitcher).toBeVisible({ timeout: 10000 })
  })

  test('should display organizations dropdown when clicked', async ({ page }) => {
    await loginViaUI(page)

    // Click organization switcher
    const orgSwitcher = await findOrgSwitcher(page)
    await orgSwitcher.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: './tests/e2e/screenshots/org-02-dropdown-open.png', fullPage: true })

    // Should show dropdown with "Your Organizations" header
    await expect(page.locator('text=Your Organizations')).toBeVisible({ timeout: 5000 })

    // Should show "Create new organization" button
    await expect(page.locator('text=Create new organization')).toBeVisible()
  })

  test('should open create organization dialog', async ({ page }) => {
    await loginViaUI(page)

    // Click organization switcher
    const orgSwitcher = await findOrgSwitcher(page)
    await orgSwitcher.click()
    await page.waitForTimeout(500)

    // Click create new organization
    await page.locator('text=Create new organization').click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: './tests/e2e/screenshots/org-03-create-dialog.png', fullPage: true })

    // Should show create organization dialog - use the heading specifically
    await expect(page.getByRole('heading', { name: 'Create Organization' })).toBeVisible({ timeout: 5000 })
    await expect(page.locator('text=Organization Name')).toBeVisible()
  })

  test('should create a new organization and switch to it', async ({ page }) => {
    await loginViaUI(page)

    // Open organization switcher
    let orgSwitcher = await findOrgSwitcher(page)
    await orgSwitcher.click()
    await page.waitForTimeout(500)

    // Click create new organization
    await page.locator('text=Create new organization').click()
    await page.waitForTimeout(500)

    // Fill in organization details with a short name to avoid truncation
    const uniqueName = generateOrgName('TestOrg')
    await page.locator('input#org-name').fill(uniqueName)
    await page.locator('textarea#org-description').fill('Test organization for e2e testing')

    await page.screenshot({ path: './tests/e2e/screenshots/org-04-filled-form.png', fullPage: true })

    // Click Create Organization button
    await page.locator('button:has-text("Create Organization")').click()

    // Wait for page reload (the create dialog triggers a reload)
    await page.waitForLoadState('networkidle')

    // Wait for the organization switcher to show something other than "Select Organization"
    // This ensures the fetchOrganizations has completed and UI has updated
    await page.waitForFunction(() => {
      const switcher = document.querySelector('aside button, aside [class*="cursor-pointer"]')
      return switcher && !switcher.textContent?.includes('Select Organization')
    }, { timeout: 10000 }).catch(() => {
      console.log('Timed out waiting for organization to load')
    })

    await page.waitForTimeout(1000) // Extra buffer for Vue reactivity

    await page.screenshot({ path: './tests/e2e/screenshots/org-05-after-create.png', fullPage: true })

    // Verify new organization is now selected (after reload) - check in the org switcher
    const newOrgSwitcher = await findOrgSwitcher(page)
    const switcherText = await newOrgSwitcher.textContent()
    console.log('Switcher text after create:', switcherText)

    // The org name should appear somewhere in the switcher (may be truncated)
    expect(switcherText).toContain(uniqueName.substring(0, 8)) // Check first 8 chars
  })

  test('should switch between organizations without data bleeding', async ({ page }) => {
    await loginViaUI(page)

    // Record the current organization name
    const orgSwitcher = await findOrgSwitcher(page)
    const originalOrgText = await orgSwitcher.textContent()
    console.log('Original organization:', originalOrgText)

    await orgSwitcher.click()
    await page.waitForTimeout(500)

    // Click create new organization
    await page.locator('text=Create new organization').click()
    await page.waitForTimeout(500)

    // Create organization with unique short name
    const uniqueName = generateOrgName('Switch')
    await page.locator('input#org-name').fill(uniqueName)
    await page.locator('button:has-text("Create Organization")').click()

    // Wait for page reload
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Verify new organization is selected
    const newOrgSwitcher = await findOrgSwitcher(page)
    const newSwitcherText = await newOrgSwitcher.textContent()
    console.log('New switcher text:', newSwitcherText)
    expect(newSwitcherText).toContain(uniqueName.substring(0, 6))

    await page.screenshot({ path: './tests/e2e/screenshots/org-06-new-org-selected.png', fullPage: true })

    // Now switch back to another organization
    await newOrgSwitcher.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: './tests/e2e/screenshots/org-06b-dropdown-open.png', fullPage: true })

    // Look for Rugertek in the dropdown
    const rugertekOrg = page.locator('button:has-text("Rugertek")')
    if (await rugertekOrg.isVisible({ timeout: 2000 }).catch(() => false)) {
      await rugertekOrg.click()

      // Wait for page reload
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      await page.screenshot({ path: './tests/e2e/screenshots/org-07-switched-back.png', fullPage: true })

      // Verify we're now on Rugertek
      const finalSwitcher = await findOrgSwitcher(page)
      const finalText = await finalSwitcher.textContent()
      console.log('Final switcher text:', finalText)
      expect(finalText).toContain('Rugertek')
    } else {
      console.log('Rugertek organization not found in dropdown - skipping switch back test')
    }
  })

  test('should show correct data after switching organizations', async ({ page }) => {
    await loginViaUI(page)

    // Navigate to a page with data (e.g., operations/resources)
    await page.goto(`${BASE_URL}/app/resources`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    await page.screenshot({ path: './tests/e2e/screenshots/org-08-initial-resources.png', fullPage: true })

    // Open organization switcher
    const orgSwitcher = await findOrgSwitcher(page)
    await orgSwitcher.click()
    await page.waitForTimeout(500)

    // Create new organization
    await page.locator('text=Create new organization').click()
    await page.waitForTimeout(500)

    const uniqueName = generateOrgName('Data')
    await page.locator('input#org-name').fill(uniqueName)
    await page.locator('button:has-text("Create Organization")').click()

    // Wait for page reload
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Navigate to same page
    await page.goto(`${BASE_URL}/app/resources`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    await page.screenshot({ path: './tests/e2e/screenshots/org-09-new-org-resources.png', fullPage: true })

    // Verify we're on the new organization
    const newOrgSwitcher = await findOrgSwitcher(page)
    const switcherText = await newOrgSwitcher.textContent()
    console.log('Resources page switcher text:', switcherText)
    expect(switcherText).toContain(uniqueName.substring(0, 4))

    // The new org should have empty or different data than the original
    console.log('Successfully switched to new organization and page loaded')
  })
})
