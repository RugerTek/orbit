/**
 * Test to verify the Save Changes button in Edit Reporting dialog
 */

import { test, expect } from '@playwright/test'

test.setTimeout(60000)

test('Edit Reporting Save Changes button works', async ({ page }) => {
  // Navigate to the org chart page specifically
  await page.goto('/app/people/org-chart')
  await page.waitForTimeout(4000)

  // Take initial screenshot
  await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-test-1.png', fullPage: true })

  // Switch to list view immediately (more reliable for clicking than Vue Flow tree)
  const listViewBtn = page.locator('button', { hasText: 'list' })
  if (await listViewBtn.isVisible()) {
    console.log('Switching to list view for reliable clicking')
    await listViewBtn.click()
    await page.waitForTimeout(1000)
  }

  await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-test-1b.png', fullPage: true })

  // Look for the second table row (non-top-level person who has manager options)
  // First row is usually CEO/top-level who can't have a manager
  const tableRows = page.locator('tbody tr')
  const rowCount = await tableRows.count()
  console.log('Found', rowCount, 'rows')

  if (rowCount < 1) {
    console.log('No table rows found')
    await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-no-rows.png', fullPage: true })
    test.skip()
    return
  }

  // Click the second row if available (subordinate), otherwise first row
  const rowToClick = rowCount > 1 ? tableRows.nth(1) : tableRows.first()
  const rowText = await rowToClick.textContent()
  console.log('Clicking row with text:', rowText?.substring(0, 50))
  await rowToClick.click()

  await page.waitForTimeout(1000)
  await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-test-2.png', fullPage: true })

  // Check if the dialog opened
  const dialog = page.locator('[role="dialog"]')
  const dialogVisible = await dialog.isVisible()
  console.log('Dialog visible:', dialogVisible)

  if (!dialogVisible) {
    console.log('No dialog appeared after clicking person')
    test.skip()
    return
  }

  // Check if this is the Edit Reporting dialog
  const dialogTitle = await dialog.locator('h2, .orbitos-heading-sm').first().textContent()
  console.log('Dialog title:', dialogTitle)

  // If it's a vacancy dialog, close and skip
  if (dialogTitle?.includes('Vacant')) {
    console.log('Opened vacancy dialog, not Edit Reporting')
    await page.keyboard.press('Escape')
    test.skip()
    return
  }

  // Find the Reports To dropdown (select element)
  const reportsToSelect = dialog.locator('select#managerId, select').first()
  const hasSelect = await reportsToSelect.isVisible().catch(() => false)
  console.log('Has Reports To select:', hasSelect)

  if (!hasSelect) {
    console.log('No select element found in dialog')
    await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-no-select.png', fullPage: true })
    await page.keyboard.press('Escape')
    test.skip()
    return
  }

  // Get current value
  const currentValue = await reportsToSelect.inputValue()
  console.log('Current manager value:', currentValue)

  // Get all available options
  const options = await reportsToSelect.locator('option').evaluateAll(opts =>
    opts.map(o => ({ value: (o as HTMLOptionElement).value, text: o.textContent }))
  )
  console.log('Manager options:', options)

  // Find a different option to select
  const differentOption = options.find(o => o.value !== currentValue)
  if (!differentOption) {
    console.log('No different option available to test with')
    await page.keyboard.press('Escape')
    test.skip()
    return
  }

  // Change the selection
  console.log('Selecting different manager:', differentOption.text)
  await reportsToSelect.selectOption(differentOption.value)
  await page.waitForTimeout(500)

  await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-test-3.png', fullPage: true })

  // The Save Changes button should now be visible (hasChanges = true)
  const saveButton = dialog.locator('button', { hasText: 'Save Changes' })
  const saveVisible = await saveButton.isVisible()
  console.log('Save Changes button visible:', saveVisible)

  expect(saveVisible).toBe(true)

  // Set up response listener for the PATCH request
  const responsePromise = page.waitForResponse(
    response => response.url().includes('/reporting') && response.request().method() === 'PATCH',
    { timeout: 15000 }
  )

  // Click Save Changes
  console.log('Clicking Save Changes...')
  await saveButton.click()

  // Wait for API response
  try {
    const response = await responsePromise
    console.log('API Response status:', response.status())
    console.log('API Response URL:', response.url())
    console.log('Request method:', response.request().method())

    // Should succeed (2xx status)
    expect(response.status()).toBeLessThan(300)

    // Dialog should close after successful save
    await page.waitForTimeout(1000)
    const dialogClosed = !(await dialog.isVisible())
    console.log('Dialog closed after save:', dialogClosed)
    expect(dialogClosed).toBe(true)

    await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-test-4.png', fullPage: true })
    console.log('Test passed! Save Changes button works correctly.')
  } catch (e) {
    console.log('Error during save:', e)
    await page.screenshot({ path: 'tests/e2e/screenshots/org-chart-test-error.png', fullPage: true })
    throw e
  }
})
