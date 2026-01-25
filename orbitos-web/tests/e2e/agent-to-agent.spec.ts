/**
 * =============================================================================
 * OrbitOS Operations - Agent-to-Agent (A2A) E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for the Agent-to-Agent functionality.
 * Tests cover Built-in agents, Custom agents, agent type sections,
 * conversation agent selection, and multi-user perspective testing.
 *
 * Spec: F003 - Multi-Agent AI Chat (A2A subset)
 * =============================================================================
 */

import { test, expect, Page } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}`

// Test data for different user personas
const USER_PERSONAS = {
  director: {
    name: 'Director-AI',
    roleTitle: 'Executive Director',
    systemPrompt: 'You are an Executive Director AI. Focus on strategic oversight and organizational alignment.',
    expertiseAreas: 'strategy,leadership,vision,goals'
  },
  hr: {
    name: 'HR-AI',
    roleTitle: 'HR Manager',
    systemPrompt: 'You are an HR Manager AI. Focus on talent, culture, and people operations.',
    expertiseAreas: 'hiring,onboarding,culture,performance,retention'
  },
  finance: {
    name: 'Finance-AI',
    roleTitle: 'Finance Manager',
    systemPrompt: 'You are a Finance Manager AI. Focus on budgeting, forecasting, and financial analysis.',
    expertiseAreas: 'budget,revenue,expenses,forecasting,metrics'
  },
  operations: {
    name: 'Ops-AI',
    roleTitle: 'Operations Manager',
    systemPrompt: 'You are an Operations Manager AI. Focus on process efficiency and workflow optimization.',
    expertiseAreas: 'processes,efficiency,workflows,automation'
  }
}

// =============================================================================
// AI AGENTS PAGE - BASIC LAYOUT
// =============================================================================

test.describe('A2A - Page Layout & Sections', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test('should display AI Agents page heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'AI Agents' })).toBeVisible()
  })

  test('should display page description', async ({ page }) => {
    await expect(page.getByText('Configure AI agents and start multi-agent conversations.')).toBeVisible()
  })

  test('should display Add Agent button', async ({ page }) => {
    await expect(page.getByRole('button', { name: /add agent/i })).toBeVisible()
  })

  test('should display New Conversation button', async ({ page }) => {
    await expect(page.getByRole('button', { name: /new conversation/i })).toBeVisible()
  })

  test('should display Agents tab', async ({ page }) => {
    await expect(page.getByRole('button', { name: /agents/i })).toBeVisible()
  })

  test('should display Conversations tab', async ({ page }) => {
    await expect(page.getByRole('button', { name: /conversations/i })).toBeVisible()
  })

  test('should display Built-in section header', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Built-in' })).toBeVisible()
  })

  test('should display Built-in section description', async ({ page }) => {
    await expect(page.getByText('Pre-configured specialists with access to your organization data')).toBeVisible()
  })

  test('should display Custom section header', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Custom', exact: true })).toBeVisible()
  })

  test('should display Custom section description', async ({ page }) => {
    await expect(page.getByText('AI personas you\'ve created for conversations')).toBeVisible()
  })

  test('should display stat cards', async ({ page }) => {
    await expect(page.getByText('Built-in').first()).toBeVisible()
    await expect(page.getByText('Custom').first()).toBeVisible()
    await expect(page.getByText('Active').first()).toBeVisible()
    await expect(page.getByText('Total').first()).toBeVisible()
  })
})

// =============================================================================
// ADD AGENT DIALOG
// =============================================================================

test.describe('A2A - Add Agent Dialog', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test('should open Add Agent dialog when clicking Add Agent button', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()
  })

  test('should display all form fields in Add Agent dialog', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByLabel(/Agent Name/i)).toBeVisible()
    await expect(page.getByLabel(/Role Title/i)).toBeVisible()
    await expect(page.getByLabel(/AI Model/i)).toBeVisible()
    await expect(page.getByLabel(/System Prompt Template/i)).toBeVisible()
    await expect(page.getByLabel(/System Prompt \*/i)).toBeVisible()
    await expect(page.getByLabel(/Max Tokens/i)).toBeVisible()
    await expect(page.getByLabel(/Temperature/i)).toBeVisible()
    await expect(page.getByText('Avatar Color')).toBeVisible()
  })

  test('should have default model selected as Claude Sonnet', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const modelSelect = page.getByLabel(/AI Model/i)
    await expect(modelSelect).toHaveValue('claude-sonnet-4-20250514')
  })

  test('should display personality section toggle', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByText('Personality & Meeting Behavior (for Emergent mode)')).toBeVisible()
  })

  test('should expand personality section when clicked', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByText('Personality & Meeting Behavior (for Emergent mode)').click()
    await page.waitForTimeout(200)

    await expect(page.getByText('Assertiveness:')).toBeVisible()
    await expect(page.getByText('Communication Style')).toBeVisible()
    await expect(page.getByText('Reaction Tendency')).toBeVisible()
    await expect(page.getByText('Expertise Areas')).toBeVisible()
    await expect(page.getByText('Seniority Level:')).toBeVisible()
  })

  test('should close dialog when clicking Cancel', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByRole('button', { name: /cancel/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).not.toBeVisible()
  })

  test('should close dialog when pressing Escape', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).not.toBeVisible()
  })

  test('should disable Create button when required fields are empty', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const createButton = page.getByRole('button', { name: /create agent/i })
    await expect(createButton).toBeDisabled()
  })

  test('should enable Create button when required fields are filled', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill('Test Agent')
    await page.getByLabel(/Role Title/i).fill('Test Role')

    const createButton = page.getByRole('button', { name: /create agent/i })
    await expect(createButton).not.toBeDisabled()
  })

  test('should NOT close dialog when clicking between form fields', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Click on various fields
    await page.getByLabel(/Agent Name/i).click()
    await page.getByLabel(/Role Title/i).click()
    await page.getByLabel(/AI Model/i).click()
    await page.getByLabel(/Max Tokens/i).click()
    await page.getByLabel(/Temperature/i).click()

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()
  })

  test('should NOT close dialog when clicking avatar color buttons', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Click on avatar color buttons
    const colorButtons = page.locator('button[style*="background-color"]')
    const count = await colorButtons.count()

    for (let i = 0; i < Math.min(count, 3); i++) {
      await colorButtons.nth(i).click()
      await page.waitForTimeout(100)
    }

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()
  })

  test('should populate system prompt when selecting CFO template', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('cfo')
    await page.waitForTimeout(100)

    const systemPrompt = page.getByLabel(/System Prompt \*/i)
    await expect(systemPrompt).toContainText('CFO')
  })

  test('should populate system prompt when selecting Operations template', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('operations')
    await page.waitForTimeout(100)

    const systemPrompt = page.getByLabel(/System Prompt \*/i)
    await expect(systemPrompt).toContainText('Operations')
  })

  test('should populate system prompt when selecting Strategy template', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('strategy')
    await page.waitForTimeout(100)

    const systemPrompt = page.getByLabel(/System Prompt \*/i)
    await expect(systemPrompt).toContainText('Strategy')
  })

  test('should populate system prompt when selecting HR template', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('hr')
    await page.waitForTimeout(100)

    const systemPrompt = page.getByLabel(/System Prompt \*/i)
    await expect(systemPrompt).toContainText('HR')
  })
})

// =============================================================================
// NEW CONVERSATION DIALOG
// =============================================================================

test.describe('A2A - New Conversation Dialog', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test('should open New Conversation dialog when clicking New Conversation button', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'New Conversation' })).toBeVisible()
  })

  test('should display all form fields in New Conversation dialog', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByLabel(/Conversation Title/i)).toBeVisible()
    await expect(page.getByLabel(/Conversation Mode/i)).toBeVisible()
    await expect(page.getByText('Select AI Agents')).toBeVisible()
  })

  test('should display conversation mode options', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    const modeSelect = page.getByLabel(/Conversation Mode/i)
    await expect(modeSelect.locator('option[value="OnDemand"]')).toBeAttached()
    await expect(modeSelect.locator('option[value="Emergent"]')).toBeAttached()
    await expect(modeSelect.locator('option[value="Moderated"]')).toBeAttached()
    await expect(modeSelect.locator('option[value="RoundRobin"]')).toBeAttached()
    await expect(modeSelect.locator('option[value="Free"]')).toBeAttached()
  })

  test('should show Emergent mode explanation when selected', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Conversation Mode/i).selectOption('Emergent')
    await page.waitForTimeout(100)

    await expect(page.getByText('Agents will use personality traits')).toBeVisible()
  })

  test('should close dialog when clicking Cancel', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    await page.getByRole('button', { name: /cancel/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'New Conversation' })).not.toBeVisible()
  })

  test('should close dialog when pressing Escape', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'New Conversation' })).not.toBeVisible()
  })

  test('should disable Start button when title is empty', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    const startButton = page.getByRole('button', { name: /start conversation/i })
    await expect(startButton).toBeDisabled()
  })
})

// =============================================================================
// TAB NAVIGATION
// =============================================================================

test.describe('A2A - Tab Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test('should default to Agents tab', async ({ page }) => {
    const agentsTab = page.getByRole('button', { name: /agents/i })
    await expect(agentsTab).toHaveClass(/bg-purple-500/)
  })

  test('should switch to Conversations tab when clicked', async ({ page }) => {
    await page.getByRole('button', { name: /conversations/i }).click()
    await page.waitForTimeout(200)

    const conversationsTab = page.getByRole('button', { name: /conversations/i })
    await expect(conversationsTab).toHaveClass(/bg-purple-500/)
  })

  test('should show Agents content when on Agents tab', async ({ page }) => {
    // Should show Built-in and Custom sections
    await expect(page.getByRole('heading', { name: 'Built-in' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Custom' })).toBeVisible()
  })

  test('should switch back to Agents tab when clicked', async ({ page }) => {
    // Go to Conversations tab
    await page.getByRole('button', { name: /conversations/i }).click()
    await page.waitForTimeout(200)

    // Go back to Agents tab
    await page.getByRole('button', { name: /agents/i }).click()
    await page.waitForTimeout(200)

    const agentsTab = page.getByRole('button', { name: /agents/i })
    await expect(agentsTab).toHaveClass(/bg-purple-500/)
  })
})

// =============================================================================
// AGENT CREATION E2E
// =============================================================================

test.describe('A2A - Agent Creation Flow', () => {
  test('should create a Custom agent successfully', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Open dialog
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Fill form
    const uniqueName = `Test-Agent-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Test Specialist')
    await page.getByLabel(/System Prompt Template/i).selectOption('custom')

    // Submit
    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).not.toBeVisible()

    // Agent should appear in Custom section
    await expect(page.getByText(uniqueName)).toBeVisible()
  })

  test('should create agent with CFO template', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const uniqueName = `CFO-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('CFO')
    await page.getByLabel(/System Prompt Template/i).selectOption('cfo')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    await expect(page.getByText(uniqueName)).toBeVisible()
  })

  test('should create agent with custom avatar color', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Select emerald color
    await page.locator('button[style*="#10B981"]').click()

    const uniqueName = `Color-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Color Tester')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    // Agent should appear with green color
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await expect(agentCard).toBeVisible()
    await expect(agentCard.locator('div[style*="#10B981"]')).toBeVisible()
  })
})

// =============================================================================
// AGENT EDIT FLOW
// =============================================================================

test.describe('A2A - Agent Edit Flow', () => {
  let testAgentName: string

  test.beforeEach(async ({ page }) => {
    // Create a test agent first
    testAgentName = `Edit-Test-${Date.now()}`

    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(testAgentName)
    await page.getByLabel(/Role Title/i).fill('Edit Tester')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)
  })

  test('should open Edit dialog for Custom agent', async ({ page }) => {
    const agentCard = page.locator('.orbitos-card').filter({ hasText: testAgentName })
    await agentCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'Edit AI Agent' })).toBeVisible()
  })

  test('should pre-populate fields in Edit dialog', async ({ page }) => {
    const agentCard = page.locator('.orbitos-card').filter({ hasText: testAgentName })
    await agentCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByLabel(/Agent Name/i)).toHaveValue(testAgentName)
    await expect(page.getByLabel(/Role Title/i)).toHaveValue('Edit Tester')
  })

  test('should update agent name when edited', async ({ page }) => {
    const agentCard = page.locator('.orbitos-card').filter({ hasText: testAgentName })
    await agentCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    const newName = `Updated-${Date.now()}`
    await page.getByLabel(/Agent Name/i).clear()
    await page.getByLabel(/Agent Name/i).fill(newName)

    await page.getByRole('button', { name: /save changes/i }).click()
    await page.waitForTimeout(1000)

    await expect(page.getByText(newName)).toBeVisible()
  })

  test('should close Edit dialog when clicking Cancel', async ({ page }) => {
    const agentCard = page.locator('.orbitos-card').filter({ hasText: testAgentName })
    await agentCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    await page.getByRole('button', { name: /cancel/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'Edit AI Agent' })).not.toBeVisible()
  })
})

// =============================================================================
// AGENT DELETE FLOW
// =============================================================================

test.describe('A2A - Agent Delete Flow', () => {
  test('should delete Custom agent when confirmed', async ({ page }) => {
    // Create a test agent first
    const testAgentName = `Delete-Test-${Date.now()}`

    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(testAgentName)
    await page.getByLabel(/Role Title/i).fill('Delete Tester')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    // Handle confirmation dialog
    page.on('dialog', dialog => dialog.accept())

    // Delete the agent
    const agentCard = page.locator('.orbitos-card').filter({ hasText: testAgentName })
    await agentCard.getByRole('button', { name: /delete/i }).click()
    await page.waitForTimeout(1000)

    // Agent should no longer be visible
    await expect(page.getByText(testAgentName)).not.toBeVisible()
  })
})

// =============================================================================
// AGENT TOGGLE ACTIVE/INACTIVE
// =============================================================================

test.describe('A2A - Agent Toggle Active', () => {
  test('should toggle agent from Active to Inactive', async ({ page }) => {
    // Create a test agent first
    const testAgentName = `Toggle-Test-${Date.now()}`

    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(testAgentName)
    await page.getByLabel(/Role Title/i).fill('Toggle Tester')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    // Toggle to inactive
    const agentCard = page.locator('.orbitos-card').filter({ hasText: testAgentName })
    await agentCard.getByRole('button', { name: /active/i }).click()
    await page.waitForTimeout(500)

    // Should show Inactive button
    await expect(agentCard.getByRole('button', { name: /inactive/i })).toBeVisible()

    // Toggle back to active
    await agentCard.getByRole('button', { name: /inactive/i }).click()
    await page.waitForTimeout(500)

    // Should show Active button again
    await expect(agentCard.getByRole('button', { name: /active/i })).toBeVisible()
  })
})

// =============================================================================
// CONVERSATION CREATION FLOW
// =============================================================================

test.describe('A2A - Conversation Creation Flow', () => {
  let testAgentName: string

  test.beforeEach(async ({ page }) => {
    // Create a test agent first
    testAgentName = `Convo-Agent-${Date.now()}`

    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(testAgentName)
    await page.getByLabel(/Role Title/i).fill('Conversation Tester')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)
  })

  test('should create conversation with selected agent', async ({ page }) => {
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    const convoTitle = `Test-Convo-${Date.now()}`
    await page.getByLabel(/Conversation Title/i).fill(convoTitle)

    // Select the agent
    await page.locator('button').filter({ hasText: testAgentName }).click()

    await page.getByRole('button', { name: /start conversation/i }).click()

    // Should navigate to conversation page
    await page.waitForURL(/\/app\/ai-agents\/conversations\//)

    // Conversation title should be visible
    await expect(page.getByText(convoTitle)).toBeVisible()
  })
})

// =============================================================================
// MULTI-USER PERSPECTIVE TESTS
// =============================================================================

test.describe('A2A - Director Perspective', () => {
  test('Director can access AI Agents page', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await expect(page.getByRole('heading', { name: 'AI Agents' })).toBeVisible()
  })

  test('Director can create strategic AI agent', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(USER_PERSONAS.director.name)
    await page.getByLabel(/Role Title/i).fill(USER_PERSONAS.director.roleTitle)
    await page.getByLabel(/System Prompt Template/i).selectOption('strategy')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    await expect(page.getByText(USER_PERSONAS.director.name)).toBeVisible()
  })
})

test.describe('A2A - HR Manager Perspective', () => {
  test('HR Manager can create HR AI agent', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(USER_PERSONAS.hr.name)
    await page.getByLabel(/Role Title/i).fill(USER_PERSONAS.hr.roleTitle)
    await page.getByLabel(/System Prompt Template/i).selectOption('hr')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    await expect(page.getByText(USER_PERSONAS.hr.name)).toBeVisible()
  })

  test('HR Manager can start hiring discussion', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create HR agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const hrName = `HR-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(hrName)
    await page.getByLabel(/Role Title/i).fill('HR Manager')
    await page.getByLabel(/System Prompt Template/i).selectOption('hr')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    // Start conversation
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Conversation Title/i).fill('Hiring Pipeline Review')
    await page.locator('button').filter({ hasText: hrName }).click()

    await page.getByRole('button', { name: /start conversation/i }).click()
    await page.waitForURL(/\/app\/ai-agents\/conversations\//)
  })
})

test.describe('A2A - Finance Manager Perspective', () => {
  test('Finance Manager can create Finance AI agent', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(USER_PERSONAS.finance.name)
    await page.getByLabel(/Role Title/i).fill(USER_PERSONAS.finance.roleTitle)
    await page.getByLabel(/System Prompt Template/i).selectOption('cfo')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    await expect(page.getByText(USER_PERSONAS.finance.name)).toBeVisible()
  })
})

test.describe('A2A - Operations Manager Perspective', () => {
  test('Operations Manager can create Operations AI agent', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill(USER_PERSONAS.operations.name)
    await page.getByLabel(/Role Title/i).fill(USER_PERSONAS.operations.roleTitle)
    await page.getByLabel(/System Prompt Template/i).selectOption('operations')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    await expect(page.getByText(USER_PERSONAS.operations.name)).toBeVisible()
  })

  test('Operations Manager can start process optimization discussion', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create Ops agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const opsName = `Ops-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(opsName)
    await page.getByLabel(/Role Title/i).fill('Operations Manager')
    await page.getByLabel(/System Prompt Template/i).selectOption('operations')

    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    // Start conversation
    await page.getByRole('button', { name: /new conversation/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Conversation Title/i).fill('Process Optimization Review')
    await page.locator('button').filter({ hasText: opsName }).click()

    await page.getByRole('button', { name: /start conversation/i }).click()
    await page.waitForURL(/\/app\/ai-agents\/conversations\//)
  })
})

// =============================================================================
// ERROR HANDLING
// =============================================================================

test.describe('A2A - Error Handling', () => {
  test('should handle empty state gracefully', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Page should load without errors
    await expect(page.getByRole('heading', { name: 'AI Agents' })).toBeVisible()
  })

  test('should show conversation not found for invalid ID', async ({ page }) => {
    await page.goto('/app/ai-agents/conversations/00000000-0000-0000-0000-000000000000')
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('Conversation not found')).toBeVisible()
  })

  test('should provide back navigation from not found', async ({ page }) => {
    await page.goto('/app/ai-agents/conversations/00000000-0000-0000-0000-000000000000')
    await page.waitForLoadState('networkidle')

    await page.getByText('Back to AI Agents').click()
    await page.waitForURL(/\/app\/ai-agents$/)
  })
})
