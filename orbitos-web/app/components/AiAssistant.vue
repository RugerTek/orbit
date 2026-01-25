<script setup lang="ts">
import type { ToolCall } from '~/composables/useAssistantChat'

const { post } = useApi()
const { currentOrganizationId } = useOrganizations()
const { messages, loadHistory, addMessage, clearHistory, getHistoryForApi } = useAssistantChat()

const isOpen = ref(false)
const isMinimized = ref(false)
const message = ref('')
const isTyping = ref(false)
const error = ref<string | null>(null)

// Tool call detail modal
const selectedToolCall = ref<{ tool: string; action: string; data?: unknown } | null>(null)
const showToolDetail = ref(false)

interface ChatResponse {
  message: string
  toolCalls?: { tool: string; action: string; data?: unknown }[]
  error?: { code: string; message: string }
}

const quickActions = [
  { label: 'Analyze health', prompt: 'Analyze the organizational coverage and identify any gaps or single points of failure' },
  { label: 'List people', prompt: 'Who are the people in my organization and what are their roles?' },
  { label: 'Find SPOFs', prompt: 'Which roles have single points of failure?' },
  { label: 'Coverage gaps', prompt: 'Are there any uncovered roles or functions?' }
]

// Tool name to display name and icon mapping
const toolConfig: Record<string, { name: string; icon: string; color: string }> = {
  get_people: { name: 'Query People', icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z', color: 'text-blue-400' },
  get_roles: { name: 'Query Roles', icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4', color: 'text-indigo-400' },
  get_functions: { name: 'Query Functions', icon: 'M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z', color: 'text-purple-400' },
  get_processes: { name: 'Query Processes', icon: 'M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2', color: 'text-emerald-400' },
  get_goals: { name: 'Query Goals', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z', color: 'text-amber-400' },
  get_partners: { name: 'Query Partners', icon: 'M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z', color: 'text-cyan-400' },
  get_channels: { name: 'Query Channels', icon: 'M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z', color: 'text-pink-400' },
  get_value_propositions: { name: 'Query Value Props', icon: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z', color: 'text-yellow-400' },
  get_customer_relationships: { name: 'Query Relationships', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z', color: 'text-rose-400' },
  get_revenue_streams: { name: 'Query Revenue', icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-400' },
  get_canvases: { name: 'Query Canvases', icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z', color: 'text-orange-400' },
  get_full_context: { name: 'Full Context', icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4', color: 'text-slate-400' },
  lookup_knowledge_base: { name: 'Knowledge Base', icon: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253', color: 'text-violet-400' },
  create_person: { name: 'Create Person', icon: 'M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z', color: 'text-blue-400' },
  create_process: { name: 'Create Process', icon: 'M12 6v6m0 0v6m0-6h6m-6 0H6', color: 'text-emerald-400' },
  bulk_create_processes: { name: 'Bulk Create', icon: 'M9 13h6m-3-3v6m-9 1V7a2 2 0 012-2h6l2 2h6a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z', color: 'text-emerald-400' },
  add_activities_to_process: { name: 'Add Activities', icon: 'M4 6h16M4 10h16M4 14h16M4 18h16', color: 'text-emerald-400' },
  analyze_coverage: { name: 'Analyze Coverage', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z', color: 'text-amber-400' }
}

// Load chat history when the component mounts and when opening the panel
onMounted(async () => {
  // Try to load history immediately when component mounts
  await loadHistory()
})

// Also reload when opening if not already loaded
watch(isOpen, async (open) => {
  if (open) {
    await loadHistory()
  }
})

const sendMessage = async () => {
  if (!message.value.trim()) return

  // Add user message using composable
  addMessage('user', message.value)

  const query = message.value
  message.value = ''
  isTyping.value = true
  error.value = null

  try {
    // Get history for API (excludes welcome and current message)
    const history = getHistoryForApi().slice(0, -1) // Exclude the message we just added

    const response = await post<ChatResponse>(
      `/api/organizations/${currentOrganizationId.value}/ai/chat`,
      {
        message: query,
        history: history.length > 0 ? history : undefined,
        context: 'people'
      }
    )

    isTyping.value = false

    if (response.error) {
      error.value = response.error.message
      addMessage('assistant', `I encountered an error: ${response.error.message}. Please try again.`)
    } else {
      addMessage('assistant', response.message, response.toolCalls)
    }
  } catch (err) {
    isTyping.value = false
    const errorMsg = err instanceof Error ? err.message : 'An unexpected error occurred'
    error.value = errorMsg

    addMessage('assistant', `I'm having trouble connecting to the AI service. Error: ${errorMsg}\n\nPlease make sure the API is running and try again.`)
  }
}

const handleQuickAction = (prompt: string) => {
  message.value = prompt
  sendMessage()
}

const toggleOpen = () => {
  if (isMinimized.value) {
    isMinimized.value = false
  } else {
    isOpen.value = !isOpen.value
  }
}

const minimize = () => {
  isMinimized.value = true
}

const handleClearHistory = async () => {
  await clearHistory()
}

const formatTime = (date: Date) => {
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

const getToolInfo = (toolName: string) => {
  return toolConfig[toolName] || { name: toolName, icon: 'M13 10V3L4 14h7v7l9-11h-7z', color: 'text-slate-400' }
}

const openToolDetail = (toolCall: ToolCall) => {
  selectedToolCall.value = toolCall
  showToolDetail.value = true
}

const formatJson = (data: unknown): string => {
  try {
    return JSON.stringify(data, null, 2)
  } catch {
    return String(data)
  }
}

const getActionBadgeClass = (action: string): string => {
  switch (action) {
    case 'query_results':
    case 'search_results':
    case 'found':
      return 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30'
    case 'created':
    case 'bulk_created':
    case 'activities_added':
      return 'bg-blue-500/20 text-blue-400 border-blue-500/30'
    case 'error':
      return 'bg-red-500/20 text-red-400 border-red-500/30'
    default:
      return 'bg-slate-500/20 text-slate-400 border-slate-500/30'
  }
}
</script>

<template>
  <!-- Floating Button -->
  <button
    v-if="!isOpen || isMinimized"
    class="fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-white shadow-lg shadow-purple-500/25 transition-all hover:scale-105 hover:shadow-xl hover:shadow-purple-500/40"
    @click="toggleOpen"
  >
    <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
    </svg>
    <span class="absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full bg-emerald-400 text-[10px] font-bold text-slate-900">
      AI
    </span>
  </button>

  <!-- Chat Panel -->
  <Transition
    enter-active-class="transition-all duration-300 ease-out"
    enter-from-class="opacity-0 translate-y-4 scale-95"
    enter-to-class="opacity-100 translate-y-0 scale-100"
    leave-active-class="transition-all duration-200 ease-in"
    leave-from-class="opacity-100 translate-y-0 scale-100"
    leave-to-class="opacity-0 translate-y-4 scale-95"
  >
    <div
      v-if="isOpen && !isMinimized"
      class="fixed bottom-6 right-6 z-50 flex h-[600px] w-[400px] flex-col overflow-hidden rounded-2xl border border-slate-700 bg-slate-800/95 shadow-2xl backdrop-blur-xl"
    >
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-slate-700 bg-gradient-to-r from-purple-500/20 to-blue-500/20 px-4 py-3">
        <div class="flex items-center gap-3">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-purple-500 to-blue-600">
            <svg class="h-4 w-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
            </svg>
          </div>
          <div>
            <h3 class="text-sm font-semibold text-white">AI Assistant</h3>
            <p class="text-xs text-slate-400">Powered by Claude</p>
          </div>
        </div>
        <div class="flex items-center gap-1">
          <!-- Clear history button -->
          <button
            v-if="messages.length > 1"
            class="rounded-lg p-1.5 text-slate-400 hover:bg-slate-700 hover:text-white"
            title="Clear chat history"
            @click="handleClearHistory"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
          <button
            class="rounded-lg p-1.5 text-slate-400 hover:bg-slate-700 hover:text-white"
            @click="minimize"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 12H4" />
            </svg>
          </button>
          <button
            class="rounded-lg p-1.5 text-slate-400 hover:bg-slate-700 hover:text-white"
            @click="isOpen = false"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Messages -->
      <div class="flex-1 overflow-y-auto p-4 space-y-4">
        <div
          v-for="msg in messages"
          :key="msg.id"
          :class="[
            'flex flex-col gap-2',
            msg.role === 'user' ? 'items-end' : 'items-start'
          ]"
        >
          <!-- Tool Calls Display (before message for assistant) -->
          <div
            v-if="msg.role === 'assistant' && msg.toolCalls && msg.toolCalls.length > 0"
            class="flex flex-wrap gap-1.5 max-w-[90%]"
          >
            <button
              v-for="(toolCall, index) in msg.toolCalls"
              :key="index"
              class="group flex items-center gap-1.5 rounded-lg border border-slate-600/50 bg-slate-700/30 px-2 py-1 text-xs transition-all hover:border-purple-500/50 hover:bg-slate-700/60"
              @click="openToolDetail(toolCall)"
            >
              <svg
                :class="['h-3.5 w-3.5 transition-colors', getToolInfo(toolCall.tool).color]"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getToolInfo(toolCall.tool).icon" />
              </svg>
              <span class="text-slate-300 group-hover:text-white">{{ getToolInfo(toolCall.tool).name }}</span>
              <span :class="['rounded px-1 py-0.5 text-[10px] font-medium border', getActionBadgeClass(toolCall.action)]">
                {{ toolCall.action }}
              </span>
            </button>
          </div>

          <!-- Message Bubble -->
          <div
            :class="[
              'max-w-[85%] rounded-2xl px-4 py-3',
              msg.role === 'user'
                ? 'bg-gradient-to-r from-purple-500 to-blue-600 text-white'
                : 'bg-slate-700/60 text-slate-200'
            ]"
          >
            <p class="text-sm whitespace-pre-wrap">{{ msg.content }}</p>
            <p
              :class="[
                'mt-1 text-[10px]',
                msg.role === 'user' ? 'text-white/60' : 'text-slate-500'
              ]"
            >
              {{ formatTime(msg.timestamp) }}
            </p>
          </div>
        </div>

        <!-- Typing indicator -->
        <div v-if="isTyping" class="flex justify-start">
          <div class="rounded-2xl bg-slate-700/60 px-4 py-3">
            <div class="flex items-center gap-1">
              <span class="h-2 w-2 rounded-full bg-purple-400 animate-bounce" style="animation-delay: 0ms" />
              <span class="h-2 w-2 rounded-full bg-purple-400 animate-bounce" style="animation-delay: 150ms" />
              <span class="h-2 w-2 rounded-full bg-purple-400 animate-bounce" style="animation-delay: 300ms" />
            </div>
          </div>
        </div>
      </div>

      <!-- Quick Actions -->
      <div v-if="messages.length <= 2" class="border-t border-slate-700/50 px-4 py-3">
        <p class="mb-2 text-xs text-slate-500">Quick actions:</p>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="action in quickActions"
            :key="action.label"
            class="rounded-lg bg-slate-700/60 px-3 py-1.5 text-xs text-slate-300 hover:bg-slate-600 transition-colors"
            @click="handleQuickAction(action.prompt)"
          >
            {{ action.label }}
          </button>
        </div>
      </div>

      <!-- Input -->
      <div class="border-t border-slate-700 p-4">
        <form @submit.prevent="sendMessage" class="flex items-center gap-2">
          <input
            v-model="message"
            type="text"
            placeholder="Ask about your organization..."
            class="flex-1 rounded-xl border border-slate-600 bg-slate-700/50 px-4 py-2.5 text-sm text-white placeholder-slate-500 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          />
          <button
            type="submit"
            :disabled="!message.trim() || isTyping"
            class="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-r from-purple-500 to-blue-600 text-white transition-all hover:opacity-90 disabled:opacity-50"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
            </svg>
          </button>
        </form>
      </div>
    </div>
  </Transition>

  <!-- Tool Detail Modal -->
  <Teleport to="body">
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-all duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="showToolDetail && selectedToolCall"
        class="fixed inset-0 z-[60] flex items-center justify-center p-4"
      >
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" @click="showToolDetail = false" />

        <!-- Modal Content -->
        <div
          class="relative w-full max-w-lg max-h-[80vh] flex flex-col rounded-2xl border border-slate-700 bg-slate-800 shadow-2xl overflow-hidden"
          @click.stop
        >
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-slate-700 px-5 py-4 bg-gradient-to-r from-slate-800 to-slate-750">
            <div class="flex items-center gap-3">
              <div :class="['flex h-10 w-10 items-center justify-center rounded-xl bg-slate-700/60', getToolInfo(selectedToolCall.tool).color]">
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getToolInfo(selectedToolCall.tool).icon" />
                </svg>
              </div>
              <div>
                <h3 class="text-base font-semibold text-white">{{ getToolInfo(selectedToolCall.tool).name }}</h3>
                <p class="text-xs text-slate-400 font-mono">{{ selectedToolCall.tool }}</p>
              </div>
            </div>
            <button
              class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
              @click="showToolDetail = false"
            >
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Body -->
          <div class="flex-1 overflow-y-auto p-5 space-y-4">
            <!-- Action Badge -->
            <div class="flex items-center gap-2">
              <span class="text-sm text-slate-400">Action:</span>
              <span :class="['rounded-lg px-3 py-1 text-sm font-medium border', getActionBadgeClass(selectedToolCall.action)]">
                {{ selectedToolCall.action }}
              </span>
            </div>

            <!-- Data Section -->
            <div v-if="selectedToolCall.data !== undefined && selectedToolCall.data !== null">
              <div class="flex items-center justify-between mb-2">
                <span class="text-sm font-medium text-slate-300">Response Data</span>
                <button
                  class="text-xs text-purple-400 hover:text-purple-300 transition-colors"
                  @click="navigator.clipboard.writeText(formatJson(selectedToolCall.data))"
                >
                  Copy JSON
                </button>
              </div>
              <div class="rounded-xl bg-slate-900/50 border border-slate-700 p-4 overflow-x-auto">
                <pre class="text-xs text-slate-300 font-mono whitespace-pre-wrap break-words">{{ formatJson(selectedToolCall.data) }}</pre>
              </div>
            </div>

            <!-- No Data Message -->
            <div v-else class="rounded-xl bg-slate-700/30 border border-slate-700/50 p-4 text-center">
              <svg class="h-8 w-8 mx-auto text-slate-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
              </svg>
              <p class="text-sm text-slate-400">No data returned from this tool call</p>
            </div>
          </div>

          <!-- Footer -->
          <div class="border-t border-slate-700 px-5 py-3 bg-slate-800/50">
            <p class="text-xs text-slate-500 text-center">
              Click outside or press Escape to close
            </p>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
