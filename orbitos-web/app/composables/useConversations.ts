import { useApi } from '~/composables/useApi'
import { useOrganizations } from '~/composables/useOrganizations'
import type { AiAgent } from '~/composables/useAiAgents'

export interface UserSummary {
  id: string
  displayName: string
  email: string
  avatarUrl?: string
}

export interface AiAgentSummary {
  id: string
  name: string
  roleTitle: string
  avatarColor?: string
  provider: string
  modelDisplayName: string
}

export interface Participant {
  id: string
  participantType: 'user' | 'ai'
  role: 'owner' | 'moderator' | 'participant'
  joinedAt: string
  isActive: boolean
  user?: UserSummary
  aiAgent?: AiAgentSummary
}

export interface ConversationSummary {
  id: string
  title: string
  mode: string
  status: string
  messageCount: number
  aiResponseCount: number
  totalTokens: number
  totalCost: number
  lastMessageAt?: string
  startedAt: string
  createdAt: string
  participantCount: number
}

export interface Conversation {
  id: string
  title: string
  mode: string
  status: string
  totalTokens: number
  totalCost: number
  messageCount: number
  aiResponseCount: number
  maxTurns?: number
  maxTokens?: number
  lastMessageAt?: string
  startedAt: string
  createdAt: string
  updatedAt: string
  participants: Participant[]
}

export interface InnerDialogueStep {
  stepNumber: number
  type: 'Routing' | 'Consulting' | 'AgentToAgent' | 'Synthesis' | 'Reasoning'
  title: string
  description?: string
  agentId?: string
  agentName?: string
  query?: string
  response?: string
  tokensUsed?: number
}

export interface Message {
  id: string
  conversationId: string
  senderType: 'user' | 'ai'
  content: string
  contentHtml?: string
  tokensUsed?: number
  responseTimeMs?: number
  cost?: number
  modelUsed?: string
  status: 'pending' | 'sent' | 'streaming' | 'failed'
  sequenceNumber: number
  createdAt: string
  senderUser?: UserSummary
  senderAiAgent?: AiAgentSummary
  mentionedAgentIds?: string[]
  // Emergent mode fields
  relevanceScore?: number
  relevanceReasoning?: string
  // A2A Inner dialogue fields
  isInnerDialogue?: boolean
  innerDialogueType?: string
  innerDialogueSteps?: InnerDialogueStep[]
}

export interface EmergentSettings {
  relevanceThreshold: number
  maxRoundsPerMessage: number
  maxResponsesPerRound: number
  useCheapModelForScoring: boolean
  scoringModelId?: string
  scoringModelProvider?: string
  requireUniqueInsight: boolean
  showRelevanceScores: boolean
  responseDelayMs: number
}

export interface PaginatedMessagesResponse {
  messages: Message[]
  hasMore: boolean
  nextCursor?: number
}

export interface CreateConversationRequest {
  title: string
  mode?: string
  aiAgentIds?: string[]
  userIds?: string[]
  maxTurns?: number
  maxTokens?: number
}

// Global state for conversations
const conversations = ref<ConversationSummary[]>([])
const currentConversation = ref<Conversation | null>(null)
const messages = ref<Message[]>([])
const isLoading = ref(false)
const isLoadingMessages = ref(false)
const isSending = ref(false)

export const useConversations = () => {
  const { get, post, put, delete: del } = useApi()

  // Get organization ID reactively from useOrganizations, with localStorage fallback
  const getOrgId = (): string => {
    try {
      const { currentOrganizationId } = useOrganizations()
      if (currentOrganizationId.value) {
        return currentOrganizationId.value
      }
    } catch {
      // Composable not available (e.g., outside Vue context)
    }
    if (typeof window !== 'undefined') {
      return localStorage.getItem('currentOrganizationId') || '11111111-1111-1111-1111-111111111111'
    }
    return '11111111-1111-1111-1111-111111111111'
  }

  const fetchConversations = async (status?: string) => {
    isLoading.value = true
    try {
      const orgId = getOrgId()
      const url = status
        ? `/api/organizations/${orgId}/conversations?status=${status}`
        : `/api/organizations/${orgId}/conversations`
      conversations.value = await get<ConversationSummary[]>(url)
    } catch (error) {
      console.error('Failed to fetch conversations:', error)
      conversations.value = []
    } finally {
      isLoading.value = false
    }
  }

  const fetchConversation = async (conversationId: string) => {
    isLoading.value = true
    try {
      const orgId = getOrgId()
      currentConversation.value = await get<Conversation>(
        `/api/organizations/${orgId}/conversations/${conversationId}`
      )
    } catch (error) {
      console.error('Failed to fetch conversation:', error)
      currentConversation.value = null
    } finally {
      isLoading.value = false
    }
  }

  const fetchMessages = async (conversationId: string, beforeSequence?: number) => {
    isLoadingMessages.value = true
    try {
      const orgId = getOrgId()
      let url = `/api/organizations/${orgId}/conversations/${conversationId}/messages`
      if (beforeSequence) {
        url += `?beforeSequence=${beforeSequence}`
      }
      const response = await get<PaginatedMessagesResponse>(url)

      if (beforeSequence) {
        // Prepend older messages
        messages.value = [...response.messages, ...messages.value]
      } else {
        messages.value = response.messages
      }

      return response
    } catch (error) {
      console.error('Failed to fetch messages:', error)
      return { messages: [], hasMore: false }
    } finally {
      isLoadingMessages.value = false
    }
  }

  const createConversation = async (request: CreateConversationRequest): Promise<Conversation | null> => {
    try {
      const orgId = getOrgId()
      const conversation = await post<Conversation>(
        `/api/organizations/${orgId}/conversations`,
        request
      )
      conversations.value.unshift({
        id: conversation.id,
        title: conversation.title,
        mode: conversation.mode,
        status: conversation.status,
        messageCount: conversation.messageCount,
        aiResponseCount: conversation.aiResponseCount,
        totalTokens: conversation.totalTokens,
        totalCost: conversation.totalCost,
        lastMessageAt: conversation.lastMessageAt,
        startedAt: conversation.startedAt,
        createdAt: conversation.createdAt,
        participantCount: conversation.participants.length
      })
      return conversation
    } catch (error) {
      console.error('Failed to create conversation:', error)
      return null
    }
  }

  const sendMessage = async (conversationId: string, content: string): Promise<Message | null> => {
    isSending.value = true
    try {
      const orgId = getOrgId()
      const message = await post<Message>(
        `/api/organizations/${orgId}/conversations/${conversationId}/messages`,
        { content }
      )

      // Use deduplication-aware append to prevent race condition with SignalR
      // (SignalR may broadcast the message before HTTP response returns)
      const exists = messages.value.some(m => m.id === message.id)
      if (!exists) {
        messages.value.push(message)
      }

      // Update conversation stats
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        currentConversation.value.messageCount++
        currentConversation.value.lastMessageAt = message.createdAt
      }

      return message
    } catch (error) {
      console.error('Failed to send message:', error)
      return null
    } finally {
      isSending.value = false
    }
  }

  const invokeAgents = async (conversationId: string, agentIds?: string[]): Promise<Message[]> => {
    isSending.value = true
    try {
      const orgId = getOrgId()
      console.log('[useConversations] Invoking agents:', { conversationId, agentIds })
      const responses = await post<Message[]>(
        `/api/organizations/${orgId}/conversations/${conversationId}/invoke`,
        { agentIds }
      )
      console.log('[useConversations] Got responses:', responses?.length || 0, 'messages', responses)
      if (responses && Array.isArray(responses) && responses.length > 0) {
        // Filter out any messages that already exist (race condition with SignalR)
        const existingIds = new Set(messages.value.map(m => m.id))
        const newResponses = responses.filter(r => !existingIds.has(r.id))
        if (newResponses.length > 0) {
          messages.value = [...messages.value, ...newResponses]
        }
        console.log('[useConversations] Messages array now has', messages.value.length, 'items (added', newResponses.length, 'new)')
      } else {
        console.warn('[useConversations] No valid responses received from invoke')
      }

      // Update conversation stats
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        currentConversation.value.messageCount += responses.length
        currentConversation.value.aiResponseCount += responses.length
        const lastMsg = responses[responses.length - 1]
        if (lastMsg) {
          currentConversation.value.lastMessageAt = lastMsg.createdAt
          currentConversation.value.totalTokens += responses.reduce((sum, m) => sum + (m.tokensUsed || 0), 0)
          currentConversation.value.totalCost += responses.reduce((sum, m) => sum + (m.cost || 0), 0)
        }
      }

      return responses
    } catch (error) {
      console.error('[useConversations] Failed to invoke agents:', error)
      // Show error in UI by adding a fake error message
      const errorMessage: Message = {
        id: `error-${Date.now()}`,
        conversationId,
        senderType: 'ai',
        content: `⚠️ Error invoking AI agents: ${error instanceof Error ? error.message : 'Unknown error'}`,
        status: 'failed',
        sequenceNumber: messages.value.length + 1,
        createdAt: new Date().toISOString()
      }
      messages.value.push(errorMessage)
      return []
    } finally {
      isSending.value = false
    }
  }

  const pauseConversation = async (conversationId: string): Promise<boolean> => {
    try {
      const orgId = getOrgId()
      const conversation = await post<Conversation>(
        `/api/organizations/${orgId}/conversations/${conversationId}/pause`,
        {}
      )
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        currentConversation.value.status = conversation.status
      }
      return true
    } catch (error) {
      console.error('Failed to pause conversation:', error)
      return false
    }
  }

  const resumeConversation = async (conversationId: string): Promise<boolean> => {
    try {
      const orgId = getOrgId()
      const conversation = await post<Conversation>(
        `/api/organizations/${orgId}/conversations/${conversationId}/resume`,
        {}
      )
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        currentConversation.value.status = conversation.status
      }
      return true
    } catch (error) {
      console.error('Failed to resume conversation:', error)
      return false
    }
  }

  const updateSettings = async (
    conversationId: string,
    settings: { title?: string; mode?: string; maxTurns?: number; maxTokens?: number }
  ): Promise<boolean> => {
    try {
      const orgId = getOrgId()
      const conversation = await put<Conversation>(
        `/api/organizations/${orgId}/conversations/${conversationId}/settings`,
        settings
      )
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        Object.assign(currentConversation.value, conversation)
      }
      return true
    } catch (error) {
      console.error('Failed to update settings:', error)
      return false
    }
  }

  const addParticipant = async (
    conversationId: string,
    participant: { userId?: string; aiAgentId?: string }
  ): Promise<Participant | null> => {
    try {
      const orgId = getOrgId()
      const newParticipant = await post<Participant>(
        `/api/organizations/${orgId}/conversations/${conversationId}/participants`,
        participant
      )
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        currentConversation.value.participants.push(newParticipant)
      }
      return newParticipant
    } catch (error) {
      console.error('Failed to add participant:', error)
      return null
    }
  }

  const removeParticipant = async (conversationId: string, participantId: string): Promise<boolean> => {
    try {
      const orgId = getOrgId()
      await del(`/api/organizations/${orgId}/conversations/${conversationId}/participants/${participantId}`)
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        currentConversation.value.participants = currentConversation.value.participants.filter(
          p => p.id !== participantId
        )
      }
      return true
    } catch (error) {
      console.error('Failed to remove participant:', error)
      return false
    }
  }

  const deleteConversation = async (conversationId: string): Promise<boolean> => {
    try {
      const orgId = getOrgId()
      await del(`/api/organizations/${orgId}/conversations/${conversationId}`)
      conversations.value = conversations.value.filter(c => c.id !== conversationId)
      if (currentConversation.value && currentConversation.value.id === conversationId) {
        currentConversation.value = null
      }
      return true
    } catch (error) {
      console.error('Failed to delete conversation:', error)
      return false
    }
  }

  // Clear messages when switching conversations
  const clearMessages = () => {
    messages.value = []
  }

  // Clear all conversation state (used when switching organizations)
  const clearAllConversationState = () => {
    conversations.value = []
    currentConversation.value = null
    messages.value = []
  }

  // Append a single message (used for real-time updates)
  const appendMessage = (message: Message) => {
    // Ensure we don't add duplicates
    const exists = messages.value.some(m => m.id === message.id)
    if (!exists) {
      messages.value = [...messages.value, message]

      // Update conversation stats
      if (currentConversation.value && currentConversation.value.id === message.conversationId) {
        currentConversation.value.messageCount++
        currentConversation.value.lastMessageAt = message.createdAt
        if (message.senderType === 'ai') {
          currentConversation.value.aiResponseCount++
          if (message.tokensUsed) {
            currentConversation.value.totalTokens += message.tokensUsed
          }
          if (message.cost) {
            currentConversation.value.totalCost += message.cost
          }
        }
      }
    }
  }

  // Get conversation mode display info
  const modeInfo: Record<string, { name: string; description: string; color: string }> = {
    OnDemand: { name: 'On-Demand', description: 'AI agents only respond when @mentioned', color: 'text-blue-400' },
    Moderated: { name: 'Moderated', description: 'AI responses require approval', color: 'text-yellow-400' },
    RoundRobin: { name: 'Round-Robin', description: 'Each AI responds in turn', color: 'text-green-400' },
    Free: { name: 'Free (Caution)', description: 'All AIs may respond freely', color: 'text-red-400' },
    Emergent: { name: 'Emergent', description: 'AI agents self-moderate and respond based on relevance', color: 'text-purple-400' }
  }

  return {
    conversations,
    currentConversation,
    messages,
    isLoading,
    isLoadingMessages,
    isSending,
    modeInfo,
    fetchConversations,
    fetchConversation,
    fetchMessages,
    createConversation,
    sendMessage,
    invokeAgents,
    pauseConversation,
    resumeConversation,
    updateSettings,
    addParticipant,
    removeParticipant,
    deleteConversation,
    clearMessages,
    clearAllConversationState,
    appendMessage
  }
}
