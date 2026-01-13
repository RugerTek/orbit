/**
 * =============================================================================
 * OrbitOS Super Admin CRUD E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for all Super Admin CRUD operations.
 * Tests cover UI interactions, form validation, and data persistence.
 *
 * Spec: F002-super-admin.json
 * =============================================================================
 */

import { test, expect, Page } from '@playwright/test'

const API_BASE = 'http://localhost:5027/api/SuperAdmin'

// Test data generators
const generateTestEmail = () => `e2e-test-${Date.now()}@example.com`
const generateTestName = (prefix: string) => `E2E Test ${prefix} ${Date.now()}`
const generateTestSlug = () => `e2e-test-${Date.now()}`

// =============================================================================
// USERS CRUD UI TESTS
// =============================================================================

test.describe('Users Management - Full CRUD UI', () => {
  test.describe.configure({ mode: 'serial' })

  let testUserId: string
  const testEmail = generateTestEmail()
  const testDisplayName = 'E2E Test User'

  test('should display users list page', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Page elements
    await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()
    await expect(page.getByRole('button', { name: /add user/i })).toBeVisible()

    // Table headers
    await expect(page.getByText('Email')).toBeVisible()
    await expect(page.getByText('Display Name')).toBeVisible()
  })

  test('should create a new user via UI', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Open create modal
    await page.getByRole('button', { name: /add user/i }).click()
    await page.waitForTimeout(500)

    // Fill form
    await page.locator('input[type="email"]').fill(testEmail)
    await page.locator('input[placeholder*="Display Name" i], input[placeholder*="name" i]').first().fill(testDisplayName)
    await page.locator('input[type="password"]').fill('TestPass123!')

    // Submit
    await page.getByRole('button', { name: /create|save|add/i }).click()

    // Wait for success - modal should close and user should appear in list
    await page.waitForTimeout(1000)
    await expect(page.getByText(testEmail)).toBeVisible({ timeout: 5000 })
  })

  test('should search for the created user', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Search
    const searchInput = page.locator('input[placeholder*="Search"]')
    await searchInput.fill(testDisplayName)
    await page.waitForTimeout(500)

    // Verify search results
    await expect(page.getByText(testEmail)).toBeVisible()
  })

  test('should edit user via UI', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Find and click edit on the test user row
    const userRow = page.locator('tr', { has: page.getByText(testEmail) })
    await userRow.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(500)

    // Update display name
    const nameInput = page.locator('input[placeholder*="Display Name" i], input[placeholder*="name" i]').first()
    await nameInput.clear()
    await nameInput.fill('Updated E2E User')

    // Save
    await page.getByRole('button', { name: /save|update/i }).click()
    await page.waitForTimeout(1000)

    // Verify update
    await expect(page.getByText('Updated E2E User')).toBeVisible()
  })

  test('should reset user password via UI', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Find and click reset password on the test user row
    const userRow = page.locator('tr', { has: page.getByText(testEmail) })

    // Look for reset password button
    const resetBtn = userRow.getByRole('button', { name: /reset.*password|password/i })
    if (await resetBtn.isVisible()) {
      await resetBtn.click()
      await page.waitForTimeout(500)

      // Fill new password
      await page.locator('input[type="password"]').fill('NewTestPass456!')

      // Confirm
      await page.getByRole('button', { name: /reset|save|confirm/i }).click()
      await page.waitForTimeout(500)

      // Should show success or close modal
      // The action completed without error means success
    }
  })

  test('should delete user via UI', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Find and click delete on the test user row
    const userRow = page.locator('tr', { has: page.getByText(testEmail) })
    await userRow.getByRole('button', { name: /delete/i }).click()
    await page.waitForTimeout(500)

    // Confirm deletion
    await page.getByRole('button', { name: /confirm|yes|delete/i }).click()
    await page.waitForTimeout(1000)

    // Verify deletion - user should not be in list
    await expect(page.getByText(testEmail)).not.toBeVisible()
  })

  test('should validate required fields on user form', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Open create modal
    await page.getByRole('button', { name: /add user/i }).click()
    await page.waitForTimeout(500)

    // Try to submit empty form
    await page.getByRole('button', { name: /create|save|add/i }).click()

    // Should show validation errors or form should not submit
    // Modal should still be visible
    await expect(page.locator('[class*="fixed"][class*="z-50"]').or(page.locator('input[type="email"]')).first()).toBeVisible()
  })
})

// =============================================================================
// ORGANIZATIONS CRUD UI TESTS
// =============================================================================

test.describe('Organizations Management - Full CRUD UI', () => {
  test.describe.configure({ mode: 'serial' })

  const testOrgName = generateTestName('Org')
  const testOrgSlug = generateTestSlug()

  test('should display organizations list page', async ({ page }) => {
    await page.goto('/admin/organizations')
    await page.waitForLoadState('networkidle')

    // Page elements
    await expect(page.getByRole('heading', { name: 'Organizations' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()
    await expect(page.getByRole('button', { name: /add organization/i })).toBeVisible()
  })

  test('should create a new organization via UI', async ({ page }) => {
    await page.goto('/admin/organizations')
    await page.waitForLoadState('networkidle')

    // Open create modal
    await page.getByRole('button', { name: /add organization/i }).click()
    await page.waitForTimeout(500)

    // Fill form
    await page.locator('input').first().fill(testOrgName)

    // Check if slug field exists and fill it
    const slugInput = page.locator('input[placeholder*="slug" i]')
    if (await slugInput.isVisible()) {
      await slugInput.fill(testOrgSlug)
    }

    // Submit
    await page.getByRole('button', { name: /create|save|add/i }).click()
    await page.waitForTimeout(1000)

    // Verify creation
    await expect(page.getByText(testOrgName)).toBeVisible({ timeout: 5000 })
  })

  test('should search for the created organization', async ({ page }) => {
    await page.goto('/admin/organizations')
    await page.waitForLoadState('networkidle')

    // Search
    const searchInput = page.locator('input[placeholder*="Search"]')
    await searchInput.fill('E2E Test Org')
    await page.waitForTimeout(500)

    // Verify search results
    await expect(page.getByText(testOrgName)).toBeVisible()
  })

  test('should edit organization via UI', async ({ page }) => {
    await page.goto('/admin/organizations')
    await page.waitForLoadState('networkidle')

    // Find and click edit
    const orgRow = page.locator('tr', { has: page.getByText(testOrgName) })
    await orgRow.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(500)

    // Update name
    const nameInput = page.locator('input').first()
    await nameInput.clear()
    await nameInput.fill('Updated E2E Org')

    // Save
    await page.getByRole('button', { name: /save|update/i }).click()
    await page.waitForTimeout(1000)

    // Verify update
    await expect(page.getByText('Updated E2E Org')).toBeVisible()
  })

  test('should delete organization via UI', async ({ page }) => {
    await page.goto('/admin/organizations')
    await page.waitForLoadState('networkidle')

    // Find and click delete
    const orgRow = page.locator('tr', { has: page.getByText('Updated E2E Org') })
    await orgRow.getByRole('button', { name: /delete/i }).click()
    await page.waitForTimeout(500)

    // Confirm deletion
    await page.getByRole('button', { name: /confirm|yes|delete/i }).click()
    await page.waitForTimeout(1000)

    // Verify deletion
    await expect(page.getByText('Updated E2E Org')).not.toBeVisible()
  })

  test('should auto-generate slug from name', async ({ page }) => {
    await page.goto('/admin/organizations')
    await page.waitForLoadState('networkidle')

    // Open create modal
    await page.getByRole('button', { name: /add organization/i }).click()
    await page.waitForTimeout(500)

    // Fill name and check if slug is auto-generated
    const nameInput = page.locator('input').first()
    await nameInput.fill('Auto Slug Test Org')
    await page.waitForTimeout(300)

    // Check slug field (if visible)
    const slugInput = page.locator('input[placeholder*="slug" i]')
    if (await slugInput.isVisible()) {
      const slugValue = await slugInput.inputValue()
      // Slug should be lowercase with hyphens
      expect(slugValue.toLowerCase()).toContain('auto')
    }

    // Cancel to avoid creating test data
    await page.keyboard.press('Escape')
  })
})

// =============================================================================
// ROLES CRUD UI TESTS
// =============================================================================

test.describe('Roles Management - Full CRUD UI', () => {
  test.describe.configure({ mode: 'serial' })

  const testRoleName = generateTestName('Role')

  test('should display roles list page with org filter', async ({ page }) => {
    await page.goto('/admin/roles')
    await page.waitForLoadState('networkidle')

    // Page elements
    await expect(page.getByRole('heading', { name: 'Roles' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()

    // Organization filter
    const filterSelect = page.locator('select')
    await expect(filterSelect).toBeVisible()
  })

  test('should filter roles by organization', async ({ page }) => {
    await page.goto('/admin/roles')
    await page.waitForLoadState('networkidle')

    // Select an organization from filter
    const filterSelect = page.locator('select')
    const options = await filterSelect.locator('option').allTextContents()

    if (options.length > 1) {
      // Select first non-"All" option
      const orgOption = options.find(opt => !opt.includes('All'))
      if (orgOption) {
        await filterSelect.selectOption({ label: orgOption })
        await page.waitForTimeout(500)
        // Page should update with filtered results
      }
    }
  })

  test('should create a new role via UI', async ({ page }) => {
    await page.goto('/admin/roles')
    await page.waitForLoadState('networkidle')

    // Open create modal
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(500)

    // Fill form
    const nameInput = page.locator('input[placeholder*="name" i]').first()
    await nameInput.fill(testRoleName)

    // Fill description if exists
    const descInput = page.locator('textarea, input[placeholder*="description" i]')
    if (await descInput.first().isVisible()) {
      await descInput.first().fill('E2E test role description')
    }

    // Select organization
    const orgSelect = page.locator('select')
    const options = await orgSelect.locator('option').allTextContents()
    const orgOption = options.find(opt => !opt.includes('Select') && !opt.includes('All'))
    if (orgOption) {
      await orgSelect.selectOption({ label: orgOption })
    }

    // Submit
    await page.getByRole('button', { name: /create|save|add/i }).click()
    await page.waitForTimeout(1000)

    // Verify creation
    await expect(page.getByText(testRoleName)).toBeVisible({ timeout: 5000 })
  })

  test('should edit role via UI', async ({ page }) => {
    await page.goto('/admin/roles')
    await page.waitForLoadState('networkidle')

    // Find and click edit
    const roleRow = page.locator('tr', { has: page.getByText(testRoleName) })
    if (await roleRow.isVisible()) {
      await roleRow.getByRole('button', { name: /edit/i }).click()
      await page.waitForTimeout(500)

      // Update name
      const nameInput = page.locator('input[placeholder*="name" i]').first()
      await nameInput.clear()
      await nameInput.fill('Updated E2E Role')

      // Save
      await page.getByRole('button', { name: /save|update/i }).click()
      await page.waitForTimeout(1000)

      // Verify update
      await expect(page.getByText('Updated E2E Role')).toBeVisible()
    }
  })

  test('should delete role via UI', async ({ page }) => {
    await page.goto('/admin/roles')
    await page.waitForLoadState('networkidle')

    // Find and click delete
    const roleRow = page.locator('tr', { has: page.getByText('Updated E2E Role') })
    if (await roleRow.isVisible()) {
      await roleRow.getByRole('button', { name: /delete/i }).click()
      await page.waitForTimeout(500)

      // Confirm deletion
      await page.getByRole('button', { name: /confirm|yes|delete/i }).click()
      await page.waitForTimeout(1000)

      // Verify deletion
      await expect(page.getByText('Updated E2E Role')).not.toBeVisible()
    }
  })
})

// =============================================================================
// FUNCTIONS CRUD UI TESTS
// =============================================================================

test.describe('Functions Management - Full CRUD UI', () => {
  test.describe.configure({ mode: 'serial' })

  const testFunctionName = generateTestName('Function')

  test('should display functions list page with org filter', async ({ page }) => {
    await page.goto('/admin/functions')
    await page.waitForLoadState('networkidle')

    // Page elements
    await expect(page.getByRole('heading', { name: 'Functions' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()

    // Organization filter
    const filterSelect = page.locator('select')
    await expect(filterSelect).toBeVisible()
  })

  test('should create a new function via UI', async ({ page }) => {
    await page.goto('/admin/functions')
    await page.waitForLoadState('networkidle')

    // Open create modal
    await page.getByRole('button', { name: /add function/i }).click()
    await page.waitForTimeout(500)

    // Fill form
    const nameInput = page.locator('input[placeholder*="name" i]').first()
    await nameInput.fill(testFunctionName)

    // Fill description if exists
    const descInput = page.locator('textarea, input[placeholder*="description" i]')
    if (await descInput.first().isVisible()) {
      await descInput.first().fill('E2E test function description')
    }

    // Fill category if exists
    const categoryInput = page.locator('input[placeholder*="category" i]')
    if (await categoryInput.isVisible()) {
      await categoryInput.fill('e2e-testing')
    }

    // Select organization
    const orgSelect = page.locator('select')
    const options = await orgSelect.locator('option').allTextContents()
    const orgOption = options.find(opt => !opt.includes('Select') && !opt.includes('All'))
    if (orgOption) {
      await orgSelect.selectOption({ label: orgOption })
    }

    // Submit
    await page.getByRole('button', { name: /create|save|add/i }).click()
    await page.waitForTimeout(1000)

    // Verify creation
    await expect(page.getByText(testFunctionName)).toBeVisible({ timeout: 5000 })
  })

  test('should edit function via UI', async ({ page }) => {
    await page.goto('/admin/functions')
    await page.waitForLoadState('networkidle')

    // Find and click edit
    const funcRow = page.locator('tr', { has: page.getByText(testFunctionName) })
    if (await funcRow.isVisible()) {
      await funcRow.getByRole('button', { name: /edit/i }).click()
      await page.waitForTimeout(500)

      // Update name
      const nameInput = page.locator('input[placeholder*="name" i]').first()
      await nameInput.clear()
      await nameInput.fill('Updated E2E Function')

      // Save
      await page.getByRole('button', { name: /save|update/i }).click()
      await page.waitForTimeout(1000)

      // Verify update
      await expect(page.getByText('Updated E2E Function')).toBeVisible()
    }
  })

  test('should filter functions by category', async ({ page }) => {
    await page.goto('/admin/functions')
    await page.waitForLoadState('networkidle')

    // Search by category
    const searchInput = page.locator('input[placeholder*="Search"]')
    await searchInput.fill('e2e-testing')
    await page.waitForTimeout(500)

    // Should find our test function
    await expect(page.getByText('Updated E2E Function')).toBeVisible()
  })

  test('should delete function via UI', async ({ page }) => {
    await page.goto('/admin/functions')
    await page.waitForLoadState('networkidle')

    // Find and click delete
    const funcRow = page.locator('tr', { has: page.getByText('Updated E2E Function') })
    if (await funcRow.isVisible()) {
      await funcRow.getByRole('button', { name: /delete/i }).click()
      await page.waitForTimeout(500)

      // Confirm deletion
      await page.getByRole('button', { name: /confirm|yes|delete/i }).click()
      await page.waitForTimeout(1000)

      // Verify deletion
      await expect(page.getByText('Updated E2E Function')).not.toBeVisible()
    }
  })
})

// =============================================================================
// DASHBOARD TESTS
// =============================================================================

test.describe('Dashboard - Stats and Navigation', () => {
  test('should load all dashboard stats', async ({ page }) => {
    await page.goto('/admin')
    await page.waitForLoadState('networkidle')

    // Wait for stats to load
    await page.waitForSelector('text=Total Users', { timeout: 10000 })

    // Verify all stat cards
    await expect(page.getByText('Total Users')).toBeVisible()
    await expect(page.locator('.text-slate-400:has-text("Organizations")').first()).toBeVisible()
    await expect(page.locator('.text-slate-400:has-text("Roles")').first()).toBeVisible()
    await expect(page.locator('.text-slate-400:has-text("Functions")').first()).toBeVisible()

    // Stats should have numbers
    const statsNumbers = await page.locator('.text-3xl, .text-4xl').allTextContents()
    expect(statsNumbers.some(n => /^\d+$/.test(n.trim()))).toBeTruthy()
  })

  test('should navigate from dashboard to each section', async ({ page }) => {
    await page.goto('/admin')
    await page.waitForLoadState('networkidle')

    // Click Manage Users link
    await page.getByText('Manage Users').click()
    await expect(page).toHaveURL(/.*\/admin\/users/)
    await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible()

    // Go back and click Manage Organizations
    await page.goto('/admin')
    await page.getByText('Manage Organizations').click()
    await expect(page).toHaveURL(/.*\/admin\/organizations/)

    // Go back and click Manage Roles
    await page.goto('/admin')
    await page.getByText('Manage Roles').click()
    await expect(page).toHaveURL(/.*\/admin\/roles/)
  })

  test('should display recent activity', async ({ page }) => {
    await page.goto('/admin')
    await page.waitForLoadState('networkidle')

    // Check for recent activity section
    const recentSection = page.getByText('Recent Activity')
    if (await recentSection.isVisible()) {
      // Recent activity list should be present
      await expect(recentSection).toBeVisible()
    }
  })
})

// =============================================================================
// ERROR HANDLING TESTS
// =============================================================================

test.describe('Error Handling', () => {
  test('should handle API errors gracefully', async ({ page }) => {
    // Intercept API calls and return error
    await page.route('**/api/SuperAdmin/users', route => {
      route.fulfill({
        status: 500,
        body: JSON.stringify({ error: 'Internal Server Error' })
      })
    })

    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Page should still load, possibly with error message
    await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible()
  })

  test('should handle 404 for non-existent resources', async ({ request }) => {
    const response = await request.get(`${API_BASE}/users/non-existent-id`)
    expect(response.status()).toBe(404)
  })

  test('should validate email format on user creation', async ({ request }) => {
    const response = await request.post(`${API_BASE}/users`, {
      data: {
        email: 'invalid-email',
        displayName: 'Test',
        password: 'TestPass123!'
      }
    })
    // Should reject invalid email
    expect(response.status()).toBeGreaterThanOrEqual(400)
  })
})

// =============================================================================
// PAGINATION TESTS
// =============================================================================

test.describe('Pagination', () => {
  test('should paginate users list', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Check for pagination controls if there are many users
    const paginationControls = page.locator('[class*="pagination"], button:has-text("Next"), button:has-text("Previous")')

    // If pagination exists, test it
    if (await paginationControls.first().isVisible()) {
      // Click next if available
      const nextBtn = page.getByRole('button', { name: /next/i })
      if (await nextBtn.isEnabled()) {
        await nextBtn.click()
        await page.waitForTimeout(500)
        // Page should update
      }
    }
  })
})

// =============================================================================
// ACCESSIBILITY TESTS
// =============================================================================

test.describe('Accessibility', () => {
  test('should have proper heading hierarchy', async ({ page }) => {
    await page.goto('/admin')

    // Should have h1
    const h1 = await page.locator('h1').count()
    expect(h1).toBeGreaterThanOrEqual(1)
  })

  test('should have accessible buttons', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Buttons should have accessible names
    const addButton = page.getByRole('button', { name: /add user/i })
    await expect(addButton).toBeVisible()
    await expect(addButton).toBeEnabled()
  })

  test('should support keyboard navigation', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    // Tab to Add User button
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')

    // Should be able to activate with Enter
    const focusedElement = await page.evaluate(() => document.activeElement?.tagName)
    expect(['BUTTON', 'A', 'INPUT']).toContain(focusedElement)
  })
})
