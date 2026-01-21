/**
 * =============================================================================
 * OrbitOS Operations - AI Conversations E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for the Multi-Agent AI Chat feature.
 * Tests cover conversation CRUD, messaging, AI invocation, and chat UI.
 *
 * Spec: F003 - Multi-Agent AI Chat (Conversations subset)
 * =============================================================================
 */

import { test, expect, Page } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}`

// Test data
const TEST_CONVERSATION = {
  title: 'Financial Planning Discussion',
  mode: 'OnDemand'
}

// Helper function to create an AI agent via API
async function createTestAgent(page: Page, name: string): Promise<string> {
  const response = await page.request.post(`${API_BASE}/ai-agents`, {
    data: {
      name,
      roleTitle: 'Test Assistant',
      provider: 'anthropic',
      modelId: 'claude-sonnet-4-20250514',
      modelDisplayName: 'Claude Sonnet 4',
      systemPrompt: 'You are a helpful test assistant.',
      maxTokensPerResponse: 1024,
      temperature: 0.7,
      isActive: true
    }
  })
  const agent = await response.json()
  return agent.id
}

// Helper function to delete an AI agent via API
async function deleteTestAgent(page: Page, agentId: string): Promise<void> {
  await page.request.delete(`${API_BASE}/ai-agents/${agentId}`)
}

// Helper function to create a conversation via API
async function createTestConversation(page: Page, title: string, agentIds: string[]): Promise<string> {
  const response = await page.request.post(`${API_BASE}/conversations`, {
    data: {
      title,
      mode: 'OnDemand',
      aiAgentIds: agentIds
    }
  })
  const conversation = await response.json()
  return conversation.id
}

// Helper function to delete a conversation via API
async function deleteTestConversation(page: Page, conversationId: string): Promise<void> {
  await page.request.delete(`${API_BASE}/conversations/${conversationId}`)
}

// =============================================================================
// CONVERSATIONS API - CRUD OPERATIONS
// =============================================================================

test.describe('Conversations API - CRUD Operations', () => {
  let testAgentId: string

  test.beforeAll(async ({ request }) => {
    // Create a test agent to use in conversations
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'ConvoTest-Agent',
        roleTitle: 'Test Assistant',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful test assistant.',
        maxTokensPerResponse: 1024,
        temperature: 0.7,
        isActive: true
      }
    })
    const agent = await response.json()
    testAgentId = agent.id
  })

  test.afterAll(async ({ request }) => {
    // Cleanup test agent
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should create a new conversation', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'API Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })

    expect(response.status()).toBe(200)
    const conversation = await response.json()

    expect(conversation.id).toBeDefined()
    expect(conversation.title).toBe('API Test Conversation')
    expect(conversation.mode).toBe('OnDemand')
    expect(conversation.status).toBe('Active')
    expect(conversation.messageCount).toBe(0)
    expect(conversation.participants.length).toBeGreaterThanOrEqual(1)

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${conversation.id}`)
  })

  test('should get conversation by ID', async ({ request }) => {
    // Create conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Get Test Conversation',
        mode: 'Moderated',
        aiAgentIds: [testAgentId]
      }
    })
    const created = await createResponse.json()

    // Get conversation
    const getResponse = await request.get(`${API_BASE}/conversations/${created.id}`)
    expect(getResponse.status()).toBe(200)
    const conversation = await getResponse.json()

    expect(conversation.id).toBe(created.id)
    expect(conversation.title).toBe('Get Test Conversation')
    expect(conversation.mode).toBe('Moderated')
    expect(conversation.participants).toBeDefined()

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${created.id}`)
  })

  test('should list all conversations', async ({ request }) => {
    // Create a conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'List Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    const created = await createResponse.json()

    // List conversations
    const listResponse = await request.get(`${API_BASE}/conversations`)
    expect(listResponse.status()).toBe(200)
    const conversations = await listResponse.json()

    expect(Array.isArray(conversations)).toBeTruthy()
    expect(conversations.length).toBeGreaterThanOrEqual(1)

    const found = conversations.find((c: any) => c.id === created.id)
    expect(found).toBeDefined()
    expect(found.title).toBe('List Test Conversation')

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${created.id}`)
  })

  test('should update conversation settings', async ({ request }) => {
    // Create conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Update Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    const created = await createResponse.json()

    // Update settings
    const updateResponse = await request.put(`${API_BASE}/conversations/${created.id}/settings`, {
      data: {
        title: 'Updated Title',
        mode: 'RoundRobin',
        maxTurns: 10
      }
    })

    expect(updateResponse.status()).toBe(200)
    const updated = await updateResponse.json()
    expect(updated.title).toBe('Updated Title')
    expect(updated.mode).toBe('RoundRobin')

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${created.id}`)
  })

  test('should pause and resume conversation', async ({ request }) => {
    // Create conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Pause Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    const created = await createResponse.json()
    expect(created.status).toBe('Active')

    // Pause conversation
    const pauseResponse = await request.post(`${API_BASE}/conversations/${created.id}/pause`)
    expect(pauseResponse.status()).toBe(200)
    const paused = await pauseResponse.json()
    expect(paused.status).toBe('Paused')

    // Resume conversation
    const resumeResponse = await request.post(`${API_BASE}/conversations/${created.id}/resume`)
    expect(resumeResponse.status()).toBe(200)
    const resumed = await resumeResponse.json()
    expect(resumed.status).toBe('Active')

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${created.id}`)
  })

  test('should delete conversation', async ({ request }) => {
    // Create conversation
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Delete Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    const created = await createResponse.json()

    // Delete conversation
    const deleteResponse = await request.delete(`${API_BASE}/conversations/${created.id}`)
    expect(deleteResponse.status()).toBe(204)

    // Verify deleted
    const getResponse = await request.get(`${API_BASE}/conversations/${created.id}`)
    expect(getResponse.status()).toBe(404)
  })
})

// =============================================================================
// CONVERSATIONS API - MESSAGING
// =============================================================================

test.describe('Conversations API - Messaging', () => {
  let testAgentId: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    // Create a test agent
    const agentResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'MessageTest-Agent',
        roleTitle: 'Message Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful test assistant. Give brief responses.',
        maxTokensPerResponse: 256,
        temperature: 0.7,
        isActive: true
      }
    })
    const agent = await agentResponse.json()
    testAgentId = agent.id
  })

  test.beforeEach(async ({ request }) => {
    // Create a fresh conversation for each test
    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Message Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    const conversation = await convoResponse.json()
    testConversationId = conversation.id
  })

  test.afterEach(async ({ request }) => {
    // Cleanup conversation after each test
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
  })

  test.afterAll(async ({ request }) => {
    // Cleanup test agent
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should send a user message', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations/${testConversationId}/messages`, {
      data: {
        content: 'Hello, this is a test message!'
      }
    })

    expect(response.status()).toBe(200)
    const message = await response.json()

    expect(message.id).toBeDefined()
    expect(message.content).toBe('Hello, this is a test message!')
    expect(message.senderType).toBe('user')
    expect(message.status).toBe('sent')
    expect(message.sequenceNumber).toBeGreaterThanOrEqual(1)
  })

  test('should get messages with pagination', async ({ request }) => {
    // Send multiple messages
    for (let i = 1; i <= 5; i++) {
      await request.post(`${API_BASE}/conversations/${testConversationId}/messages`, {
        data: { content: `Test message ${i}` }
      })
    }

    // Get messages
    const response = await request.get(`${API_BASE}/conversations/${testConversationId}/messages`)
    expect(response.status()).toBe(200)

    const result = await response.json()
    expect(result.messages).toBeDefined()
    expect(result.messages.length).toBe(5)
    expect(result.hasMore).toBeDefined()
  })

  test('should update conversation stats after sending message', async ({ request }) => {
    // Send a message
    await request.post(`${API_BASE}/conversations/${testConversationId}/messages`, {
      data: { content: 'Test message for stats' }
    })

    // Check conversation stats
    const convoResponse = await request.get(`${API_BASE}/conversations/${testConversationId}`)
    const conversation = await convoResponse.json()

    expect(conversation.messageCount).toBe(1)
    expect(conversation.lastMessageAt).toBeDefined()
  })

  // Note: This test requires actual API keys to be configured
  test.skip('should invoke AI agent and receive response', async ({ request }) => {
    // Send initial message
    await request.post(`${API_BASE}/conversations/${testConversationId}/messages`, {
      data: { content: 'What is 2+2?' }
    })

    // Invoke AI agent
    const invokeResponse = await request.post(`${API_BASE}/conversations/${testConversationId}/invoke`, {
      data: { agentIds: [testAgentId] }
    })

    expect(invokeResponse.status()).toBe(200)
    const responses = await invokeResponse.json()

    expect(Array.isArray(responses)).toBeTruthy()
    expect(responses.length).toBeGreaterThanOrEqual(1)

    const aiMessage = responses[0]
    expect(aiMessage.senderType).toBe('ai')
    expect(aiMessage.content).toBeDefined()
    expect(aiMessage.tokensUsed).toBeGreaterThan(0)
    expect(aiMessage.responseTimeMs).toBeGreaterThan(0)
  })

  test('should fail to invoke AI on empty conversation', async ({ request }) => {
    // Try to invoke without sending a message first
    const invokeResponse = await request.post(`${API_BASE}/conversations/${testConversationId}/invoke`, {
      data: {}
    })

    expect(invokeResponse.status()).toBe(400)
  })
})

// =============================================================================
// CONVERSATIONS API - PARTICIPANTS
// =============================================================================

test.describe('Conversations API - Participants', () => {
  let testAgentId1: string
  let testAgentId2: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    // Create two test agents
    const agent1Response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'Participant-Agent-1',
        roleTitle: 'First Agent',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are the first test agent.',
        isActive: true
      }
    })
    testAgentId1 = (await agent1Response.json()).id

    const agent2Response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'Participant-Agent-2',
        roleTitle: 'Second Agent',
        provider: 'anthropic',
        modelId: 'claude-3-5-haiku-20241022',
        modelDisplayName: 'Claude 3.5 Haiku',
        systemPrompt: 'You are the second test agent.',
        isActive: true
      }
    })
    testAgentId2 = (await agent2Response.json()).id
  })

  test.beforeEach(async ({ request }) => {
    // Create conversation with first agent
    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Participant Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId1]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterEach(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
  })

  test.afterAll(async ({ request }) => {
    await request.delete(`${API_BASE}/ai-agents/${testAgentId1}`)
    await request.delete(`${API_BASE}/ai-agents/${testAgentId2}`)
  })

  test('should add AI agent participant to conversation', async ({ request }) => {
    const addResponse = await request.post(`${API_BASE}/conversations/${testConversationId}/participants`, {
      data: { aiAgentId: testAgentId2 }
    })

    expect(addResponse.status()).toBe(200)
    const participant = await addResponse.json()

    expect(participant.id).toBeDefined()
    expect(participant.participantType).toBe('ai')
    expect(participant.aiAgent).toBeDefined()
    expect(participant.aiAgent.name).toBe('Participant-Agent-2')
  })

  test('should list participants in conversation', async ({ request }) => {
    // Add second agent
    await request.post(`${API_BASE}/conversations/${testConversationId}/participants`, {
      data: { aiAgentId: testAgentId2 }
    })

    // Get conversation with participants
    const convoResponse = await request.get(`${API_BASE}/conversations/${testConversationId}`)
    const conversation = await convoResponse.json()

    expect(conversation.participants.length).toBeGreaterThanOrEqual(2)

    const aiParticipants = conversation.participants.filter((p: any) => p.participantType === 'ai')
    expect(aiParticipants.length).toBe(2)
  })

  test('should remove participant from conversation', async ({ request }) => {
    // Add second agent
    const addResponse = await request.post(`${API_BASE}/conversations/${testConversationId}/participants`, {
      data: { aiAgentId: testAgentId2 }
    })
    const participant = await addResponse.json()

    // Remove participant
    const removeResponse = await request.delete(
      `${API_BASE}/conversations/${testConversationId}/participants/${participant.id}`
    )
    expect(removeResponse.status()).toBe(204)

    // Verify removed
    const convoResponse = await request.get(`${API_BASE}/conversations/${testConversationId}`)
    const conversation = await convoResponse.json()

    const aiParticipants = conversation.participants.filter((p: any) => p.participantType === 'ai')
    expect(aiParticipants.length).toBe(1)
  })
})

// =============================================================================
// CHAT UI - NAVIGATION & LAYOUT
// =============================================================================

test.describe('Chat UI - Navigation & Layout', () => {
  let testAgentId: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    // Create test agent
    const agentResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'ChatUI-Agent',
        roleTitle: 'Chat Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful assistant.',
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })
    testAgentId = (await agentResponse.json()).id

    // Create test conversation
    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Chat UI Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should display conversation title in header', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('Chat UI Test Conversation')).toBeVisible()
  })

  test('should display back navigation to AI Agents page', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const backLink = page.locator('a[href="/app/ai-agents"]')
    await expect(backLink).toBeVisible()
  })

  test('should display participant chips in header', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    // Should show agent avatar chip with first letter
    const agentChip = page.locator('div[style*="#8B5CF6"]').first()
    await expect(agentChip).toBeVisible()
  })

  test('should display mode selector', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const modeSelect = page.locator('select')
    await expect(modeSelect).toBeVisible()
    await expect(modeSelect).toHaveValue('OnDemand')
  })

  test('should display stats in header', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    // Stats should be visible (on large screens)
    await expect(page.getByText('messages')).toBeVisible()
    await expect(page.getByText('tokens')).toBeVisible()
  })

  test('should display pause/resume button', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByRole('button', { name: /Pause/i })).toBeVisible()
  })

  test('should display message input area', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await expect(textarea).toBeVisible()
  })

  test('should display send button', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    // Send button (with arrow icon)
    const sendButton = page.locator('button.orbitos-btn-primary')
    await expect(sendButton).toBeVisible()
  })

  test('should display empty state when no messages', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('No messages yet')).toBeVisible()
    await expect(page.getByText('Start the conversation by sending a message')).toBeVisible()
  })
})

// =============================================================================
// CHAT UI - MESSAGING INTERACTIONS
// =============================================================================

test.describe('Chat UI - Messaging Interactions', () => {
  let testAgentId: string
  let testConversationId: string

  test.beforeEach(async ({ request }) => {
    // Create fresh agent and conversation for each test
    const agentResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `ChatMsg-Agent-${Date.now()}`,
        roleTitle: 'Message Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful assistant.',
        isActive: true,
        avatarColor: '#10B981'
      }
    })
    testAgentId = (await agentResponse.json()).id

    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Messaging Test',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterEach(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should send message via send button', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('Hello, this is a test message!')

    const sendButton = page.locator('button.orbitos-btn-primary')
    await sendButton.click()

    await page.waitForTimeout(1000)

    // Message should appear in chat
    await expect(page.getByText('Hello, this is a test message!')).toBeVisible()
  })

  test('should send message via Enter key', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('Message sent with Enter key')
    await textarea.press('Enter')

    await page.waitForTimeout(1000)

    // Message should appear in chat
    await expect(page.getByText('Message sent with Enter key')).toBeVisible()
  })

  test('should allow newline with Shift+Enter', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('Line 1')
    await textarea.press('Shift+Enter')
    await textarea.type('Line 2')

    // Should have newline in textarea (not send)
    const value = await textarea.inputValue()
    expect(value).toContain('Line 1')
    expect(value).toContain('Line 2')
  })

  test('should disable send button when input is empty', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const sendButton = page.locator('button.orbitos-btn-primary')
    await expect(sendButton).toBeDisabled()
  })

  test('should enable send button when input has text', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('Test message')

    const sendButton = page.locator('button.orbitos-btn-primary')
    await expect(sendButton).not.toBeDisabled()
  })

  test('should clear input after sending message', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('Test message to send')

    const sendButton = page.locator('button.orbitos-btn-primary')
    await sendButton.click()

    await page.waitForTimeout(500)

    // Input should be cleared
    await expect(textarea).toHaveValue('')
  })

  test('should display user message with correct styling', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('User message test')
    await page.locator('button.orbitos-btn-primary').click()

    await page.waitForTimeout(1000)

    // User messages have purple background
    const userMessage = page.locator('.bg-purple-500\\/20').filter({ hasText: 'User message test' })
    await expect(userMessage).toBeVisible()
  })

  test('should update message count after sending', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    // Initial count should be 0
    await expect(page.getByText('0 messages')).toBeVisible()

    // Send a message
    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('Test message')
    await page.locator('button.orbitos-btn-primary').click()

    await page.waitForTimeout(1000)

    // Count should update
    await expect(page.getByText('1 messages')).toBeVisible()
  })
})

// =============================================================================
// CHAT UI - MODE SWITCHING
// =============================================================================

test.describe('Chat UI - Mode Switching', () => {
  let testAgentId: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    const agentResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'ModeSwitch-Agent',
        roleTitle: 'Mode Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful assistant.',
        isActive: true
      }
    })
    testAgentId = (await agentResponse.json()).id

    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Mode Switch Test',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should display all mode options in selector', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const modeSelect = page.locator('select')

    await expect(modeSelect.locator('option[value="OnDemand"]')).toBeAttached()
    await expect(modeSelect.locator('option[value="Moderated"]')).toBeAttached()
    await expect(modeSelect.locator('option[value="RoundRobin"]')).toBeAttached()
    await expect(modeSelect.locator('option[value="Free"]')).toBeAttached()
  })

  test('should change mode when selecting different option', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const modeSelect = page.locator('select')
    await modeSelect.selectOption('Moderated')

    await page.waitForTimeout(500)

    // Verify mode changed
    await expect(modeSelect).toHaveValue('Moderated')
  })
})

// =============================================================================
// CHAT UI - PAUSE/RESUME
// =============================================================================

test.describe('Chat UI - Pause/Resume', () => {
  let testAgentId: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    const agentResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'PauseResume-Agent',
        roleTitle: 'Pause Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful assistant.',
        isActive: true
      }
    })
    testAgentId = (await agentResponse.json()).id

    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Pause/Resume Test',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should pause conversation when clicking Pause button', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /Pause/i }).click()
    await page.waitForTimeout(500)

    // Should now show Resume button
    await expect(page.getByRole('button', { name: /Resume/i })).toBeVisible()
  })

  test('should show paused message when conversation is paused', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    // Pause conversation
    const pauseButton = page.getByRole('button', { name: /Pause|Resume/i })
    if (await pauseButton.textContent() === 'Pause') {
      await pauseButton.click()
      await page.waitForTimeout(500)
    }

    // Should show paused message
    await expect(page.getByText('Conversation is paused')).toBeVisible()
  })

  test('should resume conversation when clicking Resume button', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    // Make sure it's paused first
    const pauseButton = page.getByRole('button', { name: /Pause|Resume/i })
    if (await pauseButton.textContent() === 'Pause') {
      await pauseButton.click()
      await page.waitForTimeout(500)
    }

    // Click Resume
    await page.getByRole('button', { name: /Resume/i }).click()
    await page.waitForTimeout(500)

    // Should now show Pause button
    await expect(page.getByRole('button', { name: /Pause/i })).toBeVisible()
  })
})

// =============================================================================
// CHAT UI - PARTICIPANTS PANEL
// =============================================================================

test.describe('Chat UI - Participants Panel', () => {
  let testAgentId1: string
  let testAgentId2: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    // Create two agents
    const agent1Response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'Panel-Agent-1',
        roleTitle: 'First Panel Agent',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are the first agent.',
        isActive: true,
        avatarColor: '#F59E0B'
      }
    })
    testAgentId1 = (await agent1Response.json()).id

    const agent2Response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'Panel-Agent-2',
        roleTitle: 'Second Panel Agent',
        provider: 'anthropic',
        modelId: 'claude-3-5-haiku-20241022',
        modelDisplayName: 'Claude 3.5 Haiku',
        systemPrompt: 'You are the second agent.',
        isActive: true,
        avatarColor: '#10B981'
      }
    })
    testAgentId2 = (await agent2Response.json()).id

    // Create conversation with first agent only
    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Panel Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId1]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
    if (testAgentId1) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId1}`)
    }
    if (testAgentId2) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId2}`)
    }
  })

  test('should display participants section on large screens', async ({ page }) => {
    // Set viewport to large screen
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('Participants')).toBeVisible()
    await expect(page.getByText('Panel-Agent-1')).toBeVisible()
  })

  test('should display Add Participant button', async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('+ Add Participant')).toBeVisible()
  })

  test('should open add participant dialog', async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await page.getByText('+ Add Participant').click()
    await page.waitForTimeout(300)

    await expect(page.getByText('Add AI Agent')).toBeVisible()
  })

  test('should show available agents in add dialog', async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await page.getByText('+ Add Participant').click()
    await page.waitForTimeout(300)

    // Second agent should be available to add
    await expect(page.getByText('Panel-Agent-2')).toBeVisible()
  })

  test('should add agent when clicking in add dialog', async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await page.getByText('+ Add Participant').click()
    await page.waitForTimeout(300)

    // Click on second agent to add
    await page.locator('button').filter({ hasText: 'Panel-Agent-2' }).click()
    await page.waitForTimeout(500)

    // Dialog should close and agent should appear in participants
    await expect(page.getByRole('heading', { name: 'Add AI Agent' })).not.toBeVisible()

    // Both agents should now be in participants
    const participantsSection = page.locator('text=Participants').locator('..')
    await expect(participantsSection.getByText('Panel-Agent-1')).toBeVisible()
    await expect(participantsSection.getByText('Panel-Agent-2')).toBeVisible()
  })
})

// =============================================================================
// CHAT UI - SESSION STATS PANEL
// =============================================================================

test.describe('Chat UI - Session Stats Panel', () => {
  let testAgentId: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    const agentResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'Stats-Agent',
        roleTitle: 'Stats Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful assistant.',
        isActive: true
      }
    })
    testAgentId = (await agentResponse.json()).id

    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Stats Panel Test',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should display session stats section', async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('Session Stats')).toBeVisible()
  })

  test('should display all stat fields', async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('Messages')).toBeVisible()
    await expect(page.getByText('AI Responses')).toBeVisible()
    await expect(page.getByText('Total Tokens')).toBeVisible()
    await expect(page.getByText('Est. Cost')).toBeVisible()
    await expect(page.getByText('Duration')).toBeVisible()
  })

  test('should display current mode section', async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 900 })

    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('Current Mode')).toBeVisible()
    await expect(page.getByText('On-Demand')).toBeVisible()
    await expect(page.getByText('AI agents only respond when @mentioned')).toBeVisible()
  })
})

// =============================================================================
// CHAT UI - @MENTION DETECTION
// =============================================================================

test.describe('Chat UI - @Mention Detection', () => {
  let testAgentId: string
  let testConversationId: string

  test.beforeAll(async ({ request }) => {
    const agentResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: 'MentionTest',
        roleTitle: 'Mention Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a helpful assistant.',
        isActive: true
      }
    })
    testAgentId = (await agentResponse.json()).id

    const convoResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Mention Test Conversation',
        mode: 'OnDemand',
        aiAgentIds: [testAgentId]
      }
    })
    testConversationId = (await convoResponse.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (testConversationId) {
      await request.delete(`${API_BASE}/conversations/${testConversationId}`)
    }
    if (testAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${testAgentId}`)
    }
  })

  test('should show "Will invoke" indicator when @mentioning agent', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('@MentionTest what is your name?')

    // Should show invoke indicator
    await expect(page.getByText('Will invoke: MentionTest')).toBeVisible()
  })

  test('should not show indicator without @mention', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('Hello, this is a regular message')

    // Should NOT show invoke indicator
    await expect(page.getByText('Will invoke:')).not.toBeVisible()
  })

  test('should detect case-insensitive @mentions', async ({ page }) => {
    await page.goto(`/app/ai-agents/conversations/${testConversationId}`)
    await page.waitForLoadState('networkidle')

    const textarea = page.locator('textarea[placeholder*="Type a message"]')
    await textarea.fill('@mentiontest please respond')

    // Should show invoke indicator (case-insensitive)
    await expect(page.getByText('Will invoke: MentionTest')).toBeVisible()
  })
})

// =============================================================================
// CHAT UI - ERROR HANDLING
// =============================================================================

test.describe('Chat UI - Error Handling', () => {
  test('should show "Conversation not found" for invalid ID', async ({ page }) => {
    await page.goto('/app/ai-agents/conversations/00000000-0000-0000-0000-000000000000')
    await page.waitForLoadState('networkidle')

    await expect(page.getByText('Conversation not found')).toBeVisible()
    await expect(page.getByText('Back to AI Agents')).toBeVisible()
  })

  test('should navigate back when clicking "Back to AI Agents" link', async ({ page }) => {
    await page.goto('/app/ai-agents/conversations/00000000-0000-0000-0000-000000000000')
    await page.waitForLoadState('networkidle')

    await page.getByText('Back to AI Agents').click()
    await page.waitForURL(/\/app\/ai-agents$/)

    expect(page.url()).toContain('/app/ai-agents')
  })
})
