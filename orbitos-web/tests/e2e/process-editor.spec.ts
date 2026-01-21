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

  test('should change activity type to Decision and persist after save', async ({ page, request }) => {
    // Create a test process with one Manual activity via API
    const testProcessName = `Activity Type Test ${Date.now()}`
    const processResponse = await request.post(`${API_BASE}/processes`, {
      data: {
        name: testProcessName,
        purpose: 'Test activity type change',
        status: 0,
        stateType: 0
      }
    })
    const processData = await processResponse.json()
    const testProcessId = processData.id

    // Create a Manual activity
    await request.post(`${API_BASE}/processes/${testProcessId}/activities`, {
      data: {
        name: 'Test Activity',
        order: 1,
        activityType: 'Manual',
        positionX: 250,
        positionY: 150
      }
    })

    try {
      // Navigate to the process
      await page.goto(`/app/processes/${testProcessId}`)
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1500)

      // Verify activity node is Manual (has vue-flow__node-activity class, not decision)
      const activityNode = page.locator('.vue-flow__node-activity').first()
      await expect(activityNode).toBeVisible()

      // Double-click to open edit form
      await activityNode.dblclick()
      await page.waitForTimeout(500)

      // Verify edit form is open
      await expect(page.getByText('Editing Activity')).toBeVisible()

      // Change type to Decision
      const typeSelect = page.locator('select').filter({ has: page.locator('option[value="decision"]') })
      await typeSelect.selectOption('decision')
      await page.waitForTimeout(200)

      // Click Save
      await page.getByRole('button', { name: 'Save' }).click()
      await page.waitForTimeout(1500)

      // Verify the activity is now a Decision node (diamond shape)
      const decisionNode = page.locator('.vue-flow__node-decision')
      await expect(decisionNode).toBeVisible()

      // The old activity node class should not exist for this node
      const manualNodes = page.locator('.vue-flow__node-activity')
      const manualCount = await manualNodes.count()
      expect(manualCount).toBe(0)

      // Reload page and verify persistence
      await page.reload()
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1500)

      // Decision node should still be present after reload
      await expect(page.locator('.vue-flow__node-decision')).toBeVisible()
    } finally {
      // Cleanup
      await request.delete(`${API_BASE}/processes/${testProcessId}`)
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
// EDGE DELETION PERSISTENCE TESTS
// =============================================================================

test.describe('Edge Deletion Persistence', () => {
  test.describe.configure({ mode: 'serial' })

  let testProcessId: string
  const testProcessName = `Edge Test Process ${Date.now()}`

  test.beforeAll(async ({ request }) => {
    // Create a test process with activities via API
    const response = await request.post(`${API_BASE}/processes`, {
      data: {
        name: testProcessName,
        purpose: 'E2E Edge Deletion Test',
        trigger: 'Test Trigger',
        output: 'Test Output',
        status: 0,
        stateType: 0
      }
    })
    const processData = await response.json()
    testProcessId = processData.id

    // Add 3 activities to create a flow with edges
    // ActivityType enum: Manual=0, Automated=1, Hybrid=2, Decision=3, Handoff=4
    for (let i = 1; i <= 3; i++) {
      const activityResponse = await request.post(`${API_BASE}/processes/${testProcessId}/activities`, {
        data: {
          name: `Activity ${i}`,
          order: i,
          activityType: 0 // Manual
        }
      })
      console.log(`Created activity ${i}:`, activityResponse.status())
    }
  })

  test.afterAll(async ({ request }) => {
    if (testProcessId) {
      await request.delete(`${API_BASE}/processes/${testProcessId}`)
    }
  })

  test('should delete Start edge and persist after page reload', async ({ page }) => {
    // Capture console logs for debugging
    const consoleLogs: string[] = []
    page.on('console', msg => {
      if (msg.text().includes('[Process') || msg.text().includes('implicit') || msg.text().includes('edge')) {
        consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
      }
    })

    // Navigate to our test process
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Allow Vue Flow to render

    // Take screenshot of initial state
    await page.screenshot({ path: 'test-results/edge-delete-1-before.png', fullPage: true })

    // Count initial edges - should have Start->Activity1, Activity1->Activity2, Activity2->Activity3, Activity3->End
    const initialEdges = page.locator('.vue-flow__edge-path')
    const initialEdgeCount = await initialEdges.count()
    console.log(`Initial edge count: ${initialEdgeCount}`)
    expect(initialEdgeCount).toBeGreaterThanOrEqual(4) // At minimum: start, 2 activity edges, end

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(500)
    await expect(page.getByText('Edit Mode', { exact: true })).toBeVisible()

    // Take screenshot in edit mode
    await page.screenshot({ path: 'test-results/edge-delete-2-edit-mode.png', fullPage: true })

    // Click on the first edge (Start -> Activity 1) to select it
    const firstEdge = page.locator('.vue-flow__edge').first()
    await firstEdge.click()
    await page.waitForTimeout(300)

    // Take screenshot with edge selected
    await page.screenshot({ path: 'test-results/edge-delete-3-edge-selected.png', fullPage: true })

    // Press Delete key to remove the edge
    await page.keyboard.press('Delete')
    await page.waitForTimeout(3000) // Wait for API call and state updates

    // Take screenshot after deletion
    await page.screenshot({ path: 'test-results/edge-delete-4-after-delete.png', fullPage: true })

    // Check API state immediately after delete
    const apiResponse = await page.request.get(`${API_BASE}/processes/${testProcessId}`)
    const processState = await apiResponse.json()
    console.log(`API state after delete - useExplicitFlow: ${processState.useExplicitFlow}, entryActivityId: ${processState.entryActivityId}, exitActivityId: ${processState.exitActivityId}`)

    // Verify edge count decreased
    const afterDeleteEdges = page.locator('.vue-flow__edge-path')
    const afterDeleteCount = await afterDeleteEdges.count()
    console.log(`Edge count after delete: ${afterDeleteCount}`)
    expect(afterDeleteCount).toBeLessThan(initialEdgeCount)

    // Exit edit mode
    await page.getByRole('button', { name: /done editing/i }).click()
    await page.waitForTimeout(500)

    // Reload the page
    await page.reload()
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Allow Vue Flow to render

    // Take screenshot after reload
    await page.screenshot({ path: 'test-results/edge-delete-5-after-reload.png', fullPage: true })

    // Count edges again - should still be reduced
    const afterReloadEdges = page.locator('.vue-flow__edge-path')
    const afterReloadCount = await afterReloadEdges.count()
    console.log(`Edge count after reload: ${afterReloadCount}`)

    // Log captured console messages for debugging
    console.log('Console logs captured:')
    consoleLogs.forEach(log => console.log(log))

    // CRITICAL: Edge should NOT reappear after reload
    expect(afterReloadCount).toBe(afterDeleteCount)
  })

  test('should verify API state after edge deletion', async ({ request }) => {
    // Check API to verify the process state
    const response = await request.get(`${API_BASE}/processes/${testProcessId}`)
    const process = await response.json()

    console.log(`Process: ${process.name}`)
    console.log(`useExplicitFlow: ${process.useExplicitFlow}`)
    console.log(`entryActivityId: ${process.entryActivityId}`)
    console.log(`exitActivityId: ${process.exitActivityId}`)
    console.log(`edges count: ${process.edges?.length || 0}`)

    // After deleting Start edge, process should be in explicit mode
    expect(process.useExplicitFlow).toBe(true)

    // Entry activity should be cleared (we deleted the Start edge)
    expect(process.entryActivityId).toBeNull()
  })
})

// =============================================================================
// ADD ACTIVITY DIALOG BEHAVIOR TESTS
// =============================================================================

test.describe('Add Activity Dialog Behavior', () => {
  test.describe.configure({ mode: 'serial' })

  let testProcessId: string
  const testProcessName = `Dialog Test Process ${Date.now()}`

  test.beforeAll(async ({ request }) => {
    // Create a test process via API
    const response = await request.post(`${API_BASE}/processes`, {
      data: {
        name: testProcessName,
        purpose: 'E2E Dialog Behavior Test',
        trigger: 'Test Trigger',
        output: 'Test Output',
        status: 0,
        stateType: 0
      }
    })
    const data = await response.json()
    testProcessId = data.id
  })

  test.afterAll(async ({ request }) => {
    if (testProcessId) {
      await request.delete(`${API_BASE}/processes/${testProcessId}`)
    }
  })

  test('dialog should NOT close when clicking on form fields', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1000)

    // Open add activity dialog
    await page.getByRole('button', { name: /add first activity/i }).click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Click on the Activity Name input field
    const nameInput = page.getByPlaceholder('e.g., Review Contract')
    await nameInput.click()
    await page.waitForTimeout(200)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Type something in the name field
    await nameInput.fill('Test Activity Name')
    await page.waitForTimeout(200)

    // Click on the Type dropdown
    const typeSelect = page.locator('select').first()
    await typeSelect.click()
    await page.waitForTimeout(200)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Select a different type
    await typeSelect.selectOption('automated')
    await page.waitForTimeout(200)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Click on the Duration input
    const durationInput = page.getByPlaceholder('e.g., 30')
    await durationInput.click()
    await durationInput.fill('45')
    await page.waitForTimeout(200)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Click on the Description textarea
    const descriptionTextarea = page.getByPlaceholder('What does this activity involve?')
    await descriptionTextarea.click()
    await descriptionTextarea.fill('Test description for the activity')
    await page.waitForTimeout(200)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Click on empty space within the dialog content (not backdrop)
    const dialogContent = page.locator('.orbitos-glass').first()
    await dialogContent.click({ position: { x: 10, y: 10 } })
    await page.waitForTimeout(200)

    // Dialog should still be visible
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Now submit the form
    await page.getByRole('button', { name: 'Add Activity' }).click()
    await page.waitForTimeout(1500)

    // Dialog should be closed and activity created
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).not.toBeVisible()
    await expect(page.getByText('Test Activity Name')).toBeVisible()
  })

  test('dialog should close when clicking backdrop', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1500)

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(300)

    // Click floating add activity button
    const floatingButton = page.locator('.absolute.bottom-4.right-4').getByRole('button', { name: /add activity/i })
    await floatingButton.click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Click on the backdrop (the dark overlay)
    // The backdrop is positioned at the edges of the viewport, outside the dialog content
    await page.mouse.click(5, 5)
    await page.waitForTimeout(300)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).not.toBeVisible()
  })

  test('dialog should close when pressing Escape key', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1500)

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(300)

    // Click floating add activity button
    const floatingButton = page.locator('.absolute.bottom-4.right-4').getByRole('button', { name: /add activity/i })
    await floatingButton.click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Press Escape
    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).not.toBeVisible()
  })

  test('dialog should close when clicking Cancel button', async ({ page }) => {
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(1500)

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(300)

    // Click floating add activity button
    const floatingButton = page.locator('.absolute.bottom-4.right-4').getByRole('button', { name: /add activity/i })
    await floatingButton.click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).toBeVisible()

    // Click Cancel button
    await page.getByRole('button', { name: 'Cancel' }).click()
    await page.waitForTimeout(300)

    // Dialog should be closed
    await expect(page.getByRole('heading', { name: 'Add New Activity' })).not.toBeVisible()
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

// =============================================================================
// END NODE POSITIONING TESTS
// =============================================================================

test.describe('End Node Positioning', () => {
  test.describe.configure({ mode: 'serial' })

  let testProcessId: string
  const testProcessName = `End Node Test Process ${Date.now()}`

  test.beforeAll(async ({ request }) => {
    // Create a test process with activities via API
    const response = await request.post(`${API_BASE}/processes`, {
      data: {
        name: testProcessName,
        purpose: 'E2E End Node Position Test',
        trigger: 'Test Trigger',
        output: 'Test Output',
        status: 0,
        stateType: 0
      }
    })
    const processData = await response.json()
    testProcessId = processData.id

    // Add 2 activities to create a flow
    for (let i = 1; i <= 2; i++) {
      await request.post(`${API_BASE}/processes/${testProcessId}/activities`, {
        data: {
          name: `Activity ${i}`,
          order: i,
          activityType: 0, // Manual
          positionX: 200, // Default centered X position for activity (HORIZONTAL_CENTER - NODE_WIDTH/2)
          positionY: i * 150 // 150, 300 (VERTICAL_SPACING)
        }
      })
    }
  })

  test.afterAll(async ({ request }) => {
    if (testProcessId) {
      await request.delete(`${API_BASE}/processes/${testProcessId}`)
    }
  })

  test('End node should stay horizontally centered when moving activity nodes', async ({ page }) => {
    // Navigate to test process
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000) // Allow Vue Flow to render

    // Take screenshot of initial state
    await page.screenshot({ path: 'test-results/end-node-position-1-initial.png', fullPage: true })

    // Get initial End node position
    const endNode = page.locator('.vue-flow__node-end')
    await expect(endNode).toBeVisible()

    // Get the transform style to extract the position
    const initialTransform = await endNode.evaluate(el => {
      const style = window.getComputedStyle(el)
      return style.transform
    })
    console.log(`Initial End node transform: ${initialTransform}`)

    // Parse the transform matrix to get X position
    // transform: matrix(1, 0, 0, 1, X, Y) or translate(Xpx, Ypx)
    const getXFromTransform = (transform: string): number => {
      // Try matrix format first
      const matrixMatch = transform.match(/matrix\([^,]+,[^,]+,[^,]+,[^,]+,\s*([^,]+),/)
      if (matrixMatch) {
        return parseFloat(matrixMatch[1])
      }
      // Try translate format
      const translateMatch = transform.match(/translate\(([^,]+)px/)
      if (translateMatch) {
        return parseFloat(translateMatch[1])
      }
      return 0
    }

    const initialEndX = getXFromTransform(initialTransform)
    console.log(`Initial End node X position: ${initialEndX}`)

    // Enter edit mode
    await page.getByRole('button', { name: /edit process/i }).click()
    await page.waitForTimeout(500)
    await expect(page.getByText('Edit Mode', { exact: true })).toBeVisible()

    // Find the first activity node
    const activityNode = page.locator('.vue-flow__node-activity').first()
    await expect(activityNode).toBeVisible()

    // Get the activity's bounding box
    const activityBox = await activityNode.boundingBox()
    if (!activityBox) {
      throw new Error('Could not get activity bounding box')
    }

    // Drag the activity node horizontally to the right (by 150px)
    const startX = activityBox.x + activityBox.width / 2
    const startY = activityBox.y + activityBox.height / 2
    const endX = startX + 150
    const endY = startY

    await page.mouse.move(startX, startY)
    await page.mouse.down()
    await page.mouse.move(endX, endY, { steps: 10 })
    await page.mouse.up()

    await page.waitForTimeout(1000) // Wait for position update

    // Take screenshot after drag
    await page.screenshot({ path: 'test-results/end-node-position-2-after-drag.png', fullPage: true })

    // Get End node position after dragging activity
    const afterDragTransform = await endNode.evaluate(el => {
      const style = window.getComputedStyle(el)
      return style.transform
    })
    console.log(`After drag End node transform: ${afterDragTransform}`)

    const afterDragEndX = getXFromTransform(afterDragTransform)
    console.log(`After drag End node X position: ${afterDragEndX}`)

    // CRITICAL ASSERTION: End node X position should remain the same
    // It should stay centered at HORIZONTAL_CENTER (300), not shift with the activity
    expect(afterDragEndX).toBe(initialEndX)
  })

  test('End node should maintain center position after page reload', async ({ page }) => {
    // Navigate to test process
    await page.goto(`/app/processes/${testProcessId}`)
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(2000)

    // Get End node position
    const endNode = page.locator('.vue-flow__node-end')
    await expect(endNode).toBeVisible()

    const transform = await endNode.evaluate(el => {
      const style = window.getComputedStyle(el)
      return style.transform
    })

    // Parse X from transform
    const getXFromTransform = (t: string): number => {
      const matrixMatch = t.match(/matrix\([^,]+,[^,]+,[^,]+,[^,]+,\s*([^,]+),/)
      if (matrixMatch) return parseFloat(matrixMatch[1])
      const translateMatch = t.match(/translate\(([^,]+)px/)
      if (translateMatch) return parseFloat(translateMatch[1])
      return 0
    }

    const endX = getXFromTransform(transform)
    console.log(`End node X position: ${endX}`)

    // Get Start node position for comparison
    const startNode = page.locator('.vue-flow__node-start')
    await expect(startNode).toBeVisible()

    const startTransform = await startNode.evaluate(el => {
      const style = window.getComputedStyle(el)
      return style.transform
    })

    const startX = getXFromTransform(startTransform)
    console.log(`Start node X position: ${startX}`)

    // End node should be horizontally aligned with Start node (both at HORIZONTAL_CENTER = 300)
    expect(endX).toBe(startX)
  })
})
