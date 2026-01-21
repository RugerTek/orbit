<script setup lang="ts">
import type { AiAgent, CreateAiAgentRequest, UpdateAiAgentRequest } from '~/composables/useAiAgents'
import type { CreateConversationRequest } from '~/composables/useConversations'
import { useConversations } from '~/composables/useConversations'

definePageMeta({
  layout: 'app'
})

const router = useRouter()

const {
  agents,
  availableModels,
  modelsByProvider,
  providerInfo,
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

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const showNewConversationDialog = ref(false)
const isSubmitting = ref(false)

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
  isActive: true
})

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
    isActive: true
  }
  selectedTemplate.value = 'custom'
}

// Open edit dialog
const openEditDialog = (agent: AiAgent) => {
  editingAgent.value = { ...agent }
  showEditDialog.value = true
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
          <div class="text-xs uppercase text-white/40">Total Agents</div>
          <div class="mt-1 text-2xl font-semibold text-white">{{ agents.length }}</div>
        </div>
        <div class="orbitos-card-static">
          <div class="text-xs uppercase text-white/40">Active</div>
          <div class="mt-1 text-2xl font-semibold text-emerald-300">
            {{ agents.filter(a => a.isActive).length }}
          </div>
        </div>
        <div class="orbitos-card-static">
          <div class="text-xs uppercase text-white/40">Anthropic</div>
          <div class="mt-1 text-2xl font-semibold text-orange-300">
            {{ agents.filter(a => a.provider === 'anthropic').length }}
          </div>
        </div>
        <div class="orbitos-card-static">
          <div class="text-xs uppercase text-white/40">OpenAI / Google</div>
          <div class="mt-1 text-2xl font-semibold text-blue-300">
            {{ agents.filter(a => a.provider !== 'anthropic').length }}
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <div class="orbitos-spinner orbitos-spinner-md"></div>
      </div>

      <!-- Empty State -->
      <div v-else-if="agents.length === 0" class="orbitos-card-static p-12 text-center">
        <div class="mx-auto w-16 h-16 rounded-full bg-purple-500/20 flex items-center justify-center mb-4">
          <svg class="w-8 h-8 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
        </div>
        <h3 class="text-lg font-medium text-white mb-2">No AI Agents Yet</h3>
        <p class="orbitos-text max-w-md mx-auto">
          Create AI agents with different models (Claude, GPT-4, Gemini) and roles to help with different tasks.
        </p>
        <button
          type="button"
          @click="showAddDialog = true"
          class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
        >
          Create your first agent
        </button>
      </div>

      <!-- Agents Table -->
      <div v-else class="orbitos-card overflow-hidden">
        <table class="w-full">
          <thead>
            <tr class="border-b border-white/10">
              <th class="w-12 px-4 py-3 text-left text-xs font-medium text-white/40 uppercase tracking-wider">#</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-white/40 uppercase tracking-wider">Agent</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-white/40 uppercase tracking-wider hidden md:table-cell">Model</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-white/40 uppercase tracking-wider hidden lg:table-cell">Tokens</th>
              <th class="px-4 py-3 text-center text-xs font-medium text-white/40 uppercase tracking-wider">Status</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-white/40 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/5">
            <tr
              v-for="(agent, index) in agents"
              :key="agent.id"
              class="group hover:bg-white/5 transition-colors"
              :class="{ 'opacity-50': !agent.isActive }"
            >
              <!-- Index -->
              <td class="px-4 py-4">
                <span class="text-sm font-mono text-white/30">{{ index + 1 }}</span>
              </td>

              <!-- Agent Info -->
              <td class="px-4 py-4">
                <div class="flex items-center gap-3">
                  <div
                    class="w-10 h-10 rounded-xl flex items-center justify-center text-white font-semibold text-sm flex-shrink-0 shadow-lg"
                    :style="{ backgroundColor: agent.avatarColor || '#8B5CF6' }"
                  >
                    {{ agent.name.charAt(0) }}
                  </div>
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <span class="font-medium text-white truncate">{{ agent.name }}</span>
                    </div>
                    <p class="text-sm text-white/50 truncate">{{ agent.roleTitle }}</p>
                    <!-- Mobile: Show model badge inline -->
                    <div class="md:hidden mt-1">
                      <span
                        :class="['inline-flex rounded-full px-2 py-0.5 text-xs font-medium', getProviderBadge(agent.provider).bgColor, getProviderBadge(agent.provider).color]"
                      >
                        {{ agent.modelDisplayName }}
                      </span>
                    </div>
                  </div>
                </div>
              </td>

              <!-- Model -->
              <td class="px-4 py-4 hidden md:table-cell">
                <span
                  :class="['inline-flex rounded-full px-2.5 py-1 text-xs font-medium', getProviderBadge(agent.provider).bgColor, getProviderBadge(agent.provider).color]"
                >
                  {{ agent.modelDisplayName }}
                </span>
              </td>

              <!-- Max Tokens -->
              <td class="px-4 py-4 hidden lg:table-cell">
                <span class="text-sm text-white/60 font-mono">
                  {{ agent.maxTokensPerResponse.toLocaleString() }}
                </span>
              </td>

              <!-- Status -->
              <td class="px-4 py-4 text-center">
                <button
                  type="button"
                  @click="handleToggleActive(agent.id)"
                  :class="[
                    'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium transition-all',
                    agent.isActive
                      ? 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30'
                      : 'bg-slate-500/20 text-slate-400 hover:bg-slate-500/30'
                  ]"
                >
                  <span
                    :class="[
                      'w-1.5 h-1.5 rounded-full',
                      agent.isActive ? 'bg-emerald-400' : 'bg-slate-400'
                    ]"
                  />
                  {{ agent.isActive ? 'Active' : 'Inactive' }}
                </button>
              </td>

              <!-- Actions -->
              <td class="px-4 py-4 text-right">
                <div class="flex items-center justify-end gap-1">
                  <button
                    type="button"
                    @click="openEditDialog(agent)"
                    class="p-2 rounded-lg text-white/50 hover:text-white hover:bg-white/10 transition-colors"
                    title="Edit agent"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    @click="handleDeleteAgent(agent.id)"
                    class="p-2 rounded-lg text-white/50 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                    title="Delete agent"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
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
            <option value="Moderated">Moderated (AI responses require approval)</option>
            <option value="RoundRobin">Round-Robin (Each AI responds in turn)</option>
            <option value="Free">Free (All AIs may respond - use with caution)</option>
          </select>
        </div>

        <!-- Agent Selection -->
        <div>
          <label class="orbitos-label">Select AI Agents *</label>
          <p class="text-xs text-white/40 mb-2">Choose which AI agents will participate in this conversation.</p>
          <div class="space-y-2 max-h-48 overflow-y-auto">
            <button
              v-for="agent in agents.filter(a => a.isActive)"
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
          <p v-if="agents.filter(a => a.isActive).length === 0" class="text-sm text-white/40 py-4 text-center">
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
