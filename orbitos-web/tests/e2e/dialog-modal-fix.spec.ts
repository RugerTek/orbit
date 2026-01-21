/**
 * Dialog Modal Fix Verification Test
 * Tests that BaseDialog renders as a proper modal overlay on canvases and org-chart pages
 */

import { test, expect } from '@playwright/test'

test.describe('BaseDialog Modal Rendering', () => {
  test('canvases page - dialog should render as modal overlay', async ({ page }) => {
    await page.goto('/app/canvases')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Click New Canvas button
    await page.locator('button', { hasText: 'New Canvas' }).click()
    await page.waitForTimeout(500)

    // Check that dialog is rendering correctly
    const dialogCheck = await page.evaluate(() => {
      // Look for role="dialog"
      const dialog = document.querySelector('[role="dialog"]')
      if (!dialog) return { hasDialog: false, error: 'No dialog with role="dialog" found' }

      // Check for fixed overlay
      const fixedOverlay = document.querySelector('.fixed.inset-0')
      if (!fixedOverlay) return { hasDialog: true, error: 'No fixed overlay found' }

      // Check for backdrop
      const backdrop = document.querySelector('.bg-black\\/60, .backdrop-blur-sm')

      // Check computed styles
      const overlayStyle = getComputedStyle(fixedOverlay)

      return {
        hasDialog: true,
        hasFixedOverlay: overlayStyle.position === 'fixed',
        hasBackdrop: !!backdrop,
        dialogClasses: dialog.className,
        zIndex: overlayStyle.zIndex,
        overlayPosition: overlayStyle.position
      }
    })

    console.log('Canvas dialog check:', JSON.stringify(dialogCheck, null, 2))

    expect(dialogCheck.hasDialog).toBe(true)
    expect(dialogCheck.hasFixedOverlay).toBe(true)
    expect(dialogCheck.error).toBeUndefined()

    // Take screenshot
    await page.screenshot({ path: 'tests/e2e/screenshots/canvases-dialog-modal.png', fullPage: true })

    // Test clicking inside dialog doesn't close it
    const nameInput = page.locator('input[placeholder*="Company Business Model"], input[placeholder*="Product"]').first()
    await nameInput.click()
    await page.waitForTimeout(300)

    // Dialog should still be visible
    const dialogStillVisible = await page.locator('[role="dialog"]').isVisible()
    expect(dialogStillVisible).toBe(true)

    // Test ESC key closes the dialog
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)
    const dialogClosed = await page.locator('[role="dialog"]').isVisible()
    expect(dialogClosed).toBe(false)
  })

  test('org-chart page - vacancy dialog should render as modal overlay', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Look for "Add Vacancy" button
    const addVacancyBtn = page.locator('button', { hasText: /Add Vacancy|Add First Position/ }).first()
    const btnExists = await addVacancyBtn.isVisible().catch(() => false)

    if (!btnExists) {
      console.log('No Add Vacancy button found on org-chart page - skipping test')
      test.skip()
      return
    }

    await addVacancyBtn.click()
    await page.waitForTimeout(500)

    // Check that dialog is rendering correctly
    const dialogCheck = await page.evaluate(() => {
      const dialog = document.querySelector('[role="dialog"]')
      if (!dialog) return { hasDialog: false, error: 'No dialog found' }

      const fixedOverlay = document.querySelector('.fixed.inset-0')
      const overlayStyle = fixedOverlay ? getComputedStyle(fixedOverlay) : null

      return {
        hasDialog: true,
        hasFixedOverlay: overlayStyle?.position === 'fixed',
        dialogClasses: dialog.className,
        zIndex: overlayStyle?.zIndex || 'N/A'
      }
    })

    console.log('Org-chart dialog check:', JSON.stringify(dialogCheck, null, 2))

    expect(dialogCheck.hasDialog).toBe(true)
    expect(dialogCheck.hasFixedOverlay).toBe(true)

    // Take screenshot
    await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-dialog-modal.png', fullPage: true })

    // Test clicking inside dialog doesn't close it
    const positionInput = page.locator('input[placeholder*="Senior Software Engineer"], input#vacantPositionTitle').first()
    if (await positionInput.isVisible()) {
      await positionInput.click()
      await page.waitForTimeout(300)

      // Dialog should still be visible
      const dialogStillVisible = await page.locator('[role="dialog"]').isVisible()
      expect(dialogStillVisible).toBe(true)
    }

    // Test ESC key closes the dialog
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)
    const dialogClosed = await page.locator('[role="dialog"]').isVisible()
    expect(dialogClosed).toBe(false)
  })

  test('canvases page - clicking backdrop closes dialog', async ({ page }) => {
    await page.goto('/app/canvases')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Click New Canvas button
    await page.locator('button', { hasText: 'New Canvas' }).click()
    await page.waitForTimeout(500)

    // Dialog should be visible
    expect(await page.locator('[role="dialog"]').isVisible()).toBe(true)

    // Click on the backdrop (the fixed overlay area outside the dialog)
    // The backdrop has class bg-black/60 and is positioned behind the dialog
    const backdrop = page.locator('.bg-black\\/60.backdrop-blur-sm').first()
    if (await backdrop.isVisible()) {
      await backdrop.click({ position: { x: 10, y: 10 } })
      await page.waitForTimeout(300)

      // Dialog should be closed
      const dialogClosed = await page.locator('[role="dialog"]').isVisible()
      expect(dialogClosed).toBe(false)
    }
  })
})
