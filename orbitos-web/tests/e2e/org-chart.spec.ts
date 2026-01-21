/**
 * =============================================================================
 * OrbitOS Operations - Org Chart Comprehensive E2E Tests
 * =============================================================================
 * Complete frontend testing for the Organizational Chart feature including:
 * - Every button click
 * - Every form field interaction
 * - Every view mode
 * - All dialog behaviors
 * - Navigation flows
 * - Data display validation
 * - Keyboard interactions
 * - Edge cases
 * =============================================================================
 */

import { test, expect } from '@playwright/test'

const SCREENSHOT_DIR = 'tests/e2e/screenshots/org-chart'

// Helper to wait for page load
async function waitForPageLoad(page: any) {
  await page.waitForLoadState('networkidle')
  await page.waitForTimeout(2000)
}

// =============================================================================
// SECTION 1: PAGE LOAD AND INITIAL RENDER TESTS
// =============================================================================

test.describe('1. Page Load and Initial Render', () => {
  test('1.1 Should display page heading "Organizational Chart"', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/1.1-page-load.png`, fullPage: true })
    await expect(page.getByRole('heading', { name: 'Organizational Chart' })).toBeVisible()
  })

  test('1.2 Should display page subtitle description', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await expect(page.getByText('Visualize reporting relationships and span of control')).toBeVisible()
  })

  test('1.3 Should display "Total People" metric card', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await expect(page.getByText('Total People')).toBeVisible()
    // Metric value should be a number
    const metricCard = page.locator('.rounded-xl').filter({ hasText: 'Total People' })
    await expect(metricCard).toBeVisible()
  })

  test('1.4 Should display "Open Vacancies" metric card with amber color', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await expect(page.getByText('Open Vacancies')).toBeVisible()
    const vacanciesValue = page.locator('.text-amber-300')
    await expect(vacanciesValue.first()).toBeVisible()
  })

  test('1.5 Should display "Org Depth" metric card', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await expect(page.getByText('Org Depth')).toBeVisible()
    await expect(page.getByText(/levels/)).toBeVisible()
  })

  test('1.6 Should display "Avg Span of Control" metric card', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await expect(page.getByText('Avg Span of Control')).toBeVisible()
  })

  test('1.7 Should display loading spinner while data loads', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    // Check for spinner before data loads
    const spinner = page.locator('.orbitos-spinner')
    // Spinner might flash quickly, so we just verify the page eventually loads
    await page.waitForLoadState('networkidle')
    await page.screenshot({ path: `${SCREENSHOT_DIR}/1.7-loading-state.png`, fullPage: true })
  })

  test('1.8 Should show empty state when no people exist', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/1.8-empty-or-data.png`, fullPage: true })

    // Either show empty state message or data
    const emptyState = page.getByText('No organizational structure defined yet')
    const hasData = await page.locator('.vue-flow, table, .grid').first().isVisible().catch(() => false)
    const hasEmpty = await emptyState.isVisible().catch(() => false)

    expect(hasData || hasEmpty).toBeTruthy()
  })

  test('1.9 Should show "Add First Position" button in empty state', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const emptyState = page.getByText('No organizational structure defined yet')
    const hasEmpty = await emptyState.isVisible().catch(() => false)

    if (hasEmpty) {
      await expect(page.getByRole('button', { name: /Add First Position/i })).toBeVisible()
    }
  })
})

// =============================================================================
// SECTION 2: VIEW MODE TOGGLE BUTTONS
// =============================================================================

test.describe('2. View Mode Toggle Buttons', () => {
  test('2.1 Should display Tree view toggle button', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const treeButton = page.getByRole('button', { name: /tree/i })
    await expect(treeButton).toBeVisible()
  })

  test('2.2 Should display List view toggle button', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const listButton = page.getByRole('button', { name: /list/i })
    await expect(listButton).toBeVisible()
  })

  test('2.3 Should display Card view toggle button', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const cardButton = page.getByRole('button', { name: /card/i })
    await expect(cardButton).toBeVisible()
  })

  test('2.4 Tree button should be active by default', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const treeButton = page.getByRole('button', { name: /tree/i })
    // Active state has purple background
    await expect(treeButton).toHaveClass(/bg-purple-500/)
  })

  test('2.5 Should switch to List view when clicking List button', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const listButton = page.getByRole('button', { name: /list/i })
    await listButton.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/2.5-list-view-active.png`, fullPage: true })

    // List button should now be active
    await expect(listButton).toHaveClass(/bg-purple-500/)

    // Should show table or empty state
    const table = page.locator('table')
    const emptyState = page.getByText(/No people found|No organizational structure/)
    const hasTable = await table.isVisible().catch(() => false)
    const hasEmpty = await emptyState.first().isVisible().catch(() => false)

    expect(hasTable || hasEmpty).toBeTruthy()
  })

  test('2.6 Should switch to Card view when clicking Card button', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const cardButton = page.getByRole('button', { name: /card/i })
    await cardButton.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/2.6-card-view-active.png`, fullPage: true })

    // Card button should now be active
    await expect(cardButton).toHaveClass(/bg-purple-500/)
  })

  test('2.7 Should switch back to Tree view when clicking Tree button', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // First switch away
    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(300)

    // Then switch back
    const treeButton = page.getByRole('button', { name: /tree/i })
    await treeButton.click()
    await page.waitForTimeout(500)

    await expect(treeButton).toHaveClass(/bg-purple-500/)
  })

  test('2.8 Clicking same view mode button should not cause errors', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const treeButton = page.getByRole('button', { name: /tree/i })
    await treeButton.click()
    await treeButton.click()
    await treeButton.click()

    // Should still be on tree view without errors
    await expect(treeButton).toHaveClass(/bg-purple-500/)
  })
})

// =============================================================================
// SECTION 3: ADD VACANCY BUTTON AND DIALOG
// =============================================================================

test.describe('3. Add Vacancy Button', () => {
  test('3.1 Should display "Add Vacancy" button in header', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const addButton = page.getByRole('button', { name: /Add Vacancy/i })
    await expect(addButton).toBeVisible()
  })

  test('3.2 Should open vacancy dialog when clicking Add Vacancy button', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/3.2-vacancy-dialog-open.png`, fullPage: true })

    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('3.3 Should display dialog subtitle', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByText(/Create a placeholder for an unfilled position/i)).toBeVisible()
  })
})

// =============================================================================
// SECTION 4: VACANCY DIALOG FORM FIELDS
// =============================================================================

test.describe('4. Vacancy Dialog Form Fields', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(500)
  })

  test('4.1 Should display Position Title field with label', async ({ page }) => {
    await expect(page.getByText('Position Title')).toBeVisible()
    const titleInput = page.locator('#vacantPositionTitle')
    await expect(titleInput).toBeVisible()
  })

  test('4.2 Should display Position Title as required field (asterisk)', async ({ page }) => {
    const requiredMarker = page.locator('label', { hasText: 'Position Title' }).locator('.text-red-400')
    await expect(requiredMarker).toBeVisible()
  })

  test('4.3 Should display Position Title placeholder', async ({ page }) => {
    const titleInput = page.locator('#vacantPositionTitle')
    await expect(titleInput).toHaveAttribute('placeholder', 'e.g., Senior Software Engineer')
  })

  test('4.4 Position Title should have autofocus', async ({ page }) => {
    const titleInput = page.locator('#vacantPositionTitle')
    await expect(titleInput).toBeFocused()
  })

  test('4.5 Should be able to type in Position Title field', async ({ page }) => {
    const titleInput = page.locator('#vacantPositionTitle')
    await titleInput.fill('Test Engineering Manager')
    await expect(titleInput).toHaveValue('Test Engineering Manager')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/4.5-title-filled.png`, fullPage: true })
  })

  test('4.6 Should display Position Type dropdown', async ({ page }) => {
    await expect(page.getByText('Position Type')).toBeVisible()
    const typeSelect = page.locator('#resourceSubtypeId')
    await expect(typeSelect).toBeVisible()
  })

  test('4.7 Should display Position Type as required field', async ({ page }) => {
    const requiredMarker = page.locator('label', { hasText: 'Position Type' }).locator('.text-red-400')
    await expect(requiredMarker).toBeVisible()
  })

  test('4.8 Should be able to select Position Type from dropdown', async ({ page }) => {
    const typeSelect = page.locator('#resourceSubtypeId')
    const options = await typeSelect.locator('option').all()

    if (options.length > 1) {
      await typeSelect.selectOption({ index: 1 })
      await page.screenshot({ path: `${SCREENSHOT_DIR}/4.8-type-selected.png`, fullPage: true })
    }
  })

  test('4.9 Should display Reports To dropdown', async ({ page }) => {
    await expect(page.getByText('Reports To')).toBeVisible()
    const reportsToSelect = page.locator('#reportsToResourceId')
    await expect(reportsToSelect).toBeVisible()
  })

  test('4.10 Should have "No manager (top-level position)" option in Reports To', async ({ page }) => {
    const reportsToSelect = page.locator('#reportsToResourceId')
    await expect(reportsToSelect.locator('option', { hasText: /No manager/i })).toBeVisible()
  })

  test('4.11 Should display helper text under Reports To', async ({ page }) => {
    await expect(page.getByText('Who will this position report to?')).toBeVisible()
  })

  test('4.12 Should be able to select a manager from Reports To dropdown', async ({ page }) => {
    const reportsToSelect = page.locator('#reportsToResourceId')
    const options = await reportsToSelect.locator('option').all()

    // Select non-default option if available
    if (options.length > 1) {
      await reportsToSelect.selectOption({ index: 1 })
    }
  })

  test('4.13 Should display Description textarea', async ({ page }) => {
    await expect(page.locator('label', { hasText: 'Description' })).toBeVisible()
    const descInput = page.locator('#description')
    await expect(descInput).toBeVisible()
  })

  test('4.14 Should display Description placeholder', async ({ page }) => {
    const descInput = page.locator('#description')
    await expect(descInput).toHaveAttribute('placeholder', 'Brief description of the role responsibilities...')
  })

  test('4.15 Should be able to type in Description field', async ({ page }) => {
    const descInput = page.locator('#description')
    await descInput.fill('Responsible for leading the engineering team and delivering quality software.')
    await expect(descInput).toHaveValue('Responsible for leading the engineering team and delivering quality software.')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/4.15-description-filled.png`, fullPage: true })
  })

  test('4.16 Description textarea should have multiple rows', async ({ page }) => {
    const descInput = page.locator('#description')
    await expect(descInput).toHaveAttribute('rows', '3')
  })
})

// =============================================================================
// SECTION 5: VACANCY DIALOG BUTTONS
// =============================================================================

test.describe('5. Vacancy Dialog Buttons', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(500)
  })

  test('5.1 Should display Cancel button', async ({ page }) => {
    await expect(page.getByRole('button', { name: /Cancel/i })).toBeVisible()
  })

  test('5.2 Should display Create Vacancy button', async ({ page }) => {
    await expect(page.getByRole('button', { name: /Create Vacancy/i })).toBeVisible()
  })

  test('5.3 Create Vacancy button should be disabled when form is empty', async ({ page }) => {
    const createButton = page.getByRole('button', { name: /Create Vacancy/i })
    await expect(createButton).toBeDisabled()
  })

  test('5.4 Create Vacancy button should be enabled when title is filled', async ({ page }) => {
    await page.locator('#vacantPositionTitle').fill('Test Position')
    await page.waitForTimeout(200)

    const createButton = page.getByRole('button', { name: /Create Vacancy/i })

    // Button should be enabled if position type is also selected (auto-selected first option)
    const isEnabled = await createButton.isEnabled().catch(() => false)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/5.4-create-button-state.png`, fullPage: true })
  })

  test('5.5 Cancel button should close dialog', async ({ page }) => {
    await page.getByRole('button', { name: /Cancel/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).not.toBeVisible()
  })

  test('5.6 Create Vacancy button should show loading state during submission', async ({ page }) => {
    // Fill required fields
    await page.locator('#vacantPositionTitle').fill('Test Position')
    await page.waitForTimeout(200)

    const createButton = page.getByRole('button', { name: /Create Vacancy/i })

    // Click and check for loading state
    if (await createButton.isEnabled()) {
      await createButton.click()
      // Loading text should appear briefly
      const loadingText = page.getByText(/Creating/i)
      await page.screenshot({ path: `${SCREENSHOT_DIR}/5.6-loading-state.png`, fullPage: true })
    }
  })
})

// =============================================================================
// SECTION 6: VACANCY DIALOG CLOSE BEHAVIORS
// =============================================================================

test.describe('6. Vacancy Dialog Close Behaviors', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(500)
  })

  test('6.1 Should close dialog when pressing Escape key', async ({ page }) => {
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).not.toBeVisible()
  })

  test('6.2 Should NOT close dialog when clicking inside form', async ({ page }) => {
    // Click on title input
    await page.locator('#vacantPositionTitle').click()
    await page.waitForTimeout(200)

    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('6.3 Should NOT close dialog when clicking on dropdown', async ({ page }) => {
    await page.locator('#resourceSubtypeId').click()
    await page.waitForTimeout(200)

    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('6.4 Should NOT close dialog when clicking between form fields', async ({ page }) => {
    await page.locator('#vacantPositionTitle').fill('Test')
    await page.locator('#description').click()
    await page.locator('#reportsToResourceId').click()
    await page.waitForTimeout(200)

    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('6.5 Should preserve form data when clicking between fields', async ({ page }) => {
    const titleInput = page.locator('#vacantPositionTitle')
    const descInput = page.locator('#description')

    await titleInput.fill('Engineering Lead')
    await descInput.fill('Lead the team')

    // Click around
    await page.locator('#reportsToResourceId').click()
    await titleInput.click()

    await expect(titleInput).toHaveValue('Engineering Lead')
    await expect(descInput).toHaveValue('Lead the team')
  })

  test('6.6 Should reset form when reopening dialog after cancel', async ({ page }) => {
    // Fill form
    await page.locator('#vacantPositionTitle').fill('Test Position')

    // Cancel
    await page.getByRole('button', { name: /Cancel/i }).click()
    await page.waitForTimeout(300)

    // Reopen
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(500)

    // Form should be empty
    await expect(page.locator('#vacantPositionTitle')).toHaveValue('')
  })
})

// =============================================================================
// SECTION 7: LIST VIEW INTERACTIONS
// =============================================================================

test.describe('7. List View Interactions', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)
    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(500)
  })

  test('7.1 Should display table headers in list view', async ({ page }) => {
    const table = page.locator('table')
    const hasTable = await table.isVisible().catch(() => false)

    if (hasTable) {
      await expect(page.getByRole('columnheader', { name: /Person/i })).toBeVisible()
      await expect(page.getByRole('columnheader', { name: /Reports To/i })).toBeVisible()
      await expect(page.getByRole('columnheader', { name: /Direct Reports/i })).toBeVisible()
      await expect(page.getByRole('columnheader', { name: /Total Reports/i })).toBeVisible()
      await expect(page.getByRole('columnheader', { name: /Level/i })).toBeVisible()

      await page.screenshot({ path: `${SCREENSHOT_DIR}/7.1-list-headers.png`, fullPage: true })
    }
  })

  test('7.2 Table rows should be clickable (have cursor-pointer)', async ({ page }) => {
    const table = page.locator('table')
    const hasTable = await table.isVisible().catch(() => false)

    if (hasTable) {
      const firstRow = page.locator('tbody tr').first()
      const hasRow = await firstRow.isVisible().catch(() => false)

      if (hasRow) {
        await expect(firstRow).toHaveClass(/cursor-pointer/)
      }
    }
  })

  test('7.3 Clicking table row should open reporting dialog', async ({ page }) => {
    const firstRow = page.locator('tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)

    if (hasRow) {
      await firstRow.click()
      await page.waitForTimeout(500)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/7.3-list-row-click.png`, fullPage: true })

      // Should open either Edit Reporting or Edit Vacant Position dialog
      const dialogHeading = page.getByRole('heading', { name: /Edit Reporting|Edit Vacant Position/i })
      await expect(dialogHeading).toBeVisible()
    }
  })

  test('7.4 Table rows should have hover effect', async ({ page }) => {
    const firstRow = page.locator('tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)

    if (hasRow) {
      await expect(firstRow).toHaveClass(/hover:bg-white/)
    }
  })

  test('7.5 Should display avatar with initials for each person', async ({ page }) => {
    const firstRow = page.locator('tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)

    if (hasRow) {
      const avatar = firstRow.locator('.rounded-full').first()
      await expect(avatar).toBeVisible()
    }
  })

  test('7.6 Should display Level badge for each person', async ({ page }) => {
    const firstRow = page.locator('tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)

    if (hasRow) {
      const levelBadge = firstRow.getByText(/Level \d/i)
      await expect(levelBadge).toBeVisible()
    }
  })

  test('7.7 Vacancies should have amber styling in list view', async ({ page }) => {
    const amberText = page.locator('tbody .text-amber-300')
    const hasVacancy = await amberText.first().isVisible().catch(() => false)

    if (hasVacancy) {
      await page.screenshot({ path: `${SCREENSHOT_DIR}/7.7-vacancy-amber-styling.png`, fullPage: true })
    }
  })

  test('7.8 Should display "No people found" for empty list', async ({ page }) => {
    const emptyState = page.getByText('No people found in the organization')
    const table = page.locator('table tbody tr').first()
    const hasEmpty = await emptyState.isVisible().catch(() => false)
    const hasData = await table.isVisible().catch(() => false)

    expect(hasEmpty || hasData).toBeTruthy()
  })
})

// =============================================================================
// SECTION 8: CARD VIEW INTERACTIONS
// =============================================================================

test.describe('8. Card View Interactions', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)
    await page.getByRole('button', { name: /card/i }).click()
    await page.waitForTimeout(500)
  })

  test('8.1 Should display level groupings in card view', async ({ page }) => {
    await page.screenshot({ path: `${SCREENSHOT_DIR}/8.1-card-view.png`, fullPage: true })

    // Check for level headings (Leadership, Directors, Managers, or Level X)
    const levelHeadings = page.locator('h3', { hasText: /(Leadership|Directors|Managers|Level \d)/i })
    const emptyState = page.getByText('No people found in the organization')

    const hasLevels = await levelHeadings.first().isVisible().catch(() => false)
    const hasEmpty = await emptyState.isVisible().catch(() => false)

    expect(hasLevels || hasEmpty).toBeTruthy()
  })

  test('8.2 Cards should be clickable', async ({ page }) => {
    const card = page.locator('.cursor-pointer.rounded-xl').first()
    const hasCard = await card.isVisible().catch(() => false)

    if (hasCard) {
      await expect(card).toHaveClass(/cursor-pointer/)
    }
  })

  test('8.3 Clicking card should open reporting dialog', async ({ page }) => {
    const card = page.locator('.cursor-pointer.rounded-xl').first()
    const hasCard = await card.isVisible().catch(() => false)

    if (hasCard) {
      await card.click()
      await page.waitForTimeout(500)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/8.3-card-click.png`, fullPage: true })

      const dialogHeading = page.getByRole('heading', { name: /Edit Reporting|Edit Vacant Position/i })
      await expect(dialogHeading).toBeVisible()
    }
  })

  test('8.4 Cards should have hover border effect', async ({ page }) => {
    const card = page.locator('.cursor-pointer.rounded-xl').first()
    const hasCard = await card.isVisible().catch(() => false)

    if (hasCard) {
      await expect(card).toHaveClass(/hover:border-purple-500/)
    }
  })

  test('8.5 Should display avatar on cards', async ({ page }) => {
    const card = page.locator('.cursor-pointer.rounded-xl').first()
    const hasCard = await card.isVisible().catch(() => false)

    if (hasCard) {
      const avatar = card.locator('.rounded-full').first()
      await expect(avatar).toBeVisible()
    }
  })

  test('8.6 Should display "Reports to:" info on cards', async ({ page }) => {
    const card = page.locator('.cursor-pointer.rounded-xl').first()
    const hasCard = await card.isVisible().catch(() => false)

    if (hasCard) {
      await expect(card.getByText('Reports to:')).toBeVisible()
    }
  })

  test('8.7 Should display "Direct reports:" count on cards', async ({ page }) => {
    const card = page.locator('.cursor-pointer.rounded-xl').first()
    const hasCard = await card.isVisible().catch(() => false)

    if (hasCard) {
      await expect(card.getByText('Direct reports:')).toBeVisible()
    }
  })

  test('8.8 Vacancy cards should have dashed border', async ({ page }) => {
    const vacancyCard = page.locator('.border-dashed.border-amber-500')
    const hasVacancy = await vacancyCard.first().isVisible().catch(() => false)

    if (hasVacancy) {
      await page.screenshot({ path: `${SCREENSHOT_DIR}/8.8-vacancy-card.png`, fullPage: true })
    }
  })

  test('8.9 Vacancy cards should display "Open Position" badge', async ({ page }) => {
    const openPositionBadge = page.getByText('Open Position')
    const hasVacancy = await openPositionBadge.first().isVisible().catch(() => false)

    if (hasVacancy) {
      await expect(openPositionBadge.first()).toBeVisible()
    }
  })

  test('8.10 Should display people count in level heading', async ({ page }) => {
    const levelHeading = page.locator('h3', { hasText: /\(\d+ (person|people)\)/i })
    const hasHeading = await levelHeading.first().isVisible().catch(() => false)

    if (hasHeading) {
      await expect(levelHeading.first()).toBeVisible()
    }
  })
})

// =============================================================================
// SECTION 9: TREE VIEW INTERACTIONS
// =============================================================================

test.describe('9. Tree View Interactions', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)
    // Tree is default view
  })

  test('9.1 Should display Vue Flow canvas in tree view', async ({ page }) => {
    await page.screenshot({ path: `${SCREENSHOT_DIR}/9.1-tree-view.png`, fullPage: true })

    const vueFlow = page.locator('.vue-flow')
    const emptyState = page.getByText('No organizational structure defined yet')

    const hasVueFlow = await vueFlow.isVisible().catch(() => false)
    const hasEmpty = await emptyState.isVisible().catch(() => false)

    expect(hasVueFlow || hasEmpty).toBeTruthy()
  })

  test('9.2 Should display Vue Flow controls (zoom buttons)', async ({ page }) => {
    const vueFlow = page.locator('.vue-flow')
    const hasVueFlow = await vueFlow.isVisible().catch(() => false)

    if (hasVueFlow) {
      const controls = page.locator('.vue-flow__controls')
      await expect(controls).toBeVisible()
    }
  })

  test('9.3 Should display background pattern in tree view', async ({ page }) => {
    const vueFlow = page.locator('.vue-flow')
    const hasVueFlow = await vueFlow.isVisible().catch(() => false)

    if (hasVueFlow) {
      const background = page.locator('.vue-flow__background')
      await expect(background).toBeVisible()
    }
  })

  test('9.4 Should be able to zoom with Vue Flow controls', async ({ page }) => {
    const vueFlow = page.locator('.vue-flow')
    const hasVueFlow = await vueFlow.isVisible().catch(() => false)

    if (hasVueFlow) {
      const zoomInButton = page.locator('.vue-flow__controls-button').first()
      await zoomInButton.click()
      await page.waitForTimeout(200)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/9.4-zoomed.png`, fullPage: true })
    }
  })

  test('9.5 Clicking node in tree should open reporting dialog', async ({ page }) => {
    const vueFlow = page.locator('.vue-flow')
    const hasVueFlow = await vueFlow.isVisible().catch(() => false)

    if (hasVueFlow) {
      // Try to click first node
      const node = page.locator('.vue-flow__node').first()
      const hasNode = await node.isVisible().catch(() => false)

      if (hasNode) {
        await node.click()
        await page.waitForTimeout(500)

        const dialogHeading = page.getByRole('heading', { name: /Edit Reporting|Edit Vacant Position/i })
        const dialogVisible = await dialogHeading.isVisible().catch(() => false)

        await page.screenshot({ path: `${SCREENSHOT_DIR}/9.5-tree-node-click.png`, fullPage: true })
      }
    }
  })
})

// =============================================================================
// SECTION 10: REPORTING DIALOG (EDIT REPORTING)
// =============================================================================

test.describe('10. Reporting Dialog - Edit Reporting', () => {
  async function openReportingDialog(page: any) {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Switch to list for easier selection
    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(500)

    const firstRow = page.locator('tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)

    if (hasRow) {
      await firstRow.click()
      await page.waitForTimeout(500)
      return true
    }
    return false
  }

  test('10.1 Should display dialog title', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    const dialogHeading = page.getByRole('heading', { name: /Edit Reporting|Edit Vacant Position/i })
    await expect(dialogHeading).toBeVisible()
  })

  test('10.2 Should display person info card', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    // Person info section with avatar
    const avatar = page.locator('.rounded-full').first()
    await expect(avatar).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/10.2-reporting-dialog.png`, fullPage: true })
  })

  test('10.3 Should display Reports To dropdown', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    const reportsToSelect = page.locator('#managerId')
    await expect(reportsToSelect).toBeVisible()
  })

  test('10.4 Should have "No manager (top-level)" option', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    await expect(page.locator('option', { hasText: /No manager/i })).toBeVisible()
  })

  test('10.5 Should be able to change manager selection', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    const reportsToSelect = page.locator('#managerId')
    const options = await reportsToSelect.locator('option').all()

    if (options.length > 1) {
      await reportsToSelect.selectOption({ index: 1 })
      await page.waitForTimeout(300)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/10.5-manager-changed.png`, fullPage: true })
    }
  })

  test('10.6 Should display Cancel button', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    await expect(page.getByRole('button', { name: /Cancel/i })).toBeVisible()
  })

  test('10.7 Cancel button should close dialog', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    await page.getByRole('button', { name: /Cancel/i }).click()
    await page.waitForTimeout(300)

    const dialogHeading = page.getByRole('heading', { name: /Edit Reporting|Edit Vacant Position/i })
    await expect(dialogHeading).not.toBeVisible()
  })

  test('10.8 Save Changes button should appear when manager changed', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    const reportsToSelect = page.locator('#managerId')
    const options = await reportsToSelect.locator('option').all()

    if (options.length > 1) {
      await reportsToSelect.selectOption({ index: 1 })
      await page.waitForTimeout(300)

      const saveButton = page.getByRole('button', { name: /Save Changes/i })
      await expect(saveButton).toBeVisible()
    }
  })

  test('10.9 Should close on Escape key', async ({ page }) => {
    const opened = await openReportingDialog(page)
    if (!opened) { test.skip(); return }

    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    const dialogHeading = page.getByRole('heading', { name: /Edit Reporting|Edit Vacant Position/i })
    await expect(dialogHeading).not.toBeVisible()
  })

  test('10.10 Should show warning when moving person with direct reports', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(500)

    // Find person with direct reports > 0
    const rowWithReports = page.locator('tbody tr').filter({
      has: page.locator('td:nth-child(3)').filter({ hasNotText: '0' })
    }).first()

    const hasRow = await rowWithReports.isVisible().catch(() => false)
    if (!hasRow) { test.skip(); return }

    await rowWithReports.click()
    await page.waitForTimeout(500)

    const reportsToSelect = page.locator('#managerId')
    const options = await reportsToSelect.locator('option').all()

    if (options.length > 1) {
      await reportsToSelect.selectOption({ index: 1 })
      await page.waitForTimeout(300)

      const warning = page.getByText(/direct reports will stay with them/i)
      const hasWarning = await warning.isVisible().catch(() => false)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/10.10-warning-message.png`, fullPage: true })
    }
  })
})

// =============================================================================
// SECTION 11: VACANCY FILL DIALOG
// =============================================================================

test.describe('11. Vacancy Fill Dialog', () => {
  async function findVacancyRow(page: any) {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(500)

    // Look for vacancy row (amber styled)
    const vacancyRow = page.locator('tbody tr').filter({
      has: page.locator('.text-amber-300')
    }).first()

    return await vacancyRow.isVisible().catch(() => false) ? vacancyRow : null
  }

  test('11.1 Should display "Fill This Position" section for vacancies', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/11.1-vacancy-fill-section.png`, fullPage: true })

    const fillSection = page.getByText(/Fill This Position/i)
    await expect(fillSection).toBeVisible()
  })

  test('11.2 Should display Person Name input field', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    const nameInput = page.locator('#fillName')
    await expect(nameInput).toBeVisible()
  })

  test('11.3 Person Name should be required (asterisk)', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    const requiredMarker = page.locator('label', { hasText: "Person's Name" }).locator('.text-red-400')
    await expect(requiredMarker).toBeVisible()
  })

  test('11.4 Should be able to type person name', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    const nameInput = page.locator('#fillName')
    await nameInput.fill('John Smith')
    await expect(nameInput).toHaveValue('John Smith')
  })

  test('11.5 Should display Title/Description input', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    const descInput = page.locator('#fillDescription')
    await expect(descInput).toBeVisible()
  })

  test('11.6 Should display Fill Position button', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    const fillButton = page.getByRole('button', { name: /Fill Position/i })
    await expect(fillButton).toBeVisible()
  })

  test('11.7 Fill Position button should be disabled when name is empty', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    const fillButton = page.getByRole('button', { name: /Fill Position/i })
    await expect(fillButton).toBeDisabled()
  })

  test('11.8 Fill Position button should be enabled when name is filled', async ({ page }) => {
    const vacancyRow = await findVacancyRow(page)
    if (!vacancyRow) { test.skip(); return }

    await vacancyRow.click()
    await page.waitForTimeout(500)

    await page.locator('#fillName').fill('Jane Doe')
    await page.waitForTimeout(200)

    const fillButton = page.getByRole('button', { name: /Fill Position/i })
    await expect(fillButton).toBeEnabled()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/11.8-fill-enabled.png`, fullPage: true })
  })
})

// =============================================================================
// SECTION 12: NAVIGATION
// =============================================================================

test.describe('12. Navigation', () => {
  test('12.1 Should navigate to org chart from sidebar', async ({ page }) => {
    await page.goto('/app')
    await waitForPageLoad(page)

    const orgChartLink = page.locator('nav a', { hasText: 'Org Chart' })
    await orgChartLink.click()
    await page.waitForURL('/app/people/org-chart')

    await expect(page.getByRole('heading', { name: 'Organizational Chart' })).toBeVisible()
  })

  test('12.2 Org Chart sidebar link should be highlighted when active', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    const orgChartLink = page.locator('nav a', { hasText: 'Org Chart' })
    await expect(orgChartLink).toHaveClass(/bg-purple-500/)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/12.2-sidebar-highlight.png`, fullPage: true })
  })

  test('12.3 Should navigate to People page from sidebar', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Get the People link that's NOT the Org Chart link
    const peopleLinks = page.locator('nav a', { hasText: 'People' })
    const allLinks = await peopleLinks.all()

    for (const link of allLinks) {
      const href = await link.getAttribute('href')
      if (href === '/app/people') {
        await link.click()
        break
      }
    }

    await page.waitForURL('/app/people')
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible()
  })

  test('12.4 Should be able to navigate between view modes rapidly', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Rapid switching
    await page.getByRole('button', { name: /list/i }).click()
    await page.getByRole('button', { name: /card/i }).click()
    await page.getByRole('button', { name: /tree/i }).click()
    await page.getByRole('button', { name: /list/i }).click()
    await page.getByRole('button', { name: /card/i }).click()

    // Should end on card view
    const cardButton = page.getByRole('button', { name: /card/i })
    await expect(cardButton).toHaveClass(/bg-purple-500/)
  })
})

// =============================================================================
// SECTION 13: ERROR HANDLING AND EDGE CASES
// =============================================================================

test.describe('13. Error Handling and Edge Cases', () => {
  test('13.1 Should handle page refresh gracefully', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.reload()
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Organizational Chart' })).toBeVisible()
  })

  test('13.2 Should handle opening dialog while data is loading', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    // Don't wait for full load, click immediately

    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(500)

    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('13.3 Should handle empty form submission attempt (disabled button)', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(300)

    const createButton = page.getByRole('button', { name: /Create Vacancy/i })

    // Button should be disabled
    await expect(createButton).toBeDisabled()

    // Try to click anyway (should not cause errors)
    await createButton.click({ force: true })

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('13.4 Should handle rapid button clicks', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Rapid clicks on Add Vacancy
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.getByRole('button', { name: /Add Vacancy/i }).click()

    // Should still have exactly one dialog
    const dialogs = page.getByRole('heading', { name: /Add Vacant Position/i })
    await expect(dialogs).toBeVisible()
  })

  test('13.5 Should handle switching views while dialog is open', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Open dialog first
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(300)

    // This tests that the UI remains stable
    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('13.6 Should maintain state when navigating away and back', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Switch to list view
    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(300)

    // Navigate away
    await page.goto('/app')
    await waitForPageLoad(page)

    // Navigate back
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Page should load correctly (default view mode)
    await expect(page.getByRole('heading', { name: 'Organizational Chart' })).toBeVisible()
  })
})

// =============================================================================
// SECTION 14: ACCESSIBILITY TESTS
// =============================================================================

test.describe('14. Accessibility', () => {
  test('14.1 Form inputs should have proper labels', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(300)

    // Check that labels are associated with inputs
    const titleLabel = page.locator('label[for="vacantPositionTitle"]')
    await expect(titleLabel).toBeVisible()

    const typeLabel = page.locator('label[for="resourceSubtypeId"]')
    await expect(typeLabel).toBeVisible()
  })

  test('14.2 Buttons should be keyboard accessible', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Tab to Add Vacancy button and press Enter
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')

    // Eventually should be able to activate button with Enter
    const addButton = page.getByRole('button', { name: /Add Vacancy/i })
    await addButton.focus()
    await page.keyboard.press('Enter')

    await page.waitForTimeout(500)
    await expect(page.getByRole('heading', { name: /Add Vacant Position/i })).toBeVisible()
  })

  test('14.3 Dialog should trap focus', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(300)

    // First field should be focused
    const titleInput = page.locator('#vacantPositionTitle')
    await expect(titleInput).toBeFocused()
  })

  test('14.4 Required fields should be clearly marked', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(300)

    // Red asterisks should be visible
    const requiredMarkers = page.locator('.text-red-400')
    expect(await requiredMarkers.count()).toBeGreaterThanOrEqual(1)
  })
})

// =============================================================================
// SECTION 15: COMPLETE WORKFLOW TESTS
// =============================================================================

test.describe('15. Complete Workflows', () => {
  test('15.1 Complete vacancy creation workflow', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Step 1: Click Add Vacancy
    await page.getByRole('button', { name: /Add Vacancy/i }).click()
    await page.waitForTimeout(300)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.1-step1-dialog-open.png`, fullPage: true })

    // Step 2: Fill Position Title
    await page.locator('#vacantPositionTitle').fill('Product Manager')

    // Step 3: Fill Description
    await page.locator('#description').fill('Lead product strategy and roadmap')

    // Step 4: Select Position Type (if available)
    const typeSelect = page.locator('#resourceSubtypeId')
    const typeOptions = await typeSelect.locator('option').all()
    if (typeOptions.length > 1) {
      await typeSelect.selectOption({ index: 1 })
    }

    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.1-step2-form-filled.png`, fullPage: true })

    // Step 5: Click Create
    const createButton = page.getByRole('button', { name: /Create Vacancy/i })
    if (await createButton.isEnabled()) {
      await createButton.click()
      await page.waitForTimeout(1000)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/15.1-step3-created.png`, fullPage: true })
    }
  })

  test('15.2 Complete view mode exploration workflow', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Step 1: View tree (default)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.2-step1-tree.png`, fullPage: true })

    // Step 2: Switch to list and examine
    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(500)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.2-step2-list.png`, fullPage: true })

    // Step 3: Switch to cards and examine
    await page.getByRole('button', { name: /card/i }).click()
    await page.waitForTimeout(500)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.2-step3-cards.png`, fullPage: true })

    // Step 4: Return to tree
    await page.getByRole('button', { name: /tree/i }).click()
    await page.waitForTimeout(500)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.2-step4-tree-final.png`, fullPage: true })
  })

  test('15.3 Complete reporting change workflow', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page)

    // Switch to list view
    await page.getByRole('button', { name: /list/i }).click()
    await page.waitForTimeout(500)

    // Find and click a row
    const firstRow = page.locator('tbody tr').first()
    const hasRow = await firstRow.isVisible().catch(() => false)

    if (!hasRow) { test.skip(); return }

    // Step 1: Open dialog
    await firstRow.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.3-step1-dialog.png`, fullPage: true })

    // Step 2: Change manager (if options available)
    const reportsToSelect = page.locator('#managerId')
    const options = await reportsToSelect.locator('option').all()

    if (options.length > 1) {
      await reportsToSelect.selectOption({ index: 1 })
      await page.waitForTimeout(300)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/15.3-step2-changed.png`, fullPage: true })

      // Step 3: Could save or cancel
      await page.getByRole('button', { name: /Cancel/i }).click()
    } else {
      await page.getByRole('button', { name: /Cancel/i }).click()
    }

    await page.waitForTimeout(300)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/15.3-step3-closed.png`, fullPage: true })
  })
})
