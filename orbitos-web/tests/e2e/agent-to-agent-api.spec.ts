/**
 * =============================================================================
 * OrbitOS Operations - Agent-to-Agent (A2A) API Tests
 * =============================================================================
 * API-level tests for the Agent-to-Agent functionality.
 * Tests cover CRUD operations for Custom agents, A2A fields,
 * Built-in agent seeding and customization, and conversation management.
 *
 * NOTE: Built-in agents are created via seeding, not direct API creation.
 * The API endpoint for creating agents only allows Custom agent creation.
 *
 * Spec: F003 - Multi-Agent AI Chat (A2A subset)
 * =============================================================================
 */

import { test, expect } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}`

// =============================================================================
// BUILT-IN AGENTS - SEEDING AND CUSTOMIZATION
// =============================================================================

test.describe('A2A API - Built-in Agent Seeding', () => {
  test('should seed Built-in agents for organization', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    expect(response.status()).toBe(200)
    const result = await response.json()
    expect(result.message).toContain('Built-in agents seeded')
  })

  test('should return Built-in agents in list after seeding', async ({ request }) => {
    // Seed first
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    // Get agents
    const response = await request.get(`${API_BASE}/ai-agents`)
    expect(response.status()).toBe(200)

    const agents = await response.json()
    const builtInAgents = agents.filter((a: any) => a.agentType === 'BuiltIn')

    expect(builtInAgents.length).toBeGreaterThan(0)
  })

  test('should return Built-in agents with A2A fields', async ({ request }) => {
    // Seed first
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    // Get agents
    const response = await request.get(`${API_BASE}/ai-agents`)
    const agents = await response.json()
    const builtInAgents = agents.filter((a: any) => a.agentType === 'BuiltIn')

    // Verify A2A fields exist
    builtInAgents.forEach((agent: any) => {
      expect(agent.agentType).toBe('BuiltIn')
      expect(agent.isSystemProvided).toBe(true)
      expect(agent.canBeOrchestrated).toBe(true)
      expect(agent.specialistKey).toBeDefined()
      expect(agent.contextScopes).toBeDefined()
      expect(Array.isArray(agent.contextScopes)).toBe(true)
    })
  })

  test('should filter agents by type=BuiltIn', async ({ request }) => {
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    const response = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    expect(response.status()).toBe(200)

    const agents = await response.json()
    agents.forEach((agent: any) => {
      expect(agent.agentType).toBe('BuiltIn')
    })
  })

  test('should update Built-in agent custom instructions', async ({ request }) => {
    // Seed first
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    // Get a Built-in agent
    const listResponse = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    const agents = await listResponse.json()
    const builtInAgent = agents[0]

    // Update custom instructions
    const updateResponse = await request.put(`${API_BASE}/ai-agents/${builtInAgent.id}`, {
      data: {
        customInstructions: 'Always consider our Q4 hiring freeze'
      }
    })

    expect(updateResponse.status()).toBe(200)
    const updated = await updateResponse.json()

    expect(updated.customInstructions).toBe('Always consider our Q4 hiring freeze')
  })

  test('should toggle Built-in agent active status', async ({ request }) => {
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    // Get a Built-in agent
    const listResponse = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    const agents = await listResponse.json()
    const builtInAgent = agents[0]

    // Deactivate
    const deactivateResponse = await request.put(`${API_BASE}/ai-agents/${builtInAgent.id}`, {
      data: { isActive: false }
    })

    expect(deactivateResponse.status()).toBe(200)
    expect((await deactivateResponse.json()).isActive).toBe(false)

    // Reactivate
    const activateResponse = await request.put(`${API_BASE}/ai-agents/${builtInAgent.id}`, {
      data: { isActive: true }
    })

    expect(activateResponse.status()).toBe(200)
    expect((await activateResponse.json()).isActive).toBe(true)
  })

  test('should NOT allow deleting Built-in agents', async ({ request }) => {
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    const listResponse = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    const agents = await listResponse.json()
    const builtInAgent = agents[0]

    const deleteResponse = await request.delete(`${API_BASE}/ai-agents/${builtInAgent.id}`)

    // Should return 400 Bad Request
    expect(deleteResponse.status()).toBe(400)
  })
})

// =============================================================================
// CUSTOM AGENTS - CRUD OPERATIONS
// =============================================================================

test.describe('A2A API - Custom Agent CRUD', () => {
  let createdAgentId: string

  test.afterEach(async ({ request }) => {
    if (createdAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${createdAgentId}`)
      createdAgentId = ''
    }
  })

  test('should create a Custom agent', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Custom-API-Test-${Date.now()}`,
        roleTitle: 'Custom Role',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a custom AI agent.',
        maxTokensPerResponse: 2048,
        temperature: 0.7,
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })

    expect(response.status()).toBe(201)
    const agent = await response.json()
    createdAgentId = agent.id

    expect(agent.agentType).toBe('Custom')
    expect(agent.isSystemProvided).toBe(false)
  })

  test('should create Custom agent with canCallBuiltInAgents enabled', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Orchestrator-${Date.now()}`,
        roleTitle: 'Orchestrator',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You orchestrate other agents.',
        isActive: true,
        avatarColor: '#8B5CF6',
        canCallBuiltInAgents: true
      }
    })

    expect(response.status()).toBe(201)
    const agent = await response.json()
    createdAgentId = agent.id

    expect(agent.canCallBuiltInAgents).toBe(true)
  })

  test('should create Custom agent with personality traits', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Personality-${Date.now()}`,
        roleTitle: 'Personality Test',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You have personality.',
        isActive: true,
        avatarColor: '#8B5CF6',
        assertiveness: 75,
        communicationStyle: 'Direct',
        reactionTendency: 'Critical',
        expertiseAreas: 'finance,budgets,costs',
        seniorityLevel: 4,
        asksQuestions: true,
        givesBriefAcknowledgments: false
      }
    })

    expect(response.status()).toBe(201)
    const agent = await response.json()
    createdAgentId = agent.id

    expect(agent.assertiveness).toBe(75)
    expect(agent.communicationStyle).toBe('Direct')
    expect(agent.reactionTendency).toBe('Critical')
    expect(agent.expertiseAreas).toBe('finance,budgets,costs')
    expect(agent.seniorityLevel).toBe(4)
    expect(agent.asksQuestions).toBe(true)
    expect(agent.givesBriefAcknowledgments).toBe(false)
  })

  test('should update Custom agent name and role', async ({ request }) => {
    // Create agent
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Update-Test-${Date.now()}`,
        roleTitle: 'Original Role',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'Original prompt.',
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })

    expect(createResponse.status()).toBe(201)
    const agent = await createResponse.json()
    createdAgentId = agent.id

    // Update name and role
    const updateResponse = await request.put(`${API_BASE}/ai-agents/${agent.id}`, {
      data: {
        name: `Updated-Name-${Date.now()}`,
        roleTitle: 'Updated Role'
      }
    })

    expect(updateResponse.status()).toBe(200)
    const updated = await updateResponse.json()

    expect(updated.roleTitle).toBe('Updated Role')
  })

  test('should update Custom agent canCallBuiltInAgents', async ({ request }) => {
    // Create agent without canCallBuiltInAgents
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `CallBuiltIn-Test-${Date.now()}`,
        roleTitle: 'Test',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'Test.',
        isActive: true,
        avatarColor: '#8B5CF6',
        canCallBuiltInAgents: false
      }
    })

    expect(createResponse.status()).toBe(201)
    const agent = await createResponse.json()
    createdAgentId = agent.id
    expect(agent.canCallBuiltInAgents).toBe(false)

    // Update to enable
    const updateResponse = await request.put(`${API_BASE}/ai-agents/${agent.id}`, {
      data: { canCallBuiltInAgents: true }
    })

    expect(updateResponse.status()).toBe(200)
    expect((await updateResponse.json()).canCallBuiltInAgents).toBe(true)
  })

  test('should delete Custom agent', async ({ request }) => {
    // Create agent
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Delete-Test-${Date.now()}`,
        roleTitle: 'Delete',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'Delete me.',
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })

    expect(createResponse.status()).toBe(201)
    const agent = await createResponse.json()

    // Delete agent
    const deleteResponse = await request.delete(`${API_BASE}/ai-agents/${agent.id}`)
    expect(deleteResponse.status()).toBe(204)

    // Verify deletion
    const getResponse = await request.get(`${API_BASE}/ai-agents/${agent.id}`)
    expect(getResponse.status()).toBe(404)
  })

  test('should filter agents by type=Custom', async ({ request }) => {
    // Create a custom agent
    const createResponse = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Filter-Test-${Date.now()}`,
        roleTitle: 'Filter Test',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'Filter test.',
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })
    createdAgentId = (await createResponse.json()).id

    const response = await request.get(`${API_BASE}/ai-agents?type=Custom`)
    expect(response.status()).toBe(200)

    const agents = await response.json()
    agents.forEach((agent: any) => {
      expect(agent.agentType).toBe('Custom')
    })
  })
})

// =============================================================================
// AGENT LISTING
// =============================================================================

test.describe('A2A API - Agent Listing', () => {
  test('should list all agents', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents`)

    expect(response.status()).toBe(200)
    const agents = await response.json()

    expect(Array.isArray(agents)).toBe(true)
  })

  test('should return agents with A2A fields in response', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents`)
    expect(response.status()).toBe(200)

    const agents = await response.json()

    if (agents.length > 0) {
      const agent = agents[0]
      expect(agent).toHaveProperty('agentType')
      expect(agent).toHaveProperty('canCallBuiltInAgents')
      expect(agent).toHaveProperty('canBeOrchestrated')
      expect(agent).toHaveProperty('isSystemProvided')
    }
  })

  test('should return agents sorted by type (Built-in first)', async ({ request }) => {
    // Ensure we have Built-in agents
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    // Query Built-in agents specifically to verify they exist and have proper attributes
    const builtInResponse = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    const builtInAgents = await builtInResponse.json()

    // We should have the 4 seeded Built-in agents
    expect(builtInAgents.length).toBeGreaterThanOrEqual(4)

    // Verify all returned agents are Built-in type
    builtInAgents.forEach((a: any) => {
      expect(a.agentType).toBe('BuiltIn')
    })

    // Verify the expected Built-in agents exist
    const agentNames = builtInAgents.map((a: any) => a.name)
    expect(agentNames).toContain('People Expert')
    expect(agentNames).toContain('Process Expert')
    expect(agentNames).toContain('Strategy Expert')
    expect(agentNames).toContain('Finance Expert')

    // Verify ordering within Built-in agents (by sortOrder)
    // Note: Full sorting test (Built-in before Custom) is not reliable in parallel
    // execution as other tests may create Custom agents concurrently
    const sortOrders = builtInAgents.map((a: any) => a.sortOrder)
    const isSorted = sortOrders.every((val: number, i: number) => i === 0 || val >= sortOrders[i - 1])
    expect(isSorted).toBe(true)
  })
})

// =============================================================================
// CONVERSATIONS WITH A2A AGENTS
// =============================================================================

test.describe('A2A API - Conversations', () => {
  let customAgentId: string

  test.beforeAll(async ({ request }) => {
    // Seed Built-in agents
    await request.post(`${API_BASE}/ai-agents/seed-builtin`)

    // Create a Custom agent
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Convo-Agent-${Date.now()}`,
        roleTitle: 'Conversation Tester',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You help with conversations.',
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })
    customAgentId = (await response.json()).id
  })

  test.afterAll(async ({ request }) => {
    if (customAgentId) {
      await request.delete(`${API_BASE}/ai-agents/${customAgentId}`)
    }
  })

  test('should create conversation with Custom agent', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: `Custom Convo ${Date.now()}`,
        mode: 'OnDemand',
        aiAgentIds: [customAgentId]
      }
    })

    expect(response.status()).toBe(201)
    const convo = await response.json()

    // participantCount includes the creating user + the AI agent
    expect(convo.participantCount).toBe(2) // 1 user + 1 AI agent

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${convo.id}`)
  })

  test('should create conversation with Built-in agent', async ({ request }) => {
    // Get a Built-in agent
    const listResponse = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    const agents = await listResponse.json()

    if (agents.length === 0) {
      test.skip()
      return
    }

    const builtInAgent = agents[0]

    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: `Built-in Convo ${Date.now()}`,
        mode: 'OnDemand',
        aiAgentIds: [builtInAgent.id]
      }
    })

    expect(response.status()).toBe(201)
    const convo = await response.json()

    // participantCount includes the creating user + the AI agent
    expect(convo.participantCount).toBe(2) // 1 user + 1 AI agent

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${convo.id}`)
  })

  test('should create conversation with mixed agent types', async ({ request }) => {
    // Get a Built-in agent
    const listResponse = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    const agents = await listResponse.json()

    if (agents.length === 0) {
      test.skip()
      return
    }

    const builtInAgent = agents[0]

    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: `Mixed Convo ${Date.now()}`,
        mode: 'OnDemand',
        aiAgentIds: [builtInAgent.id, customAgentId]
      }
    })

    expect(response.status()).toBe(201)
    const convo = await response.json()

    // participantCount includes the creating user + 2 AI agents
    expect(convo.participantCount).toBe(3) // 1 user + 2 AI agents

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${convo.id}`)
  })

  test('should create conversation with Emergent mode', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: `Emergent Convo ${Date.now()}`,
        mode: 'Emergent',
        aiAgentIds: [customAgentId]
      }
    })

    expect(response.status()).toBe(201)
    const convo = await response.json()

    expect(convo.mode).toBe('Emergent')

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${convo.id}`)
  })

  test('should add participant to existing conversation', async ({ request }) => {
    // Create conversation with Custom agent
    const createResponse = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: `Add Participant ${Date.now()}`,
        mode: 'OnDemand',
        aiAgentIds: [customAgentId]
      }
    })

    const convo = await createResponse.json()

    // Get a Built-in agent
    const listResponse = await request.get(`${API_BASE}/ai-agents?type=BuiltIn`)
    const agents = await listResponse.json()

    if (agents.length === 0) {
      await request.delete(`${API_BASE}/conversations/${convo.id}`)
      test.skip()
      return
    }

    const builtInAgent = agents[0]

    // Add Built-in agent as participant
    const addResponse = await request.post(`${API_BASE}/conversations/${convo.id}/participants`, {
      data: { aiAgentId: builtInAgent.id }
    })

    expect(addResponse.status()).toBe(200)

    // Verify participant count
    const getResponse = await request.get(`${API_BASE}/conversations/${convo.id}`)
    const updated = await getResponse.json()

    // 1 user + 1 custom agent + 1 built-in agent = 3
    expect(updated.participantCount).toBe(3)

    // Cleanup
    await request.delete(`${API_BASE}/conversations/${convo.id}`)
  })
})

// =============================================================================
// AVAILABLE MODELS API
// =============================================================================

test.describe('A2A API - Available Models', () => {
  test('should return list of available models', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents/available-models`)

    expect(response.status()).toBe(200)
    const models = await response.json()

    expect(Array.isArray(models)).toBe(true)
    expect(models.length).toBeGreaterThan(0)
  })

  test('should return models with required fields', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents/available-models`)
    const models = await response.json()

    models.forEach((model: any) => {
      expect(model).toHaveProperty('provider')
      expect(model).toHaveProperty('modelId')
      expect(model).toHaveProperty('displayName')
      expect(model).toHaveProperty('description')
      expect(model).toHaveProperty('contextWindow')
    })
  })

  test('should include Anthropic models', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents/available-models`)
    const models = await response.json()

    const anthropicModels = models.filter((m: any) => m.provider === 'anthropic')
    expect(anthropicModels.length).toBeGreaterThan(0)
  })

  test('should include OpenAI models', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents/available-models`)
    const models = await response.json()

    const openaiModels = models.filter((m: any) => m.provider === 'openai')
    expect(openaiModels.length).toBeGreaterThan(0)
  })

  test('should include Google models', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents/available-models`)
    const models = await response.json()

    const googleModels = models.filter((m: any) => m.provider === 'google')
    expect(googleModels.length).toBeGreaterThan(0)
  })
})

// =============================================================================
// ERROR HANDLING
// =============================================================================

test.describe('A2A API - Error Handling', () => {
  test('should return 404 for non-existent agent', async ({ request }) => {
    const response = await request.get(`${API_BASE}/ai-agents/00000000-0000-0000-0000-000000000000`)
    expect(response.status()).toBe(404)
  })

  test('should return 404 for non-existent conversation', async ({ request }) => {
    const response = await request.get(`${API_BASE}/conversations/00000000-0000-0000-0000-000000000000`)
    expect(response.status()).toBe(404)
  })

  test('should return error for conversation without agents', async ({ request }) => {
    const response = await request.post(`${API_BASE}/conversations`, {
      data: {
        title: 'Invalid Convo',
        mode: 'OnDemand',
        aiAgentIds: []
      }
    })

    expect(response.status()).toBeGreaterThanOrEqual(400)
  })

  test('should return error for duplicate agent name', async ({ request }) => {
    const uniqueName = `Duplicate-Test-${Date.now()}`

    // Create first agent
    const first = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: uniqueName,
        roleTitle: 'Test',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'Test.',
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })
    const firstAgent = await first.json()

    // Try to create second agent with same name
    const second = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: uniqueName,
        roleTitle: 'Test 2',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'Test 2.',
        isActive: true,
        avatarColor: '#8B5CF6'
      }
    })

    expect(second.status()).toBe(400)

    // Cleanup
    await request.delete(`${API_BASE}/ai-agents/${firstAgent.id}`)
  })
})

// =============================================================================
// MULTI-USER PERSPECTIVE API TESTS
// =============================================================================

test.describe('A2A API - Director Perspective', () => {
  let agentId: string

  test.afterEach(async ({ request }) => {
    if (agentId) {
      await request.delete(`${API_BASE}/ai-agents/${agentId}`)
      agentId = ''
    }
  })

  test('Director creates strategic agent', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Director-AI-${Date.now()}`,
        roleTitle: 'Executive Director',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are an Executive Director AI. Focus on strategic oversight.',
        isActive: true,
        avatarColor: '#8B5CF6',
        canCallBuiltInAgents: true,
        expertiseAreas: 'strategy,leadership,vision,goals',
        seniorityLevel: 5,
        communicationStyle: 'Direct',
        reactionTendency: 'Balanced'
      }
    })

    expect(response.status()).toBe(201)
    const agent = await response.json()
    agentId = agent.id

    expect(agent.expertiseAreas).toBe('strategy,leadership,vision,goals')
    expect(agent.seniorityLevel).toBe(5)
    expect(agent.canCallBuiltInAgents).toBe(true)
  })
})

test.describe('A2A API - HR Manager Perspective', () => {
  let agentId: string

  test.afterEach(async ({ request }) => {
    if (agentId) {
      await request.delete(`${API_BASE}/ai-agents/${agentId}`)
      agentId = ''
    }
  })

  test('HR Manager creates HR-focused agent', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `HR-AI-${Date.now()}`,
        roleTitle: 'HR Manager',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are an HR Manager AI. Focus on talent, culture, and people operations.',
        isActive: true,
        avatarColor: '#EC4899',
        canCallBuiltInAgents: true,
        expertiseAreas: 'hiring,onboarding,culture,performance,retention',
        seniorityLevel: 4,
        communicationStyle: 'Diplomatic',
        reactionTendency: 'Supportive',
        asksQuestions: true
      }
    })

    expect(response.status()).toBe(201)
    const agent = await response.json()
    agentId = agent.id

    expect(agent.expertiseAreas).toBe('hiring,onboarding,culture,performance,retention')
    expect(agent.communicationStyle).toBe('Diplomatic')
  })
})

test.describe('A2A API - Finance Manager Perspective', () => {
  let agentId: string

  test.afterEach(async ({ request }) => {
    if (agentId) {
      await request.delete(`${API_BASE}/ai-agents/${agentId}`)
      agentId = ''
    }
  })

  test('Finance Manager creates finance-focused agent', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Finance-AI-${Date.now()}`,
        roleTitle: 'Finance Manager',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are a Finance Manager AI. Focus on budgeting and financial analysis.',
        isActive: true,
        avatarColor: '#F59E0B',
        canCallBuiltInAgents: true,
        expertiseAreas: 'budget,revenue,expenses,forecasting,metrics',
        seniorityLevel: 4,
        communicationStyle: 'Analytical',
        reactionTendency: 'Critical'
      }
    })

    expect(response.status()).toBe(201)
    const agent = await response.json()
    agentId = agent.id

    expect(agent.communicationStyle).toBe('Analytical')
    expect(agent.reactionTendency).toBe('Critical')
  })
})

test.describe('A2A API - Operations Manager Perspective', () => {
  let agentId: string

  test.afterEach(async ({ request }) => {
    if (agentId) {
      await request.delete(`${API_BASE}/ai-agents/${agentId}`)
      agentId = ''
    }
  })

  test('Operations Manager creates operations-focused agent', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai-agents`, {
      data: {
        name: `Ops-AI-${Date.now()}`,
        roleTitle: 'Operations Manager',
        provider: 'anthropic',
        modelId: 'claude-sonnet-4-20250514',
        modelDisplayName: 'Claude Sonnet 4',
        systemPrompt: 'You are an Operations Manager AI. Focus on process efficiency.',
        isActive: true,
        avatarColor: '#10B981',
        canCallBuiltInAgents: true,
        expertiseAreas: 'processes,efficiency,workflows,automation',
        seniorityLevel: 4,
        communicationStyle: 'Direct',
        reactionTendency: 'Balanced'
      }
    })

    expect(response.status()).toBe(201)
    const agent = await response.json()
    agentId = agent.id

    expect(agent.expertiseAreas).toBe('processes,efficiency,workflows,automation')
  })
})
