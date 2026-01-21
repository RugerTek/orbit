/**
 * =============================================================================
 * OrbitOS Operations - AI Agents E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for the AI Agents management page.
 * Tests cover agent CRUD operations, model selection, and UI interactions.
 *
 * Spec: F003 - Multi-Agent AI Chat (Agent Management subset)
 * =============================================================================
 */

import { test, expect, Page } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}`

// Test data
const TEST_AGENTS = {
  cfo: {
    name: 'CFO-AI',
    roleTitle: 'Chief Financial Officer',
    provider: 'anthropic',
    modelId: 'claude-sonnet-4-20250514',
    modelDisplayName: 'Claude Sonnet 4',
    systemPrompt: 'You are the CFO AI assistant. Provide financial analysis and budget recommendations.',
    avatarColor: '#F59E0B'
  },
  ops: {
    name: 'Ops-AI',
    roleTitle: 'Operations Manager',
    provider: 'anthropic',
    modelId: 'claude-3-5-haiku-20241022',
    modelDisplayName: 'Claude 3.5 Haiku',
    systemPrompt: 'You are the Operations Manager AI. Focus on process optimization and efficiency.',
    avatarColor: '#10B981'
  },
  strategy: {
    name: 'Strategy-AI',
    roleTitle: 'Strategy Consultant',
    provider: 'anthropic',
    modelId: 'claude-opus-4-20250514',
    modelDisplayName: 'Claude Opus 4',
    systemPrompt: 'You are a Strategy Consultant AI. Provide strategic insights and market analysis.',
    avatarColor: '#8B5CF6'
  }
}

// Helper function to login
async function login(page: Page) {
  await page.goto('/login')
  await page.fill('input[type="email"]', 'rodrigo@rugertek.com')
  await page.fill('input[type="password"]', '123456')
  await page.click('button[type="submit"]')
  await page.waitForURL(/\/app/)
}

// =============================================================================
// AI AGENTS PAGE - NAVIGATION & LAYOUT
// =============================================================================

test.describe('AI Agents Page - Navigation & Layout', () => {
  test.beforeEach(async ({ page }) => {
    // Mock auth for tests or skip login
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test('should display AI Agents page with correct heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'AI Agents' })).toBeVisible()
    await expect(page.getByText('Configure AI agents with different models')).toBeVisible()
  })

  test('should display Add Agent button', async ({ page }) => {
    await expect(page.getByRole('button', { name: /add agent/i })).toBeVisible()
  })

  test('should display stats cards', async ({ page }) => {
    await expect(page.getByText('Total Agents')).toBeVisible()
    await expect(page.getByText('Active')).toBeVisible()
    await expect(page.getByText('Anthropic')).toBeVisible()
    await expect(page.getByText('OpenAI / Google')).toBeVisible()
  })

  test('should display empty state when no agents exist', async ({ page }) => {
    // If no agents, should show empty state
    const emptyState = page.getByText('No AI Agents Yet')
    const agentCards = page.locator('.orbitos-card')

    // Either empty state or agent cards should be visible
    const hasEmptyState = await emptyState.isVisible().catch(() => false)
    const hasAgentCards = await agentCards.count() > 0

    expect(hasEmptyState || hasAgentCards).toBeTruthy()
  })

  test('should have AI Agents link in sidebar navigation', async ({ page }) => {
    const sidebarLink = page.locator('nav').getByRole('link', { name: 'AI Agents' })
    await expect(sidebarLink).toBeVisible()
    await expect(sidebarLink).toHaveAttribute('href', '/app/ai-agents')
  })
})

// =============================================================================
// AI AGENTS - CREATE AGENT DIALOG
// =============================================================================

test.describe('AI Agents - Create Agent Dialog', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test('should open create agent dialog when clicking Add Agent', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()
    await expect(page.getByText('Configure a new AI agent')).toBeVisible()
  })

  test('should display all form fields in create dialog', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Basic fields
    await expect(page.getByLabel(/Agent Name/i)).toBeVisible()
    await expect(page.getByLabel(/Role Title/i)).toBeVisible()

    // Model selection
    await expect(page.getByLabel(/AI Model/i)).toBeVisible()

    // System prompt template and textarea
    await expect(page.getByLabel(/System Prompt Template/i)).toBeVisible()
    await expect(page.getByLabel(/System Prompt \*/i)).toBeVisible()

    // Advanced settings
    await expect(page.getByLabel(/Max Tokens/i)).toBeVisible()
    await expect(page.getByLabel(/Temperature/i)).toBeVisible()

    // Avatar color selection
    await expect(page.getByText('Avatar Color')).toBeVisible()
  })

  test('should have correct default values in create dialog', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Check default model is Claude Sonnet
    const modelSelect = page.getByLabel(/AI Model/i)
    await expect(modelSelect).toHaveValue('claude-sonnet-4-20250514')

    // Check default max tokens
    const maxTokens = page.getByLabel(/Max Tokens/i)
    await expect(maxTokens).toHaveValue('4096')

    // Check default temperature
    const temperature = page.getByLabel(/Temperature/i)
    await expect(temperature).toHaveValue('0.7')
  })

  test('should show only Anthropic models in dropdown', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const modelSelect = page.getByLabel(/AI Model/i)

    // Check Anthropic models are present
    await expect(modelSelect.locator('option[value="claude-sonnet-4-20250514"]')).toBeAttached()
    await expect(modelSelect.locator('option[value="claude-opus-4-20250514"]')).toBeAttached()
    await expect(modelSelect.locator('option[value="claude-3-5-haiku-20241022"]')).toBeAttached()

    // Check OpenAI/Google models are NOT present (disabled)
    await expect(modelSelect.locator('option[value="gpt-4o"]')).not.toBeAttached()
    await expect(modelSelect.locator('option[value="gemini-1.5-pro"]')).not.toBeAttached()
  })

  test('should populate system prompt when selecting template', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Select CFO template
    await page.getByLabel(/System Prompt Template/i).selectOption('cfo')
    await page.waitForTimeout(100)

    // Check system prompt contains CFO-related content
    const systemPrompt = page.getByLabel(/System Prompt \*/i)
    await expect(systemPrompt).toContainText('CFO')
  })

  test('should close dialog when clicking Cancel', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByRole('button', { name: /Cancel/i }).click()
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

  test('should close dialog when clicking backdrop', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Click backdrop (outside dialog)
    await page.locator('.fixed.inset-0.bg-black\\/60').click({ position: { x: 10, y: 10 } })
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).not.toBeVisible()
  })

  test('should NOT close dialog when clicking between form fields', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on Agent Name field
    const nameField = page.getByLabel(/Agent Name/i)
    await nameField.click()
    await nameField.fill('Test Agent')
    await page.waitForTimeout(100)

    // Click on Role Title field (simulating clicking between fields)
    const roleField = page.getByLabel(/Role Title/i)
    await roleField.click()
    await roleField.fill('Test Role')
    await page.waitForTimeout(100)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on Model dropdown
    const modelSelect = page.getByLabel(/AI Model/i)
    await modelSelect.click()
    await page.waitForTimeout(100)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on System Prompt textarea
    const promptArea = page.getByLabel(/System Prompt \*/i)
    await promptArea.click()
    await page.waitForTimeout(100)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on Max Tokens field
    const maxTokens = page.getByLabel(/Max Tokens/i)
    await maxTokens.click()
    await page.waitForTimeout(100)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on Temperature field
    const temperature = page.getByLabel(/Temperature/i)
    await temperature.click()
    await page.waitForTimeout(100)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Tab between fields (keyboard navigation)
    await nameField.focus()
    await page.keyboard.press('Tab')
    await page.waitForTimeout(100)
    await page.keyboard.press('Tab')
    await page.waitForTimeout(100)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()
  })

  test('should NOT close dialog when clicking avatar color buttons', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on different avatar color buttons
    const colorButtons = page.locator('button[style*="background-color"]')
    const colorCount = await colorButtons.count()

    for (let i = 0; i < Math.min(colorCount, 4); i++) {
      await colorButtons.nth(i).click()
      await page.waitForTimeout(100)

      // Dialog should still be open after each color click
      await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()
    }
  })

  test('should NOT close dialog when clicking inside dialog content area', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on the dialog card itself (the white content area)
    const dialogCard = page.locator('.orbitos-glass').first()
    await dialogCard.click({ position: { x: 50, y: 50 } })
    await page.waitForTimeout(200)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()

    // Click on empty space within the dialog
    await dialogCard.click({ position: { x: 100, y: 200 } })
    await page.waitForTimeout(200)

    // Dialog should still be open
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).toBeVisible()
  })

  test('should disable Create button when required fields are empty', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const createButton = page.getByRole('button', { name: /Create Agent/i })
    await expect(createButton).toBeDisabled()
  })

  test('should enable Create button when required fields are filled', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Fill required fields
    await page.getByLabel(/Agent Name/i).fill('Test Agent')
    await page.getByLabel(/Role Title/i).fill('Test Role')
    // System prompt has default value from template

    const createButton = page.getByRole('button', { name: /Create Agent/i })
    await expect(createButton).not.toBeDisabled()
  })
})

// =============================================================================
// AI AGENTS - CRUD OPERATIONS
// =============================================================================

test.describe('AI Agents - CRUD Operations', () => {
  test('should create a new agent successfully', async ({ page }) => {
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
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Create AI Agent' })).not.toBeVisible()

    // Agent should appear in list
    await expect(page.getByText(uniqueName)).toBeVisible()
  })

  test('should display agent card with correct information', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create an agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const uniqueName = `Display-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Display Tester')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Check agent card content
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await expect(agentCard).toBeVisible()
    await expect(agentCard.getByText('Display Tester')).toBeVisible()
    await expect(agentCard.getByText(/Claude Sonnet/i)).toBeVisible()
    await expect(agentCard.getByRole('button', { name: /Edit/i })).toBeVisible()
    await expect(agentCard.getByRole('button', { name: /Delete/i })).toBeVisible()
  })

  test('should open edit dialog when clicking Edit', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create an agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)
    const uniqueName = `Edit-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Edit Tester')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Click edit
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await agentCard.getByRole('button', { name: /Edit/i }).click()
    await page.waitForTimeout(300)

    // Edit dialog should be visible
    await expect(page.getByRole('heading', { name: 'Edit AI Agent' })).toBeVisible()

    // Fields should be pre-populated
    const nameField = page.getByLabel(/Agent Name/i)
    await expect(nameField).toHaveValue(uniqueName)
  })

  test('should update agent when editing', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create an agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)
    const originalName = `Update-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(originalName)
    await page.getByLabel(/Role Title/i).fill('Original Role')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Click edit
    const agentCard = page.locator('.orbitos-card').filter({ hasText: originalName })
    await agentCard.getByRole('button', { name: /Edit/i }).click()
    await page.waitForTimeout(300)

    // Update name
    const newName = `Updated-${Date.now()}`
    await page.getByLabel(/Agent Name/i).clear()
    await page.getByLabel(/Agent Name/i).fill(newName)
    await page.getByRole('button', { name: /Save Changes/i }).click()
    await page.waitForTimeout(1000)

    // Dialog should close
    await expect(page.getByRole('heading', { name: 'Edit AI Agent' })).not.toBeVisible()

    // Updated name should appear
    await expect(page.getByText(newName)).toBeVisible()
  })

  test('should toggle agent active/inactive', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create an agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)
    const uniqueName = `Toggle-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Toggle Tester')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Click Disable
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await agentCard.getByRole('button', { name: /Disable/i }).click()
    await page.waitForTimeout(500)

    // Should now show Enable button and Inactive badge
    await expect(agentCard.getByRole('button', { name: /Enable/i })).toBeVisible()
    await expect(agentCard.getByText(/Inactive/i)).toBeVisible()

    // Click Enable
    await agentCard.getByRole('button', { name: /Enable/i }).click()
    await page.waitForTimeout(500)

    // Should now show Disable button and no Inactive badge
    await expect(agentCard.getByRole('button', { name: /Disable/i })).toBeVisible()
  })

  test('should delete agent when clicking Delete', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create an agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)
    const uniqueName = `Delete-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Delete Tester')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Handle confirmation dialog
    page.on('dialog', dialog => dialog.accept())

    // Click Delete
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await agentCard.getByRole('button', { name: /Delete/i }).click()
    await page.waitForTimeout(1000)

    // Agent should no longer appear
    await expect(page.getByText(uniqueName)).not.toBeVisible()
  })
})

// =============================================================================
// AI AGENTS - MODEL SELECTION & CONFIGURATION
// =============================================================================

test.describe('AI Agents - Model Configuration', () => {
  test('should create agent with Claude Sonnet model', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const uniqueName = `Sonnet-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Sonnet User')
    await page.getByLabel(/AI Model/i).selectOption('claude-sonnet-4-20250514')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await expect(agentCard.getByText(/Claude Sonnet 4/i)).toBeVisible()
  })

  test('should create agent with Claude Opus model', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const uniqueName = `Opus-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Opus User')
    await page.getByLabel(/AI Model/i).selectOption('claude-opus-4-20250514')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await expect(agentCard.getByText(/Claude Opus 4/i)).toBeVisible()
  })

  test('should create agent with Claude Haiku model', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const uniqueName = `Haiku-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Haiku User')
    await page.getByLabel(/AI Model/i).selectOption('claude-3-5-haiku-20241022')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await expect(agentCard.getByText(/Claude 3.5 Haiku/i)).toBeVisible()
  })

  test('should configure custom max tokens', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const uniqueName = `Tokens-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Token Tester')
    await page.getByLabel(/Max Tokens/i).clear()
    await page.getByLabel(/Max Tokens/i).fill('8192')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Verify in card
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await expect(agentCard.getByText(/8,192 max tokens/i)).toBeVisible()
  })

  test('should configure custom temperature', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/Agent Name/i).fill('Temp-Test')
    await page.getByLabel(/Role Title/i).fill('Temp Tester')
    await page.getByLabel(/Temperature/i).clear()
    await page.getByLabel(/Temperature/i).fill('1.5')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Open edit to verify temperature was saved
    const agentCard = page.locator('.orbitos-card').filter({ hasText: 'Temp-Test' })
    await agentCard.getByRole('button', { name: /Edit/i }).click()
    await page.waitForTimeout(300)

    const tempField = page.getByLabel(/Temperature/i)
    await expect(tempField).toHaveValue('1.5')
  })
})

// =============================================================================
// AI AGENTS - AVATAR COLOR SELECTION
// =============================================================================

test.describe('AI Agents - Avatar Colors', () => {
  test('should display avatar color options', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Should have color buttons
    const colorButtons = page.locator('button[style*="background-color"]')
    const count = await colorButtons.count()
    expect(count).toBeGreaterThanOrEqual(5)
  })

  test('should select different avatar color', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Click a color button (emerald green)
    await page.locator('button[style*="#10B981"]').click()

    // Fill form and create
    const uniqueName = `Color-Test-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Color Tester')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Agent card should have the selected color
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    const avatar = agentCard.locator('div[style*="#10B981"]')
    await expect(avatar).toBeVisible()
  })
})

// =============================================================================
// AI AGENTS - SYSTEM PROMPT TEMPLATES
// =============================================================================

test.describe('AI Agents - System Prompt Templates', () => {
  test('should load CFO template', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('cfo')
    await page.waitForTimeout(100)

    const promptArea = page.getByLabel(/System Prompt \*/i)
    const promptText = await promptArea.inputValue()
    expect(promptText).toContain('CFO')
    expect(promptText).toContain('financial')
  })

  test('should load Operations template', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('operations')
    await page.waitForTimeout(100)

    const promptArea = page.getByLabel(/System Prompt \*/i)
    const promptText = await promptArea.inputValue()
    expect(promptText).toContain('Operations')
    expect(promptText).toContain('process')
  })

  test('should load Strategy template', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('strategy')
    await page.waitForTimeout(100)

    const promptArea = page.getByLabel(/System Prompt \*/i)
    const promptText = await promptArea.inputValue()
    expect(promptText).toContain('Strategy')
    expect(promptText).toContain('strategic')
  })

  test('should load HR template', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await page.getByLabel(/System Prompt Template/i).selectOption('hr')
    await page.waitForTimeout(100)

    const promptArea = page.getByLabel(/System Prompt \*/i)
    const promptText = await promptArea.inputValue()
    expect(promptText).toContain('HR')
    expect(promptText).toContain('talent')
  })

  test('should allow custom system prompt editing', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    const customPrompt = 'This is a custom system prompt for testing.'
    await page.getByLabel(/System Prompt \*/i).clear()
    await page.getByLabel(/System Prompt \*/i).fill(customPrompt)

    const promptArea = page.getByLabel(/System Prompt \*/i)
    await expect(promptArea).toHaveValue(customPrompt)
  })
})

// =============================================================================
// AI AGENTS - STATS DISPLAY
// =============================================================================

test.describe('AI Agents - Stats Cards', () => {
  test('should update Total Agents count after creating agent', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Get initial count
    const totalCard = page.locator('.orbitos-card-static').filter({ hasText: 'Total Agents' })
    const initialCountText = await totalCard.locator('.text-2xl').textContent()
    const initialCount = parseInt(initialCountText || '0')

    // Create agent
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)
    await page.getByLabel(/Agent Name/i).fill(`Stats-Test-${Date.now()}`)
    await page.getByLabel(/Role Title/i).fill('Stats Tester')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Check count increased
    const newCountText = await totalCard.locator('.text-2xl').textContent()
    const newCount = parseInt(newCountText || '0')
    expect(newCount).toBe(initialCount + 1)
  })

  test('should update Active count when toggling agent', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Create an active agent first
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)
    const uniqueName = `Active-Stats-${Date.now()}`
    await page.getByLabel(/Agent Name/i).fill(uniqueName)
    await page.getByLabel(/Role Title/i).fill('Active Tester')
    await page.getByRole('button', { name: /Create Agent/i }).click()
    await page.waitForTimeout(1000)

    // Get initial active count
    const activeCard = page.locator('.orbitos-card-static').filter({ hasText: /^Active$/ })
    const initialActiveText = await activeCard.locator('.text-2xl').textContent()
    const initialActive = parseInt(initialActiveText || '0')

    // Disable the agent
    const agentCard = page.locator('.orbitos-card').filter({ hasText: uniqueName })
    await agentCard.getByRole('button', { name: /Disable/i }).click()
    await page.waitForTimeout(500)

    // Check active count decreased
    const newActiveText = await activeCard.locator('.text-2xl').textContent()
    const newActive = parseInt(newActiveText || '0')
    expect(newActive).toBe(initialActive - 1)
  })
})
