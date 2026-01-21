/**
 * Test to verify that BaseDialog content is NOT rendered when the dialog is closed.
 * This tests the fix for the issue where form content appeared at the bottom of every page.
 */

import { test, expect } from '@playwright/test'

const SCREENSHOT_DIR = 'tests/e2e/screenshots'

test.describe('Dialog Content Should Not Leak', () => {
  // Increased timeout for these tests
  test.setTimeout(60000)

  test('resources page - no form elements visible when dialog is closed', async ({ page }) => {
    // Go directly to resources page
    await page.goto('/app/resources')
    // Use domcontentloaded instead of networkidle to avoid timeout
    await page.waitForLoadState('domcontentloaded')
    // Wait for Vue app to initialize
    await page.waitForTimeout(4000)

    // Take full page screenshot
    await page.screenshot({ path: `${SCREENSHOT_DIR}/stray-01-resources-page.png`, fullPage: true })

    // The resources page has a BaseDialog for adding/editing resources.
    // When the dialog is closed, we should NOT see the dialog's form content.

    // Check for specific form labels that would indicate the dialog content leaked
    const strayLabels = await page.evaluate(() => {
      // Look for label elements in main content (not in teleported dialog)
      const mainArea = document.querySelector('main')
      if (!mainArea) return { found: false, labels: [], noMain: true }

      const labels = mainArea.querySelectorAll('label')
      const labelTexts = Array.from(labels).map(l => l.textContent?.trim() || '')

      // Check for labels that belong to the resource form dialog
      const dialogLabels = ['Name', 'Description', 'Type', 'Status']
      const foundDialogLabels = labelTexts.filter(t => dialogLabels.some(dl => t.includes(dl)))

      return {
        found: foundDialogLabels.length > 0,
        labels: foundDialogLabels,
        allLabels: labelTexts,
        noMain: false
      }
    })

    console.log('Stray labels check:', JSON.stringify(strayLabels, null, 2))

    // Check for input elements in main that would be from the dialog
    const strayInputs = await page.evaluate(() => {
      const mainArea = document.querySelector('main')
      if (!mainArea) return { found: false, inputs: [] }

      const inputs = mainArea.querySelectorAll('input[type="text"], textarea')
      const inputInfo = Array.from(inputs).map(i => ({
        placeholder: (i as HTMLInputElement).placeholder,
        id: i.id,
        name: (i as HTMLInputElement).name
      }))

      // Check for inputs that belong to the resource form dialog
      const dialogInputPlaceholders = ['Resource name', 'Optional description']
      const foundDialogInputs = inputInfo.filter(i =>
        dialogInputPlaceholders.some(dp => i.placeholder?.includes(dp))
      )

      return {
        found: foundDialogInputs.length > 0,
        inputs: foundDialogInputs,
        allInputs: inputInfo
      }
    })

    console.log('Stray inputs check:', JSON.stringify(strayInputs, null, 2))

    // The main content should NOT have the form labels or inputs when dialog is closed
    // If this fails, the dialog content is leaking outside the teleport
    expect(strayLabels.found, 'Found stray form labels in main content').toBe(false)
    expect(strayInputs.found, 'Found stray form inputs in main content').toBe(false)
  })

  test('canvases page - no form elements visible when dialog is closed', async ({ page }) => {
    await page.goto('/app/canvases')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(4000)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/stray-02-canvases-page.png`, fullPage: true })

    // Check for stray form elements
    const strayContent = await page.evaluate(() => {
      const mainArea = document.querySelector('main')
      if (!mainArea) return { hasStrayForm: false, noMain: true }

      // Look for form structure that should only be in dialog
      const forms = mainArea.querySelectorAll('form')
      const hasCreateCanvasForm = Array.from(forms).some(form => {
        const inputs = form.querySelectorAll('input, textarea, select')
        return inputs.length > 2 // A form with multiple inputs likely from dialog
      })

      // Check for labels that would be in the canvas creation dialog
      const labels = mainArea.querySelectorAll('label')
      const labelTexts = Array.from(labels).map(l => l.textContent?.trim() || '')
      const dialogLabels = ['Canvas Name', 'Description', 'Scope', 'Type']
      const hasDialogLabels = labelTexts.some(t => dialogLabels.some(dl => t.includes(dl)))

      return {
        hasStrayForm: hasCreateCanvasForm,
        hasDialogLabels,
        formCount: forms.length,
        labelTexts,
        noMain: false
      }
    })

    console.log('Canvases page stray content:', JSON.stringify(strayContent, null, 2))

    // Should not have dialog form visible when dialog is closed
    expect(strayContent.hasDialogLabels, 'Found dialog labels in main content').toBe(false)
  })

  test('overview page - no create org dialog content visible', async ({ page }) => {
    await page.goto('/app')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(4000)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/stray-03-overview-page.png`, fullPage: true })

    // Check specifically for the organization creation dialog content
    const strayOrgForm = await page.evaluate(() => {
      // Look for organization-specific form labels
      const body = document.body
      const labels = body.querySelectorAll('label')
      const labelTexts = Array.from(labels).map(l => l.textContent?.trim() || '')

      // The create org dialog has "Organization Name" label
      const hasOrgNameLabel = labelTexts.some(t => t.includes('Organization Name'))

      // Check for the placeholder text
      const inputs = body.querySelectorAll('input')
      const hasOrgPlaceholder = Array.from(inputs).some(i =>
        (i as HTMLInputElement).placeholder?.includes('Acme Corporation')
      )

      return {
        hasOrgNameLabel,
        hasOrgPlaceholder,
        labelTexts: labelTexts.filter(t => t.includes('Organization') || t.includes('Name'))
      }
    })

    console.log('Org dialog stray content:', JSON.stringify(strayOrgForm, null, 2))

    // The organization form should not be visible when dialog is closed
    expect(strayOrgForm.hasOrgPlaceholder, 'Found org dialog placeholder in body').toBe(false)
  })

  test('verify teleport is working correctly', async ({ page }) => {
    await page.goto('/app/resources')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(4000)

    // When dialog is closed, there should be NO teleported content in body
    const teleportedContent = await page.evaluate(() => {
      // Get direct children of body that might be teleported dialogs
      const bodyDirectChildren = Array.from(document.body.children)

      // Filter out the app root and known elements
      const potentialTeleports = bodyDirectChildren.filter(el => {
        const tagName = el.tagName.toLowerCase()
        // Skip script, style, and the main app div
        if (tagName === 'script' || tagName === 'style' || tagName === 'link') return false
        if (el.id === '__nuxt' || el.id === 'app' || el.id === 'teleports') return false
        return true
      })

      return {
        potentialTeleportCount: potentialTeleports.length,
        teleportClasses: potentialTeleports.map(el => el.className),
        hasDialogRole: !!document.querySelector('[role="dialog"]'),
        hasBackdrop: !!document.querySelector('.bg-black\\/60')
      }
    })

    console.log('Teleport analysis:', JSON.stringify(teleportedContent, null, 2))

    // When dialog is closed, there should be no dialog role element
    expect(teleportedContent.hasDialogRole, 'Found dialog role when dialog should be closed').toBe(false)
    expect(teleportedContent.hasBackdrop, 'Found backdrop when dialog should be closed').toBe(false)
  })

  test('click Add Resource and verify dialog appears as modal', async ({ page }) => {
    await page.goto('/app/resources')
    await page.waitForLoadState('domcontentloaded')
    await page.waitForTimeout(4000)

    // Before clicking - verify no dialog content
    let beforeClick = await page.evaluate(() => ({
      hasDialogRole: !!document.querySelector('[role="dialog"]'),
      hasBackdrop: !!document.querySelector('.bg-black\\/60')
    }))
    console.log('Before click:', beforeClick)
    expect(beforeClick.hasDialogRole).toBe(false)

    // Click Add Resource
    const addBtn = page.locator('button:has-text("Add Resource")')
    if (await addBtn.isVisible()) {
      await addBtn.click()
      await page.waitForTimeout(500)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/stray-04-dialog-open.png`, fullPage: true })

      // After clicking - dialog should be visible
      let afterClick = await page.evaluate(() => ({
        hasDialogRole: !!document.querySelector('[role="dialog"]'),
        hasBackdrop: !!document.querySelector('.bg-black\\/60'),
        dialogInBody: document.body.querySelector('[role="dialog"]')?.parentElement?.tagName === 'DIV'
      }))
      console.log('After click:', afterClick)
      expect(afterClick.hasDialogRole).toBe(true)
      expect(afterClick.hasBackdrop).toBe(true)

      // Close with Escape
      await page.keyboard.press('Escape')
      await page.waitForTimeout(500)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/stray-05-dialog-closed.png`, fullPage: true })

      // After closing - dialog should NOT be visible
      let afterClose = await page.evaluate(() => ({
        hasDialogRole: !!document.querySelector('[role="dialog"]'),
        hasBackdrop: !!document.querySelector('.bg-black\\/60')
      }))
      console.log('After close:', afterClose)
      expect(afterClose.hasDialogRole).toBe(false)
      expect(afterClose.hasBackdrop).toBe(false)
    } else {
      console.log('Add Resource button not found - page might require auth')
    }
  })
})
