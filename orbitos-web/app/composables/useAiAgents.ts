import { useApi } from '~/composables/useApi'
import { useOrganizations } from '~/composables/useOrganizations'

export interface AiAgent {
  id: string
  name: string
  roleTitle: string
  avatarUrl?: string
  avatarColor?: string
  provider: 'anthropic' | 'openai' | 'google'
  modelId: string
  modelDisplayName: string
  systemPrompt: string
  maxTokensPerResponse: number
  temperature: number
  isActive: boolean
  sortOrder: number
  createdAt: string
  updatedAt: string
}

export interface AvailableModel {
  provider: string
  modelId: string
  displayName: string
  description: string
  contextWindow: number
}

export interface CreateAiAgentRequest {
  name: string
  roleTitle: string
  avatarUrl?: string
  avatarColor?: string
  provider: string
  modelId: string
  modelDisplayName: string
  systemPrompt: string
  maxTokensPerResponse?: number
  temperature?: number
  isActive?: boolean
  sortOrder?: number
}

export interface UpdateAiAgentRequest {
  name?: string
  roleTitle?: string
  avatarUrl?: string
  avatarColor?: string
  provider?: string
  modelId?: string
  modelDisplayName?: string
  systemPrompt?: string
  maxTokensPerResponse?: number
  temperature?: number
  isActive?: boolean
  sortOrder?: number
}

// Global state for AI agents
const agents = ref<AiAgent[]>([])
const availableModels = ref<AvailableModel[]>([])
const isLoading = ref(false)
const isLoadingModels = ref(false)

export const useAiAgents = () => {
  const { get, post, put, delete: del } = useApi()

  // Get organization ID reactively from useOrganizations, falling back to localStorage
  const getOrgId = (): string => {
    try {
      const { currentOrganizationId } = useOrganizations()
      if (currentOrganizationId.value) {
        return currentOrganizationId.value
      }
    } catch {
      // Composable not available
    }
    const storedOrgId = localStorage.getItem('currentOrganizationId')
    return storedOrgId || '11111111-1111-1111-1111-111111111111'
  }

  const fetchAgents = async () => {
    isLoading.value = true
    try {
      const orgId = getOrgId()
      agents.value = await get<AiAgent[]>(`/api/organizations/${orgId}/ai-agents`)
    } catch (error) {
      console.error('Failed to fetch AI agents:', error)
      agents.value = []
    } finally {
      isLoading.value = false
    }
  }

  const fetchAvailableModels = async () => {
    if (availableModels.value.length > 0) return // Already loaded

    isLoadingModels.value = true
    try {
      const orgId = getOrgId()
      availableModels.value = await get<AvailableModel[]>(`/api/organizations/${orgId}/ai-agents/available-models`)
    } catch (error) {
      console.error('Failed to fetch available models:', error)
      availableModels.value = []
    } finally {
      isLoadingModels.value = false
    }
  }

  const createAgent = async (request: CreateAiAgentRequest): Promise<AiAgent> => {
    const orgId = getOrgId()
    const newAgent = await post<AiAgent>(`/api/organizations/${orgId}/ai-agents`, request)
    agents.value.push(newAgent)
    // Sort by sortOrder then name
    agents.value.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name))
    return newAgent
  }

  const updateAgent = async (agentId: string, request: UpdateAiAgentRequest): Promise<AiAgent> => {
    const orgId = getOrgId()
    const updated = await put<AiAgent>(`/api/organizations/${orgId}/ai-agents/${agentId}`, request)
    const index = agents.value.findIndex(a => a.id === agentId)
    if (index >= 0) {
      agents.value[index] = updated
    }
    return updated
  }

  const deleteAgent = async (agentId: string): Promise<void> => {
    const orgId = getOrgId()
    await del(`/api/organizations/${orgId}/ai-agents/${agentId}`)
    agents.value = agents.value.filter(a => a.id !== agentId)
  }

  const toggleAgentActive = async (agentId: string): Promise<void> => {
    const agent = agents.value.find(a => a.id === agentId)
    if (!agent) return
    await updateAgent(agentId, { isActive: !agent.isActive })
  }

  // Get models grouped by provider
  const modelsByProvider = computed(() => {
    const grouped: Record<string, AvailableModel[]> = {}
    for (const model of availableModels.value) {
      if (!grouped[model.provider]) {
        grouped[model.provider] = []
      }
      grouped[model.provider].push(model)
    }
    return grouped
  })

  // Provider display info
  const providerInfo: Record<string, { name: string; color: string; bgColor: string }> = {
    anthropic: { name: 'Anthropic', color: 'text-orange-400', bgColor: 'bg-orange-500/20' },
    openai: { name: 'OpenAI', color: 'text-emerald-400', bgColor: 'bg-emerald-500/20' },
    google: { name: 'Google', color: 'text-blue-400', bgColor: 'bg-blue-500/20' }
  }

  return {
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
  }
}
