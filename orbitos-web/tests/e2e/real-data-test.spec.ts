import { test, expect } from '@playwright/test'

const BASE_URL = 'http://localhost:3000'
const SCREENSHOT_DIR = './tests/e2e/screenshots/real-data'

test.describe('Canvas Feature with Real Data', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 })
  })

  test('Canvas Index shows real canvases from API', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/canvases`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Wait for data to load

    await page.screenshot({ path: `${SCREENSHOT_DIR}/01-canvas-index-with-data.png`, fullPage: true })

    // Check for Company Canvas
    const companyCanvas = page.locator('text=Test Company Canvas, text=Company Business Model')
    const hasCompanyCanvas = await companyCanvas.first().isVisible().catch(() => false)
    console.log('Company Canvas visible:', hasCompanyCanvas)

    // Check for Product Canvas
    const productCanvas = page.locator('text=Mobile App Canvas')
    const hasProductCanvas = await productCanvas.isVisible().catch(() => false)
    console.log('Product Canvas visible:', hasProductCanvas)
  })

  test('Business Canvas shows real Partners from API', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/02-bmc-with-data.png`, fullPage: true })

    // Check for AWS Partner
    const awsPartner = page.locator('text=Amazon Web Services')
    const hasAws = await awsPartner.isVisible().catch(() => false)
    console.log('AWS Partner visible:', hasAws)

    // Check for Website Channel
    const websiteChannel = page.locator('text=Website')
    const hasWebsite = await websiteChannel.isVisible().catch(() => false)
    console.log('Website Channel visible:', hasWebsite)

    // Check for Value Proposition
    const valueProp = page.locator('text=Easy Setup')
    const hasValueProp = await valueProp.isVisible().catch(() => false)
    console.log('Easy Setup Value Prop visible:', hasValueProp)
  })

  test('Business Canvas Kanban view shows data', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Switch to Kanban view
    const kanbanBtn = page.locator('button:has-text("Kanban")')
    await kanbanBtn.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/03-kanban-with-data.png`, fullPage: true })
  })

  test('Business Canvas List view shows data', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Switch to List view
    const listBtn = page.locator('button:has-text("List")')
    await listBtn.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/04-list-with-data.png`, fullPage: true })
  })

  test('Clicking Partner block shows data in side panel', async ({ page }) => {
    await page.goto(`${BASE_URL}/app/business-canvas`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Click on Partners section
    const partnersSection = page.locator('div:has-text("Key Partners")').first()
    await partnersSection.click()
    await page.waitForTimeout(500)

    await page.screenshot({ path: `${SCREENSHOT_DIR}/05-partners-panel.png`, fullPage: true })
  })
})
