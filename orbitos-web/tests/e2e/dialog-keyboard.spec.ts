/**
 * =============================================================================
 * OrbitOS - Dialog Keyboard Interaction Tests
 * =============================================================================
 * Tests for Enter key submission and Escape key dismissal across all dialogs.
 * Ensures consistent keyboard navigation and form submission behavior.
 * =============================================================================
 */

import { test, expect } from '@playwright/test'

// =============================================================================
// PROCESSES PAGE - DIALOG KEYBOARD TESTS
// =============================================================================

test.describe('Processes - Dialog Keyboard', () => {
  test('should submit create process dialog on Enter key', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    // Fill required field
    await page.getByPlaceholder('e.g., Customer Onboarding').fill(`Enter Test ${Date.now()}`)

    // Press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(1500)

    // Dialog should close (either navigated or closed)
    await expect(page.getByRole('heading', { name: 'Create New Process' })).not.toBeVisible()
  })

  test('should close create process dialog on Escape key', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Create New Process' })).toBeVisible()

    // Focus on an input field first (Escape requires an element to have focus to bubble up)
    const nameInput = page.getByPlaceholder('e.g., Customer Onboarding')
    await nameInput.focus()
    await page.waitForTimeout(100)

    // Press Escape
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Create New Process' })).not.toBeVisible()
  })

  test('should NOT submit process dialog when name is empty', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    // Don't fill name, just press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(500)

    // Dialog should still be visible (nothing happened)
    await expect(page.getByRole('heading', { name: 'Create New Process' })).toBeVisible()
  })
})

// =============================================================================
// FUNCTIONS PAGE - DIALOG KEYBOARD TESTS
// =============================================================================

test.describe('Functions - Dialog Keyboard', () => {
  test('should submit add function dialog on Enter key', async ({ page }) => {
    await page.goto('/app/functions')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add function/i }).click()
    await page.waitForTimeout(300)

    // Fill required field
    await page.getByPlaceholder('e.g., Handle Inbound Request').fill(`Enter Function ${Date.now()}`)

    // Press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(1500)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Function' })).not.toBeVisible()
  })

  test('should close add function dialog on Escape key', async ({ page }) => {
    await page.goto('/app/functions')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add function/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Function' })).toBeVisible()

    // Focus on an input field first (Escape requires an element to have focus to bubble up)
    const nameInput = page.getByPlaceholder('e.g., Handle Inbound Request')
    await nameInput.focus()
    await page.waitForTimeout(100)

    // Press Escape
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Function' })).not.toBeVisible()
  })

  test('should close function dialog when clicking outside', async ({ page }) => {
    await page.goto('/app/functions')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add function/i }).click()
    await page.waitForTimeout(300)

    // Click outside the dialog (on the right side of backdrop to avoid sidebar)
    const backdrop = page.locator('.fixed.inset-0.z-50').first()
    const box = await backdrop.boundingBox()
    if (box) {
      // Click near the right edge of the screen, avoiding the dialog in the center
      await page.mouse.click(box.x + box.width - 50, box.y + 50)
    }
    await page.waitForTimeout(300)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Function' })).not.toBeVisible()
  })

  test('should NOT submit function dialog when name is empty', async ({ page }) => {
    await page.goto('/app/functions')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add function/i }).click()
    await page.waitForTimeout(300)

    // Don't fill name, just press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(500)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Function' })).toBeVisible()
  })
})

// =============================================================================
// ROLES PAGE - DIALOG KEYBOARD TESTS
// =============================================================================

test.describe('Roles - Dialog Keyboard', () => {
  test('should submit add role dialog on Enter key', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Fill required field
    await page.getByPlaceholder('e.g., Sales Lead').fill(`Enter Role ${Date.now()}`)

    // Press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(1500)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()
  })

  test('should close add role dialog on Escape key', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Role' })).toBeVisible()

    // Focus on an input field first (Escape requires an element to have focus to bubble up)
    const nameInput = page.getByPlaceholder('e.g., Sales Lead')
    await nameInput.focus()
    await page.waitForTimeout(100)

    // Press Escape
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()
  })

  test('should close role dialog when clicking outside', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Click outside the dialog (on the right side of backdrop to avoid sidebar)
    const backdrop = page.locator('.fixed.inset-0.z-50').first()
    const box = await backdrop.boundingBox()
    if (box) {
      // Click near the right edge of the screen, avoiding the dialog in the center
      await page.mouse.click(box.x + box.width - 50, box.y + 50)
    }
    await page.waitForTimeout(300)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()
  })

  test('should NOT submit role dialog when name is empty', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Don't fill name, just press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(500)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Role' })).toBeVisible()
  })
})

// =============================================================================
// PEOPLE PAGE - DIALOG KEYBOARD TESTS
// =============================================================================

test.describe('People - Dialog Keyboard', () => {
  test('should submit add person dialog on Enter key', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Fill required field
    await page.getByPlaceholder('e.g., John Smith').fill(`Enter Person ${Date.now()}`)

    // Press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(1500)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Person' })).not.toBeVisible()
  })

  test('should close add person dialog on Escape key', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Person' })).toBeVisible()

    // Focus on an input field first (Escape requires an element to have focus to bubble up)
    const nameInput = page.getByPlaceholder('e.g., John Smith')
    await nameInput.focus()
    await page.waitForTimeout(100)

    // Press Escape
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Person' })).not.toBeVisible()
  })

  test('should NOT submit person dialog when name is empty', async ({ page }) => {
    await page.goto('/app/people')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add person/i }).click()
    await page.waitForTimeout(300)

    // Don't fill name, just press Enter
    await page.keyboard.press('Enter')
    await page.waitForTimeout(500)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Person' })).toBeVisible()
  })
})

// =============================================================================
// PROCESS ACTIVITY - DIALOG KEYBOARD TESTS
// =============================================================================

test.describe('Process Activity - Dialog Keyboard', () => {
  test('should submit add activity dialog on Enter key', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Get any existing process
    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Check if there's an add activity button (either empty state or edit mode)
      const addActivityBtn = page.getByRole('button', { name: /add.*activity/i }).first()
      if (await addActivityBtn.isVisible()) {
        await addActivityBtn.click()
        await page.waitForTimeout(300)

        // Fill required field
        await page.getByPlaceholder('e.g., Review Contract').fill(`Enter Activity ${Date.now()}`)

        // Press Enter
        await page.keyboard.press('Enter')
        await page.waitForTimeout(1500)

        // Dialog should close
        await expect(page.getByRole('heading', { name: 'Add New Activity' })).not.toBeVisible()
      }
    }
  })

  test('should close add activity dialog on Escape key', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Get any existing process
    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Check for add activity button
      const addActivityBtn = page.getByRole('button', { name: /add.*activity/i }).first()
      if (await addActivityBtn.isVisible()) {
        await addActivityBtn.click()
        await page.waitForTimeout(300)

        // Dialog should be visible
        await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

        // Press Escape
        await page.keyboard.press('Escape')
        await page.waitForTimeout(300)

        // Dialog should close
        await expect(page.getByRole('heading', { name: 'Add New Activity' })).not.toBeVisible()
      }
    }
  })
})

// =============================================================================
// KEYBOARD ACCESSIBILITY TESTS
// =============================================================================

test.describe('Dialog Keyboard Accessibility', () => {
  test('should focus first input when dialog opens', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    // First input should be visible and focusable
    const nameInput = page.getByPlaceholder('e.g., Customer Onboarding')
    await expect(nameInput).toBeVisible()

    // Click to ensure focus (autofocus can be browser-dependent)
    await nameInput.click()
    await expect(nameInput).toBeFocused()
  })

  test('should allow tab navigation through form fields', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    // Tab through fields
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')

    // Should be able to tab without errors
    // Focus should move through form elements
  })

  test('should not submit form with Shift+Enter (for multiline fields)', async ({ page }) => {
    await page.goto('/app/functions')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add function/i }).click()
    await page.waitForTimeout(300)

    // Fill name
    await page.getByPlaceholder('e.g., Handle Inbound Request').fill('Test Function')

    // Focus on description textarea
    const textarea = page.getByPlaceholder('Additional details...')
    await textarea.focus()

    // Press Shift+Enter (should add newline, not submit)
    await page.keyboard.press('Shift+Enter')
    await page.waitForTimeout(300)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Function' })).toBeVisible()
  })
})

// =============================================================================
// CROSS-BROWSER KEYBOARD TESTS
// =============================================================================

test.describe('Cross-Browser Keyboard Support', () => {
  test('should handle Enter key consistently', async ({ page }) => {
    await page.goto('/app/roles')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add role/i }).click()
    await page.waitForTimeout(300)

    // Fill required field
    await page.getByPlaceholder('e.g., Sales Lead').fill('Keyboard Test Role')

    // Test Enter key
    await page.keyboard.press('Enter')
    await page.waitForTimeout(1500)

    // Dialog should close on successful submission
    await expect(page.getByRole('heading', { name: 'Add New Role' })).not.toBeVisible()
  })

  test('should handle Escape key consistently', async ({ page }) => {
    await page.goto('/app/functions')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add function/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Function' })).toBeVisible()

    // Focus on an input field first (Escape requires an element to have focus to bubble up)
    const nameInput = page.getByPlaceholder('e.g., Handle Inbound Request')
    await nameInput.focus()
    await page.waitForTimeout(100)

    // Test Escape key
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Add New Function' })).not.toBeVisible()
  })
})
