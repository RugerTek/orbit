/**
 * Composable for managing floating AI assistant chat history
 * Provides persistence to the backend database
 */

export interface ToolCall {
  tool: string
  action: string
  data?: unknown
}

export interface AssistantChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: Date
  toolCalls?: ToolCall[]
}

const WELCOME_MESSAGE: AssistantChatMessage = {
  id: 'welcome',
  role: 'assistant',
  content: `Hi! I'm your OrbitOS AI assistant. I can help you with:

- Viewing people, roles, and functions in your organization
- Analyzing coverage gaps and single points of failure
- Creating and updating organizational data
- Understanding your business processes

What would you like to know?`,
  timestamp: new Date()
}

// Module-level state to persist across component mounts
const messages = ref<AssistantChatMessage[]>([{ ...WELCOME_MESSAGE, timestamp: new Date() }])
const isLoaded = ref(false)
const isSaving = ref(false)
const currentOrgUserId = ref<string | null>(null) // Track which org/user combo we loaded for

export function useAssistantChat() {
  const { get, post, delete: del } = useApi()
  const { currentOrganization } = useOrganizations()
  const { user } = useAuth()

  // Load chat history from backend
  const loadHistory = async (forceReload = false) => {
    if (!currentOrganization.value?.id || !user.value?.id) return

    // Check if we need to reload for a different org/user combo
    const orgUserId = `${currentOrganization.value.id}-${user.value.id}`
    if (isLoaded.value && currentOrgUserId.value === orgUserId && !forceReload) return

    currentOrgUserId.value = orgUserId

    try {
      const response = await get<{ messages: Array<{ id: string; role: string; content: string; timestamp: string }> }>(
        `/api/organizations/${currentOrganization.value.id}/ai/assistant/history?userId=${user.value.id}`
      )

      if (response.messages && response.messages.length > 0) {
        messages.value = response.messages.map(m => ({
          id: m.id,
          role: m.role as 'user' | 'assistant',
          content: m.content,
          timestamp: new Date(m.timestamp)
        }))
      } else {
        // No history, start fresh with welcome message
        messages.value = [{ ...WELCOME_MESSAGE, timestamp: new Date() }]
      }
      isLoaded.value = true
    } catch (error) {
      console.error('Failed to load chat history:', error)
      // Start fresh on error
      messages.value = [{ ...WELCOME_MESSAGE, timestamp: new Date() }]
      isLoaded.value = true
    }
  }

  // Save chat history to backend (debounced)
  let saveTimeout: ReturnType<typeof setTimeout> | null = null

  const saveHistory = async () => {
    if (!currentOrganization.value?.id || !user.value?.id) return
    if (isSaving.value) return

    // Clear any pending save
    if (saveTimeout) {
      clearTimeout(saveTimeout)
    }

    // Debounce saves to avoid too many API calls
    saveTimeout = setTimeout(async () => {
      isSaving.value = true
      try {
        await post(
          `/api/organizations/${currentOrganization.value!.id}/ai/assistant/history?userId=${user.value!.id}`,
          {
            messages: messages.value.map(m => ({
              role: m.role,
              content: m.content
            }))
          }
        )
      } catch (error) {
        console.error('Failed to save chat history:', error)
      } finally {
        isSaving.value = false
      }
    }, 1000) // 1 second debounce
  }

  // Add a message to the chat
  const addMessage = (role: 'user' | 'assistant', content: string, toolCalls?: ToolCall[]) => {
    const message: AssistantChatMessage = {
      id: crypto.randomUUID(),
      role,
      content,
      timestamp: new Date(),
      toolCalls
    }
    messages.value.push(message)

    // Auto-save after adding message
    saveHistory()

    return message
  }

  // Clear chat history
  const clearHistory = async () => {
    if (!currentOrganization.value?.id || !user.value?.id) return

    try {
      await del(`/api/organizations/${currentOrganization.value.id}/ai/assistant/history?userId=${user.value.id}`)
    } catch (error) {
      console.error('Failed to clear chat history:', error)
    }

    // Reset to welcome message
    messages.value = [{ ...WELCOME_MESSAGE, timestamp: new Date() }]
    isLoaded.value = true
  }

  // Get history for API requests (excludes welcome message)
  const getHistoryForApi = () => {
    return messages.value
      .slice(1) // Skip welcome message
      .map(m => ({ role: m.role, content: m.content }))
  }

  return {
    messages: readonly(messages),
    isLoaded: readonly(isLoaded),
    isSaving: readonly(isSaving),
    loadHistory,
    saveHistory,
    addMessage,
    clearHistory,
    getHistoryForApi
  }
}
