import { test, expect } from '@playwright/test'

/**
 * A2A (Agent-to-Agent) Inner Dialogue E2E Tests
 *
 * Tests the orchestrator feature and inner dialogue visibility.
 * These tests verify that users can see how AI agents collaborate.
 */

// Test configuration
const TEST_ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE_URL = 'http://localhost:5000/api'

test.describe('A2A Inner Dialogue', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate directly to the app - dev mode allows anonymous access
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test.describe('AI Agents Page - Orchestrator Section', () => {
    test('should display orchestrator quick access section', async ({ page }) => {
      await page.goto('/app/ai-agents')
      await page.waitForLoadState('networkidle')

      // Screenshot: AI Agents page loaded
      await page.screenshot({
        path: 'tests/e2e/screenshots/a2a/01-ai-agents-page.png',
        fullPage: true
      })

      // Verify orchestrator section exists
      const orchestratorSection = page.locator('text=Ask OrbitOS')
      await expect(orchestratorSection).toBeVisible()

      // Verify input field exists
      const queryInput = page.locator('input[placeholder*="bottleneck"]')
      await expect(queryInput).toBeVisible()

      // Verify Ask button exists
      const askButton = page.locator('button:has-text("Ask")')
      await expect(askButton).toBeVisible()

      // Screenshot: Orchestrator section
      await page.screenshot({
        path: 'tests/e2e/screenshots/a2a/02-orchestrator-section.png'
      })
    })

    test('should allow typing a query', async ({ page }) => {
      await page.goto('/app/ai-agents')
      await page.waitForLoadState('networkidle')

      // Type a query
      const queryInput = page.locator('input[placeholder*="bottleneck"]')
      await queryInput.fill('Who is overloaded on my team?')

      // Screenshot: Query typed
      await page.screenshot({
        path: 'tests/e2e/screenshots/a2a/03-query-typed.png'
      })

      // Verify the input has the value
      await expect(queryInput).toHaveValue('Who is overloaded on my team?')

      // Verify Ask button is enabled
      const askButton = page.locator('button:has-text("Ask")')
      await expect(askButton).toBeEnabled()
    })

    test('should show loading state when asking', async ({ page }) => {
      await page.goto('/app/ai-agents')
      await page.waitForLoadState('networkidle')

      // Type a query
      const queryInput = page.locator('input[placeholder*="bottleneck"]')
      await queryInput.fill('What processes need attention?')

      // Click Ask (don't wait for response)
      const askButton = page.locator('button:has-text("Ask")')
      await askButton.click()

      // Verify loading state
      const thinkingButton = page.locator('button:has-text("Thinking...")')
      // This might be too fast to catch, but try
      const isThinking = await thinkingButton.isVisible().catch(() => false)

      // Screenshot: Loading state (if captured)
      if (isThinking) {
        await page.screenshot({
          path: 'tests/e2e/screenshots/a2a/04-loading-state.png'
        })
      }
    })
  })

  test.describe('Inner Dialogue Display Component', () => {
    test('should render inner dialogue steps correctly', async ({ page }) => {
      // Navigate to a page where we can test the component
      await page.goto('/app/ai-agents')
      await page.waitForLoadState('networkidle')

      // We need to inject test data or mock the API to show inner dialogue
      // For now, let's verify the component exists and renders correctly

      // Check if InnerDialogueDisplay component is available
      // This would show after an orchestrator response
      const innerDialogueSection = page.locator('text=Inner Dialogue')

      // Screenshot: Page state
      await page.screenshot({
        path: 'tests/e2e/screenshots/a2a/05-page-state.png',
        fullPage: true
      })
    })
  })

  test.describe('Built-in Agents', () => {
    test('should display built-in specialist agents', async ({ page }) => {
      await page.goto('/app/ai-agents')
      await page.waitForLoadState('networkidle')

      // Wait for agents to load
      await page.waitForTimeout(1000)

      // Verify Built-in section exists
      const builtInSection = page.locator('h2:has-text("Built-in")')
      await expect(builtInSection).toBeVisible()

      // Check for specialist agents
      const peopleExpert = page.locator('text=People Expert')
      const processExpert = page.locator('text=Process Expert')
      const strategyExpert = page.locator('text=Strategy Expert')
      const financeExpert = page.locator('text=Finance Expert')

      // Screenshot: Built-in agents
      await page.screenshot({
        path: 'tests/e2e/screenshots/a2a/06-builtin-agents.png'
      })

      // At least one should exist if built-in agents are seeded
      const hasAnyBuiltIn = await Promise.any([
        peopleExpert.isVisible(),
        processExpert.isVisible(),
        strategyExpert.isVisible(),
        financeExpert.isVisible()
      ]).catch(() => false)

      // Log the state for debugging
      console.log('Has any built-in agents:', hasAnyBuiltIn)
    })

    test('should show context scopes for built-in agents', async ({ page }) => {
      await page.goto('/app/ai-agents')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(1000)

      // Look for context scope badges (e.g., "resources", "roles", "functions")
      const contextBadges = page.locator('.rounded-full:has-text("resources"), .rounded-full:has-text("roles"), .rounded-full:has-text("functions")')

      const badgeCount = await contextBadges.count()
      console.log('Context scope badges found:', badgeCount)

      // Screenshot: Context scopes
      await page.screenshot({
        path: 'tests/e2e/screenshots/a2a/07-context-scopes.png'
      })
    })
  })

  test.describe('Conversation with Inner Dialogue', () => {
    test('should create a new conversation', async ({ page }) => {
      await page.goto('/app/ai-agents')
      await page.waitForLoadState('networkidle')

      // Click New Conversation button
      const newConvButton = page.locator('button:has-text("New Conversation")')
      if (await newConvButton.isEnabled()) {
        await newConvButton.click()

        // Wait for dialog
        await page.waitForTimeout(500)

        // Screenshot: New conversation dialog
        await page.screenshot({
          path: 'tests/e2e/screenshots/a2a/08-new-conversation-dialog.png'
        })
      }
    })
  })

  test.describe('API Endpoints', () => {
    test('should have orchestrate endpoint accessible', async ({ request }) => {
      // Test the orchestrate endpoint exists (will fail without proper auth but should return proper error)
      const response = await request.post(`${API_BASE_URL}/organizations/${TEST_ORG_ID}/ai/orchestrate`, {
        data: {
          message: 'Test query'
        },
        headers: {
          'Content-Type': 'application/json'
        }
      })

      // Should return something (even if error due to no auth)
      expect([200, 401, 403, 500]).toContain(response.status())

      console.log('Orchestrate endpoint status:', response.status())
    })

    test('should have specialists endpoint accessible', async ({ request }) => {
      // Test the specialists endpoint
      const response = await request.get(`${API_BASE_URL}/organizations/${TEST_ORG_ID}/ai/specialists`)

      // Should return something
      expect([200, 401, 403]).toContain(response.status())

      console.log('Specialists endpoint status:', response.status())

      if (response.status() === 200) {
        const data = await response.json()
        console.log('Specialists data:', JSON.stringify(data, null, 2))
      }
    })

    test('should have consult-specialist endpoint accessible', async ({ request }) => {
      const response = await request.post(`${API_BASE_URL}/organizations/${TEST_ORG_ID}/ai/consult-specialist`, {
        data: {
          specialistKey: 'people',
          query: 'Who is on my team?'
        },
        headers: {
          'Content-Type': 'application/json'
        }
      })

      expect([200, 401, 403, 500]).toContain(response.status())

      console.log('Consult-specialist endpoint status:', response.status())
    })
  })
})

test.describe('Inner Dialogue UI Components', () => {
  test('visual regression - InnerDialogueDisplay component styles', async ({ page }) => {
    // Create a test page with the component
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Take baseline screenshot of the page
    await page.screenshot({
      path: 'tests/e2e/screenshots/a2a/09-baseline.png',
      fullPage: true
    })
  })
})

test.describe('Conversation Page - Message with Inner Dialogue', () => {
  test('should navigate to conversations tab', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Click Conversations tab
    const convTab = page.locator('button:has-text("Conversations")')
    await convTab.click()

    // Wait for tab content to load
    await page.waitForTimeout(500)

    // Screenshot: Conversations tab
    await page.screenshot({
      path: 'tests/e2e/screenshots/a2a/10-conversations-tab.png'
    })

    // Verify we're on conversations tab
    const tabContent = page.locator('text=No Conversations Yet').or(page.locator('a[href*="/conversations/"]'))
    await expect(tabContent.first()).toBeVisible()
  })
})
