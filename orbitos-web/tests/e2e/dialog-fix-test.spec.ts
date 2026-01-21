/**
 * Dialog rendering test - verify BaseDialog shows as modal overlay
 */

import { test, expect } from '@playwright/test'

const SCREENSHOT_DIR = 'tests/e2e/screenshots'

test.describe('Dialog Modal Rendering', () => {
  test('canvases page - New Canvas dialog should be modal overlay', async ({ page }) => {
    // Go to canvases page
    await page.goto('/app/canvases')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Take screenshot of initial state
    await page.screenshot({ path: `${SCREENSHOT_DIR}/dialog-01-canvases-initial.png`, fullPage: true })

    // Click "New Canvas" button
    const newCanvasBtn = page.locator('button', { hasText: 'New Canvas' })
    await expect(newCanvasBtn).toBeVisible()
    console.log('Found New Canvas button')

    await newCanvasBtn.click()
    await page.waitForTimeout(1000)

    // Take screenshot after clicking
    await page.screenshot({ path: `${SCREENSHOT_DIR}/dialog-02-after-click.png`, fullPage: true })

    // Check if backdrop exists (should be fixed position covering entire viewport)
    const backdrop = page.locator('.fixed.inset-0.bg-black\\/60')
    const backdropVisible = await backdrop.isVisible().catch(() => false)
    console.log('Backdrop visible:', backdropVisible)

    // Check if dialog content is in a fixed position container
    const fixedDialog = page.locator('.fixed.inset-0.z-\\[100\\]')
    const fixedDialogVisible = await fixedDialog.isVisible().catch(() => false)
    console.log('Fixed dialog container visible:', fixedDialogVisible)

    // Check for the dialog title
    const dialogTitle = page.locator('text=Create New Canvas')
    const titleVisible = await dialogTitle.isVisible().catch(() => false)
    console.log('Dialog title visible:', titleVisible)

    // Get position of dialog content
    const dialogContent = page.locator('.orbitos-glass').first()
    if (await dialogContent.isVisible()) {
      const box = await dialogContent.boundingBox()
      console.log('Dialog content bounding box:', box)

      // If dialog is a proper modal, it should be roughly centered
      // and not at the bottom of a scrolled page
      if (box) {
        const viewportSize = page.viewportSize()
        console.log('Viewport size:', viewportSize)

        // Check if dialog appears to be centered (within reasonable margin)
        if (viewportSize) {
          const centerY = viewportSize.height / 2
          const dialogCenterY = box.y + box.height / 2
          const isCentered = Math.abs(dialogCenterY - centerY) < 200
          console.log(`Dialog center Y: ${dialogCenterY}, Viewport center Y: ${centerY}, Is centered: ${isCentered}`)
        }
      }
    }

    // Check if form fields are in the dialog
    const canvasNameInput = page.locator('input[placeholder*="Company Business Model"], input[placeholder*="Product"]')
    const inputVisible = await canvasNameInput.isVisible().catch(() => false)
    console.log('Canvas name input visible:', inputVisible)

    // Try to press Escape to close
    await page.keyboard.press('Escape')
    await page.waitForTimeout(500)

    // Check if dialog closed
    const dialogAfterEscape = page.locator('text=Create New Canvas')
    const stillVisible = await dialogAfterEscape.isVisible().catch(() => false)
    console.log('Dialog visible after Escape:', stillVisible)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/dialog-03-after-escape.png`, fullPage: true })
  })

  test('org-chart page - Add Vacancy dialog should be modal overlay', async ({ page }) => {
    // Go to org-chart page
    await page.goto('/app/people/org-chart')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Take screenshot of initial state
    await page.screenshot({ path: `${SCREENSHOT_DIR}/dialog-04-orgchart-initial.png`, fullPage: true })

    // Click "Add Vacancy" button
    const addVacancyBtn = page.locator('button', { hasText: 'Add Vacancy' })
    const btnVisible = await addVacancyBtn.isVisible().catch(() => false)
    console.log('Add Vacancy button visible:', btnVisible)

    if (btnVisible) {
      await addVacancyBtn.click()
      await page.waitForTimeout(1000)

      // Take screenshot after clicking
      await page.screenshot({ path: `${SCREENSHOT_DIR}/dialog-05-orgchart-after-click.png`, fullPage: true })

      // Check if backdrop exists
      const backdrop = page.locator('.fixed.inset-0.bg-black\\/60')
      const backdropVisible = await backdrop.isVisible().catch(() => false)
      console.log('Backdrop visible:', backdropVisible)

      // Check for the dialog title
      const dialogTitle = page.locator('text=Add Vacant Position')
      const titleVisible = await dialogTitle.isVisible().catch(() => false)
      console.log('Dialog title visible:', titleVisible)

      // Check if Position Title input is visible
      const positionInput = page.locator('input#vacantPositionTitle, input[placeholder*="Senior Software"]')
      const inputVisible = await positionInput.isVisible().catch(() => false)
      console.log('Position title input visible:', inputVisible)
    }
  })

  test('debug DOM structure when dialog is open', async ({ page }) => {
    await page.goto('/app/canvases')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Click New Canvas
    await page.locator('button', { hasText: 'New Canvas' }).click()
    await page.waitForTimeout(1000)

    // Check the body element for teleported content
    const bodyChildren = await page.evaluate(() => {
      const body = document.body
      return {
        childCount: body.children.length,
        lastChildClasses: body.lastElementChild?.className || 'none',
        lastChildTagName: body.lastElementChild?.tagName || 'none',
        hasFixedOverlay: !!document.querySelector('.fixed.inset-0.z-\\[100\\]'),
        hasBackdrop: !!document.querySelector('.fixed.inset-0.bg-black\\/60'),
        allFixedElements: Array.from(document.querySelectorAll('.fixed')).map(el => ({
          classes: el.className,
          zIndex: getComputedStyle(el).zIndex
        }))
      }
    })

    console.log('Body structure:', JSON.stringify(bodyChildren, null, 2))

    // Check if ClientOnly is rendering
    const clientOnlyContent = await page.evaluate(() => {
      // Look for any element that might be the dialog
      const dialogs = document.querySelectorAll('[role="dialog"]')
      return {
        dialogCount: dialogs.length,
        dialogParents: Array.from(dialogs).map(d => d.parentElement?.className || 'none')
      }
    })
    console.log('ClientOnly/Dialog structure:', JSON.stringify(clientOnlyContent, null, 2))
  })
})
