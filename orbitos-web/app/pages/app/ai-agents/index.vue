<script setup lang="ts">
import type { AiAgent, CreateAiAgentRequest, UpdateAiAgentRequest, CommunicationStyle, ReactionTendency } from '~/composables/useAiAgents'
import { AVAILABLE_CONTEXT_SCOPES } from '~/composables/useAiAgents'
import type { CreateConversationRequest } from '~/composables/useConversations'
import { useConversations } from '~/composables/useConversations'

// Personality options for dropdown selects
const communicationStyles: { value: CommunicationStyle; label: string; description: string }[] = [
  { value: 'Formal', label: 'Formal', description: 'Professional, structured responses' },
  { value: 'Casual', label: 'Casual', description: 'Friendly, conversational tone' },
  { value: 'Direct', label: 'Direct', description: 'Straight to the point, no fluff' },
  { value: 'Diplomatic', label: 'Diplomatic', description: 'Tactful, considers feelings and politics' },
  { value: 'Analytical', label: 'Analytical', description: 'Data-driven, methodical approach' }
]

const reactionTendencies: { value: ReactionTendency; label: string; description: string }[] = [
  { value: 'Supportive', label: 'Supportive', description: 'Tends to agree and build on ideas' },
  { value: 'Critical', label: 'Critical', description: 'Looks for flaws and risks' },
  { value: 'Balanced', label: 'Balanced', description: 'Weighs pros and cons objectively' },
  { value: 'DevilsAdvocate', label: "Devil's Advocate", description: 'Intentionally challenges assumptions' },
  { value: 'ConsensusBuilder', label: 'Consensus Builder', description: 'Seeks common ground and alignment' }
]

definePageMeta({
  layout: 'app'
})

const router = useRouter()

const {
  agents,
  builtInAgents,
  customAgents,
  activeAgents,
  availableModels,
  modelsByProvider,
  providerInfo,
  specialistIcons,
  getAgentIcon,
  isLoading,
  isLoadingModels,
  fetchAgents,
  fetchAvailableModels,
  createAgent,
  updateAgent,
  deleteAgent,
  toggleAgentActive
} = useAiAgents()

const {
  conversations,
  isLoading: isLoadingConversations,
  fetchConversations,
  createConversation
} = useConversations()

// Tab state
const activeTab = ref<'agents' | 'conversations'>('agents')

// Orchestrator state
const orchestratorQuery = ref('')
const isAskingOrchestrator = ref(false)
const orchestratorResponse = ref<{
  message: string
  agentsConsulted: string[]
  innerDialogue: Array<{
    stepNumber: number
    type: string
    title: string
    description?: string
    agentName?: string
    response?: string
    tokensUsed?: number
  }>
  error?: string
} | null>(null)

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const showEditBuiltInDialog = ref(false)
const showNewConversationDialog = ref(false)
const isSubmitting = ref(false)

// Editing Built-in agent
const editingBuiltInAgent = ref<AiAgent | null>(null)
const builtInCustomInstructions = ref('')

// Default system prompts for different roles
const systemPromptTemplates: Record<string, string> = {
  cfo: `You are the CFO AI assistant for this organization. Your expertise includes:
- Financial analysis and reporting
- Budget planning and forecasting
- Revenue projections and metrics
- Cost optimization strategies
- Investment recommendations

When analyzing data, always reference the specific sources you're using. Provide actionable insights backed by numbers.`,

  operations: `You are the Operations Manager AI assistant. Your expertise includes:
- Process optimization and efficiency
- Resource allocation and planning
- Workflow management
- Quality assurance
- Performance metrics and KPIs

Focus on practical, implementable recommendations that improve operational efficiency.`,

  strategy: `You are a Strategy Consultant AI. Your expertise includes:
- Market analysis and competitive positioning
- Business model optimization
- Growth strategies and expansion planning
- Risk assessment and mitigation
- Long-term planning and vision

Provide strategic insights that consider both short-term wins and long-term sustainability.`,

  hr: `You are the HR Director AI assistant. Your expertise includes:
- Talent acquisition and retention
- Organizational structure and design
- Employee development and training
- Culture and engagement initiatives
- Compliance and policy guidance

Focus on people-centric recommendations that align with business objectives.`,

  custom: `You are an AI assistant for this organization. Help users with their questions and tasks related to the organization's operations, data, and goals.

Be helpful, concise, and always back up recommendations with data when available.`
}

// New agent form
const newAgent = ref<CreateAiAgentRequest>({
  name: '',
  roleTitle: '',
  provider: 'anthropic',
  modelId: 'claude-sonnet-4-20250514',
  modelDisplayName: 'Claude Sonnet',
  systemPrompt: systemPromptTemplates.custom,
  avatarColor: '#8B5CF6',
  maxTokensPerResponse: 4096,
  temperature: 0.7,
  isActive: true,
  // Personality defaults
  assertiveness: 50,
  communicationStyle: 'Formal',
  reactionTendency: 'Balanced',
  expertiseAreas: '',
  seniorityLevel: 3,
  asksQuestions: false,
  givesBriefAcknowledgments: true
})

// Toggle for showing personality section
const showPersonalitySection = ref(false)
const showEditPersonalitySection = ref(false)

// Toggle for showing data access section
const showDataAccessSection = ref(false)
const showEditDataAccessSection = ref(false)

// Available context scopes for UI
const availableContextScopes = AVAILABLE_CONTEXT_SCOPES

// Toggle context scope in newAgent
const toggleContextScope = (scopeKey: string) => {
  if (!newAgent.value.contextScopes) {
    newAgent.value.contextScopes = []
  }
  const index = newAgent.value.contextScopes.indexOf(scopeKey)
  if (index === -1) {
    newAgent.value.contextScopes.push(scopeKey)
  } else {
    newAgent.value.contextScopes.splice(index, 1)
  }
}

// Toggle context scope in editingAgent
const toggleEditContextScope = (scopeKey: string) => {
  if (!editingAgent.value) return
  if (!editingAgent.value.contextScopes) {
    editingAgent.value.contextScopes = []
  }
  const index = editingAgent.value.contextScopes.indexOf(scopeKey)
  if (index === -1) {
    editingAgent.value.contextScopes.push(scopeKey)
  } else {
    editingAgent.value.contextScopes.splice(index, 1)
  }
}

// Editing agent
const editingAgent = ref<AiAgent | null>(null)

// Selected template for system prompt
const selectedTemplate = ref('custom')

// New conversation form
const newConversation = ref<CreateConversationRequest>({
  title: '',
  mode: 'OnDemand',
  aiAgentIds: []
})

// Fetch data on mount
onMounted(async () => {
  await Promise.all([fetchAgents(), fetchAvailableModels(), fetchConversations()])
})

// Use API for orchestrator
const { post } = useApi()
const { currentOrganization } = useOrganizations()

// Ask the orchestrator
const askOrchestrator = async () => {
  if (!orchestratorQuery.value.trim() || !currentOrganization.value) return

  isAskingOrchestrator.value = true
  orchestratorResponse.value = null

  try {
    const response = await post<{
      message: string
      agentsConsulted: string[]
      innerDialogue: Array<{
        stepNumber: number
        type: string
        title: string
        description?: string
        agentName?: string
        response?: string
        tokensUsed?: number
      }>
      error?: string
    }>(`/organizations/${currentOrganization.value.id}/ai/orchestrate`, {
      message: orchestratorQuery.value
    })

    orchestratorResponse.value = response
  } catch (e) {
    console.error('Failed to ask orchestrator:', e)
    orchestratorResponse.value = {
      message: 'Failed to get a response. Please try again.',
      agentsConsulted: [],
      innerDialogue: [],
      error: String(e)
    }
  } finally {
    isAskingOrchestrator.value = false
  }
}

// Watch for model selection to auto-update display name
watch(() => newAgent.value.modelId, (modelId) => {
  const model = availableModels.value.find(m => m.modelId === modelId)
  if (model) {
    newAgent.value.modelDisplayName = model.displayName
    newAgent.value.provider = model.provider as 'anthropic' | 'openai' | 'google'
  }
})

// Watch for template selection
watch(selectedTemplate, (template) => {
  if (template && systemPromptTemplates[template]) {
    newAgent.value.systemPrompt = systemPromptTemplates[template]
  }
})

// Add agent
const handleAddAgent = async () => {
  if (!newAgent.value.name || !newAgent.value.roleTitle) return

  isSubmitting.value = true
  try {
    await createAgent(newAgent.value)
    resetNewAgentForm()
    showAddDialog.value = false
  } catch (e) {
    console.error('Failed to add agent:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Reset form
const resetNewAgentForm = () => {
  newAgent.value = {
    name: '',
    roleTitle: '',
    provider: 'anthropic',
    modelId: 'claude-sonnet-4-20250514',
    modelDisplayName: 'Claude Sonnet',
    systemPrompt: systemPromptTemplates.custom,
    avatarColor: '#8B5CF6',
    maxTokensPerResponse: 4096,
    temperature: 0.7,
    isActive: true,
    // Personality defaults
    assertiveness: 50,
    communicationStyle: 'Formal',
    reactionTendency: 'Balanced',
    expertiseAreas: '',
    seniorityLevel: 3,
    asksQuestions: false,
    givesBriefAcknowledgments: true,
    contextScopes: []
  }
  selectedTemplate.value = 'custom'
  showPersonalitySection.value = false
  showDataAccessSection.value = false
}

// Open edit dialog (for Custom agents)
const openEditDialog = (agent: AiAgent) => {
  editingAgent.value = { ...agent }
  showEditDialog.value = true
}

// Open edit dialog for Built-in agents
const openEditBuiltInDialog = (agent: AiAgent) => {
  editingBuiltInAgent.value = { ...agent }
  builtInCustomInstructions.value = agent.customInstructions || ''
  showEditBuiltInDialog.value = true
}

// Save Built-in agent changes
const handleSaveBuiltInAgent = async () => {
  if (!editingBuiltInAgent.value) return

  isSubmitting.value = true
  try {
    await updateAgent(editingBuiltInAgent.value.id, {
      customInstructions: builtInCustomInstructions.value || undefined,
      isActive: editingBuiltInAgent.value.isActive
    })
    editingBuiltInAgent.value = null
    showEditBuiltInDialog.value = false
  } catch (e) {
    console.error('Failed to update Built-in agent:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Edit agent
const handleEditAgent = async () => {
  if (!editingAgent.value || !editingAgent.value.name) return

  isSubmitting.value = true
  try {
    await updateAgent(editingAgent.value.id, editingAgent.value as UpdateAiAgentRequest)
    editingAgent.value = null
    showEditDialog.value = false
  } catch (e) {
    console.error('Failed to update agent:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Delete agent
const handleDeleteAgent = async (id: string) => {
  if (confirm('Are you sure you want to delete this AI agent?')) {
    try {
      await deleteAgent(id)
    } catch (e) {
      console.error('Failed to delete agent:', e)
    }
  }
}

// Toggle active status
const handleToggleActive = async (id: string) => {
  try {
    await toggleAgentActive(id)
  } catch (e) {
    console.error('Failed to toggle agent status:', e)
  }
}

// Available colors for avatars
const avatarColors = [
  '#8B5CF6', // Purple
  '#EC4899', // Pink
  '#F59E0B', // Amber
  '#10B981', // Emerald
  '#3B82F6', // Blue
  '#EF4444', // Red
  '#06B6D4', // Cyan
  '#84CC16'  // Lime
]

// Get provider badge color
const getProviderBadge = (provider: string) => {
  const info = providerInfo[provider]
  return info || { name: provider, color: 'text-slate-400', bgColor: 'bg-slate-500/20' }
}

// Toggle agent selection for new conversation
const toggleAgentSelection = (agentId: string) => {
  const index = newConversation.value.aiAgentIds?.indexOf(agentId) ?? -1
  if (index >= 0) {
    newConversation.value.aiAgentIds?.splice(index, 1)
  } else {
    newConversation.value.aiAgentIds?.push(agentId)
  }
}

// Create new conversation
const handleCreateConversation = async () => {
  if (!newConversation.value.title || !newConversation.value.aiAgentIds?.length) return

  isSubmitting.value = true
  try {
    const conversation = await createConversation(newConversation.value)
    if (conversation) {
      showNewConversationDialog.value = false
      // Reset form
      newConversation.value = { title: '', mode: 'OnDemand', aiAgentIds: [] }
      // Navigate to the conversation
      router.push(`/app/ai-agents/conversations/${conversation.id}`)
    }
  } catch (e) {
    console.error('Failed to create conversation:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Format relative time
const formatRelativeTime = (dateStr: string | undefined) => {
  if (!dateStr) return 'Never'
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`
  return `${Math.floor(diffMins / 1440)}d ago`
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">AI Agents</h1>
        <p class="orbitos-text">Configure AI agents and start multi-agent conversations.</p>
      </div>
      <div class="flex gap-2">
        <button
          type="button"
          @click="showNewConversationDialog = true"
          class="orbitos-btn-secondary py-2 px-4 text-sm"
          :disabled="agents.filter(a => a.isActive).length === 0"
        >
          <svg class="w-4 h-4 mr-2 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
          New Conversation
        </button>
        <button
          type="button"
          @click="showAddDialog = true"
          class="orbitos-btn-primary py-2 px-4 text-sm"
        >
          <svg class="w-4 h-4 mr-2 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Add Agent
        </button>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 p-1 backdrop-blur-xl bg-white/5 rounded-xl border border-white/10 w-fit">
      <button
        :class="[
          'px-4 py-2 text-sm font-medium rounded-lg transition-colors',
          activeTab === 'agents' ? 'bg-purple-500/20 text-purple-300' : 'text-white/60 hover:text-white'
        ]"
        @click="activeTab = 'agents'"
      >
        Agents ({{ agents.length }})
      </button>
      <button
        :class="[
          'px-4 py-2 text-sm font-medium rounded-lg transition-colors',
          activeTab === 'conversations' ? 'bg-purple-500/20 text-purple-300' : 'text-white/60 hover:text-white'
        ]"
        @click="activeTab = 'conversations'"
      >
        Conversations ({{ conversations.length }})
      </button>
    </div>

    <!-- Agents Tab Content -->
    <template v-if="activeTab === 'agents'">
      <!-- Stats -->
      <div class="grid gap-4 md:grid-cols-4">
        <div class="orbitos-card-static">
          <div class="text-xs uppercase text-white/40">Built-in</div>
          <div class="mt-1 text-2xl font-semibold text-blue-300">{{ builtInAgents.length }}</div>
        </div>
        <div class="orbitos-card-static">
          <div class="text-xs uppercase text-white/40">Custom</div>
          <div class="mt-1 text-2xl font-semibold text-purple-300">{{ customAgents.length }}</div>
        </div>
        <div class="orbitos-card-static">
          <div class="text-xs uppercase text-white/40">Active</div>
          <div class="mt-1 text-2xl font-semibold text-emerald-300">
            {{ activeAgents.length }}
          </div>
        </div>
        <div class="orbitos-card-static">
          <div class="text-xs uppercase text-white/40">Total</div>
          <div class="mt-1 text-2xl font-semibold text-white">{{ agents.length }}</div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <div class="orbitos-spinner orbitos-spinner-md"></div>
      </div>

      <template v-else>
        <!-- ORCHESTRATOR QUICK ACCESS -->
        <div class="orbitos-card p-4 bg-gradient-to-r from-purple-500/10 to-blue-500/10 border-purple-500/30">
          <div class="flex items-start gap-4">
            <div class="w-14 h-14 rounded-2xl bg-gradient-to-br from-purple-500 to-blue-500 flex items-center justify-center text-3xl shadow-lg">
              🎯
            </div>
            <div class="flex-1">
              <h2 class="text-lg font-semibold text-white mb-1">Ask OrbitOS</h2>
              <p class="text-sm text-white/60 mb-3">
                Ask any question and the orchestrator will automatically route it to the right specialist agents.
                Watch the inner dialogue to see how agents collaborate to answer your question.
              </p>
              <div class="flex items-center gap-3">
                <input
                  v-model="orchestratorQuery"
                  type="text"
                  class="orbitos-input flex-1"
                  placeholder="e.g., What's the bottleneck in our processes? Who's overloaded?"
                  @keydown.enter="askOrchestrator"
                  :disabled="isAskingOrchestrator"
                />
                <button
                  type="button"
                  class="orbitos-btn-primary px-4 py-2 text-sm"
                  :disabled="!orchestratorQuery.trim() || isAskingOrchestrator"
                  @click="askOrchestrator"
                >
                  <svg v-if="isAskingOrchestrator" class="animate-spin -ml-1 mr-2 h-4 w-4 inline" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                  </svg>
                  {{ isAskingOrchestrator ? 'Thinking...' : 'Ask' }}
                </button>
              </div>
            </div>
          </div>

          <!-- Orchestrator Response -->
          <div v-if="orchestratorResponse" class="mt-4 pt-4 border-t border-white/10">
            <!-- Inner Dialogue Steps -->
            <InnerDialogueDisplay
              v-if="orchestratorResponse.innerDialogue?.length"
              :steps="orchestratorResponse.innerDialogue"
              :is-collapsible="true"
              class="mb-4"
            />

            <!-- Response -->
            <div class="bg-white/5 rounded-lg p-4">
              <div class="flex items-center gap-2 mb-2">
                <span class="text-xs text-white/40">Response from:</span>
                <span
                  v-for="agent in orchestratorResponse.agentsConsulted"
                  :key="agent"
                  class="text-xs px-2 py-0.5 rounded-full bg-purple-500/20 text-purple-300"
                >
                  {{ agent }}
                </span>
              </div>
              <div class="text-white/90 whitespace-pre-wrap text-sm">{{ orchestratorResponse.message }}</div>
            </div>
          </div>
        </div>

        <!-- BUILT-IN AGENTS SECTION -->
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-lg font-semibold text-white">Built-in</h2>
              <p class="text-sm text-white/50">Pre-configured specialists with access to your organization data</p>
            </div>
          </div>

          <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <div
              v-for="agent in builtInAgents"
              :key="agent.id"
              class="orbitos-card p-4 relative group"
              :class="{ 'opacity-60': !agent.isActive }"
            >
              <!-- Icon & Name -->
              <div class="flex items-center gap-3 mb-3">
                <div
                  class="w-12 h-12 rounded-xl flex items-center justify-center text-2xl shadow-lg"
                  :style="{ backgroundColor: agent.avatarColor || '#3B82F6' }"
                >
                  {{ specialistIcons[agent.specialistKey || ''] || '🤖' }}
                </div>
                <div class="flex-1 min-w-0">
                  <h3 class="font-medium text-white truncate">{{ agent.name }}</h3>
                  <p class="text-xs text-white/50 truncate">{{ agent.roleTitle }}</p>
                </div>
              </div>

              <!-- Context Scopes -->
              <div class="mb-3">
                <div class="flex flex-wrap gap-1">
                  <span
                    v-for="scope in (agent.contextScopes || []).slice(0, 3)"
                    :key="scope"
                    class="text-xs px-2 py-0.5 rounded-full bg-white/10 text-white/60"
                  >
                    {{ scope }}
                  </span>
                  <span
                    v-if="(agent.contextScopes || []).length > 3"
                    class="text-xs px-2 py-0.5 rounded-full bg-white/10 text-white/40"
                  >
                    +{{ (agent.contextScopes || []).length - 3 }}
                  </span>
                </div>
              </div>

              <!-- Custom Instructions Indicator -->
              <div v-if="agent.customInstructions" class="mb-3">
                <span class="text-xs text-purple-400">
                  <svg class="w-3 h-3 inline mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" />
                  </svg>
                  Customized
                </span>
              </div>

              <!-- Status & Actions -->
              <div class="flex items-center justify-between mt-auto pt-2 border-t border-white/10">
                <button
                  type="button"
                  @click="handleToggleActive(agent.id)"
                  :class="[
                    'text-xs px-2 py-1 rounded-full transition-all',
                    agent.isActive
                      ? 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30'
                      : 'bg-slate-500/20 text-slate-400 hover:bg-slate-500/30'
                  ]"
                >
                  {{ agent.isActive ? 'Active' : 'Inactive' }}
                </button>
                <button
                  type="button"
                  @click="openEditBuiltInDialog(agent)"
                  class="text-xs text-white/50 hover:text-white transition-colors"
                >
                  Edit
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- CUSTOM AGENTS SECTION -->
        <div class="space-y-3 mt-8">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-lg font-semibold text-white">Custom</h2>
              <p class="text-sm text-white/50">AI personas you've created for conversations</p>
            </div>
          </div>

          <!-- Empty State for Custom -->
          <div v-if="customAgents.length === 0" class="orbitos-card-static p-8 text-center">
            <div class="mx-auto w-12 h-12 rounded-full bg-purple-500/20 flex items-center justify-center mb-3">
              <svg class="w-6 h-6 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
            </div>
            <h3 class="text-sm font-medium text-white mb-1">No custom agents yet</h3>
            <p class="text-xs text-white/50 max-w-xs mx-auto mb-3">
              Create custom AI personas with different personalities and expertise
            </p>
            <button
              type="button"
              @click="showAddDialog = true"
              class="text-sm text-purple-400 hover:text-purple-300 transition-colors"
            >
              Create your first agent
            </button>
          </div>

          <!-- Custom Agents Grid -->
          <div v-else class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <div
              v-for="agent in customAgents"
              :key="agent.id"
              class="orbitos-card p-4 relative group"
              :class="{ 'opacity-60': !agent.isActive }"
            >
              <!-- Icon & Name -->
              <div class="flex items-center gap-3 mb-3">
                <div
                  class="w-12 h-12 rounded-xl flex items-center justify-center text-white font-semibold text-lg shadow-lg"
                  :style="{ backgroundColor: agent.avatarColor || '#8B5CF6' }"
                >
                  {{ agent.name.charAt(0) }}
                </div>
                <div class="flex-1 min-w-0">
                  <h3 class="font-medium text-white truncate">{{ agent.name }}</h3>
                  <p class="text-xs text-white/50 truncate">{{ agent.roleTitle }}</p>
                </div>
              </div>

              <!-- Model Badge -->
              <div class="mb-3">
                <span
                  :class="['inline-flex rounded-full px-2 py-0.5 text-xs font-medium', getProviderBadge(agent.provider).bgColor, getProviderBadge(agent.provider).color]"
                >
                  {{ agent.modelDisplayName }}
                </span>
              </div>

              <!-- Can Call Built-in Indicator -->
              <div v-if="agent.canCallBuiltInAgents" class="mb-3">
                <span class="text-xs text-blue-400">
                  <svg class="w-3 h-3 inline mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-3a5.972 5.972 0 00-.75-2.906A3.005 3.005 0 0119 15v3h-3zM4.75 12.094A5.973 5.973 0 004 15v3H1v-3a3 3 0 013.75-2.906z" />
                  </svg>
                  Can consult specialists
                </span>
              </div>

              <!-- Status & Actions -->
              <div class="flex items-center justify-between mt-auto pt-2 border-t border-white/10">
                <button
                  type="button"
                  @click="handleToggleActive(agent.id)"
                  :class="[
                    'text-xs px-2 py-1 rounded-full transition-all',
                    agent.isActive
                      ? 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30'
                      : 'bg-slate-500/20 text-slate-400 hover:bg-slate-500/30'
                  ]"
                >
                  {{ agent.isActive ? 'Active' : 'Inactive' }}
                </button>
                <div class="flex items-center gap-2">
                  <button
                    type="button"
                    @click="openEditDialog(agent)"
                    class="text-xs text-white/50 hover:text-white transition-colors"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    @click="handleDeleteAgent(agent.id)"
                    class="text-xs text-white/50 hover:text-red-400 transition-colors"
                  >
                    Delete
                  </button>
                </div>
              </div>
            </div>

            <!-- Add Agent Card -->
            <button
              type="button"
              @click="showAddDialog = true"
              class="orbitos-card-static p-4 border-2 border-dashed border-white/20 hover:border-purple-500/50 hover:bg-purple-500/5 transition-all flex flex-col items-center justify-center min-h-[180px]"
            >
              <div class="w-12 h-12 rounded-xl bg-purple-500/20 flex items-center justify-center mb-3">
                <svg class="w-6 h-6 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
              </div>
              <span class="text-sm text-white/60">Add Agent</span>
            </button>
          </div>
        </div>
      </template>
    </template>

    <!-- Conversations Tab Content -->
    <template v-if="activeTab === 'conversations'">
      <!-- Loading State -->
      <div v-if="isLoadingConversations" class="flex items-center justify-center py-12">
        <div class="orbitos-spinner orbitos-spinner-md"></div>
      </div>

      <!-- Empty State -->
      <div v-else-if="conversations.length === 0" class="orbitos-card-static p-12 text-center">
        <div class="mx-auto w-16 h-16 rounded-full bg-purple-500/20 flex items-center justify-center mb-4">
          <svg class="w-8 h-8 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
        </div>
        <h3 class="text-lg font-medium text-white mb-2">No Conversations Yet</h3>
        <p class="orbitos-text max-w-md mx-auto">
          Start a conversation with your AI agents to discuss strategy, analyze data, or get insights.
        </p>
        <button
          type="button"
          @click="showNewConversationDialog = true"
          :disabled="agents.filter(a => a.isActive).length === 0"
          class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Start your first conversation
        </button>
      </div>

      <!-- Conversations List -->
      <div v-else class="space-y-3">
        <NuxtLink
          v-for="conv in conversations"
          :key="conv.id"
          :to="`/app/ai-agents/conversations/${conv.id}`"
          class="orbitos-card block p-4 hover:border-purple-500/50 transition-colors"
        >
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <div class="flex items-center gap-2">
                <h3 class="font-medium text-white">{{ conv.title }}</h3>
                <span
                  :class="[
                    'text-xs px-2 py-0.5 rounded',
                    conv.status === 'Active' ? 'bg-emerald-500/20 text-emerald-400' :
                    conv.status === 'Paused' ? 'bg-yellow-500/20 text-yellow-400' :
                    'bg-slate-500/20 text-slate-400'
                  ]"
                >
                  {{ conv.status }}
                </span>
              </div>
              <div class="mt-1 flex items-center gap-4 text-sm text-white/40">
                <span>{{ conv.participantCount }} participants</span>
                <span>{{ conv.messageCount }} messages</span>
                <span>{{ conv.totalTokens.toLocaleString() }} tokens</span>
                <span>${{ conv.totalCost.toFixed(2) }}</span>
              </div>
            </div>
            <div class="text-sm text-white/40">
              {{ formatRelativeTime(conv.lastMessageAt) }}
            </div>
          </div>
        </NuxtLink>
      </div>
    </template>

    <!-- Add Agent Dialog -->
    <BaseDialog
      v-model="showAddDialog"
      size="2xl"
      title="Create AI Agent"
      subtitle="Configure a new AI agent with a specific model and role."
      @close="resetNewAgentForm"
      @submit="handleAddAgent"
    >
        <div class="space-y-4">
          <!-- Basic Info Row -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="orbitos-label">Agent Name *</label>
              <input
                v-model="newAgent.name"
                type="text"
                class="orbitos-input"
                placeholder="e.g., CFO-AI"
                autofocus
              />
            </div>
            <div>
              <label class="orbitos-label">Role Title *</label>
              <input
                v-model="newAgent.roleTitle"
                type="text"
                class="orbitos-input"
                placeholder="e.g., Chief Financial Officer"
              />
            </div>
          </div>

          <!-- Model Selection -->
          <div>
            <label class="orbitos-label">AI Model *</label>
            <select v-model="newAgent.modelId" class="orbitos-input">
              <optgroup
                v-for="(models, provider) in modelsByProvider"
                :key="provider"
                :label="providerInfo[provider]?.name || provider"
              >
                <option
                  v-for="model in models"
                  :key="model.modelId"
                  :value="model.modelId"
                >
                  {{ model.displayName }} - {{ model.description }}
                </option>
              </optgroup>
            </select>
          </div>

          <!-- System Prompt Template -->
          <div>
            <label class="orbitos-label">System Prompt Template</label>
            <select v-model="selectedTemplate" class="orbitos-input">
              <option value="custom">Custom</option>
              <option value="cfo">CFO / Finance</option>
              <option value="operations">Operations Manager</option>
              <option value="strategy">Strategy Consultant</option>
              <option value="hr">HR Director</option>
            </select>
          </div>

          <!-- System Prompt -->
          <div>
            <label class="orbitos-label">System Prompt *</label>
            <textarea
              v-model="newAgent.systemPrompt"
              rows="6"
              class="orbitos-input font-mono text-sm"
              placeholder="Define the agent's personality, expertise, and behavior..."
            ></textarea>
          </div>

          <!-- Advanced Settings -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="orbitos-label">Max Tokens</label>
              <input
                v-model.number="newAgent.maxTokensPerResponse"
                type="number"
                class="orbitos-input"
                min="100"
                max="32000"
              />
            </div>
            <div>
              <label class="orbitos-label">Temperature (0-2)</label>
              <input
                v-model.number="newAgent.temperature"
                type="number"
                class="orbitos-input"
                min="0"
                max="2"
                step="0.1"
              />
            </div>
          </div>

          <!-- Avatar Color -->
          <div>
            <label class="orbitos-label">Avatar Color</label>
            <div class="flex gap-2 mt-2">
              <button
                v-for="color in avatarColors"
                :key="color"
                type="button"
                @click="newAgent.avatarColor = color"
                class="w-8 h-8 rounded-lg transition-transform"
                :class="{ 'ring-2 ring-white ring-offset-2 ring-offset-slate-900 scale-110': newAgent.avatarColor === color }"
                :style="{ backgroundColor: color }"
              />
            </div>
          </div>

          <!-- Data Access Section -->
          <div class="border-t border-white/10 pt-4">
            <button
              type="button"
              @click="showDataAccessSection = !showDataAccessSection"
              class="flex items-center gap-2 text-sm text-blue-300 hover:text-blue-200 transition-colors"
            >
              <svg
                class="w-4 h-4 transition-transform"
                :class="{ 'rotate-90': showDataAccessSection }"
                fill="none" stroke="currentColor" viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
              Data Access (Context Scopes)
            </button>

            <div v-if="showDataAccessSection" class="mt-4 space-y-3 pl-4 border-l-2 border-blue-500/30">
              <p class="text-xs text-white/50">
                Select which organization data this agent can access. This determines what information is loaded into the agent's context when responding.
              </p>

              <div class="grid grid-cols-2 gap-2">
                <label
                  v-for="scope in availableContextScopes"
                  :key="scope.key"
                  class="flex items-start gap-2 p-2 rounded-lg bg-white/5 hover:bg-white/10 cursor-pointer transition-colors"
                  :class="{ 'ring-1 ring-blue-500/50 bg-blue-500/10': (newAgent.contextScopes || []).includes(scope.key) }"
                >
                  <input
                    type="checkbox"
                    :checked="(newAgent.contextScopes || []).includes(scope.key)"
                    @change="toggleContextScope(scope.key)"
                    class="mt-0.5 w-4 h-4 rounded bg-white/10 border-white/20 accent-blue-500"
                  />
                  <div class="flex-1 min-w-0">
                    <span class="text-sm text-white/90 block">{{ scope.label }}</span>
                    <span class="text-xs text-white/40 block">{{ scope.description }}</span>
                  </div>
                </label>
              </div>

              <p v-if="(newAgent.contextScopes || []).length === 0" class="text-xs text-amber-400/70">
                No data access selected. The agent won't have access to your organization data.
              </p>
              <p v-else class="text-xs text-blue-400/70">
                {{ (newAgent.contextScopes || []).length }} scope{{ (newAgent.contextScopes || []).length === 1 ? '' : 's' }} selected
              </p>
            </div>
          </div>

          <!-- Personality & Meeting Behavior Section -->
          <div class="border-t border-white/10 pt-4">
            <button
              type="button"
              @click="showPersonalitySection = !showPersonalitySection"
              class="flex items-center gap-2 text-sm text-purple-300 hover:text-purple-200 transition-colors"
            >
              <svg
                class="w-4 h-4 transition-transform"
                :class="{ 'rotate-90': showPersonalitySection }"
                fill="none" stroke="currentColor" viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
              Personality & Meeting Behavior (for Emergent mode)
            </button>

            <div v-if="showPersonalitySection" class="mt-4 space-y-4 pl-4 border-l-2 border-purple-500/30">
              <!-- Assertiveness Slider -->
              <div>
                <label class="orbitos-label">Assertiveness: {{ newAgent.assertiveness }}%</label>
                <p class="text-xs text-white/40 mb-2">How likely the agent is to speak up (0=waits, 100=speaks first)</p>
                <input
                  v-model.number="newAgent.assertiveness"
                  type="range"
                  min="0"
                  max="100"
                  class="w-full h-2 bg-white/10 rounded-lg appearance-none cursor-pointer accent-purple-500"
                />
              </div>

              <!-- Communication Style -->
              <div>
                <label class="orbitos-label">Communication Style</label>
                <select v-model="newAgent.communicationStyle" class="orbitos-input">
                  <option v-for="style in communicationStyles" :key="style.value" :value="style.value">
                    {{ style.label }} - {{ style.description }}
                  </option>
                </select>
              </div>

              <!-- Reaction Tendency -->
              <div>
                <label class="orbitos-label">Reaction Tendency</label>
                <select v-model="newAgent.reactionTendency" class="orbitos-input">
                  <option v-for="tendency in reactionTendencies" :key="tendency.value" :value="tendency.value">
                    {{ tendency.label }} - {{ tendency.description }}
                  </option>
                </select>
              </div>

              <!-- Expertise Areas -->
              <div>
                <label class="orbitos-label">Expertise Areas</label>
                <p class="text-xs text-white/40 mb-2">Comma-separated keywords for stake detection (e.g., "finance,budget,revenue,costs")</p>
                <input
                  v-model="newAgent.expertiseAreas"
                  type="text"
                  class="orbitos-input"
                  placeholder="e.g., finance, budget, revenue, costs"
                />
              </div>

              <!-- Seniority Level -->
              <div>
                <label class="orbitos-label">Seniority Level: {{ newAgent.seniorityLevel }}</label>
                <p class="text-xs text-white/40 mb-2">Affects deference patterns (1=junior, 5=senior executive)</p>
                <input
                  v-model.number="newAgent.seniorityLevel"
                  type="range"
                  min="1"
                  max="5"
                  class="w-full h-2 bg-white/10 rounded-lg appearance-none cursor-pointer accent-purple-500"
                />
              </div>

              <!-- Boolean Behaviors -->
              <div class="grid grid-cols-2 gap-4">
                <label class="flex items-center gap-2 cursor-pointer">
                  <input
                    v-model="newAgent.asksQuestions"
                    type="checkbox"
                    class="w-4 h-4 rounded bg-white/10 border-white/20 accent-purple-500"
                  />
                  <span class="text-sm text-white/70">Asks clarifying questions</span>
                </label>
                <label class="flex items-center gap-2 cursor-pointer">
                  <input
                    v-model="newAgent.givesBriefAcknowledgments"
                    type="checkbox"
                    class="w-4 h-4 rounded bg-white/10 border-white/20 accent-purple-500"
                  />
                  <span class="text-sm text-white/70">Gives brief acknowledgments</span>
                </label>
              </div>
            </div>
          </div>
        </div>

      <template #footer="{ close }">
        <div class="flex gap-3">
          <button
            type="button"
            @click="close"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleAddAgent"
            :disabled="!newAgent.name || !newAgent.roleTitle || !newAgent.systemPrompt || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Creating...
            </span>
            <span v-else>Create Agent</span>
          </button>
        </div>
      </template>
    </BaseDialog>

    <!-- Edit Agent Dialog -->
    <BaseDialog
      v-model="showEditDialog"
      size="2xl"
      title="Edit AI Agent"
      subtitle="Update agent configuration."
      @close="editingAgent = null"
      @submit="handleEditAgent"
    >
      <template v-if="editingAgent">
        <div class="space-y-4">
          <!-- Basic Info Row -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="orbitos-label">Agent Name *</label>
              <input
                v-model="editingAgent.name"
                type="text"
                class="orbitos-input"
                autofocus
              />
            </div>
            <div>
              <label class="orbitos-label">Role Title *</label>
              <input
                v-model="editingAgent.roleTitle"
                type="text"
                class="orbitos-input"
              />
            </div>
          </div>

          <!-- Model Selection -->
          <div>
            <label class="orbitos-label">AI Model *</label>
            <select v-model="editingAgent.modelId" class="orbitos-input">
              <optgroup
                v-for="(models, provider) in modelsByProvider"
                :key="provider"
                :label="providerInfo[provider]?.name || provider"
              >
                <option
                  v-for="model in models"
                  :key="model.modelId"
                  :value="model.modelId"
                >
                  {{ model.displayName }} - {{ model.description }}
                </option>
              </optgroup>
            </select>
          </div>

          <!-- System Prompt -->
          <div>
            <label class="orbitos-label">System Prompt *</label>
            <textarea
              v-model="editingAgent.systemPrompt"
              rows="6"
              class="orbitos-input font-mono text-sm"
            ></textarea>
          </div>

          <!-- Advanced Settings -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="orbitos-label">Max Tokens</label>
              <input
                v-model.number="editingAgent.maxTokensPerResponse"
                type="number"
                class="orbitos-input"
                min="100"
                max="32000"
              />
            </div>
            <div>
              <label class="orbitos-label">Temperature (0-2)</label>
              <input
                v-model.number="editingAgent.temperature"
                type="number"
                class="orbitos-input"
                min="0"
                max="2"
                step="0.1"
              />
            </div>
          </div>

          <!-- Avatar Color -->
          <div>
            <label class="orbitos-label">Avatar Color</label>
            <div class="flex gap-2 mt-2">
              <button
                v-for="color in avatarColors"
                :key="color"
                type="button"
                @click="editingAgent.avatarColor = color"
                class="w-8 h-8 rounded-lg transition-transform"
                :class="{ 'ring-2 ring-white ring-offset-2 ring-offset-slate-900 scale-110': editingAgent.avatarColor === color }"
                :style="{ backgroundColor: color }"
              />
            </div>
          </div>

          <!-- Active Status -->
          <div class="flex items-center gap-3">
            <input
              v-model="editingAgent.isActive"
              type="checkbox"
              id="isActive"
              class="w-4 h-4 rounded bg-white/10 border-white/20"
            />
            <label for="isActive" class="orbitos-label mb-0">Agent is active</label>
          </div>

          <!-- Data Access Section (Edit) -->
          <div class="border-t border-white/10 pt-4">
            <button
              type="button"
              @click="showEditDataAccessSection = !showEditDataAccessSection"
              class="flex items-center gap-2 text-sm text-blue-300 hover:text-blue-200 transition-colors"
            >
              <svg
                class="w-4 h-4 transition-transform"
                :class="{ 'rotate-90': showEditDataAccessSection }"
                fill="none" stroke="currentColor" viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
              Data Access (Context Scopes)
            </button>

            <div v-if="showEditDataAccessSection" class="mt-4 space-y-3 pl-4 border-l-2 border-blue-500/30">
              <p class="text-xs text-white/50">
                Select which organization data this agent can access.
              </p>

              <div class="grid grid-cols-2 gap-2">
                <label
                  v-for="scope in availableContextScopes"
                  :key="scope.key"
                  class="flex items-start gap-2 p-2 rounded-lg bg-white/5 hover:bg-white/10 cursor-pointer transition-colors"
                  :class="{ 'ring-1 ring-blue-500/50 bg-blue-500/10': (editingAgent.contextScopes || []).includes(scope.key) }"
                >
                  <input
                    type="checkbox"
                    :checked="(editingAgent.contextScopes || []).includes(scope.key)"
                    @change="toggleEditContextScope(scope.key)"
                    class="mt-0.5 w-4 h-4 rounded bg-white/10 border-white/20 accent-blue-500"
                  />
                  <div class="flex-1 min-w-0">
                    <span class="text-sm text-white/90 block">{{ scope.label }}</span>
                    <span class="text-xs text-white/40 block">{{ scope.description }}</span>
                  </div>
                </label>
              </div>

              <p v-if="(editingAgent.contextScopes || []).length === 0" class="text-xs text-amber-400/70">
                No data access selected.
              </p>
              <p v-else class="text-xs text-blue-400/70">
                {{ (editingAgent.contextScopes || []).length }} scope{{ (editingAgent.contextScopes || []).length === 1 ? '' : 's' }} selected
              </p>
            </div>
          </div>

          <!-- Personality & Meeting Behavior Section -->
          <div class="border-t border-white/10 pt-4">
            <button
              type="button"
              @click="showEditPersonalitySection = !showEditPersonalitySection"
              class="flex items-center gap-2 text-sm text-purple-300 hover:text-purple-200 transition-colors"
            >
              <svg
                class="w-4 h-4 transition-transform"
                :class="{ 'rotate-90': showEditPersonalitySection }"
                fill="none" stroke="currentColor" viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
              Personality & Meeting Behavior (for Emergent mode)
            </button>

            <div v-if="showEditPersonalitySection" class="mt-4 space-y-4 pl-4 border-l-2 border-purple-500/30">
              <!-- Assertiveness Slider -->
              <div>
                <label class="orbitos-label">Assertiveness: {{ editingAgent.assertiveness }}%</label>
                <p class="text-xs text-white/40 mb-2">How likely the agent is to speak up (0=waits, 100=speaks first)</p>
                <input
                  v-model.number="editingAgent.assertiveness"
                  type="range"
                  min="0"
                  max="100"
                  class="w-full h-2 bg-white/10 rounded-lg appearance-none cursor-pointer accent-purple-500"
                />
              </div>

              <!-- Communication Style -->
              <div>
                <label class="orbitos-label">Communication Style</label>
                <select v-model="editingAgent.communicationStyle" class="orbitos-input">
                  <option v-for="style in communicationStyles" :key="style.value" :value="style.value">
                    {{ style.label }} - {{ style.description }}
                  </option>
                </select>
              </div>

              <!-- Reaction Tendency -->
              <div>
                <label class="orbitos-label">Reaction Tendency</label>
                <select v-model="editingAgent.reactionTendency" class="orbitos-input">
                  <option v-for="tendency in reactionTendencies" :key="tendency.value" :value="tendency.value">
                    {{ tendency.label }} - {{ tendency.description }}
                  </option>
                </select>
              </div>

              <!-- Expertise Areas -->
              <div>
                <label class="orbitos-label">Expertise Areas</label>
                <p class="text-xs text-white/40 mb-2">Comma-separated keywords for stake detection (e.g., "finance,budget,revenue,costs")</p>
                <input
                  v-model="editingAgent.expertiseAreas"
                  type="text"
                  class="orbitos-input"
                  placeholder="e.g., finance, budget, revenue, costs"
                />
              </div>

              <!-- Seniority Level -->
              <div>
                <label class="orbitos-label">Seniority Level: {{ editingAgent.seniorityLevel }}</label>
                <p class="text-xs text-white/40 mb-2">Affects deference patterns (1=junior, 5=senior executive)</p>
                <input
                  v-model.number="editingAgent.seniorityLevel"
                  type="range"
                  min="1"
                  max="5"
                  class="w-full h-2 bg-white/10 rounded-lg appearance-none cursor-pointer accent-purple-500"
                />
              </div>

              <!-- Boolean Behaviors -->
              <div class="grid grid-cols-2 gap-4">
                <label class="flex items-center gap-2 cursor-pointer">
                  <input
                    v-model="editingAgent.asksQuestions"
                    type="checkbox"
                    class="w-4 h-4 rounded bg-white/10 border-white/20 accent-purple-500"
                  />
                  <span class="text-sm text-white/70">Asks clarifying questions</span>
                </label>
                <label class="flex items-center gap-2 cursor-pointer">
                  <input
                    v-model="editingAgent.givesBriefAcknowledgments"
                    type="checkbox"
                    class="w-4 h-4 rounded bg-white/10 border-white/20 accent-purple-500"
                  />
                  <span class="text-sm text-white/70">Gives brief acknowledgments</span>
                </label>
              </div>
            </div>
          </div>
        </div>
      </template>

      <template #footer="{ close }">
        <div v-if="editingAgent" class="flex gap-3">
          <button
            type="button"
            @click="close"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleEditAgent"
            :disabled="!editingAgent.name || !editingAgent.roleTitle || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Saving...
            </span>
            <span v-else>Save Changes</span>
          </button>
        </div>
      </template>
    </BaseDialog>

    <!-- Edit Built-in Agent Dialog -->
    <BaseDialog
      v-model="showEditBuiltInDialog"
      size="lg"
      title="Edit Built-in Agent"
      :subtitle="editingBuiltInAgent?.roleTitle || ''"
      @close="editingBuiltInAgent = null"
      @submit="handleSaveBuiltInAgent"
    >
      <template v-if="editingBuiltInAgent">
        <div class="space-y-4">
          <!-- Agent Header -->
          <div class="flex items-center gap-4 p-4 rounded-xl bg-white/5">
            <div
              class="w-14 h-14 rounded-xl flex items-center justify-center text-3xl shadow-lg"
              :style="{ backgroundColor: editingBuiltInAgent.avatarColor || '#3B82F6' }"
            >
              {{ specialistIcons[editingBuiltInAgent.specialistKey || ''] || '🤖' }}
            </div>
            <div>
              <h3 class="text-lg font-medium text-white">{{ editingBuiltInAgent.name }}</h3>
              <p class="text-sm text-white/50">{{ editingBuiltInAgent.roleTitle }}</p>
            </div>
          </div>

          <!-- Data Access (read-only) -->
          <div>
            <label class="orbitos-label">Data Access</label>
            <div class="flex flex-wrap gap-2">
              <span
                v-for="scope in editingBuiltInAgent.contextScopes || []"
                :key="scope"
                class="px-3 py-1.5 rounded-lg bg-blue-500/20 text-blue-300 text-sm"
              >
                {{ scope }}
              </span>
            </div>
          </div>

          <!-- Base Behavior (read-only, collapsible) -->
          <div>
            <label class="orbitos-label flex items-center gap-2">
              Base Behavior
              <span class="text-xs text-white/40 font-normal">(read-only)</span>
            </label>
            <div class="p-3 rounded-lg bg-white/5 border border-white/10 max-h-40 overflow-y-auto">
              <pre class="text-xs text-white/60 whitespace-pre-wrap font-mono">{{ editingBuiltInAgent.basePrompt }}</pre>
            </div>
          </div>

          <!-- Custom Instructions -->
          <div>
            <label class="orbitos-label">Custom Instructions</label>
            <p class="text-xs text-white/40 mb-2">Add organization-specific context, preferences, or rules</p>
            <textarea
              v-model="builtInCustomInstructions"
              rows="6"
              class="orbitos-input font-mono text-sm"
              placeholder="- Always consider our Q4 hiring freeze
- Flag any team with span of control > 8
- We prefer flat hierarchies"
            ></textarea>
          </div>

          <!-- Status -->
          <div class="flex items-center gap-3 p-3 rounded-lg bg-white/5">
            <input
              v-model="editingBuiltInAgent.isActive"
              type="checkbox"
              id="builtInIsActive"
              class="w-4 h-4 rounded bg-white/10 border-white/20 accent-purple-500"
            />
            <label for="builtInIsActive" class="text-sm text-white">Agent is active</label>
          </div>
        </div>
      </template>

      <template #footer="{ close }">
        <div class="flex gap-3">
          <button
            type="button"
            @click="close"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleSaveBuiltInAgent"
            :disabled="isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Saving...
            </span>
            <span v-else>Save Changes</span>
          </button>
        </div>
      </template>
    </BaseDialog>

    <!-- New Conversation Dialog -->
    <BaseDialog
      v-model="showNewConversationDialog"
      size="lg"
      title="New Conversation"
      subtitle="Start a multi-agent conversation with your AI team."
      @submit="handleCreateConversation"
    >
      <div class="space-y-4">
        <!-- Title -->
        <div>
          <label class="orbitos-label">Conversation Title *</label>
          <input
            v-model="newConversation.title"
            type="text"
            class="orbitos-input"
            placeholder="e.g., Q4 Strategy Review"
            autofocus
          />
        </div>

        <!-- Mode -->
        <div>
          <label class="orbitos-label">Conversation Mode</label>
          <select v-model="newConversation.mode" class="orbitos-input">
            <option value="OnDemand">On-Demand (AI responds only when @mentioned)</option>
            <option value="Emergent">Emergent (Agents self-moderate based on relevance)</option>
            <option value="Moderated">Moderated (AI responses require approval)</option>
            <option value="RoundRobin">Round-Robin (Each AI responds in turn)</option>
            <option value="Free">Free (All AIs may respond - use with caution)</option>
          </select>
          <p v-if="newConversation.mode === 'Emergent'" class="mt-2 text-xs text-purple-300">
            Agents will use personality traits and expertise matching to decide when to speak.
          </p>
        </div>

        <!-- Agent Selection -->
        <div>
          <label class="orbitos-label">Select AI Agents *</label>
          <p class="text-xs text-white/40 mb-2">Choose which AI agents will participate in this conversation.</p>
          <div class="space-y-4 max-h-64 overflow-y-auto">
            <!-- Built-in Agents Section -->
            <div v-if="builtInAgents.filter(a => a.isActive).length > 0">
              <div class="flex items-center gap-2 mb-2">
                <span class="text-xs font-medium text-blue-400 uppercase tracking-wider">Built-in Specialists</span>
                <div class="flex-1 h-px bg-blue-500/20"></div>
              </div>
              <div class="space-y-2">
                <button
                  v-for="agent in builtInAgents.filter(a => a.isActive)"
                  :key="agent.id"
                  type="button"
                  @click="toggleAgentSelection(agent.id)"
                  :class="[
                    'w-full flex items-center gap-3 p-3 rounded-xl border transition-colors text-left',
                    newConversation.aiAgentIds?.includes(agent.id)
                      ? 'border-blue-500/50 bg-blue-500/10'
                      : 'border-white/10 hover:border-white/20'
                  ]"
                >
                  <div
                    class="w-10 h-10 rounded-xl flex items-center justify-center text-xl"
                    :style="{ backgroundColor: agent.avatarColor || '#3B82F6' }"
                  >
                    {{ specialistIcons[agent.specialistKey || ''] || '🤖' }}
                  </div>
                  <div class="flex-1">
                    <div class="text-white font-medium">{{ agent.name }}</div>
                    <div class="text-white/60 text-sm">{{ agent.roleTitle }}</div>
                  </div>
                  <div
                    :class="[
                      'w-5 h-5 rounded border flex items-center justify-center',
                      newConversation.aiAgentIds?.includes(agent.id)
                        ? 'bg-blue-500 border-blue-500'
                        : 'border-white/30'
                    ]"
                  >
                    <svg v-if="newConversation.aiAgentIds?.includes(agent.id)" class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                    </svg>
                  </div>
                </button>
              </div>
            </div>

            <!-- Custom Agents Section -->
            <div v-if="customAgents.filter(a => a.isActive).length > 0">
              <div class="flex items-center gap-2 mb-2">
                <span class="text-xs font-medium text-purple-400 uppercase tracking-wider">Custom Agents</span>
                <div class="flex-1 h-px bg-purple-500/20"></div>
              </div>
              <div class="space-y-2">
                <button
                  v-for="agent in customAgents.filter(a => a.isActive)"
                  :key="agent.id"
                  type="button"
                  @click="toggleAgentSelection(agent.id)"
                  :class="[
                    'w-full flex items-center gap-3 p-3 rounded-xl border transition-colors text-left',
                    newConversation.aiAgentIds?.includes(agent.id)
                      ? 'border-purple-500/50 bg-purple-500/10'
                      : 'border-white/10 hover:border-white/20'
                  ]"
                >
                  <div
                    class="w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium text-white"
                    :style="{ backgroundColor: agent.avatarColor || '#8B5CF6' }"
                  >
                    {{ agent.name.charAt(0) }}
                  </div>
                  <div class="flex-1">
                    <div class="text-white font-medium">{{ agent.name }}</div>
                    <div class="text-white/60 text-sm">{{ agent.roleTitle }}</div>
                  </div>
                  <div
                    :class="[
                      'w-5 h-5 rounded border flex items-center justify-center',
                      newConversation.aiAgentIds?.includes(agent.id)
                        ? 'bg-purple-500 border-purple-500'
                        : 'border-white/30'
                    ]"
                  >
                    <svg v-if="newConversation.aiAgentIds?.includes(agent.id)" class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                    </svg>
                  </div>
                </button>
              </div>
            </div>
          </div>
          <p v-if="activeAgents.length === 0" class="text-sm text-white/40 py-4 text-center">
            No active AI agents available. Create an agent first.
          </p>
        </div>
      </div>

      <template #footer="{ close }">
        <div class="flex gap-3">
          <button
            type="button"
            @click="close"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleCreateConversation"
            :disabled="!newConversation.title || !newConversation.aiAgentIds?.length || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Creating...
            </span>
            <span v-else>Start Conversation</span>
          </button>
        </div>
      </template>
    </BaseDialog>
  </div>
</template>
