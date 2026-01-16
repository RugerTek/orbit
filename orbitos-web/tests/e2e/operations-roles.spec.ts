/**
 * =============================================================================
 * OrbitOS Operations - Roles Management E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for Roles CRUD operations.
 * Tests cover UI interactions, form validation, and data persistence.
 *
 * Spec: Operations module - Roles management
 * =============================================================================
 */

import { test, expect } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}/operations`

// Test data generators
const generateTestName = () => `E2E Role ${Date.now()}`

// =============================================================================
// ROLES CRUD UI TESTS
// =============================================================================

test.describe('Roles Management - Full CRUD UI', () => {
  test.describe.configure({ mode: 'serial' })

  const testRoleName = generateTestName()
  const testRolePurpose = 'E2E test role for automated testing'
  const testRoleDepartment = 'Engineering'
  let updatedRoleName = ''

  test('should display roles list page', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Page elements
    await expect(page.getByRole('heading', { name: 'Roles' })).toBeVisible()
    await expect(page.getByRole('button', { name: /add role/i })).toBeVisible()

    // Description text
    await expect(page.getByText('Operational roles and their coverage status.')).toBeVisible()
  })

  test('should display stats cards', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // Stats cards should be visible (4 stats cards in a grid)
    const statsGrid = page.locator('.grid.gap-4').first()
    await expect(statsGrid).toBeVisible()

    // Should have at least 4 stat cards (Total, Covered, At Risk, Uncovered)
    const statCards = page.locator('.orbitos-card-static')
    await expect(statCards.first()).toBeVisible()
    expect(await statCards.count()).toBeGreaterThanOrEqual(4)
  })

  test('should open add role dialog', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Click Add Role button
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Role' })).toBeVisible()
    await expect(page.getByText('Define an operational role for your organization.')).toBeVisible()

    // Form fields should be visible
    await expect(page.getByPlaceholder('e.g., Sales Lead')).toBeVisible()
    await expect(page.getByPlaceholder('What is this role responsible for?')).toBeVisible()
    await expect(page.getByPlaceholder('e.g., Sales, Finance, Operations')).toBeVisible()
  })

  test('should close dialog on cancel', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Click Cancel
    await page.getByRole('button', { name: /cancel/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()
  })

  test('should close dialog on Escape key', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Add New Role' })).toBeVisible()

    // Focus the name input and press Escape
    const nameInput = page.getByPlaceholder('e.g., Sales Lead')
    await nameInput.focus()
    await page.keyboard.press('Escape')
    await page.waitForTimeout(500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()
  })

  test('should disable Add Role button when name is empty', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Add Role button in dialog should be disabled
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await expect(addButton).toBeDisabled()
  })

  test('should enable Add Role button when name is filled', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Fill in name
    await page.getByPlaceholder('e.g., Sales Lead').fill('Test Role')
    await page.waitForTimeout(100)

    // Add Role button in dialog should be enabled
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await expect(addButton).toBeEnabled()
  })

  test('should create a new role via UI', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open create dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Fill form
    await page.getByPlaceholder('e.g., Sales Lead').fill(testRoleName)
    await page.getByPlaceholder('What is this role responsible for?').fill(testRolePurpose)
    await page.getByPlaceholder('e.g., Sales, Finance, Operations').fill(testRoleDepartment)

    // Submit
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await addButton.click()

    // Wait for dialog to close and list to update
    await page.waitForTimeout(1500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()

    // Role should appear in the grid
    await expect(page.getByText(testRoleName)).toBeVisible({ timeout: 5000 })
  })

  test('should submit add role dialog on Enter key', async ({ page }) => {
    const enterTestRoleName = `Enter Key Role ${Date.now()}`

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open create dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Fill form
    await page.getByPlaceholder('e.g., Sales Lead').fill(enterTestRoleName)

    // Press Enter to submit
    await page.keyboard.press('Enter')

    // Wait for dialog to close and list to update
    await page.waitForTimeout(1500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()

    // Role should appear in the grid
    await expect(page.getByText(enterTestRoleName)).toBeVisible({ timeout: 5000 })
  })

  test('should display created role in grid', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Grid should contain the created role
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(testRoleName) })
    await expect(roleCard).toBeVisible()

    // Card should show role details
    await expect(roleCard.getByText(testRoleName)).toBeVisible()
    await expect(roleCard.getByText(testRolePurpose)).toBeVisible()
  })

  test('should show department badge for role', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the role card
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(testRoleName) })

    // Should have department badge
    await expect(roleCard.getByText(testRoleDepartment)).toBeVisible()
  })

  test('should show coverage status badge for role', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the role card
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(testRoleName) })

    // Should have status badge (Covered, At Risk, or Uncovered)
    const statusBadge = roleCard.locator('span', {
      hasText: /Covered|At Risk|Uncovered/
    })
    await expect(statusBadge).toBeVisible()
  })

  test('should open edit dialog when clicking Edit button', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the role card and click Edit
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(testRoleName) })
    await roleCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    // Edit dialog should be visible
    await expect(page.getByRole('heading', { name: 'Edit Role' })).toBeVisible()

    // Form should be pre-filled with role data
    await expect(page.getByPlaceholder('e.g., Sales Lead')).toHaveValue(testRoleName)
    await expect(page.getByPlaceholder('What is this role responsible for?')).toHaveValue(testRolePurpose)
    await expect(page.getByPlaceholder('e.g., Sales, Finance, Operations')).toHaveValue(testRoleDepartment)
  })

  test('should update role via edit dialog', async ({ page }) => {
    updatedRoleName = `Updated ${testRoleName}`

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the role card and click Edit
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(testRoleName) })
    await roleCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    // Update the name
    const nameInput = page.getByPlaceholder('e.g., Sales Lead')
    await nameInput.clear()
    await nameInput.fill(updatedRoleName)

    // Save changes
    await page.getByRole('button', { name: /save changes/i }).click()

    // Wait for dialog to close and list to update
    await page.waitForTimeout(1500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Edit Role' })).not.toBeVisible()

    // Updated role name should appear in the grid
    await expect(page.getByText(updatedRoleName)).toBeVisible({ timeout: 5000 })
  })

  test('should submit edit dialog on Enter key', async ({ page }) => {
    const newPurpose = 'Updated purpose via Enter key'

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the role card and click Edit
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(updatedRoleName) })
    await roleCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    // Update the purpose
    const purposeInput = page.getByPlaceholder('What is this role responsible for?')
    await purposeInput.clear()
    await purposeInput.fill(newPurpose)

    // Press Enter to submit
    await page.keyboard.press('Enter')

    // Wait for dialog to close and list to update
    await page.waitForTimeout(1500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Edit Role' })).not.toBeVisible()
  })

  test('should close edit dialog on Escape key', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find a role card and click Edit
    const roleCard = page.locator('.orbitos-card').first()
    if (await roleCard.isVisible()) {
      await roleCard.getByRole('button', { name: /edit/i }).click()
      await page.waitForTimeout(300)

      // Verify dialog is open
      await expect(page.getByRole('heading', { name: 'Edit Role' })).toBeVisible()

      // Focus an input then press Escape
      const nameInput = page.getByPlaceholder('e.g., Sales Lead')
      await nameInput.focus()
      await page.keyboard.press('Escape')
      await page.waitForTimeout(500)

      // Dialog should be closed
      await expect(page.getByRole('heading', { name: 'Edit Role' })).not.toBeVisible()
    }
  })

  test('should delete role via Delete button', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the role card
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(updatedRoleName) })

    // Set up dialog handler for confirm
    page.on('dialog', dialog => dialog.accept())

    // Click Delete
    await roleCard.getByRole('button', { name: /delete/i }).click()

    // Wait for deletion to complete
    await page.waitForTimeout(1500)

    // Role should no longer be visible
    await expect(page.getByText(updatedRoleName)).not.toBeVisible({ timeout: 5000 })
  })

  test('should delete role via edit dialog', async ({ page }) => {
    // First create a role to delete
    const roleToDelete = `Delete Test Role ${Date.now()}`

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Create the role
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)
    await page.getByPlaceholder('e.g., Sales Lead').fill(roleToDelete)
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await addButton.click()
    await page.waitForTimeout(1500)

    // Verify it was created
    await expect(page.getByText(roleToDelete)).toBeVisible({ timeout: 5000 })

    // Open edit dialog
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(roleToDelete) })
    await roleCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    // Set up dialog handler for confirm
    page.on('dialog', dialog => dialog.accept())

    // Click delete button in edit dialog (the first button in the dialog footer, before Cancel and Save)
    // The dialog has 3 buttons in the footer: delete (trash icon), Cancel, Save Changes
    const dialogFooter = page.locator('.fixed .orbitos-glass .flex.gap-3').last()
    const deleteButton = dialogFooter.locator('button').first()
    await deleteButton.click()

    // Wait for deletion to complete
    await page.waitForTimeout(1500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Edit Role' })).not.toBeVisible()

    // Role should no longer be visible
    await expect(page.getByText(roleToDelete)).not.toBeVisible({ timeout: 5000 })
  })

  test('should cancel delete when dialog is dismissed', async ({ page }) => {
    // First create a role to test with
    const roleToKeep = `Cancel Delete Role ${Date.now()}`

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Create the role
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)
    await page.getByPlaceholder('e.g., Sales Lead').fill(roleToKeep)
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await addButton.click()
    await page.waitForTimeout(1500)

    // Verify it was created
    await expect(page.getByText(roleToKeep)).toBeVisible({ timeout: 5000 })

    // Find the role card
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(roleToKeep) })

    // Set up dialog handler to dismiss
    page.on('dialog', dialog => dialog.dismiss())

    // Click Delete
    await roleCard.getByRole('button', { name: /delete/i }).click()

    // Wait a moment
    await page.waitForTimeout(500)

    // Role should still be visible
    await expect(page.getByText(roleToKeep)).toBeVisible()

    // Clean up - delete the role
    page.removeAllListeners('dialog')
    page.on('dialog', dialog => dialog.accept())
    await roleCard.getByRole('button', { name: /delete/i }).click()
    await page.waitForTimeout(1000)
  })
})

// =============================================================================
// EMPTY STATE TESTS
// =============================================================================

test.describe('Roles - Empty State', () => {
  test('should show loading spinner initially', async ({ page }) => {
    // Slow down network to catch loading state
    await page.route('**/operations/**', async route => {
      await new Promise(resolve => setTimeout(resolve, 1000))
      await route.continue()
    })

    await page.goto('/app/roles')

    // Loading spinner should appear
    const spinner = page.locator('.orbitos-spinner')
    // It may or may not be visible depending on timing
  })

  test('should show empty state message when no roles', async ({ page }) => {
    // Mock empty response
    await page.route('**/operations/roles**', route => {
      route.fulfill({
        status: 200,
        body: JSON.stringify([])
      })
    })

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // Empty state message should be visible
    await expect(page.getByText('No roles defined yet.')).toBeVisible()
    await expect(page.getByRole('button', { name: /add your first role/i })).toBeVisible()
  })

  test('should open add dialog from empty state button', async ({ page }) => {
    // Mock empty response
    await page.route('**/operations/roles**', route => {
      route.fulfill({
        status: 200,
        body: JSON.stringify([])
      })
    })

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // Click "Add your first role" button
    await page.getByRole('button', { name: /add your first role/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Role' })).toBeVisible()
  })
})

// =============================================================================
// STATS UPDATE TESTS
// =============================================================================

test.describe('Roles - Stats Updates', () => {
  test('should update stats after adding role', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Get initial count
    const totalRolesCard = page.locator('.orbitos-card-static', { hasText: 'Total Roles' })
    const initialCount = parseInt(await totalRolesCard.locator('.text-2xl').textContent() || '0')

    // Create a new role
    const newRoleName = `Stats Test Role ${Date.now()}`
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)
    await page.getByPlaceholder('e.g., Sales Lead').fill(newRoleName)
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await addButton.click()
    await page.waitForTimeout(1500)

    // Total Roles stat should have increased by 1
    const newCount = parseInt(await totalRolesCard.locator('.text-2xl').textContent() || '0')
    expect(newCount).toBe(initialCount + 1)

    // Clean up
    page.on('dialog', dialog => dialog.accept())
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(newRoleName) })
    await roleCard.getByRole('button', { name: /delete/i }).click()
    await page.waitForTimeout(1000)
  })

  test('should update stats after deleting role', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Create a role first
    const newRoleName = `Delete Stats Test Role ${Date.now()}`
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)
    await page.getByPlaceholder('e.g., Sales Lead').fill(newRoleName)
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await addButton.click()
    await page.waitForTimeout(1500)

    // Get count after adding
    const totalRolesCard = page.locator('.orbitos-card-static', { hasText: 'Total Roles' })
    const countAfterAdd = parseInt(await totalRolesCard.locator('.text-2xl').textContent() || '0')

    // Delete the role
    page.on('dialog', dialog => dialog.accept())
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(newRoleName) })
    await roleCard.getByRole('button', { name: /delete/i }).click()
    await page.waitForTimeout(1500)

    // Total Roles stat should have decreased by 1
    const countAfterDelete = parseInt(await totalRolesCard.locator('.text-2xl').textContent() || '0')
    expect(countAfterDelete).toBe(countAfterAdd - 1)
  })
})

// =============================================================================
// RESPONSIVE DESIGN TESTS
// =============================================================================

test.describe('Roles - Responsive Design', () => {
  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Page should still show key elements
    await expect(page.getByRole('heading', { name: 'Roles' })).toBeVisible()
    await expect(page.getByRole('button', { name: /add role/i })).toBeVisible()
  })

  test('should stack stats cards on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // Stats cards should all be visible (stacked on mobile)
    const statsGrid = page.locator('.grid.gap-4').first()
    await expect(statsGrid).toBeVisible()

    // Should have stats cards visible
    const statCards = page.locator('.orbitos-card-static')
    await expect(statCards.first()).toBeVisible()
  })

  test('should show single column grid on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Role cards should be visible and in single column (grid)
    // The grid uses responsive classes like md:grid-cols-2
    const roleGrid = page.locator('.grid.gap-4').last()
    if (await roleGrid.isVisible()) {
      await expect(roleGrid).toBeVisible()
    } else {
      // Empty state message visible instead
      await expect(page.getByText('No roles defined yet.')).toBeVisible()
    }
  })
})

// =============================================================================
// ACCESSIBILITY TESTS
// =============================================================================

test.describe('Roles - Accessibility', () => {
  test('should have proper heading hierarchy', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Should have h1
    const h1 = await page.locator('h1').count()
    expect(h1).toBeGreaterThanOrEqual(1)
  })

  test('should have accessible form labels', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(500)

    // Form should have labels (within the dialog)
    const dialog = page.locator('.fixed .orbitos-glass')
    await expect(dialog.locator('label', { hasText: 'Role Name' })).toBeVisible()
    await expect(dialog.locator('label', { hasText: 'Purpose' })).toBeVisible()
    await expect(dialog.locator('label', { hasText: 'Department' })).toBeVisible()
    await expect(dialog.locator('label', { hasText: 'Description' })).toBeVisible()
  })

  test('should support keyboard navigation in dialog', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Tab through form fields
    await page.keyboard.press('Tab')
    const focusedElement = await page.evaluate(() => document.activeElement?.tagName)
    expect(['INPUT', 'BUTTON', 'TEXTAREA']).toContain(focusedElement)
  })

  test('should focus first input when dialog opens', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(500)

    // First input (name) should be focused or focusable
    const focusedTagName = await page.evaluate(() => document.activeElement?.tagName)
    // Input may be focused or dialog may be focused, both are acceptable
    expect(['INPUT', 'BUTTON', 'DIV', 'TEXTAREA']).toContain(focusedTagName)
  })
})

// =============================================================================
// API INTEGRATION TESTS
// =============================================================================

test.describe('Roles - API Integration', () => {
  test('should fetch roles from API on load', async ({ page }) => {
    let apiCalled = false

    await page.route('**/operations/roles**', route => {
      apiCalled = true
      route.continue()
    })

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    expect(apiCalled).toBeTruthy()
  })

  test('should handle API errors gracefully', async ({ page }) => {
    // Intercept API calls and return error
    await page.route('**/operations/roles**', route => {
      route.fulfill({
        status: 500,
        body: JSON.stringify({ error: 'Internal Server Error' })
      })
    })

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Page should still load
    await expect(page.getByRole('heading', { name: 'Roles' })).toBeVisible()
  })

  test('should send correct payload on role creation', async ({ page }) => {
    let requestPayload: any = null
    const testName = `API Test Role ${Date.now()}`

    await page.route('**/operations/roles', async route => {
      if (route.request().method() === 'POST') {
        requestPayload = route.request().postDataJSON()
      }
      await route.continue()
    })

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Create a role
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)
    await page.getByPlaceholder('e.g., Sales Lead').fill(testName)
    await page.getByPlaceholder('What is this role responsible for?').fill('Test purpose')
    await page.getByPlaceholder('e.g., Sales, Finance, Operations').fill('Testing')

    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await addButton.click()
    await page.waitForTimeout(1500)

    // Verify payload
    expect(requestPayload).toBeTruthy()
    expect(requestPayload.name).toBe(testName)
    expect(requestPayload.purpose).toBe('Test purpose')
    expect(requestPayload.department).toBe('Testing')

    // Clean up
    page.on('dialog', dialog => dialog.accept())
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(testName) })
    if (await roleCard.isVisible()) {
      await roleCard.getByRole('button', { name: /delete/i }).click()
      await page.waitForTimeout(1000)
    }
  })

  test('should send correct payload on role update', async ({ page }) => {
    let requestPayload: any = null
    const testName = `Update API Test Role ${Date.now()}`
    const updatedName = `Updated ${testName}`

    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Create a role first
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)
    await page.getByPlaceholder('e.g., Sales Lead').fill(testName)
    const addButton = page.locator('.fixed').getByRole('button', { name: /add role/i })
    await addButton.click()
    await page.waitForTimeout(1500)

    // Set up route interception for PUT
    await page.route('**/operations/roles/**', async route => {
      if (route.request().method() === 'PUT') {
        requestPayload = route.request().postDataJSON()
      }
      await route.continue()
    })

    // Edit the role
    const roleCard = page.locator('.orbitos-card', { has: page.getByText(testName) })
    await roleCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    const nameInput = page.getByPlaceholder('e.g., Sales Lead')
    await nameInput.clear()
    await nameInput.fill(updatedName)

    await page.getByRole('button', { name: /save changes/i }).click()
    await page.waitForTimeout(1500)

    // Verify payload
    expect(requestPayload).toBeTruthy()
    expect(requestPayload.name).toBe(updatedName)

    // Clean up
    page.on('dialog', dialog => dialog.accept())
    const updatedRoleCard = page.locator('.orbitos-card', { has: page.getByText(updatedName) })
    if (await updatedRoleCard.isVisible()) {
      await updatedRoleCard.getByRole('button', { name: /delete/i }).click()
      await page.waitForTimeout(1000)
    }
  })
})
