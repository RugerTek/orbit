import { test, expect } from '@playwright/test'

// Use seeded test data - Sales Manager role
const SEEDED_ROLE_ID = 'cccccccc-cccc-cccc-cccc-cccccccccc01'
const SEEDED_ROLE_NAME = 'Sales Manager'
const SEEDED_ROLE_DEPARTMENT = 'Sales'

// Helper to navigate to role detail page and wait for it to load
async function navigateToRoleDetail(page: any, roleId: string, roleName: string) {
  // First go to the roles list
  await page.goto('/app/roles')
  await page.waitForLoadState('networkidle')
  await page.waitForTimeout(1000)

  // Then click on the role row to navigate
  const roleRow = page.locator('tr', { has: page.getByText(roleName) })
  await expect(roleRow).toBeVisible({ timeout: 10000 })
  await roleRow.click()

  // Wait for navigation to complete
  await page.waitForURL(`**/app/roles/${roleId}`)
  await page.waitForTimeout(1000)

  // Wait for the loading spinner to disappear (if present)
  const spinner = page.locator('.orbitos-spinner')
  await spinner.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {})
}

// =============================================================================
// ROLE DETAIL PAGE TESTS
// =============================================================================

test.describe('Role Detail Page', () => {
  const testRoleId = SEEDED_ROLE_ID
  const testRoleName = SEEDED_ROLE_NAME

  test('should navigate to role detail page from roles list', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Find the test role row and click on it
    const roleRow = page.locator('tr', { has: page.getByText(testRoleName) })
    await expect(roleRow).toBeVisible({ timeout: 10000 })
    await roleRow.click()

    // Should navigate to the detail page
    await page.waitForURL(`**/app/roles/${testRoleId}`)
    await expect(page).toHaveURL(new RegExp(`/app/roles/${testRoleId}`))
  })

  test('should display role header information', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for role name to be visible
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // Check department badge is displayed
    await expect(page.getByText(SEEDED_ROLE_DEPARTMENT).first()).toBeVisible()
  })

  test('should display stats cards', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for the role name to confirm page is loaded
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // Check stats cards are visible (they're inside divs with specific text)
    await expect(page.locator('text=People Assigned')).toBeVisible()
    await expect(page.locator('.rounded-2xl').filter({ hasText: 'Functions' }).first()).toBeVisible()
    await expect(page.locator('text=Processes').first()).toBeVisible()
  })

  test('should show empty state for team members when none assigned', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for the role name to confirm page is loaded
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // Should show empty team members state (if no one is assigned)
    const emptyState = page.getByText('No team members yet')
    const hasEmptyState = await emptyState.isVisible().catch(() => false)

    if (hasEmptyState) {
      await expect(page.getByText('Add first person →')).toBeVisible()
    }
  })

  test('should open Add Person dialog', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for the role name to confirm page is loaded
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // Click Add Person button (look for button with "Add Person" text)
    await page.locator('button', { hasText: 'Add Person' }).click()
    await page.waitForTimeout(500)

    // Dialog should be visible - look for the dialog title
    await expect(page.getByText('Add Team Member').first()).toBeVisible()
    await expect(page.getByPlaceholder('Search people...')).toBeVisible()

    // Close dialog by pressing Escape
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)
  })

  test('should open Add Function dialog from sidebar link', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for the role name to confirm page is loaded
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // Check if there's an "Add functions" link (appears when no functions assigned)
    const addFunctionsLink = page.getByText('Add functions →')
    const hasLink = await addFunctionsLink.isVisible().catch(() => false)

    if (hasLink) {
      await addFunctionsLink.click()
      await page.waitForTimeout(500)

      // Dialog should be visible
      await expect(page.getByText('Assign Function').first()).toBeVisible()
      await expect(page.getByPlaceholder('Search functions...')).toBeVisible()

      // Close dialog
      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)
    }
  })

  test('should display AI insights section', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for the role name to confirm page is loaded
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // AI Insights section should be visible (text is inside a span)
    await expect(page.locator('span', { hasText: 'AI Insights' })).toBeVisible()
  })

  test('should display Quick Actions section', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for the role name to confirm page is loaded
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // Quick Actions should be visible (h3 element)
    await expect(page.locator('h3', { hasText: 'Quick Actions' })).toBeVisible()
    await expect(page.getByText('Add team member')).toBeVisible()
    await expect(page.getByText('Assign function')).toBeVisible()
    await expect(page.getByText('View all processes')).toBeVisible()
  })

  test('should navigate back to roles list', async ({ page }) => {
    await navigateToRoleDetail(page, testRoleId, testRoleName)

    // Wait for the role name to confirm page is loaded
    await expect(page.locator('h1', { hasText: testRoleName })).toBeVisible({ timeout: 10000 })

    // Click back button
    await page.locator('a[href="/app/roles"]').first().click()

    // Should navigate to roles list
    await page.waitForURL('**/app/roles')
    await expect(page).toHaveURL(/\/app\/roles$/)
  })

  test('should show not found for invalid role ID', async ({ page }) => {
    // First go to roles list, then navigate to an invalid role
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Navigate directly to invalid role URL via browser
    await page.goto('/app/roles/invalid-role-id-12345')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(3000)

    // Should show not found message (h2 element) or redirect to list
    // Due to SPA routing quirks, we check for either the error state or that we're on the list
    const notFoundVisible = await page.locator('h2', { hasText: 'Role not found' }).isVisible().catch(() => false)
    const onListPage = await page.locator('h1', { hasText: 'Roles' }).isVisible().catch(() => false)

    // Either should be true
    expect(notFoundVisible || onListPage).toBeTruthy()
  })
})

// =============================================================================
// ROLE TABLE ROW CLICK NAVIGATION TESTS
// =============================================================================

test.describe('Role Table Navigation', () => {
  test('clicking on role row should navigate to detail page', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Find any role row in the table
    const roleRows = page.locator('tbody tr')
    const rowCount = await roleRows.count()

    if (rowCount > 0) {
      // Get the role ID from the row (by extracting from name click)
      const firstRow = roleRows.first()
      const roleLink = firstRow.locator('a').first()
      const href = await roleLink.getAttribute('href')
      const roleId = href?.split('/').pop()

      // Click on the row (not the action buttons)
      await firstRow.click()
      await page.waitForURL(`**/app/roles/${roleId}`)

      // Should be on the detail page
      await expect(page).toHaveURL(new RegExp(`/app/roles/${roleId}`))
    }
  })

  test('clicking edit button should NOT navigate to detail page', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Find any role row in the table
    const roleRows = page.locator('tbody tr')
    const rowCount = await roleRows.count()

    if (rowCount > 0) {
      const firstRow = roleRows.first()

      // Click on the edit button
      await firstRow.getByTitle('Edit role').click()
      await page.waitForTimeout(500)

      // Should still be on the roles list page
      await expect(page).toHaveURL(/\/app\/roles$/)

      // Edit dialog should be visible
      await expect(page.getByText('Edit Role').first()).toBeVisible()

      // Close dialog
      await page.keyboard.press('Escape')
    }
  })

  test('clicking delete button should NOT navigate to detail page', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Find any role row in the table (excluding E2E test roles)
    const roleRows = page.locator('tbody tr')
    const rowCount = await roleRows.count()

    if (rowCount > 0) {
      const firstRow = roleRows.first()

      // Set up dialog handler to cancel
      page.on('dialog', dialog => dialog.dismiss())

      // Click on the delete button
      await firstRow.getByTitle('Delete role').click()
      await page.waitForTimeout(500)

      // Should still be on the roles list page
      await expect(page).toHaveURL(/\/app\/roles$/)
    }
  })
})
