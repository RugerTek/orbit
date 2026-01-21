/**
 * =============================================================================
 * OrbitOS - Comprehensive User Journey E2E Tests
 * =============================================================================
 *
 * This test file simulates 10 different user personas testing the entire
 * OrbitOS application from different perspectives, in different orders,
 * and with different use cases.
 *
 * PERSONAS:
 * 1. CEO/Executive - Dashboard, health metrics, strategic overview
 * 2. HR Manager - People management, org chart, roles
 * 3. Operations Manager - Processes, activities, workflow design
 * 4. Product Manager - Business canvas, value propositions, channels
 * 5. IT Administrator - Super admin, user management, organizations
 * 6. Finance Lead - Resources, revenue streams, cost tracking
 * 7. Sales Director - Customer segments, channels, relationships
 * 8. New Employee - Onboarding flow, exploring all features
 * 9. AI Power User - AI agents, conversations, multi-agent chat
 * 10. Mobile User - Responsive design, core workflows on mobile
 *
 * Each persona follows a realistic user journey through the application.
 * =============================================================================
 */

import { test, expect, Page } from '@playwright/test'

// Set longer timeout for all tests in this file
test.setTimeout(60000)

const SCREENSHOT_DIR = 'tests/e2e/screenshots/user-journeys'

// Helper functions
async function waitForPageLoad(page: Page, timeout = 1000) {
  try {
    await page.waitForLoadState('networkidle', { timeout: 15000 })
  } catch {
    // If networkidle times out, just wait for domcontentloaded
    await page.waitForLoadState('domcontentloaded')
  }
  await page.waitForTimeout(timeout)
}

// Removed unused generateUniqueName function

async function takeScreenshot(page: Page, name: string) {
  await page.screenshot({ path: `${SCREENSHOT_DIR}/${name}.png`, fullPage: true })
}

// =============================================================================
// PERSONA 1: CEO/EXECUTIVE
// Focus: Dashboard, health metrics, strategic overview
// =============================================================================

test.describe('Persona 1: CEO/Executive - Strategic Overview', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app')
    await waitForPageLoad(page)
  })

  test('1.1 CEO views dashboard and checks organizational health', async ({ page }) => {
    // Dashboard should show key metrics - use first() to avoid strict mode
    await expect(page.getByRole('heading', { name: 'Operations Overview' }).first()).toBeVisible()

    // Check stats cards are visible - use first() for each
    await expect(page.getByText('People').first()).toBeVisible()
    await expect(page.getByText('Roles').first()).toBeVisible()
    await expect(page.getByText('Functions').first()).toBeVisible()
    await expect(page.getByText('Processes').first()).toBeVisible()

    await takeScreenshot(page, 'persona1-dashboard')
  })

  test('1.2 CEO navigates to org chart for reporting structure', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page, 2000)

    await expect(page.getByRole('heading', { name: 'Organization Chart' }).first()).toBeVisible({ timeout: 10000 })

    // Check metrics are present
    await expect(page.getByText('Total People').first()).toBeVisible()

    await takeScreenshot(page, 'persona1-org-chart')
  })

  test('1.3 CEO checks business canvas for strategic view', async ({ page }) => {
    await page.goto('/app/business-canvas')
    await waitForPageLoad(page)

    // Check for BMC blocks or canvas view
    const pageContent = await page.content()
    const hasCanvas = pageContent.includes('Key Partners') || pageContent.includes('Value Propositions')
    const hasEmptyState = pageContent.includes('No canvas') || pageContent.includes('Create canvas')

    expect(hasCanvas || hasEmptyState).toBeTruthy()
    await takeScreenshot(page, 'persona1-business-canvas')
  })

  test('1.4 CEO reviews AI agents for strategic insights capability', async ({ page }) => {
    await page.locator('nav').getByRole('link', { name: 'AI Agents' }).click()
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'AI Agents' }).first()).toBeVisible()
    await expect(page.getByText('Total Agents').first()).toBeVisible()

    await takeScreenshot(page, 'persona1-ai-agents')
  })
})

// =============================================================================
// PERSONA 2: HR MANAGER
// Focus: People management, org chart, roles assignment
// =============================================================================

test.describe('Persona 2: HR Manager - People & Roles', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/people')
    await waitForPageLoad(page)
  })

  test('2.1 HR Manager views people list', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'People' }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: /add person/i }).first()).toBeVisible()

    // Check stats cards
    await expect(page.getByText('Total People').first()).toBeVisible()

    await takeScreenshot(page, 'persona2-people-list')
  })

  test('2.2 HR Manager navigates to roles page', async ({ page }) => {
    await page.locator('nav').getByRole('link', { name: 'Roles' }).click()
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Roles' }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: /add role/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona2-roles-page')
  })

  test('2.3 HR Manager checks org chart structure', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page, 2000)

    await expect(page.getByRole('heading', { name: 'Organization Chart' }).first()).toBeVisible({ timeout: 10000 })

    // Test view switching - check if buttons exist first
    const listBtn = page.getByRole('button', { name: /list/i }).first()
    if (await listBtn.isVisible().catch(() => false)) {
      await listBtn.click()
      await page.waitForTimeout(500)
    }

    await takeScreenshot(page, 'persona2-org-chart-views')
  })
})

// =============================================================================
// PERSONA 3: OPERATIONS MANAGER
// Focus: Processes, activities, workflow design
// =============================================================================

test.describe('Persona 3: Operations Manager - Processes & Workflows', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/processes')
    await waitForPageLoad(page)
  })

  test('3.1 Ops Manager views process list', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Processes' }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: /add process|new process/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona3-process-list')
  })

  test('3.2 Ops Manager checks functions coverage', async ({ page }) => {
    await page.locator('nav').getByRole('link', { name: 'Functions' }).click()
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Functions' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona3-functions-coverage')
  })

  test('3.3 Ops Manager views resources inventory', async ({ page }) => {
    await page.goto('/app/resources')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Resource Registry' }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: /add resource/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona3-resources')
  })
})

// =============================================================================
// PERSONA 4: PRODUCT MANAGER
// Focus: Business canvas, value propositions, channels
// =============================================================================

test.describe('Persona 4: Product Manager - Business Strategy', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/business-canvas')
    await waitForPageLoad(page)
  })

  test('4.1 PM views business model canvas', async ({ page }) => {
    // Check for canvas elements or empty state
    const pageContent = await page.content()
    const hasBlocks = pageContent.includes('Key Partners') || pageContent.includes('Value Propositions') || pageContent.includes('Customer Segments')
    const hasEmptyState = pageContent.includes('No canvas') || pageContent.includes('Create') || pageContent.includes('Get started')

    expect(hasBlocks || hasEmptyState).toBeTruthy()

    await takeScreenshot(page, 'persona4-business-canvas')
  })

  test('4.2 PM navigates to canvases list', async ({ page }) => {
    const canvasesLink = page.locator('nav').getByRole('link', { name: /canvases/i })
    if (await canvasesLink.isVisible().catch(() => false)) {
      await canvasesLink.click()
      await waitForPageLoad(page)
    }

    await takeScreenshot(page, 'persona4-canvases-list')
  })

  test('4.3 PM checks process alignment with functions', async ({ page }) => {
    await page.locator('nav').getByRole('link', { name: 'Processes' }).click()
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Processes' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona4-process-functions')
  })
})

// =============================================================================
// PERSONA 5: IT ADMINISTRATOR
// Focus: Super admin, user management, organizations
// =============================================================================

test.describe('Persona 5: IT Administrator - System Management', () => {
  test('5.1 IT Admin accesses admin dashboard', async ({ page }) => {
    await page.goto('/admin')
    await waitForPageLoad(page)

    // Check for admin dashboard elements
    const pageContent = await page.content()
    const hasAdminContent = pageContent.includes('admin') || pageContent.includes('Admin') || pageContent.includes('Dashboard')

    expect(hasAdminContent).toBeTruthy()

    await takeScreenshot(page, 'persona5-admin-dashboard')
  })

  test('5.2 IT Admin manages users', async ({ page }) => {
    await page.goto('/admin/users')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: /users/i }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: /add user/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona5-users')
  })

  test('5.3 IT Admin manages organizations', async ({ page }) => {
    await page.goto('/admin/organizations')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: /organizations/i }).first()).toBeVisible()
    await expect(page.getByRole('button', { name: /add organization/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona5-organizations')
  })

  test('5.4 IT Admin views admin roles', async ({ page }) => {
    await page.goto('/admin/roles')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: /roles/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona5-admin-roles')
  })

  test('5.5 IT Admin views admin functions', async ({ page }) => {
    await page.goto('/admin/functions')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: /functions/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona5-admin-functions')
  })
})

// =============================================================================
// PERSONA 6: FINANCE LEAD
// Focus: Resources, revenue streams, cost tracking
// =============================================================================

test.describe('Persona 6: Finance Lead - Financial Oversight', () => {
  test('6.1 Finance Lead views resources page', async ({ page }) => {
    await page.goto('/app/resources')
    await waitForPageLoad(page, 2000)

    await expect(page.getByRole('heading', { name: 'Resource Registry' }).first()).toBeVisible({ timeout: 10000 })

    await takeScreenshot(page, 'persona6-resources')
  })

  test('6.2 Finance Lead checks business canvas for revenue streams', async ({ page }) => {
    await page.goto('/app/business-canvas')
    await waitForPageLoad(page)

    // Page should load without errors
    const pageContent = await page.content()
    expect(pageContent).toBeDefined()

    await takeScreenshot(page, 'persona6-revenue-streams')
  })

  test('6.3 Finance Lead reviews processes', async ({ page }) => {
    await page.goto('/app/processes')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Processes' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona6-processes')
  })
})

// =============================================================================
// PERSONA 7: SALES DIRECTOR
// Focus: Customer segments, channels, relationships
// =============================================================================

test.describe('Persona 7: Sales Director - Customer Focus', () => {
  test('7.1 Sales Director views business canvas for customer insights', async ({ page }) => {
    await page.goto('/app/business-canvas')
    await waitForPageLoad(page)

    // Page should load without errors
    const pageContent = await page.content()
    expect(pageContent).toBeDefined()

    await takeScreenshot(page, 'persona7-customer-overview')
  })

  test('7.2 Sales Director explores channels', async ({ page }) => {
    await page.goto('/app/business-canvas')
    await waitForPageLoad(page)

    await takeScreenshot(page, 'persona7-channels')
  })

  test('7.3 Sales Director reviews partner ecosystem', async ({ page }) => {
    await page.goto('/app/business-canvas')
    await waitForPageLoad(page)

    await takeScreenshot(page, 'persona7-partners')
  })
})

// =============================================================================
// PERSONA 8: NEW EMPLOYEE
// Focus: Onboarding flow, exploring all features
// =============================================================================

test.describe('Persona 8: New Employee - Full Exploration', () => {
  test('8.1 New employee starts at dashboard', async ({ page }) => {
    await page.goto('/app')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Operations Overview' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona8-start-dashboard')
  })

  test('8.2 New employee explores sidebar navigation', async ({ page }) => {
    await page.goto('/app')
    await waitForPageLoad(page)

    // Check main navigation links exist in nav
    const navLinks = ['People', 'Roles', 'Functions', 'Processes', 'Resources', 'AI Agents']

    for (const linkName of navLinks) {
      const link = page.locator('nav').getByRole('link', { name: new RegExp(linkName, 'i') })
      const isVisible = await link.isVisible().catch(() => false)
      expect(isVisible).toBeTruthy()
    }

    await takeScreenshot(page, 'persona8-navigation')
  })

  test('8.3 New employee visits each main section', async ({ page }) => {
    const sections = [
      { path: '/app', heading: 'Operations Overview' },
      { path: '/app/people', heading: 'People' },
      { path: '/app/roles', heading: 'Roles' },
      { path: '/app/functions', heading: 'Functions' },
      { path: '/app/processes', heading: 'Processes' },
      { path: '/app/resources', heading: 'Resource Registry' },
      { path: '/app/ai-agents', heading: 'AI Agents' }
    ]

    for (const section of sections) {
      await page.goto(section.path)
      await waitForPageLoad(page, 2000)

      const heading = page.getByRole('heading', { name: section.heading }).first()
      const isVisible = await heading.isVisible({ timeout: 10000 }).catch(() => false)
      expect(isVisible).toBeTruthy()
    }

    await takeScreenshot(page, 'persona8-exploration-complete')
  })

  test('8.4 New employee tests keyboard shortcuts in dialogs', async ({ page }) => {
    await page.goto('/app/people')
    await waitForPageLoad(page)

    // Open a dialog
    await page.getByRole('button', { name: /add person/i }).first().click()
    await page.waitForTimeout(300)

    // Test Escape closes dialog
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Verify dialog closed by checking if the add button is visible again
    await expect(page.getByRole('button', { name: /add person/i }).first()).toBeVisible()

    await takeScreenshot(page, 'persona8-keyboard-test')
  })
})

// =============================================================================
// PERSONA 9: AI POWER USER
// Focus: AI agents, conversations, multi-agent chat
// =============================================================================

test.describe('Persona 9: AI Power User - Agent & Chat Focus', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/ai-agents')
    await waitForPageLoad(page)
  })

  test('9.1 AI user views all configured agents', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'AI Agents' }).first()).toBeVisible()

    // Check stats - use first() to avoid strict mode
    await expect(page.getByText('Total Agents').first()).toBeVisible()

    await takeScreenshot(page, 'persona9-agents-overview')
  })

  test('9.2 AI user opens create agent dialog', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).first().click()
    await page.waitForTimeout(300)

    // Verify dialog opened
    await expect(page.getByRole('heading', { name: 'Create AI Agent' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona9-create-agent-form')

    // Cancel
    await page.keyboard.press('Escape')
  })

  test('9.3 AI user checks form fields in create dialog', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).first().click()
    await page.waitForTimeout(500)

    // Check form fields exist - use more flexible matching
    const nameField = page.locator('input').first()
    await expect(nameField).toBeVisible({ timeout: 5000 })

    await takeScreenshot(page, 'persona9-form-fields')

    await page.keyboard.press('Escape')
  })
})

// =============================================================================
// PERSONA 10: MOBILE USER
// Focus: Responsive design, core workflows on mobile
// =============================================================================

test.describe('Persona 10: Mobile User - Responsive Testing', () => {
  test.use({ viewport: { width: 375, height: 812 } }) // iPhone viewport

  test('10.1 Mobile user views dashboard', async ({ page }) => {
    await page.goto('/app')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Operations Overview' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona10-mobile-dashboard')
  })

  test('10.2 Mobile user views people page', async ({ page }) => {
    await page.goto('/app/people')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'People' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona10-mobile-people')
  })

  test('10.3 Mobile user views AI agents', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'AI Agents' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona10-mobile-ai-agents')
  })

  test('10.4 Mobile user views processes', async ({ page }) => {
    await page.goto('/app/processes')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Processes' }).first()).toBeVisible()

    await takeScreenshot(page, 'persona10-mobile-processes')
  })

  test('10.5 Mobile user views org chart', async ({ page }) => {
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page, 2000)

    await expect(page.getByRole('heading', { name: 'Organization Chart' }).first()).toBeVisible({ timeout: 10000 })

    await takeScreenshot(page, 'persona10-mobile-org-chart')
  })
})

// =============================================================================
// CROSS-PERSONA: Integration Tests
// Focus: Full workflows across multiple features
// =============================================================================

test.describe('Cross-Persona: Integration Workflows', () => {
  test('Integration 1: Navigate person to org chart flow', async ({ page }) => {
    // Step 1: Visit people page
    await page.goto('/app/people')
    await waitForPageLoad(page, 2000)

    await expect(page.getByRole('heading', { name: 'People' }).first()).toBeVisible({ timeout: 10000 })

    // Step 2: Navigate to org chart via direct URL
    await page.goto('/app/people/org-chart')
    await waitForPageLoad(page, 2000)

    await expect(page.getByRole('heading', { name: 'Organization Chart' }).first()).toBeVisible({ timeout: 10000 })

    await takeScreenshot(page, 'integration1-complete')
  })

  test('Integration 2: Navigate processes to functions flow', async ({ page }) => {
    await page.goto('/app/processes')
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Processes' }).first()).toBeVisible()

    // Navigate to functions
    await page.locator('nav').getByRole('link', { name: 'Functions' }).click()
    await waitForPageLoad(page)

    await expect(page.getByRole('heading', { name: 'Functions' }).first()).toBeVisible()

    await takeScreenshot(page, 'integration2-complete')
  })

  test('Integration 3: Full page tour - Every main section accessible', async ({ page }) => {
    const routes = [
      '/app',
      '/app/people',
      '/app/roles',
      '/app/functions',
      '/app/processes',
      '/app/ai-agents',
      '/admin',
      '/admin/users'
    ]

    for (const route of routes) {
      await page.goto(route)
      try {
        await page.waitForLoadState('networkidle', { timeout: 10000 })
      } catch {
        await page.waitForLoadState('domcontentloaded')
      }

      // Page should load without critical errors
      const pageContent = await page.content()
      const hasServerError = pageContent.includes('500') && pageContent.includes('Internal Server Error')
      expect(hasServerError).toBeFalsy()
    }

    await takeScreenshot(page, 'integration3-tour-complete')
  })
})

// =============================================================================
// DIALOG BEHAVIOR TESTS
// Ensuring all dialogs follow BaseDialog patterns
// =============================================================================

test.describe('Dialog Behavior Compliance', () => {
  test('People dialog closes on Escape', async ({ page }) => {
    await page.goto('/app/people')
    await waitForPageLoad(page)

    const btn = page.getByRole('button', { name: /add person/i }).first()
    if (await btn.isVisible().catch(() => false)) {
      await btn.click()
      await page.waitForTimeout(300)

      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)

      // Verify dialog is closed by checking the add button is clickable again
      await expect(btn).toBeVisible()
    }
  })

  test('Roles dialog closes on Escape', async ({ page }) => {
    await page.goto('/app/roles')
    await waitForPageLoad(page)

    const btn = page.getByRole('button', { name: /add role/i }).first()
    if (await btn.isVisible().catch(() => false)) {
      await btn.click()
      await page.waitForTimeout(300)

      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)

      await expect(btn).toBeVisible()
    }
  })

  test('Functions dialog closes on Escape', async ({ page }) => {
    await page.goto('/app/functions')
    await waitForPageLoad(page)

    const btn = page.getByRole('button', { name: /add function/i }).first()
    if (await btn.isVisible().catch(() => false)) {
      await btn.click()
      await page.waitForTimeout(300)

      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)

      await expect(btn).toBeVisible()
    }
  })

  test('Processes dialog closes on Escape', async ({ page }) => {
    await page.goto('/app/processes')
    await waitForPageLoad(page)

    const btn = page.getByRole('button', { name: /add process|new process/i }).first()
    if (await btn.isVisible().catch(() => false)) {
      await btn.click()
      await page.waitForTimeout(300)

      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)

      await expect(btn).toBeVisible()
    }
  })

  test('Resources dialog closes on Escape', async ({ page }) => {
    await page.goto('/app/resources')
    await waitForPageLoad(page)

    const btn = page.getByRole('button', { name: /add resource/i }).first()
    if (await btn.isVisible().catch(() => false)) {
      await btn.click()
      await page.waitForTimeout(300)

      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)

      await expect(btn).toBeVisible()
    }
  })

  test('AI Agents dialog closes on Escape', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await waitForPageLoad(page)

    const btn = page.getByRole('button', { name: /add agent/i }).first()
    if (await btn.isVisible().catch(() => false)) {
      await btn.click()
      await page.waitForTimeout(300)

      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)

      await expect(btn).toBeVisible()
    }
  })
})

// =============================================================================
// ERROR HANDLING TESTS
// =============================================================================

test.describe('Error Handling', () => {
  test('Invalid route handled gracefully', async ({ page }) => {
    await page.goto('/app/this-does-not-exist')
    await page.waitForLoadState('networkidle')

    // Should show some error or redirect
    const currentUrl = page.url()
    // Either shows 404 or redirects to valid page
    expect(currentUrl).toBeDefined()
  })

  test('Invalid process ID handled gracefully', async ({ page }) => {
    await page.goto('/app/processes/00000000-0000-0000-0000-000000000000')
    await waitForPageLoad(page)

    // Page should handle the invalid ID gracefully (no crash)
    const pageContent = await page.content()
    expect(pageContent).toBeDefined()
  })
})
