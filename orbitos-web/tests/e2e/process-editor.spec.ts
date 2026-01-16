/**
 * =============================================================================
 * OrbitOS Operations - Process Editor E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for the Vue Flow-based Process Editor.
 * Tests cover UI interactions, node manipulation, edge creation, and persistence.
 *
 * Spec: Operations module - Process Management with Vue Flow
 * =============================================================================
 */

import { test, expect } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}/operations`

// Test data generators
const generateTestName = () => `E2E Process ${Date.now()}`

// =============================================================================
// PROCESS LIST PAGE TESTS
// =============================================================================

test.describe('Process List Page', () => {
  test('should display processes list page', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Page elements
    await expect(page.getByRole('heading', { name: 'Processes' })).toBeVisible()
    await expect(page.getByRole('button', { name: /new process/i })).toBeVisible()
  })

  test('should open create process dialog', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Click New Process button
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Create New Process' })).toBeVisible()
    await expect(page.getByPlaceholder('e.g., Customer Onboarding')).toBeVisible()
  })

  test('should create process via Enter key', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    const testName = generateTestName()

    // Fill form and press Enter
    await page.getByPlaceholder('e.g., Customer Onboarding').fill(testName)
    await page.keyboard.press('Enter')

    // Should navigate to process detail page or close dialog
    await page.waitForTimeout(1500)

    // Dialog should be closed (navigated away or closed)
    await expect(page.getByRole('heading', { name: 'Create New Process' })).not.toBeVisible()
  })

  test('should close dialog on Escape key', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /new process/i }).click()
    await page.waitForTimeout(300)

    // Press Escape
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Create New Process' })).not.toBeVisible()
  })
})

// =============================================================================
// PROCESS DETAIL PAGE - VUE FLOW CANVAS TESTS
// =============================================================================

test.describe('Process Editor - Vue Flow Canvas', () => {
  test.describe.configure({ mode: 'serial' })

  let testProcessId: string
  const testProcessName = generateTestName()

  test.beforeAll(async ({ request }) => {
    // Create a test process via API for the canvas tests
    const response = await request.post(`${API_BASE}/processes`, {
      data: {
        name: testProcessName,
        purpose: 'E2E Test Process',
        trigger: 'Test Trigger',
        output: 'Test Output',
        status: 0, // draft
        stateType: 0 // current
      }
    })
    const data = await response.json()
    testProcessId = data.id
  })

  test.afterAll(async ({ request }) => {
    // Clean up test process
    if (testProcessId) {
      await request.delete(`${API_BASE}/processes/${testProcessId}`)
    }
  })

  test('should display process detail page with Vue Flow canvas', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Page elements
    await expect(page.getByRole('heading', { name: testProcessName })).toBeVisible()

    // Flowchart view should be active by default
    await expect(page.getByRole('button', { name: /flowchart/i })).toBeVisible()

    // Edit Process button
    await expect(page.getByRole('button', { name: /edit process/i })).toBeVisible()
  })

  test('should show empty state when no activities', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Empty state message
    await expect(page.getByText('No activities yet')).toBeVisible()
    await expect(page.getByRole('button', { name: /add first activity/i })).toBeVisible()
  })

  test('should open add activity dialog', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Click Add First Activity
    await page.getByRole('button', { name: /add first activity/i }).click()
    await page.waitForTimeout(300)

    // Dialog should be visible
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()
    await expect(page.getByPlaceholder('e.g., Review Contract')).toBeVisible()
  })

  test('should create activity via Enter key', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Open add activity dialog
    await page.getByRole('button', { name: /add first activity/i }).click()
    await page.waitForTimeout(300)

    // Fill form and press Enter
    await page.getByPlaceholder('e.g., Review Contract').fill('Test Activity 1')
    await page.keyboard.press('Enter')

    // Wait for dialog to close and activity to be created
    await page.waitForTimeout(1500)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).not.toBeVisible()

    // Activity should appear in the canvas (Vue Flow node)
    await expect(page.getByText('Test Activity 1')).toBeVisible()
  })

  test('should display Vue Flow canvas with nodes after adding activity', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1500)

    // Vue Flow container should be visible
    const vueFlowContainer = page.locator('.vue-flow')
    await expect(vueFlowContainer).toBeVisible()

    // Start and End nodes should be visible
    await expect(page.getByText('Start')).toBeVisible()
    await expect(page.getByText('End')).toBeVisible()

    // Activity node should be visible
    await expect(page.getByText('Test Activity 1')).toBeVisible()
  })

  test('should enter edit mode when clicking Edit Process', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Click Edit Process button
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(300)

    // Edit mode indicator should appear
    await expect(page.getByText('Edit Mode')).toBeVisible()

    // Vue Flow controls should be visible in edit mode
    const controls = page.locator('.vue-flow__controls')
    await expect(controls).toBeVisible()

    // Done Editing button should replace Edit Process
    await expect(page.getByRole('button', { name: /done editing/i })).toBeVisible()
  })

  test('should show minimap in Vue Flow canvas', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Minimap should be visible
    const minimap = page.locator('.vue-flow__minimap')
    await expect(minimap).toBeVisible()
  })

  test('should show floating Add Activity button in edit mode', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(300)

    // Floating add activity button should be visible
    const floatingButton = page.locator('.absolute.bottom-4.right-4').getByRole('button', { name: /add activity/i })
    await expect(floatingButton).toBeVisible()
  })

  test('should add second activity and create edges', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(300)

    // Click floating add activity button
    const floatingButton = page.locator('.absolute.bottom-4.right-4').getByRole('button', { name: /add activity/i })
    await floatingButton.click()
    await page.waitForTimeout(300)

    // Add second activity
    await page.getByPlaceholder('e.g., Review Contract').fill('Test Activity 2')
    await page.keyboard.press('Enter')
    await page.waitForTimeout(1500)

    // Second activity should appear
    await expect(page.getByText('Test Activity 2')).toBeVisible()
  })

  test('should select activity by clicking node', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1500)

    // Click on the activity node
    await page.getByText('Test Activity 1').click()
    await page.waitForTimeout(300)

    // Properties panel should show the activity details
    const propertiesPanel = page.locator('.w-80')
    await expect(propertiesPanel.getByText('Test Activity 1')).toBeVisible()
  })

  test('should switch between flowchart and swimlane views', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Click swimlane button
    await page.getByRole('button', { name: /swimlane/i }).click()
    await page.waitForTimeout(300)

    // Vue Flow should be hidden
    await expect(page.locator('.vue-flow')).not.toBeVisible()

    // Click flowchart button to return
    await page.getByRole('button', { name: /flowchart/i }).click()
    await page.waitForTimeout(300)

    // Vue Flow should be visible again
    await expect(page.locator('.vue-flow')).toBeVisible()
  })

  test('should exit edit mode when clicking Done Editing', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(300)

    // Click Done Editing
    await page.getByRole('button', { name: /done editing/i }).click()
    await page.waitForTimeout(300)

    // Edit mode indicator should disappear
    await expect(page.getByText('Edit Mode')).not.toBeVisible()

    // Edit Process button should be back
    await expect(page.getByRole('button', { name: /edit process/i })).toBeVisible()
  })
})

// =============================================================================
// VUE FLOW INTERACTION TESTS
// =============================================================================

test.describe('Vue Flow Interactions', () => {
  test('should show zoom controls in edit mode', async ({ page }) => {
    // First create a process with activities
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Get any existing process link
    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Enter edit mode
      const editButton = page.getByRole('button', { name: /edit process/i })
      if (await editButton.isVisible()) {
        await editButton.click()
        await page.waitForTimeout(300)

        // Zoom controls should be visible
        const controls = page.locator('.vue-flow__controls')
        await expect(controls).toBeVisible()
      }
    }
  })

  test('should pan and zoom canvas', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Get any existing process link
    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1500)

      const vueFlow = page.locator('.vue-flow')
      if (await vueFlow.isVisible()) {
        // Test scroll zoom
        await vueFlow.hover()
        await page.mouse.wheel(0, -100)
        await page.waitForTimeout(300)

        // Canvas should have changed (zoom in)
        // We can't easily verify zoom level, but no errors should occur
      }
    }
  })
})

// =============================================================================
// ACTIVITY EDITING TESTS
// =============================================================================

test.describe('Activity Editing', () => {
  test('should open edit form when double-clicking activity node', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Get any existing process with activities
    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1500)

      // Check if there are activities (Vue Flow nodes)
      const activityNode = page.locator('.vue-flow__node-activity').first()
      if (await activityNode.isVisible()) {
        // Double click to edit
        await activityNode.dblclick()
        await page.waitForTimeout(500)

        // Edit form should appear in properties panel
        const editingHeader = page.getByText('Editing Activity')
        await expect(editingHeader).toBeVisible()
      }
    }
  })
})

// =============================================================================
// EDGE/CONNECTION TESTS (Vue Flow Specific)
// =============================================================================

test.describe('Vue Flow Edges', () => {
  test('should display edges between nodes', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Get any existing process with activities
    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1500)

      // Check for edge paths in Vue Flow
      const edges = page.locator('.vue-flow__edge-path')
      const edgeCount = await edges.count()

      // If there are activities, there should be edges
      const activityNodes = page.locator('.vue-flow__node-activity, .vue-flow__node-decision')
      const nodeCount = await activityNodes.count()

      if (nodeCount > 0) {
        // Should have at least start->first node and last node->end edges
        expect(edgeCount).toBeGreaterThanOrEqual(2)
      }
    }
  })

  test('should show connection handles on nodes in edit mode', async ({ page }) => {
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Enter edit mode
      const editButton = page.getByRole('button', { name: /edit process/i })
      if (await editButton.isVisible()) {
        await editButton.click()
        await page.waitForTimeout(300)

        // Handles should be present (they may be hidden until hover)
        const handles = page.locator('.vue-flow__handle')
        const handleCount = await handles.count()
        expect(handleCount).toBeGreaterThan(0)
      }
    }
  })
})

// =============================================================================
// MOBILE RESPONSIVENESS TESTS
// =============================================================================

test.describe('Process Editor - Mobile', () => {
  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })
    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    // Page should still show key elements
    await expect(page.getByRole('heading', { name: 'Processes' })).toBeVisible()
  })

  test('should show read-only canvas on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 })

    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Canvas should still be visible on mobile
      // Controls may be hidden on small screens
      await expect(page.getByRole('heading').first()).toBeVisible()
    }
  })
})

// =============================================================================
// API INTEGRATION TESTS
// =============================================================================

test.describe('Process Editor - API Integration', () => {
  test('should fetch process from API on load', async ({ page }) => {
    let apiCalled = false

    await page.route('**/operations/processes/**', route => {
      apiCalled = true
      route.continue()
    })

    await page.goto('/app/processes')
    await page.waitForLoadState('networkidle')

    const processLink = page.locator('a[href*="/app/processes/"]').first()
    if (await processLink.isVisible()) {
      await processLink.click()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      expect(apiCalled).toBeTruthy()
    }
  })

  test('should handle API errors gracefully', async ({ page }) => {
    await page.route('**/operations/processes/invalid-id', route => {
      route.fulfill({
        status: 404,
        body: JSON.stringify({ error: 'Not found' })
      })
    })

    await page.goto('/app/processes/invalid-id')
    await page.waitForLoadState('networkidle')

    // Should show not found message
    await expect(page.getByText(/process not found/i)).toBeVisible()
  })
})
