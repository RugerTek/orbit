/**
 * =============================================================================
 * OrbitOS Operations - AI Chat Tools E2E Tests
 * =============================================================================
 * Tests for AI agents' ability to manage other AI agents and conversations
 * through the AI Chat Service tools. This enables autonomous setup of
 * company agents and communication channels.
 *
 * Spec: F003 - Multi-Agent AI Chat (AI Tools subset)
 * =============================================================================
 */

import { test, expect, Page } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}`

// Test data for AI agents with full personality configuration
const TEST_AGENT_FULL = {
  name: 'E2E-Test-CFO',
  roleTitle: 'Chief Financial Officer',
  provider: 'Anthropic',
  modelId: 'claude-sonnet-4-20250514',
  modelDisplayName: 'Claude Sonnet',
  systemPrompt: 'You are the CFO AI assistant. Provide financial analysis and budget recommendations.',
  avatarColor: '#F59E0B',
  maxTokensPerResponse: 4096,
  temperature: 0.7,
  assertiveness: 75,
  communicationStyle: 'Formal',
  reactionTendency: 'Balanced',
  expertiseAreas: 'finance,budget,revenue,costs,investment',
  seniorityLevel: 5,
  asksQuestions: true,
  givesBriefAcknowledgments: true,
  isActive: true
}

const TEST_AGENT_MINIMAL = {
  name: 'E2E-Test-Assistant',
  roleTitle: 'General Assistant',
  provider: 'Anthropic',
  modelId: 'claude-sonnet-4-20250514',
  modelDisplayName: 'Claude Sonnet',
  systemPrompt: 'You are a helpful assistant.',
  isActive: true
}

// Helper to generate unique names for tests
function uniqueName(base: string): string {
  return `${base}-${Date.now()}-${Math.random().toString(36).substring(7)}`
}

// =============================================================================
// AI AGENTS API - FULL CRUD WITH ALL PERSONALITY FIELDS
// =============================================================================

test.describe('AI Agents API - Full CRUD Operations', () => {
  let createdAgentIds: string[] = []

  test.afterAll(async ({ request }) => {
    // Cleanup all created agents
    for (const id of createdAgentIds) {
      try {
        await request.delete(`${API_BASE}/ai-agents/${id}`)
      } catch (e) {
        // Ignore cleanup errors
      }
    }
  })

  test('should create AI agent with all personality fields', async ({ request }) => {
    const agentData = {
      ...TEST_AGENT_FULL,
      name: uniqueName('CFO-Full')
    }

    const response = await request.post(`${API_BASE}/ai-agents`, { data: agentData })
    expect(response.status()).toBe(201)

    const agent = await response.json()
    createdAgentIds.push(agent.id)

    // Verify all fields were saved correctly
    expect(agent.name).toBe(agentData.name)
    expect(agent.roleTitle).toBe(agentData.roleTitle)
    expect(agent.provider).toBe('Anthropic')
    expect(agent.modelId).toBe(agentData.modelId)
    expect(agent.systemPrompt).toBe(agentData.systemPrompt)
    expect(agent.avatarColor).toBe(agentData.avatarColor)
    expect(agent.maxTokensPerResponse).toBe(agentData.maxTokensPerResponse)
    expect(agent.assertiveness).toBe(agentData.assertiveness)
    expect(agent.communicationStyle).toBe('Formal')
    expect(agent.reactionTendency).toBe('Balanced')
    expect(agent.expertiseAreas).toBe(agentData.expertiseAreas)
    expect(agent.seniorityLevel).toBe(agentData.seniorityLevel)
    expect(agent.asksQuestions).toBe(agentData.asksQuestions)
    expect(agent.givesBriefAcknowledgments).toBe(agentData.givesBriefAcknowledgments)
    expect(agent.isActive).toBe(true)
  })

  test('should create AI agent with minimal required fields', async ({ request }) => {
    const agentData = {
      ...TEST_AGENT_MINIMAL,
      name: uniqueName('Assistant-Min')
    }

    const response = await request.post(`${API_BASE}/ai-agents`, { data: agentData })
    expect(response.status()).toBe(201)

    const agent = await response.json()
    createdAgentIds.push(agent.id)

    expect(agent.name).toBe(agentData.name)
    expect(agent.roleTitle).toBe(agentData.roleTitle)
    // Defaults should be applied
    expect(agent.assertiveness).toBe(50) // Default
    expect(agent.seniorityLevel).toBe(3) // Default
  })

  test('should update AI agent personality fields', async ({ request }) => {
    // Create agent first
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: { ...TEST_AGENT_MINIMAL, name: uniqueName('Update-Test') }
    })
    const created = await createResponse.json()
    createdAgentIds.push(created.id)

    // Update personality fields
    const updateResponse = await request.put(`${API_BASE}/ai-agents/${created.id}`, {
      data: {
        assertiveness: 90,
        communicationStyle: 'Direct',
        reactionTendency: 'DevilsAdvocate',
        expertiseAreas: 'strategy,competition',
        seniorityLevel: 4,
        asksQuestions: false
      }
    })
    expect(updateResponse.status()).toBe(200)

    const updated = await updateResponse.json()
    expect(updated.assertiveness).toBe(90)
    expect(updated.communicationStyle).toBe('Direct')
    expect(updated.reactionTendency).toBe('DevilsAdvocate')
    expect(updated.expertiseAreas).toBe('strategy,competition')
    expect(updated.seniorityLevel).toBe(4)
    expect(updated.asksQuestions).toBe(false)
  })

  test('should update AI agent system prompt', async ({ request }) => {
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: { ...TEST_AGENT_MINIMAL, name: uniqueName('Prompt-Test') }
    })
    const created = await createResponse.json()
    createdAgentIds.push(created.id)

    const newPrompt = 'Updated: You are now a specialized financial analyst focusing on risk assessment.'

    const updateResponse = await request.put(`${API_BASE}/ai-agents/${created.id}`, {
      data: { systemPrompt: newPrompt }
    })
    expect(updateResponse.status()).toBe(200)

    const updated = await updateResponse.json()
    expect(updated.systemPrompt).toBe(newPrompt)
  })

  test('should list all AI agents', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents`)
    expect(response.status()).toBe(200)

    const agents = await response.json()
    expect(Array.isArray(agents)).toBe(true)
  })

  test('should get AI agent by ID', async ({ request }) => {
    // Create agent first
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: { ...TEST_AGENT_MINIMAL, name: uniqueName('Get-Test') }
    })
    const created = await createResponse.json()
    createdAgentIds.push(created.id)

    // Get by ID
    const getResponse = await request.get(`${API_BASE}/ai-agents/${created.id}`)
    expect(getResponse.status()).toBe(200)

    const agent = await getResponse.json()
    expect(agent.id).toBe(created.id)
    expect(agent.name).toBe(created.name)
  })

  test('should soft delete AI agent', async ({ request }) => {
    // Create agent first
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: { ...TEST_AGENT_MINIMAL, name: uniqueName('Delete-Test') }
    })
    const created = await createResponse.json()
    // Don't add to cleanup list since we're deleting it

    // Delete
    const deleteResponse = await request.delete(`${API_BASE}/ai-agents/${created.id}`)
    expect(deleteResponse.status()).toBe(204)

    // Verify not in list
    const listResponse = await request.get(`${API_BASE}/ai-agents`)
    const agents = await listResponse.json()
    const found = agents.find((a: any) => a.id === created.id)
    expect(found).toBeUndefined()
  })

  test('should return 404 for non-existent agent', async ({ request }) => {
    const fakeId = '00000000-0000-0000-0000-000000000000'
    const response = await request.get(`${API_BASE}/ai-agents/${fakeId}`)
    expect(response.status()).toBe(404)
  })
})

// =============================================================================
// CONVERSATIONS API - FULL CRUD WITH PARTICIPANTS
// =============================================================================

test.describe('Conversations API - Full CRUD Operations', () => {
  let testAgentId: string
  let testAgentId2: string
  let createdConversationIds: string[] = []

  test.beforeAll(async ({ request }) => {
    // Create test agents
    const agent1Response = await request.post(`${API_BASE}/ai-agents`, {
      data: { ...TEST_AGENT_MINIMAL, name: uniqueName('ConvoTest-Agent1') }
    })
    testAgentId = (await agent1Response.json()).id

    const agent2Response = await request.post(`${API_BASE}/ai-agents`, {
      data: { ...TEST_AGENT_MINIMAL, name: uniqueName('ConvoTest-Agent2') }
    })
    testAgentId2 = (await agent2Response.json()).id
  })

  test.afterAll(async ({ request }) => {
    // Cleanup conversations
    for (const id of createdConversationIds) {
      try {
        await request.delete(`${API_BASE}/conversations/${id}`)
      } catch (e) {
        // Ignore cleanup errors
      }
    }
    // Cleanup agents
    if (testAgentId) await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    if (testAgentId2) await request.delete(`${API_BASE}/ai-agents/${testAgentId2}`)
  })

  test('should create conversation with title and mode', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: uniqueName('Strategy Discussion'),
        mode: 'OnDemand'
      }
    })
    expect(response.status()).toBe(201)

    const conversation = await response.json()
    createdConversationIds.push(conversation.id)

    expect(conversation.title).toContain('Strategy Discussion')
    expect(conversation.mode).toBe('OnDemand')
    expect(conversation.status).toBe('Active')
  })

  test('should create conversation with AI agent participants', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: uniqueName('Team Meeting'),
        mode: 'RoundRobin',
        aiAgentIds: [testAgentId]
      }
    })
    expect(response.status()).toBe(201)

    const conversation = await response.json()
    createdConversationIds.push(conversation.id)

    expect(conversation.mode).toBe('RoundRobin')
    // Should have at least 1 AI participant
    expect(conversation.participants.some((p: any) => p.aiAgentId === testAgentId)).toBe(true)
  })

  test('should create conversation with multiple agents', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: uniqueName('Executive Review'),
        mode: 'Emergent',
        aiAgentIds: [testAgentId, testAgentId2]
      }
    })
    expect(response.status()).toBe(201)

    const conversation = await response.json()
    createdConversationIds.push(conversation.id)

    expect(conversation.mode).toBe('Emergent')
    const aiParticipants = conversation.participants.filter((p: any) => p.aiAgentId)
    expect(aiParticipants.length).toBeGreaterThanOrEqual(2)
  })

  test('should update conversation title and mode', async ({ request }) => {
    // Create conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: { title: uniqueName('Original Title'), mode: 'OnDemand' }
    })
    const created = await createResponse.json()
    createdConversationIds.push(created.id)

    // Update
    const updateResponse = await request.put(`${API_BASE}/conversations/${created.id}`, {
      data: {
        title: 'Updated: Quarterly Review',
        mode: 'Free'
      }
    })
    expect(updateResponse.status()).toBe(200)

    const updated = await updateResponse.json()
    expect(updated.title).toBe('Updated: Quarterly Review')
    expect(updated.mode).toBe('Free')
  })

  test('should add AI agent to existing conversation', async ({ request }) => {
    // Create conversation without agents
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: { title: uniqueName('Add Agent Test'), mode: 'OnDemand' }
    })
    const created = await createResponse.json()
    createdConversationIds.push(created.id)

    // Add agent
    const addResponse = await request.post(`${API_BASE}/conversations/${created.id}/participants`, {
      data: { aiAgentId: testAgentId }
    })
    expect(addResponse.status()).toBe(200)

    // Verify agent is in participants
    const getResponse = await request.get(`${API_BASE}/conversations/${created.id}`)
    const conversation = await getResponse.json()
    expect(conversation.participants.some((p: any) => p.aiAgentId === testAgentId)).toBe(true)
  })

  test('should remove AI agent from conversation', async ({ request }) => {
    // Create conversation with agent
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: uniqueName('Remove Agent Test'),
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    const created = await createResponse.json()
    createdConversationIds.push(created.id)

    // Find the participant ID
    const participantToRemove = created.participants.find((p: any) => p.aiAgentId === testAgentId)
    expect(participantToRemove).toBeDefined()

    // Remove agent
    const removeResponse = await request.delete(
      `${API_BASE}/conversations/${created.id}/participants/${participantToRemove.id}`
    )
    expect(removeResponse.status()).toBe(204)
  })

  test('should list all conversations', async ({ request }) => {
    const response = await request.get(`${API_BASE}/conversations`)
    expect(response.status()).toBe(200)

    const conversations = await response.json()
    expect(Array.isArray(conversations)).toBe(true)
  })

  test('should get conversation by ID', async ({ request }) => {
    // Create conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: { title: uniqueName('Get Test'), mode: 'OnDemand' }
    })
    const created = await createResponse.json()
    createdConversationIds.push(created.id)

    // Get by ID
    const getResponse = await request.get(`${API_BASE}/conversations/${created.id}`)
    expect(getResponse.status()).toBe(200)

    const conversation = await getResponse.json()
    expect(conversation.id).toBe(created.id)
  })

  test('should soft delete conversation', async ({ request }) => {
    // Create conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: { title: uniqueName('Delete Test'), mode: 'OnDemand' }
    })
    const created = await createResponse.json()
    // Don't add to cleanup since we're deleting

    // Delete
    const deleteResponse = await request.delete(`${API_BASE}/conversations/${created.id}`)
    expect(deleteResponse.status()).toBe(204)

    // Verify not in list
    const listResponse = await request.get(`${API_BASE}/conversations`)
    const conversations = await listResponse.json()
    const found = conversations.find((c: any) => c.id === created.id)
    expect(found).toBeUndefined()
  })

  test('should support all conversation modes', async ({ request }) => {
    const modes = ['OnDemand', 'Moderated', 'RoundRobin', 'Free', 'Emergent']

    for (const mode of modes) {
      const response = await request.post(`${API_BASE}/conversations`, {
        data: { title: uniqueName(`Mode-${mode}`), mode }
      })
      expect(response.status()).toBe(201)

      const conversation = await response.json()
      createdConversationIds.push(conversation.id)
      expect(conversation.mode).toBe(mode)
    }
  })
})

// =============================================================================
// AI AGENTS UI - CREATE DIALOG WITH ALL FIELDS
// =============================================================================

test.describe('AI Agents UI - Create Dialog', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')
  })

  test('should open create dialog and show all personality fields', async ({ page }) => {
    // Click Add Agent button
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Verify dialog is open
    await expect(page.getByRole('heading', { name: /create.*agent/i })).toBeVisible()

    // Verify required fields exist
    await expect(page.getByLabel(/agent name/i)).toBeVisible()
    await expect(page.getByLabel(/role.*title/i)).toBeVisible()
    await expect(page.getByLabel(/system prompt/i)).toBeVisible()

    // Verify personality fields exist
    await expect(page.getByText(/assertiveness/i)).toBeVisible()
    await expect(page.getByText(/communication style/i)).toBeVisible()
  })

  test('should create agent with custom personality via UI', async ({ page }) => {
    const testName = uniqueName('UI-Custom-Agent')

    // Open dialog
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Fill required fields
    await page.getByLabel(/agent name/i).fill(testName)
    await page.getByLabel(/role.*title/i).fill('Strategy Consultant')
    await page.getByLabel(/system prompt/i).fill('You are a strategic advisor with expertise in market analysis.')

    // Try to set personality fields if available
    const assertivenessSlider = page.locator('input[type="range"]').first()
    if (await assertivenessSlider.isVisible()) {
      await assertivenessSlider.fill('80')
    }

    // Submit
    await page.getByRole('button', { name: /create agent/i }).click()
    await page.waitForTimeout(1000)

    // Verify agent appears in list
    await expect(page.getByText(testName)).toBeVisible()

    // Cleanup: Delete the agent
    const agentCard = page.locator('.orbitos-card', { hasText: testName })
    const deleteButton = agentCard.getByRole('button', { name: /delete/i })
    if (await deleteButton.isVisible()) {
      await deleteButton.click()
      await page.getByRole('button', { name: /confirm|delete/i }).click()
    }
  })

  test('should validate required fields', async ({ page }) => {
    // Open dialog
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    // Try to submit without filling fields
    const createButton = page.getByRole('button', { name: /create agent/i })
    await expect(createButton).toBeDisabled()

    // Fill only name
    await page.getByLabel(/agent name/i).fill('Test Agent')
    await expect(createButton).toBeDisabled()

    // Fill role title
    await page.getByLabel(/role.*title/i).fill('Test Role')
    await expect(createButton).toBeDisabled()

    // Fill system prompt
    await page.getByLabel(/system prompt/i).fill('Test prompt')

    // Now button should be enabled
    await expect(createButton).toBeEnabled()
  })

  test('should close dialog on escape key', async ({ page }) => {
    await page.getByRole('button', { name: /add agent/i }).click()
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: /create.*agent/i })).toBeVisible()

    await page.keyboard.press('Escape')
    await page.waitForTimeout(300)

    await expect(page.getByRole('heading', { name: /create.*agent/i })).not.toBeVisible()
  })
})

// =============================================================================
// AI AGENTS UI - EDIT DIALOG WITH PERSONALITY FIELDS
// =============================================================================

test.describe('AI Agents UI - Edit Dialog', () => {
  let testAgentId: string

  test.beforeAll(async ({ request }) => {
    // Create a test agent to edit
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        ...TEST_AGENT_FULL,
        name: uniqueName('Edit-UI-Test')
      }
    })
    testAgentId = (await response.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should open edit dialog with existing values', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Find the agent card and click edit
    const agentCard = page.locator('.orbitos-card', { hasText: 'Edit-UI-Test' })
    await agentCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    // Verify edit dialog shows existing values
    await expect(page.getByRole('heading', { name: /edit.*agent/i })).toBeVisible()

    // Check that name field has the existing value
    const nameField = page.getByLabel(/agent name/i)
    await expect(nameField).toHaveValue(/Edit-UI-Test/)
  })

  test('should update agent and save changes', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Find and edit agent
    const agentCard = page.locator('.orbitos-card', { hasText: 'Edit-UI-Test' })
    await agentCard.getByRole('button', { name: /edit/i }).click()
    await page.waitForTimeout(300)

    // Update the role title
    const roleField = page.getByLabel(/role.*title/i)
    await roleField.clear()
    await roleField.fill('Updated Role Title')

    // Save
    await page.getByRole('button', { name: /save|update/i }).click()
    await page.waitForTimeout(1000)

    // Verify update is reflected
    await expect(page.getByText('Updated Role Title')).toBeVisible()
  })
})

// =============================================================================
// CONVERSATIONS UI - CREATE WITH AGENTS
// =============================================================================

test.describe('Conversations UI - Create with Agents', () => {
  let testAgentId: string

  test.beforeAll(async ({ request }) => {
    // Create a test agent
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        ...TEST_AGENT_MINIMAL,
        name: uniqueName('ConvoUI-Agent')
      }
    })
    testAgentId = (await response.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should create conversation from AI Agents page', async ({ page }) => {
    await page.goto('/app/ai-agents')
    await page.waitForLoadState('networkidle')

    // Click on Conversations tab or link
    await page.getByRole('link', { name: /conversations/i }).click()
    await page.waitForLoadState('networkidle')

    // Click create conversation button
    await page.getByRole('button', { name: /new conversation|create conversation/i }).click()
    await page.waitForTimeout(300)

    // Fill in conversation details
    await page.getByLabel(/title/i).fill(uniqueName('UI Test Conversation'))

    // Select mode if dropdown exists
    const modeSelect = page.locator('select').filter({ hasText: /mode/i })
    if (await modeSelect.isVisible()) {
      await modeSelect.selectOption('RoundRobin')
    }

    // Submit
    await page.getByRole('button', { name: /create/i }).click()
    await page.waitForTimeout(1000)

    // Verify we're on the conversation page or it appears in list
    await expect(page.url()).toContain('conversation')
  })
})

// =============================================================================
// INTEGRATION: AI ASSISTANT MANAGING AGENTS (via API)
// =============================================================================

test.describe('Integration - AI Managing Agents via Chat API', () => {
  test.skip('should allow AI to create agent through chat', async ({ request }) => {
    // This test is skipped because it requires actual AI API keys
    // In production testing, this would verify the AI chat endpoint
    // can use the create_ai_agent tool

    const chatRequest = {
      message: 'Create a new AI agent named "Marketing Expert" with role "CMO" and expertise in digital marketing.'
    }

    const response = await request.post(`${API_BASE}/ai/chat`, {
      data: chatRequest
    })

    // Would verify the response includes tool call results
    expect(response.status()).toBe(200)
  })

  test.skip('should allow AI to create conversation through chat', async ({ request }) => {
    // This test is skipped because it requires actual AI API keys

    const chatRequest = {
      message: 'Create a new group chat called "Weekly Sync" for our executive team.'
    }

    const response = await request.post(`${API_BASE}/ai/chat`, {
      data: chatRequest
    })

    expect(response.status()).toBe(200)
  })
})
