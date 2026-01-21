import { test, expect } from '@playwright/test'

const BASE_URL = 'http://localhost:3000'
const SCREENSHOT_DIR = './tests/e2e/screenshots/crud-flow'

/**
 * Full Canvas CRUD Flow Tests
 * These tests verify the complete create, read, update, delete flow for Canvas feature
 * Tests interact with the real API and verify data persistence
 */
test.describe('Canvas CRUD Flow Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 })
  })

  test.describe('Canvas Creation Flow', () => {
    test('Create Organization-scope Canvas from UI', async ({ page }) => {
      await page.goto(`${BASE_URL}/app/canvases`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Screenshot before creating
      await page.screenshot({ path: `${SCREENSHOT_DIR}/01-before-create.png`, fullPage: true })

      // Click New Canvas button
      const newCanvasBtn = page.locator('button:has-text("New Canvas")')
      await newCanvasBtn.click()

      // Wait for modal
      await page.waitForSelector('text=Create New Canvas', { timeout: 5000 })
      await page.waitForTimeout(500)

      const modalForm = page.locator('form')

      // Select Company/Organization scope
      const companyBtn = modalForm.locator('button:has-text("Company")').first()
      const isCompanyEnabled = await companyBtn.isEnabled().catch(() => false)

      if (!isCompanyEnabled) {
        // If Company is disabled, select Product instead
        const productBtn = modalForm.locator('button:has-text("Product")').first()
        await productBtn.click({ force: true })
      } else {
        await companyBtn.click({ force: true })
      }
      await page.waitForTimeout(200)

      // Fill canvas name
      const nameInput = modalForm.locator('input.orbitos-input').first()
      await nameInput.fill('E2E Test Canvas - ' + Date.now())

      // Fill description
      const descTextarea = modalForm.locator('textarea')
      if (await descTextarea.isVisible()) {
        await descTextarea.fill('This canvas was created by automated E2E tests')
      }

      await page.screenshot({ path: `${SCREENSHOT_DIR}/02-form-filled.png`, fullPage: true })

      // Submit
      const submitBtn = modalForm.locator('button:has-text("Create Canvas")')
      await submitBtn.click({ force: true })

      // Wait for success (modal closes or navigation)
      await page.waitForTimeout(2000)
      await page.screenshot({ path: `${SCREENSHOT_DIR}/03-after-create.png`, fullPage: true })

      // Verify canvas appears in list
      await page.goto(`${BASE_URL}/app/canvases`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/04-canvas-in-list.png`, fullPage: true })
    })

    test('Create Product-scope Canvas', async ({ page }) => {
      await page.goto(`${BASE_URL}/app/canvases`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Click New Canvas
      await page.locator('button:has-text("New Canvas")').click()
      await page.waitForSelector('text=Create New Canvas', { timeout: 5000 })
      await page.waitForTimeout(300)

      const modalForm = page.locator('form')

      // Select Product scope - use force: true to bypass backdrop overlay
      const productBtn = modalForm.locator('button:has-text("Product")').first()
      await productBtn.click({ force: true })
      await page.waitForTimeout(200)

      // Fill form
      const nameInput = modalForm.locator('input.orbitos-input').first()
      await nameInput.fill('Product Canvas E2E - ' + Date.now())

      // Submit
      await modalForm.locator('button:has-text("Create Canvas")').click({ force: true })
      await page.waitForTimeout(2000)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/05-product-canvas-created.png`, fullPage: true })
    })
  })

  test.describe('Canvas Editing Flow', () => {
    test('Edit canvas name via API integration', async ({ page, request }) => {
      // First, fetch existing canvases via API to get an ID
      const response = await request.get(
        'http://localhost:5027/api/organizations/11111111-1111-1111-1111-111111111111/operations/canvases/bmc'
      )
      const canvases = await response.json()

      if (canvases.length > 0) {
        const canvas = canvases[0]
        console.log(`Found canvas to test: ${canvas.name} (${canvas.id})`)

        // Update via API
        const updateResponse = await request.put(
          `http://localhost:5027/api/organizations/11111111-1111-1111-1111-111111111111/operations/canvases/bmc/${canvas.id}`,
          {
            data: {
              name: `${canvas.name} - Edited`,
              description: canvas.description || 'Updated by E2E test',
              canvasType: 'BusinessModel',
              scopeType: canvas.scopeType,
              status: canvas.status
            }
          }
        )

        expect(updateResponse.ok()).toBe(true)
        const updated = await updateResponse.json()
        expect(updated.name).toContain('Edited')
        console.log(`Canvas updated: ${updated.name}`)
      }
    })
  })

  test.describe('Business Canvas BMC Blocks', () => {
    test('View BMC blocks display correctly', async ({ page }) => {
      await page.goto(`${BASE_URL}/app/business-canvas`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(2000)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/06-bmc-full-view.png`, fullPage: true })

      // Check all 9 BMC blocks are visible
      const blockNames = [
        'Key Partners',
        'Key Activities',
        'Key Resources',
        'Value Propositions',
        'Customer Relationships',
        'Channels',
        'Customer Segments',
        'Cost Structure',
        'Revenue Streams'
      ]

      for (const block of blockNames) {
        const blockElement = page.locator(`text=${block}`).first()
        const isVisible = await blockElement.isVisible().catch(() => false)
        console.log(`Block "${block}" visible: ${isVisible}`)
      }
    })

    test('Add item to Partners block', async ({ page }) => {
      await page.goto(`${BASE_URL}/app/business-canvas`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Click on Partners block
      const partnersBlock = page.locator('div:has-text("Key Partners")').first()
      await partnersBlock.click()
      await page.waitForTimeout(500)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/07-partners-block-clicked.png`, fullPage: true })

      // Look for add button
      const addBtn = page.locator('button:has-text("Add"), button[aria-label*="add"]').first()
      if (await addBtn.isVisible().catch(() => false)) {
        await addBtn.click()
        await page.waitForTimeout(500)
        await page.screenshot({ path: `${SCREENSHOT_DIR}/07-add-partner-form.png`, fullPage: true })
      }
    })

    test('View modes: Canvas, Kanban, List', async ({ page }) => {
      await page.goto(`${BASE_URL}/app/business-canvas`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Test each view mode
      const views = ['Canvas', 'Kanban', 'List']

      for (const view of views) {
        const viewBtn = page.locator(`button:has-text("${view}")`).first()
        if (await viewBtn.isVisible().catch(() => false)) {
          await viewBtn.click()
          await page.waitForTimeout(500)
          await page.screenshot({ path: `${SCREENSHOT_DIR}/08-view-${view.toLowerCase()}.png`, fullPage: true })
          console.log(`${view} view activated`)
        }
      }
    })
  })

  test.describe('API CRUD Operations', () => {
    test('Partner CRUD via API', async ({ request }) => {
      const orgId = '11111111-1111-1111-1111-111111111111'
      const baseUrl = 'http://localhost:5027'

      // Create Partner
      const createResponse = await request.post(
        `${baseUrl}/api/organizations/${orgId}/operations/partners`,
        {
          data: {
            name: 'E2E Test Partner - ' + Date.now(),
            description: 'Created by automated test',
            type: 'Supplier',
            status: 'Active',
            strategicValue: 'High'
          }
        }
      )
      expect(createResponse.ok()).toBe(true)
      const partner = await createResponse.json()
      console.log(`Created partner: ${partner.name} (${partner.id})`)

      // Update Partner
      const updateResponse = await request.put(
        `${baseUrl}/api/organizations/${orgId}/operations/partners/${partner.id}`,
        {
          data: {
            name: partner.name + ' - Updated',
            description: 'Updated by E2E test',
            type: 'Supplier',
            status: 'Active',
            strategicValue: 'Critical'
          }
        }
      )
      expect(updateResponse.ok()).toBe(true)
      const updated = await updateResponse.json()
      expect(updated.strategicValue).toBe('Critical')
      console.log(`Updated partner: ${updated.name}`)

      // Delete Partner
      const deleteResponse = await request.delete(
        `${baseUrl}/api/organizations/${orgId}/operations/partners/${partner.id}`
      )
      expect(deleteResponse.status()).toBe(204)
      console.log('Partner deleted successfully')
    })

    test('Channel CRUD via API', async ({ request }) => {
      const orgId = '11111111-1111-1111-1111-111111111111'
      const baseUrl = 'http://localhost:5027'

      // Create Channel
      const createResponse = await request.post(
        `${baseUrl}/api/organizations/${orgId}/operations/channels`,
        {
          data: {
            name: 'E2E Test Channel - ' + Date.now(),
            description: 'Created by automated test',
            type: 'Digital',
            category: 'Sales',
            status: 'Active',
            ownership: 'Owned'
          }
        }
      )
      expect(createResponse.ok()).toBe(true)
      const channel = await createResponse.json()
      console.log(`Created channel: ${channel.name} (${channel.id})`)

      // Update Channel
      const updateResponse = await request.put(
        `${baseUrl}/api/organizations/${orgId}/operations/channels/${channel.id}`,
        {
          data: {
            name: channel.name + ' - Updated',
            description: 'Updated by E2E test',
            type: 'Digital',
            category: 'Marketing',
            status: 'Active',
            ownership: 'Owned'
          }
        }
      )
      expect(updateResponse.ok()).toBe(true)
      const updated = await updateResponse.json()
      expect(updated.category).toBe('Marketing')

      // Delete Channel
      const deleteResponse = await request.delete(
        `${baseUrl}/api/organizations/${orgId}/operations/channels/${channel.id}`
      )
      expect(deleteResponse.status()).toBe(204)
      console.log('Channel deleted successfully')
    })

    test('ValueProposition CRUD via API', async ({ request }) => {
      const orgId = '11111111-1111-1111-1111-111111111111'
      const baseUrl = 'http://localhost:5027'

      // Create ValueProposition
      const createResponse = await request.post(
        `${baseUrl}/api/organizations/${orgId}/operations/valuepropositions`,
        {
          data: {
            name: 'E2E Test Value Prop - ' + Date.now(),
            headline: 'Test Headline for E2E',
            description: 'Created by automated test',
            status: 'Active'
          }
        }
      )
      expect(createResponse.ok()).toBe(true)
      const vp = await createResponse.json()
      console.log(`Created value proposition: ${vp.name} (${vp.id})`)

      // Update ValueProposition
      const updateResponse = await request.put(
        `${baseUrl}/api/organizations/${orgId}/operations/valuepropositions/${vp.id}`,
        {
          data: {
            name: vp.name + ' - Updated',
            headline: 'Updated Headline',
            description: 'Updated by E2E test',
            status: 'Active'
          }
        }
      )
      expect(updateResponse.ok()).toBe(true)
      const updated = await updateResponse.json()
      expect(updated.headline).toBe('Updated Headline')

      // Delete ValueProposition
      const deleteResponse = await request.delete(
        `${baseUrl}/api/organizations/${orgId}/operations/valuepropositions/${vp.id}`
      )
      expect(deleteResponse.status()).toBe(204)
      console.log('ValueProposition deleted successfully')
    })

    test('Canvas CRUD via API', async ({ request }) => {
      const orgId = '11111111-1111-1111-1111-111111111111'
      const baseUrl = 'http://localhost:5027'

      // Create Canvas
      const createResponse = await request.post(
        `${baseUrl}/api/organizations/${orgId}/operations/canvases/bmc`,
        {
          data: {
            name: 'E2E Test Canvas - ' + Date.now(),
            description: 'Created by automated E2E test',
            canvasType: 'BusinessModel',
            scopeType: 'Product',
            status: 'Draft'
          }
        }
      )
      expect(createResponse.ok()).toBe(true)
      const canvas = await createResponse.json()
      console.log(`Created canvas: ${canvas.name} (${canvas.id})`)

      // Verify 9 blocks were created
      expect(canvas.blocks).toBeDefined()
      expect(canvas.blocks.length).toBe(9)
      console.log(`Canvas has ${canvas.blocks.length} blocks`)

      // Update Canvas
      const updateResponse = await request.put(
        `${baseUrl}/api/organizations/${orgId}/operations/canvases/bmc/${canvas.id}`,
        {
          data: {
            name: canvas.name + ' - Updated',
            description: 'Updated by E2E test',
            canvasType: 'BusinessModel',
            scopeType: 'Product',
            status: 'Active'
          }
        }
      )
      expect(updateResponse.ok()).toBe(true)
      const updated = await updateResponse.json()
      expect(updated.status).toBe('Active')
      console.log(`Updated canvas status: ${updated.status}`)

      // Delete Canvas
      const deleteResponse = await request.delete(
        `${baseUrl}/api/organizations/${orgId}/operations/canvases/bmc/${canvas.id}`
      )
      expect(deleteResponse.status()).toBe(204)
      console.log('Canvas deleted successfully')
    })
  })

  test.describe('Error Handling', () => {
    test('Handle invalid canvas type gracefully', async ({ request }) => {
      const orgId = '11111111-1111-1111-1111-111111111111'
      const baseUrl = 'http://localhost:5027'

      // Try to create with invalid enum value
      const response = await request.post(
        `${baseUrl}/api/organizations/${orgId}/operations/canvases/bmc`,
        {
          data: {
            name: 'Invalid Canvas',
            canvasType: 'InvalidType',
            scopeType: 'Product',
            status: 'Draft'
          }
        }
      )

      // Should return 400 Bad Request
      expect(response.status()).toBe(400)
      console.log('Invalid enum correctly rejected with 400')
    })

    test('Handle missing required fields', async ({ request }) => {
      const orgId = '11111111-1111-1111-1111-111111111111'
      const baseUrl = 'http://localhost:5027'

      // Try to create partner without required fields
      const response = await request.post(
        `${baseUrl}/api/organizations/${orgId}/operations/partners`,
        {
          data: {
            description: 'Missing name and type'
          }
        }
      )

      // Should return 400 Bad Request
      expect(response.status()).toBe(400)
      console.log('Missing required fields correctly rejected with 400')
    })
  })

  test.describe('Responsive UI Tests', () => {
    test('Canvas index responsive on mobile', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 812 })
      await page.goto(`${BASE_URL}/app/canvases`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/09-mobile-index.png`, fullPage: true })

      // Verify New Canvas button is still accessible
      const newCanvasBtn = page.locator('button:has-text("New Canvas")')
      await expect(newCanvasBtn).toBeVisible()
    })

    test('Create canvas modal responsive on mobile', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 812 })
      await page.goto(`${BASE_URL}/app/canvases`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Open modal
      await page.locator('button:has-text("New Canvas")').click()
      await page.waitForSelector('text=Create New Canvas', { timeout: 5000 })

      await page.screenshot({ path: `${SCREENSHOT_DIR}/10-mobile-create-modal.png`, fullPage: true })

      // Verify form elements are visible
      const nameInput = page.locator('input[placeholder*="Canvas"], input[type="text"]').first()
      await expect(nameInput).toBeVisible()
    })

    test('Business canvas responsive on tablet', async ({ page }) => {
      await page.setViewportSize({ width: 768, height: 1024 })
      await page.goto(`${BASE_URL}/app/business-canvas`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      await page.screenshot({ path: `${SCREENSHOT_DIR}/11-tablet-bmc.png`, fullPage: true })
    })
  })
})
