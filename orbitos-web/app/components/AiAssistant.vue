<script setup lang="ts">
import { useApi } from '~/composables/useApi'
import { useOrganizations } from '~/composables/useOrganizations'

const { post } = useApi()
const { currentOrganizationId } = useOrganizations()

const isOpen = ref(false)
const isMinimized = ref(false)
const message = ref('')
const isTyping = ref(false)
const error = ref<string | null>(null)

interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: Date
}

interface ChatHistory {
  role: string
  content: string
}

interface ChatResponse {
  message: string
  toolCalls?: { tool: string; action: string; data?: unknown }[]
  error?: { code: string; message: string }
}

const messages = ref<ChatMessage[]>([
  {
    id: '1',
    role: 'assistant',
    content: "Hi! I'm your OrbitOS AI assistant. I can help you with:\n\n- Viewing people, roles, and functions in your organization\n- Adding new people\n- Assigning roles to people\n- Analyzing organizational health\n\nWhat would you like to explore?",
    timestamp: new Date()
  }
])

const quickActions = [
  { label: 'Analyze health', prompt: 'Analyze the organizational coverage and identify any gaps or single points of failure' },
  { label: 'List people', prompt: 'Who are the people in my organization and what are their roles?' },
  { label: 'Find SPOFs', prompt: 'Which roles have single points of failure?' },
  { label: 'Coverage gaps', prompt: 'Are there any uncovered roles or functions?' }
]

const sendMessage = async () => {
  if (!message.value.trim()) return

  const userMessage: ChatMessage = {
    id: Date.now().toString(),
    role: 'user',
    content: message.value,
    timestamp: new Date()
  }

  messages.value.push(userMessage)
  const query = message.value
  message.value = ''
  isTyping.value = true
  error.value = null

  try {
    // Build history (exclude the welcome message and current message)
    const history: ChatHistory[] = messages.value
      .slice(1, -1) // Skip welcome message and current user message
      .map(msg => ({
        role: msg.role,
        content: msg.content
      }))

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
      const errorMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: `I encountered an error: ${response.error.message}. Please try again.`,
        timestamp: new Date()
      }
      messages.value.push(errorMessage)
    } else {
      const assistantMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: response.message,
        timestamp: new Date()
      }
      messages.value.push(assistantMessage)
    }
  } catch (err) {
    isTyping.value = false
    const errorMsg = err instanceof Error ? err.message : 'An unexpected error occurred'
    error.value = errorMsg

    const errorMessage: ChatMessage = {
      id: (Date.now() + 1).toString(),
      role: 'assistant',
      content: `I'm having trouble connecting to the AI service. Error: ${errorMsg}\n\nPlease make sure the API is running and try again.`,
      timestamp: new Date()
    }
    messages.value.push(errorMessage)
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

const formatTime = (date: Date) => {
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
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
            'flex',
            msg.role === 'user' ? 'justify-end' : 'justify-start'
          ]"
        >
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
</template>
