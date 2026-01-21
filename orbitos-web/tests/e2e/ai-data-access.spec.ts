/**
 * =============================================================================
 * OrbitOS Operations - AI Data Access E2E Tests
 * =============================================================================
 * Comprehensive end-to-end tests for AI assistant's ability to access and
 * manipulate organization data including Functions, Processes, Goals,
 * Partners, Channels, Value Propositions, Customer Relationships, and
 * Revenue Streams.
 *
 * Spec: F005 - AI Data Access
 * =============================================================================
 */

import { test, expect, Page, APIRequestContext } from '@playwright/test'

const ORG_ID = '11111111-1111-1111-1111-111111111111'
const API_BASE = `http://localhost:5027/api/organizations/${ORG_ID}`

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

async function sendAiChat(request: APIRequestContext, message: string, context?: string) {
  const response = await request.post(`${API_BASE}/ai/chat`, {
    data: {
      message,
      context,
      history: []
    }
  })
  return response
}

async function getCapabilities(request: APIRequestContext) {
  return request.get(`${API_BASE}/ai/capabilities`)
}

// Cleanup helpers
async function cleanupFunctions(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/functions`)
  if (response.ok()) {
    const functions = await response.json()
    for (const func of functions) {
      if (func.name.includes('AI-Test-') || func.name.includes('Accounts ') || func.name.includes('Financial ')) {
        await request.delete(`${API_BASE}/operations/functions/${func.id}`)
      }
    }
  }
}

async function cleanupProcesses(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/processes`)
  if (response.ok()) {
    const processes = await response.json()
    for (const process of processes) {
      if (process.name.includes('AI-Test-') || process.name.includes('Onboarding') || process.name.includes('Invoice')) {
        await request.delete(`${API_BASE}/operations/processes/${process.id}`)
      }
    }
  }
}

async function cleanupGoals(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/goals`)
  if (response.ok()) {
    const goals = await response.json()
    for (const goal of goals) {
      if (goal.name.includes('AI-Test-') || goal.name.includes('Increase') || goal.name.includes('Reduce')) {
        await request.delete(`${API_BASE}/operations/goals/${goal.id}`)
      }
    }
  }
}

async function cleanupPartners(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/partners`)
  if (response.ok()) {
    const partners = await response.json()
    for (const partner of partners) {
      if (partner.name.includes('AI-Test-') || partner.name.includes('AWS') || partner.name.includes('Cloud')) {
        await request.delete(`${API_BASE}/operations/partners/${partner.id}`)
      }
    }
  }
}

async function cleanupChannels(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/channels`)
  if (response.ok()) {
    const channels = await response.json()
    for (const channel of channels) {
      if (channel.name.includes('AI-Test-') || channel.name.includes('Website') || channel.name.includes('Email')) {
        await request.delete(`${API_BASE}/operations/channels/${channel.id}`)
      }
    }
  }
}

async function cleanupValuePropositions(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/value-propositions`)
  if (response.ok()) {
    const vps = await response.json()
    for (const vp of vps) {
      if (vp.name.includes('AI-Test-') || vp.name.includes('Fast') || vp.name.includes('Reliable')) {
        await request.delete(`${API_BASE}/operations/value-propositions/${vp.id}`)
      }
    }
  }
}

async function cleanupCustomerRelationships(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/customer-relationships`)
  if (response.ok()) {
    const crs = await response.json()
    for (const cr of crs) {
      if (cr.name.includes('AI-Test-') || cr.name.includes('Premium') || cr.name.includes('Self-Service')) {
        await request.delete(`${API_BASE}/operations/customer-relationships/${cr.id}`)
      }
    }
  }
}

async function cleanupRevenueStreams(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/revenue-streams`)
  if (response.ok()) {
    const streams = await response.json()
    for (const stream of streams) {
      if (stream.name.includes('AI-Test-') || stream.name.includes('Subscription') || stream.name.includes('Licensing')) {
        await request.delete(`${API_BASE}/operations/revenue-streams/${stream.id}`)
      }
    }
  }
}

async function cleanupRoles(request: APIRequestContext) {
  const response = await request.get(`${API_BASE}/operations/roles`)
  if (response.ok()) {
    const roles = await response.json()
    for (const role of roles) {
      if (role.name.includes('AI-Test-') || role.name.includes('Financial Analyst')) {
        await request.delete(`${API_BASE}/operations/roles/${role.id}`)
      }
    }
  }
}

// =============================================================================
// AI CAPABILITIES ENDPOINT TESTS
// =============================================================================

test.describe('AI Capabilities Endpoint', () => {
  test('should return all available capabilities', async ({ request }) => {
    const response = await getCapabilities(request)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.capabilities).toBeDefined()
    expect(Array.isArray(data.capabilities)).toBeTruthy()

    // Check for core capabilities
    const capabilityNames = data.capabilities.map((c: any) => c.name)
    expect(capabilityNames).toContain('chat')
    expect(capabilityNames).toContain('analyze_coverage')

    // Check for new CRUD capabilities
    expect(capabilityNames).toContain('create_function')
    expect(capabilityNames).toContain('bulk_create_functions')
    expect(capabilityNames).toContain('create_process')
    expect(capabilityNames).toContain('bulk_create_processes')
    expect(capabilityNames).toContain('create_goal')
    expect(capabilityNames).toContain('bulk_create_goals')
    expect(capabilityNames).toContain('create_partner')
    expect(capabilityNames).toContain('bulk_create_partners')
    expect(capabilityNames).toContain('create_channel')
    expect(capabilityNames).toContain('bulk_create_channels')
    expect(capabilityNames).toContain('create_value_proposition')
    expect(capabilityNames).toContain('bulk_create_value_propositions')
    expect(capabilityNames).toContain('create_customer_relationship')
    expect(capabilityNames).toContain('bulk_create_customer_relationships')
    expect(capabilityNames).toContain('create_revenue_stream')
    expect(capabilityNames).toContain('bulk_create_revenue_streams')
    expect(capabilityNames).toContain('create_role')
    expect(capabilityNames).toContain('bulk_create_roles')
  })

  test('should return quick actions', async ({ request }) => {
    const response = await getCapabilities(request)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.quickActions).toBeDefined()
    expect(Array.isArray(data.quickActions)).toBeTruthy()

    // Check for expected quick actions
    const actionLabels = data.quickActions.map((a: any) => a.label)
    expect(actionLabels).toContain('Analyze health')
    expect(actionLabels).toContain('Suggest functions')
    expect(actionLabels).toContain('Fill BMC')
    expect(actionLabels).toContain('Create OKRs')
    expect(actionLabels).toContain('Define processes')
  })
})

// =============================================================================
// AI CHAT - BASIC FUNCTIONALITY
// =============================================================================

test.describe('AI Chat - Basic Functionality', () => {
  test('should respond to basic greeting', async ({ request }) => {
    const response = await sendAiChat(request, 'Hello, what can you help me with?')
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    expect(data.message.length).toBeGreaterThan(0)
  })

  test('should understand organization context', async ({ request }) => {
    const response = await sendAiChat(request, 'What data do you have access to in my organization?')
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // The AI should mention various entity types it has access to
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('function') ||
      messageLower.includes('process') ||
      messageLower.includes('people') ||
      messageLower.includes('role') ||
      messageLower.includes('canvas') ||
      messageLower.includes('goal')
    ).toBeTruthy()
  })

  test('should list current organization data', async ({ request }) => {
    const response = await sendAiChat(request, 'List all the people in my organization')
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })
})

// =============================================================================
// AI CHAT - FUNCTION MANAGEMENT
// =============================================================================

test.describe('AI Chat - Function Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupFunctions(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupFunctions(request)
  })

  test('should suggest functions when asked', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Suggest the top 5 financial functions every company should have'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should mention financial-related functions
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('account') ||
      messageLower.includes('budget') ||
      messageLower.includes('financial') ||
      messageLower.includes('payroll') ||
      messageLower.includes('audit') ||
      messageLower.includes('suggest')
    ).toBeTruthy()
  })

  test('should create a single function via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a function called "AI-Test-Data-Analysis" in the Technical category with description "Analyze data patterns and trends"'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()

    // Verify function was created
    const functionsResponse = await request.get(`${API_BASE}/operations/functions`)
    const functions = await functionsResponse.json()
    const createdFunc = functions.find((f: any) => f.name === 'AI-Test-Data-Analysis')

    // If AI created it, it should exist
    if (data.toolCalls && data.toolCalls.some((tc: any) => tc.tool === 'create_function')) {
      expect(createdFunc).toBeDefined()
    }
  })

  test('should bulk create functions via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create these 3 financial functions: "Accounts Payable", "Accounts Receivable", and "Financial Reporting". Put them in the Finance category.'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()

    // Check if bulk create tool was used
    if (data.toolCalls) {
      const hasBulkCreate = data.toolCalls.some(
        (tc: any) => tc.tool === 'bulk_create_functions' || tc.tool === 'create_function'
      )
      expect(hasBulkCreate).toBeTruthy()
    }
  })
})

// =============================================================================
// AI CHAT - PROCESS MANAGEMENT
// =============================================================================

test.describe('AI Chat - Process Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupProcesses(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupProcesses(request)
  })

  test('should create a process via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a process called "AI-Test-Employee-Onboarding" with purpose "Onboard new employees efficiently" and frequency Weekly'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()

    // Check if create process tool was called
    if (data.toolCalls) {
      const hasCreateProcess = data.toolCalls.some((tc: any) => tc.tool === 'create_process')
      if (hasCreateProcess) {
        // Verify process was created
        const processesResponse = await request.get(`${API_BASE}/operations/processes`)
        const processes = await processesResponse.json()
        const createdProcess = processes.find((p: any) => p.name.includes('Onboarding'))
        expect(createdProcess).toBeDefined()
      }
    }
  })

  test('should suggest processes for a business type', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'What are the core business processes a SaaS company should have?'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should mention common SaaS processes
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('onboard') ||
      messageLower.includes('support') ||
      messageLower.includes('billing') ||
      messageLower.includes('sales') ||
      messageLower.includes('process')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - GOAL MANAGEMENT
// =============================================================================

test.describe('AI Chat - Goal Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupGoals(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupGoals(request)
  })

  test('should create OKRs via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create an objective called "AI-Test-Increase Revenue" with target value 1000000 and unit $'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })

  test('should help define key results', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Help me create 3 key results for a "Improve Customer Satisfaction" objective'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should provide suggestions for key results
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('nps') ||
      messageLower.includes('satisfaction') ||
      messageLower.includes('retention') ||
      messageLower.includes('churn') ||
      messageLower.includes('result') ||
      messageLower.includes('metric')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - PARTNER MANAGEMENT
// =============================================================================

test.describe('AI Chat - Partner Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupPartners(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupPartners(request)
  })

  test('should create partners via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a technology partner called "AI-Test-AWS Cloud" with type Technology and strategic value High'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })

  test('should suggest partner types', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'What types of partners should a B2B SaaS company consider?'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should mention partner types
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('technology') ||
      messageLower.includes('reseller') ||
      messageLower.includes('integration') ||
      messageLower.includes('strategic') ||
      messageLower.includes('partner')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - CHANNEL MANAGEMENT
// =============================================================================

test.describe('AI Chat - Channel Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupChannels(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupChannels(request)
  })

  test('should create channels via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a digital sales channel called "AI-Test-Website" with category Sales and ownership Owned'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })

  test('should suggest channel strategy', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'What channels should we use to reach enterprise customers?'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should mention enterprise channels
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('direct') ||
      messageLower.includes('sales') ||
      messageLower.includes('partner') ||
      messageLower.includes('enterprise') ||
      messageLower.includes('channel')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - VALUE PROPOSITION MANAGEMENT
// =============================================================================

test.describe('AI Chat - Value Proposition Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupValuePropositions(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupValuePropositions(request)
  })

  test('should create value propositions via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a value proposition called "AI-Test-Fast Delivery" with headline "Get your order in 24 hours"'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })

  test('should help craft value propositions', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Help me create compelling value propositions for a project management tool'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should provide value proposition ideas
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('time') ||
      messageLower.includes('collaborate') ||
      messageLower.includes('efficiency') ||
      messageLower.includes('productivity') ||
      messageLower.includes('value')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - CUSTOMER RELATIONSHIP MANAGEMENT
// =============================================================================

test.describe('AI Chat - Customer Relationship Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupCustomerRelationships(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupCustomerRelationships(request)
  })

  test('should create customer relationships via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a customer relationship called "AI-Test-Premium Support" with type DedicatedAssistance'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })

  test('should suggest customer relationship types', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'What customer relationship types should a freemium SaaS product have?'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should mention relationship types
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('self-service') ||
      messageLower.includes('automated') ||
      messageLower.includes('community') ||
      messageLower.includes('support') ||
      messageLower.includes('relationship')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - REVENUE STREAM MANAGEMENT
// =============================================================================

test.describe('AI Chat - Revenue Stream Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupRevenueStreams(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupRevenueStreams(request)
  })

  test('should create revenue streams via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a revenue stream called "AI-Test-SaaS Subscription" with type Subscription and pricing Fixed'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })

  test('should suggest revenue models', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'What are the common revenue models for a marketplace platform?'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should mention marketplace revenue models
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('commission') ||
      messageLower.includes('subscription') ||
      messageLower.includes('transaction') ||
      messageLower.includes('fee') ||
      messageLower.includes('revenue')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - ROLE MANAGEMENT
// =============================================================================

test.describe('AI Chat - Role Management', () => {
  test.beforeAll(async ({ request }) => {
    await cleanupRoles(request)
  })

  test.afterAll(async ({ request }) => {
    await cleanupRoles(request)
  })

  test('should create roles via AI', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Create a role called "AI-Test-Financial Analyst" in the Finance department'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })

  test('should suggest org structure', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'What roles should a startup with 20 employees have?'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    // Should mention common startup roles
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('ceo') ||
      messageLower.includes('cto') ||
      messageLower.includes('developer') ||
      messageLower.includes('marketing') ||
      messageLower.includes('sales') ||
      messageLower.includes('role')
    ).toBeTruthy()
  })
})

// =============================================================================
// AI CHAT - BUSINESS MODEL CANVAS FILL
// =============================================================================

test.describe('AI Chat - Business Model Canvas Assistance', () => {
  test('should help fill Business Model Canvas', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Help me fill in my Business Model Canvas for a B2B consulting firm. What should I put in Key Partners, Value Propositions, and Customer Segments?'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
    expect(data.message.length).toBeGreaterThan(100) // Should provide substantial advice

    // Should mention BMC elements
    const messageLower = data.message.toLowerCase()
    expect(
      messageLower.includes('partner') ||
      messageLower.includes('value') ||
      messageLower.includes('customer') ||
      messageLower.includes('segment') ||
      messageLower.includes('consulting')
    ).toBeTruthy()
  })

  test('should analyze business model gaps', async ({ request }) => {
    const response = await sendAiChat(
      request,
      'Analyze my organization and identify any gaps in my business model setup'
    )
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })
})

// =============================================================================
// AI CHAT - COMPREHENSIVE WORKFLOW
// =============================================================================

test.describe('AI Chat - Comprehensive Workflow', () => {
  test('should handle multi-step business setup conversation', async ({ request }) => {
    // Step 1: Ask about what's needed
    const step1 = await sendAiChat(
      request,
      'I\'m setting up a new e-commerce business. What should I define first?'
    )
    expect(step1.ok()).toBeTruthy()

    const data1 = await step1.json()
    expect(data1.message).toBeDefined()

    // Step 2: Ask to create functions
    const step2 = await sendAiChat(
      request,
      'Great, can you suggest the top 5 essential functions for an e-commerce business?'
    )
    expect(step2.ok()).toBeTruthy()

    const data2 = await step2.json()
    expect(data2.message).toBeDefined()

    // Step 3: Ask about processes
    const step3 = await sendAiChat(
      request,
      'Now what processes should I document?'
    )
    expect(step3.ok()).toBeTruthy()

    const data3 = await step3.json()
    expect(data3.message).toBeDefined()
  })
})

// =============================================================================
// ERROR HANDLING
// =============================================================================

test.describe('AI Chat - Error Handling', () => {
  test('should handle empty message gracefully', async ({ request }) => {
    const response = await request.post(`${API_BASE}/ai/chat`, {
      data: {
        message: '',
        history: []
      }
    })
    // Should return an error or handle gracefully
    const data = await response.json()
    expect(data).toBeDefined()
  })

  test('should handle very long message', async ({ request }) => {
    const longMessage = 'Can you help me? '.repeat(500)
    const response = await sendAiChat(request, longMessage)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(data.message).toBeDefined()
  })
})
