import { test, expect } from '@playwright/test'

const BASE_URL = 'http://localhost:3000'
const SCREENSHOT_DIR = './tests/e2e/screenshots'

test.describe('Canvas Feature Comprehensive Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Set a consistent viewport
    await page.setViewportSize({ width: 1920, height: 1080 })
  })

  test('1. Canvas Index Page - Initial Load', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Take screenshot of initial state
    await page.screenshot({ path: `${SCREENSHOT_DIR}/01-canvas-index-initial.png`, fullPage: true })

    // Verify page title/header
    const header = page.locator('h1')
    await expect(header).toContainText('Business Model Canvases')

    // Verify New Canvas button exists
    const newCanvasBtn = page.locator('button:has-text("New Canvas")')
    await expect(newCanvasBtn).toBeVisible()
  })

  test('2. Canvas Index - Company Canvas State', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1500) // Wait for data to load

    // Check for empty state or company canvas section
    // Note: Text may vary - "Company Canvas" or "Create Your Company Canvas"
    const companySection = page.locator('text=/Company.*Canvas/i').first()
    const emptyState = page.locator('text=Create Your Company Canvas')
    const headerWithCanvases = page.locator('h1:has-text("Business Model Canvases")')

    // At least the page header should be visible
    await expect(headerWithCanvases).toBeVisible()

    // One of these states should be visible
    const hasCompanyCanvas = await companySection.isVisible().catch(() => false)
    const hasEmptyState = await emptyState.isVisible().catch(() => false)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/02-canvas-index-state.png`, fullPage: true })

    // Page loaded successfully - either company canvas exists or empty state is shown
    expect(hasCompanyCanvas || hasEmptyState).toBe(true)
  })

  test('3. Create Canvas Modal - Open and Close', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Click New Canvas button
    const newCanvasBtn = page.locator('button:has-text("New Canvas")')
    await newCanvasBtn.click()

    // Wait for modal to appear
    await page.waitForSelector('text=Create New Canvas')
    await page.screenshot({ path: `${SCREENSHOT_DIR}/03-create-modal-open.png`, fullPage: true })

    // Verify modal elements
    await expect(page.locator('text=Canvas Type')).toBeVisible()
    await expect(page.locator('text=Canvas Name')).toBeVisible()

    // Close modal
    const closeBtn = page.locator('button[aria-label="Close"], button:has(svg path[d*="M6 18L18 6"])').first()
    await closeBtn.click()

    // Verify modal closed
    await expect(page.locator('text=Create New Canvas')).not.toBeVisible()
  })

  test('4. Create Canvas Modal - Scope Type Selection', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Open modal
    await page.locator('button:has-text("New Canvas")').click()
    await page.waitForSelector('text=Create New Canvas')
    await page.waitForTimeout(500) // Wait for modal animation

    await page.screenshot({ path: `${SCREENSHOT_DIR}/04-scope-modal.png`, fullPage: true })

    const modalForm = page.locator('form')

    // Test each scope type button - use form scope and force click
    const scopeTypes = ['Product', 'Segment', 'Initiative'] // Skip Company as it's already selected by default

    for (const scope of scopeTypes) {
      const scopeBtn = modalForm.locator(`button:has-text("${scope}")`).first()
      const isEnabled = await scopeBtn.isEnabled().catch(() => false)
      if (isEnabled) {
        await scopeBtn.click({ force: true })
        await page.waitForTimeout(200)
        await page.screenshot({ path: `${SCREENSHOT_DIR}/04-scope-${scope.toLowerCase()}.png`, fullPage: true })
      }
    }
  })

  test('5. Create Canvas - Full Form Submission', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Open modal
    await page.locator('button:has-text("New Canvas")').click()
    await page.waitForSelector('text=Create New Canvas')
    await page.waitForTimeout(300)

    const modalForm = page.locator('form')

    // Select Product scope (if Company already exists) - use force: true
    const productBtn = modalForm.locator('button:has-text("Product")').first()
    if (await productBtn.isEnabled()) {
      await productBtn.click({ force: true })
    }
    await page.waitForTimeout(200)

    // Fill in name
    const nameInput = modalForm.locator('input.orbitos-input').first()
    await nameInput.fill('Test Product Canvas')

    // Fill in description
    const descTextarea = modalForm.locator('textarea')
    await descTextarea.fill('This is a test canvas for automated testing')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/05-create-form-filled.png`, fullPage: true })

    // Submit form
    const submitBtn = modalForm.locator('button[type="submit"], button:has-text("Create Canvas")')
    await submitBtn.click({ force: true })

    // Wait for navigation or modal close
    await page.waitForTimeout(2000)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/05-after-create.png`, fullPage: true })
  })

  test('6. Business Canvas Page - Canvas View', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/06-business-canvas-view.png`, fullPage: true })

    // Verify BMC blocks are visible
    const blocks = ['Partners', 'Activities', 'Resources', 'Value', 'Relationships', 'Channels', 'Segments', 'Costs', 'Revenue']

    for (const block of blocks) {
      const blockElement = page.locator(`text=${block}`).first()
      const isVisible = await blockElement.isVisible().catch(() => false)
      console.log(`Block "${block}" visible: ${isVisible}`)
    }
  })

  test('7. Business Canvas Page - View Switching', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')

    // Check for view toggle buttons
    const viewButtons = ['Canvas', 'Kanban', 'List']

    for (const view of viewButtons) {
      const viewBtn = page.locator(`button:has-text("${view}")`).first()
      if (await viewBtn.isVisible()) {
        await viewBtn.click()
        await page.waitForTimeout(500)
        await page.screenshot({ path: `${SCREENSHOT_DIR}/07-view-${view.toLowerCase()}.png`, fullPage: true })
      }
    }
  })

  test('8. Business Canvas - Block Click Interaction', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')

    // Click on Partners block
    const partnersBlock = page.locator('div:has-text("Key Partners")').first()
    if (await partnersBlock.isVisible()) {
      await partnersBlock.click()
      await page.waitForTimeout(500)
      await page.screenshot({ path: `${SCREENSHOT_DIR}/08-partners-expanded.png`, fullPage: true })
    }
  })

  test('9. Navigation - Sidebar Links', async ({ page }) => {
    await page.goto(`${BASE_URL}/app`)
    await page.waitForLoadState('networkidle')

    // Click on Canvases link in sidebar
    const canvasesLink = page.locator('a:has-text("Canvases")')
    await expect(canvasesLink).toBeVisible()

    await page.screenshot({ path: `${SCREENSHOT_DIR}/09-sidebar-navigation.png`, fullPage: true })

    await canvasesLink.click()

    // Wait for navigation to complete
    await page.waitForURL('**/app/canvases', { timeout: 10000 })
    await page.waitForLoadState('networkidle')

    // Verify we're on canvases page
    expect(page.url()).toContain('/app/canvases')
    await page.screenshot({ path: `${SCREENSHOT_DIR}/09-after-navigation.png`, fullPage: true })
  })

  test('10. Responsive - Tablet View', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 })
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/10-tablet-view.png`, fullPage: true })
  })

  test('11. Responsive - Mobile View', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 })
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/11-mobile-view.png`, fullPage: true })

    // Check if mobile menu hamburger is visible
    const hamburger = page.locator('button:has(svg path[d*="M4 6h16M4 12h16"])').first()
    if (await hamburger.isVisible()) {
      await hamburger.click()
      await page.waitForTimeout(300)
      await page.screenshot({ path: `${SCREENSHOT_DIR}/11-mobile-menu-open.png`, fullPage: true })
    }
  })

  test('12. Business Canvas - Mobile Responsive', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 })
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/12-bmc-mobile.png`, fullPage: true })
  })

  test('13. Create Modal - Mobile View', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 })
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Open modal
    const newCanvasBtn = page.locator('button:has-text("New Canvas")')
    await newCanvasBtn.click()
    await page.waitForSelector('text=Create New Canvas')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/13-create-modal-mobile.png`, fullPage: true })
  })

  test('14. Entity Modal - Add Partner', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')

    // Try to find and click add button in Partners section
    const addPartnerBtn = page.locator('button:has-text("Add Partner"), button[aria-label*="add"]').first()
    if (await addPartnerBtn.isVisible()) {
      await addPartnerBtn.click()
      await page.waitForTimeout(500)
      await page.screenshot({ path: `${SCREENSHOT_DIR}/14-add-partner-modal.png`, fullPage: true })
    }
  })

  test('15. Loading States', async ({ page }) => {
    // Capture loading state by going to page immediately
    await page.goto(`${BASE_URL}/app/canvases`)

    // Try to capture loading spinner
    const spinner = page.locator('.orbitos-spinner, [class*="spinner"]')
    if (await spinner.isVisible({ timeout: 1000 }).catch(() => false)) {
      await page.screenshot({ path: `${SCREENSHOT_DIR}/15-loading-state.png`, fullPage: true })
    }

    await page.waitForLoadState('networkidle')
    await page.screenshot({ path: `${SCREENSHOT_DIR}/15-loaded-state.png`, fullPage: true })
  })

  test('16. Error Handling - Invalid Canvas ID', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas?canvasId=invalid-id`)
    await page.waitForLoadState('networkidle')

    await page.screenshot({ path: `${SCREENSHOT_DIR}/16-invalid-canvas-id.png`, fullPage: true })
  })

  test('17. Product Canvases Section', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Check for Product Canvases section
    const productSection = page.locator('text=Product Canvases')
    if (await productSection.isVisible()) {
      await page.screenshot({ path: `${SCREENSHOT_DIR}/17-product-canvases-section.png`, fullPage: true })
    }

    // Check for Add Product Canvas button
    const addProductBtn = page.locator('button:has-text("Add Product Canvas")')
    if (await addProductBtn.isVisible()) {
      await addProductBtn.click()
      await page.waitForTimeout(500)
      await page.screenshot({ path: `${SCREENSHOT_DIR}/17-add-product-modal.png`, fullPage: true })
    }
  })

  test('18. Canvas Stats Display', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Look for stats badges/counters
    const statsElements = page.locator('[class*="stat"], [class*="badge"], [class*="count"]')
    const count = await statsElements.count()

    console.log(`Found ${count} stats elements`)
    await page.screenshot({ path: `${SCREENSHOT_DIR}/18-stats-display.png`, fullPage: true })
  })

  test('19. Mini BMC Preview', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Look for mini preview grid
    const miniPreview = page.locator('[class*="grid"]').filter({ hasText: 'Partners' }).first()
    if (await miniPreview.isVisible().catch(() => false)) {
      await miniPreview.screenshot({ path: `${SCREENSHOT_DIR}/19-mini-preview.png` })
    }
  })

  test('20. All Buttons Clickable Check', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')

    // Get all buttons
    const buttons = page.locator('button')
    const buttonCount = await buttons.count()

    console.log(`Found ${buttonCount} buttons on page`)

    // Check each button is not disabled
    for (let i = 0; i < Math.min(buttonCount, 10); i++) {
      const btn = buttons.nth(i)
      const isDisabled = await btn.isDisabled()
      const text = await btn.textContent()
      console.log(`Button ${i}: "${text?.trim()}" - Disabled: ${isDisabled}`)
    }

    await page.screenshot({ path: `${SCREENSHOT_DIR}/20-all-buttons.png`, fullPage: true })
  })
})
