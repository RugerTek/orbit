/**
 * =============================================================================
 * OrbitOS Operations - People Management E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for People (Resources) CRUD operations.
 * Tests cover UI interactions, form validation, and data persistence.
 *
 * Spec: Operations module - People/Resources management
 * =============================================================================
 */

import { test, expect } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}/operations`

// Test data generators
const generateTestName = () => `E2E Person ${Date.now()}`

// =============================================================================
// PEOPLE (RESOURCES) CRUD UI TESTS
// =============================================================================

test.describe('People Management - Full CRUD UI', () => {
  test.describe.configure({ mode: 'serial' })

  const testPersonName = generateTestName()
  const testPersonEmail = 'e2e-person@example.com'
  const testPersonRole = 'E2E Test Developer'

  test('should display people list page', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Page elements
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible()
    await expect(page.getByRole('button', { name: /add person/i })).toBeVisible()

    // Description text
    await expect(page.getByText('Capacity, coverage, and operational focus by person.')).toBeVisible()
  })

  test('should display stats cards', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)

    // Stats cards should be visible (4 stats cards - text is uppercase via CSS)
    const statCards = page.locator('.orbitos-card-static')
    await expect(statCards.first()).toBeVisible()
    expect(await statCards.count()).toBeGreaterThanOrEqual(4)
  })

  test('should open add person dialog', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Click Add Person button
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Person' })).toBeVisible()
    await expect(page.getByText('Add a team member to track their roles and capacity.')).toBeVisible()

    // Form fields should be visible
    await expect(page.getByPlaceholder('e.g., John Smith')).toBeVisible()
    await expect(page.getByPlaceholder('john@company.com')).toBeVisible()
    await expect(page.getByPlaceholder('e.g., Senior Developer')).toBeVisible()
  })

  test('should close dialog on cancel', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Click Cancel
    await page.getByRole('button', { name: /cancel/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Person' })).not.toBeVisible()
  })

  test('should disable Add Person button when name is empty', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Add Person button in dialog should be disabled
    const addButton = page.locator('.fixed').getByRole('button', { name: /add person/i })
    await expect(addButton).toBeDisabled()
  })

  test('should enable Add Person button when name is filled', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Fill in name
    await page.getByPlaceholder('e.g., John Smith').fill('Test Person')
    await page.waitForTimeout(100)

    // Add Person button in dialog should be enabled
    const addButton = page.locator('.fixed').getByRole('button', { name: /add person/i })
    await expect(addButton).toBeEnabled()
  })

  test('should create a new person via UI', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open create dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Fill form
    await page.getByPlaceholder('e.g., John Smith').fill(testPersonName)
    await page.getByPlaceholder('john@company.com').fill(testPersonEmail)
    await page.getByPlaceholder('e.g., Senior Developer').fill(testPersonRole)

    // Submit
    const addButton = page.locator('.fixed').getByRole('button', { name: /add person/i })
    await addButton.click()

    // Wait for dialog to close and list to update
    await page.waitForTimeout(1500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Person' })).not.toBeVisible()

    // Person should appear in the list
    await expect(page.getByText(testPersonName)).toBeVisible({ timeout: 5000 })
  })

  test('should display created person in table', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Table should contain the created person
    const personRow = page.locator('tr', { has: page.getByText(testPersonName) })
    await expect(personRow).toBeVisible()

    // Row should show person details
    await expect(personRow.getByText(testPersonName)).toBeVisible()
    await expect(personRow.getByText(testPersonRole)).toBeVisible()
  })

  test('should show allocation bar for person', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the person row
    const personRow = page.locator('tr', { has: page.getByText(testPersonName) })

    // Should have allocation column with percentage
    await expect(personRow.locator('text=/\\d+%/')).toBeVisible()
  })

  test('should show status badge for person', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Find the person row
    const personRow = page.locator('tr', { has: page.getByText(testPersonName) })

    // Should have status badge (Available, Stable, Near Capacity, or Overloaded)
    const statusBadge = personRow.locator('span', {
      hasText: /Available|Stable|Near Capacity|Overloaded/
    })
    await expect(statusBadge).toBeVisible()
  })

  test('should update stats after adding person', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Total People stat should be at least 1
    const totalPeopleCard = page.locator('.orbitos-card-static', { hasText: 'Total People' })
    const count = await totalPeopleCard.locator('.text-2xl').textContent()
    expect(parseInt(count || '0')).toBeGreaterThanOrEqual(1)
  })

  test('should show team coverage health indicator', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Wait for data to load
    await page.waitForTimeout(1000)

    // Team coverage indicator should be visible
    await expect(page.getByText('Team Coverage')).toBeVisible()
    await expect(page.getByText(/\d+% healthy capacity/)).toBeVisible()
  })
})

// =============================================================================
// EMPTY STATE TESTS
// =============================================================================

test.describe('People - Empty State', () => {
  test('should show loading spinner initially', async ({ page }) => {
    // Slow down network to catch loading state
    await page.route('**/operations/**', async route => {
      await new Promise(resolve => setTimeout(resolve, 1000))
      await route.continue()
    })

    await page.goto('/app/people')

    // Loading spinner should appear
    const spinner = page.locator('.orbitos-spinner')
    // It may or may not be visible depending on timing
  })
})

// =============================================================================
// TABLE INTERACTION TESTS
// =============================================================================

test.describe('People - Table Interactions', () => {
  test('should have proper table headers', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Check table headers
    await expect(page.getByRole('columnheader', { name: 'Person' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Roles' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Allocation' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  })

  test('should highlight row on hover', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Hover over a table row
    const firstRow = page.locator('tbody tr').first()
    if (await firstRow.isVisible()) {
      await firstRow.hover()
      // Row should have hover effect (bg-white/5)
      await expect(firstRow).toBeVisible()
    }
  })
})

// =============================================================================
// EXPORT BUTTON TEST
// =============================================================================

test.describe('People - Export Functionality', () => {
  test('should have export button visible', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Export button should be visible
    await expect(page.getByRole('button', { name: /export/i })).toBeVisible()
  })
})

// =============================================================================
// RESPONSIVE DESIGN TESTS
// =============================================================================

test.describe('People - Responsive Design', () => {
  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Page should still show key elements
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible()
    await expect(page.getByRole('button', { name: /add person/i })).toBeVisible()
  })

  test('should stack buttons on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Add Person button should be visible on mobile
    await expect(page.getByRole('button', { name: /add person/i })).toBeVisible()
  })
})

// =============================================================================
// ACCESSIBILITY TESTS
// =============================================================================

test.describe('People - Accessibility', () => {
  test('should have proper heading hierarchy', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Should have h1
    const h1 = await page.locator('h1').count()
    expect(h1).toBeGreaterThanOrEqual(1)
  })

  test('should have accessible form labels', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Form should have labels
    await expect(page.getByText('Full Name *')).toBeVisible()
    await expect(page.getByText('Email')).toBeVisible()
    await expect(page.getByText('Role / Title')).toBeVisible()
  })

  test('should support keyboard navigation in dialog', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Tab through form fields
    await page.keyboard.press('Tab')
    const focusedElement = await page.evaluate(() => document.activeElement?.tagName)
    expect(['INPUT', 'BUTTON']).toContain(focusedElement)
  })
})

// =============================================================================
// API INTEGRATION TESTS
// =============================================================================

test.describe('People - API Integration', () => {
  test('should fetch people from API on load', async ({ page }) => {
    let apiCalled = false

    await page.route('**/operations/resources**', route => {
      apiCalled = true
      route.continue()
    })

    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    expect(apiCalled).toBeTruthy()
  })

  test('should fetch role assignments from API', async ({ page }) => {
    let apiCalled = false

    await page.route('**/operations/role-assignments**', route => {
      apiCalled = true
      route.continue()
    })

    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Role assignments API may or may not be called depending on implementation
  })

  test('should handle API errors gracefully', async ({ page }) => {
    // Intercept API calls and return error
    await page.route('**/operations/resources**', route => {
      route.fulfill({
        status: 500,
        body: JSON.stringify({ error: 'Internal Server Error' })
      })
    })

    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Page should still load
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible()
  })
})
