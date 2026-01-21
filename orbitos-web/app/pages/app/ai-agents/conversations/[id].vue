<script setup lang="ts">
import type { AiAgent } from '~/composables/useAiAgents'
import type { Conversation, Message, Participant } from '~/composables/useConversations'
import { useNotifications, type MessageDto } from '~/composables/useNotifications'

definePageMeta({
  layout: 'app'
})

const route = useRoute()
const router = useRouter()
const conversationId = computed(() => route.params.id as string)

const {
  currentConversation,
  messages,
  isLoading,
  isLoadingMessages,
  isSending,
  modeInfo,
  fetchConversation,
  fetchMessages,
  sendMessage,
  invokeAgents,
  pauseConversation,
  resumeConversation,
  updateSettings,
  addParticipant,
  removeParticipant,
  clearMessages,
  appendMessage
} = useConversations()

const { agents, fetchAgents } = useAiAgents()
const { joinConversation, leaveConversation, onMessage, markAsRead, clearUnreadCount } = useNotifications()

// Message input
const messageInput = ref('')
const messageInputRef = ref<HTMLTextAreaElement | null>(null)

// UI state
const showSettingsPanel = ref(false)
const showAddParticipantDialog = ref(false)
const selectedMode = ref('')

// Cleanup function for message handler
let cleanupMessageHandler: (() => void) | null = null

// Load conversation and messages
onMounted(async () => {
  clearMessages()
  await Promise.all([
    fetchConversation(conversationId.value),
    fetchMessages(conversationId.value),
    fetchAgents()
  ])
  if (currentConversation.value) {
    selectedMode.value = currentConversation.value.mode
  }

  // Join SignalR conversation group for real-time messages
  await joinConversation(conversationId.value)

  // Listen for real-time messages from other users
  cleanupMessageHandler = onMessage(conversationId.value, (newMessage: MessageDto) => {
    // Only add if it's not already in the list (avoid duplicates from our own sends)
    const exists = messages.value.some(m => m.id === newMessage.id)
    if (!exists) {
      appendMessage(newMessage as Message)
      nextTick(() => scrollToBottom())
    }
  })

  // Clear unread count for this conversation since we're viewing it
  clearUnreadCount(conversationId.value)

  // Mark as read when last message is available
  if (messages.value.length > 0) {
    const lastMessage = messages.value[messages.value.length - 1]
    markAsRead(conversationId.value, lastMessage.id)
  }
})

// Cleanup when leaving the page
onUnmounted(async () => {
  if (cleanupMessageHandler) {
    cleanupMessageHandler()
  }
  await leaveConversation(conversationId.value)
})

// Parse @mentions from input
const mentionedAgents = computed(() => {
  if (!currentConversation.value || !messageInput.value) return []

  const aiParticipants = currentConversation.value.participants
    .filter(p => p.participantType === 'ai' && p.aiAgent)
    .map(p => p.aiAgent!)

  const inputLower = messageInput.value.toLowerCase()

  return aiParticipants.filter(agent => {
    const nameLower = agent.name.toLowerCase()
    // Check exact match: @Updated CFO
    if (inputLower.includes(`@${nameLower}`)) return true
    // Check no spaces: @updatedcfo
    if (inputLower.includes(`@${nameLower.replace(/ /g, '')}`)) return true
    // Check with hyphen: @updated-cfo
    if (inputLower.includes(`@${nameLower.replace(/ /g, '-')}`)) return true
    // Check with underscore: @updated_cfo
    if (inputLower.includes(`@${nameLower.replace(/ /g, '_')}`)) return true
    return false
  })
})

// Handle sending message
const handleSend = async () => {
  if (!messageInput.value.trim() || isSending.value) return

  const content = messageInput.value.trim()

  // IMPORTANT: Capture mentioned agents BEFORE clearing the input
  // Otherwise mentionedAgents computed will return [] since it reads from messageInput
  const mentionedAgentIds = mentionedAgents.value.map(a => a.id)
  console.log('[Conversation] Sending message:', { content, mentionedAgentIds, mode: currentConversation.value?.mode })

  messageInput.value = ''

  // Send user message
  const userMessage = await sendMessage(conversationId.value, content)
  if (!userMessage) {
    console.error('[Conversation] Failed to send user message')
    return
  }
  console.log('[Conversation] User message sent:', userMessage.id)

  // Auto-scroll to bottom
  await nextTick()
  scrollToBottom()

  // Invoke mentioned agents (or all if mode is not OnDemand)
  // - OnDemand mode: only invoke if agents were @mentioned
  // - Other modes: invoke all agents (undefined = all) or specific mentioned ones
  const agentIdsToInvoke = mentionedAgentIds.length > 0
    ? mentionedAgentIds
    : (currentConversation.value?.mode !== 'OnDemand' ? undefined : null)

  console.log('[Conversation] Will invoke agents?', { agentIdsToInvoke, willInvoke: agentIdsToInvoke !== null })

  if (agentIdsToInvoke !== null && (agentIdsToInvoke === undefined || agentIdsToInvoke.length > 0)) {
    console.log('[Conversation] Calling invokeAgents...')
    await invokeAgents(conversationId.value, agentIdsToInvoke)
    console.log('[Conversation] invokeAgents completed, messages count:', messages.value.length)
    await nextTick()
    scrollToBottom()
  } else {
    console.log('[Conversation] Skipping agent invocation (OnDemand mode with no mentions)')
  }
}

// Scroll chat to bottom
const scrollToBottom = () => {
  const container = document.getElementById('message-container')
  if (container) {
    container.scrollTop = container.scrollHeight
  }
}

// Handle mode change
const handleModeChange = async (newMode: string) => {
  selectedMode.value = newMode
  await updateSettings(conversationId.value, { mode: newMode })
}

// Handle pause/resume
const handlePauseResume = async () => {
  if (!currentConversation.value) return

  if (currentConversation.value.status === 'Paused') {
    await resumeConversation(conversationId.value)
  } else {
    await pauseConversation(conversationId.value)
  }
}

// Add agent to conversation
const handleAddAgent = async (agentId: string) => {
  await addParticipant(conversationId.value, { aiAgentId: agentId })
  showAddParticipantDialog.value = false
}

// Remove participant
const handleRemoveParticipant = async (participantId: string) => {
  await removeParticipant(conversationId.value, participantId)
}

// Get available agents not already in conversation
const availableAgentsToAdd = computed(() => {
  if (!currentConversation.value) return []

  const existingAgentIds = new Set(
    currentConversation.value.participants
      .filter(p => p.aiAgent)
      .map(p => p.aiAgent!.id)
  )

  return agents.value.filter(a => a.isActive && !existingAgentIds.has(a.id))
})

// Format timestamp
const formatTime = (dateStr: string) => {
  const date = new Date(dateStr)
  return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
}

// Format duration
const formatDuration = computed(() => {
  if (!currentConversation.value?.startedAt) return '0s'

  const start = new Date(currentConversation.value.startedAt)
  const last = currentConversation.value.lastMessageAt
    ? new Date(currentConversation.value.lastMessageAt)
    : new Date()

  const diffMs = last.getTime() - start.getTime()
  const diffMins = Math.floor(diffMs / 60000)

  if (diffMins < 1) return `${Math.floor(diffMs / 1000)}s`
  if (diffMins < 60) return `${diffMins}m`
  return `${Math.floor(diffMins / 60)}h ${diffMins % 60}m`
})

// Provider colors
const providerColors: Record<string, string> = {
  anthropic: 'bg-orange-500/20 text-orange-300',
  openai: 'bg-emerald-500/20 text-emerald-300',
  google: 'bg-blue-500/20 text-blue-300'
}

// Handle keyboard shortcuts
const handleKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    handleSend()
  }
}
</script>

<template>
  <div v-if="isLoading" class="flex items-center justify-center h-96">
    <div class="flex items-center gap-3 text-white/60">
      <svg class="animate-spin h-6 w-6" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
      <span>Loading conversation...</span>
    </div>
  </div>

  <div v-else-if="!currentConversation" class="text-center py-16">
    <p class="text-white/60">Conversation not found</p>
    <NuxtLink to="/app/ai-agents" class="mt-4 inline-block text-purple-400 hover:text-purple-300">
      Back to AI Agents
    </NuxtLink>
  </div>

  <div v-else class="h-[calc(100vh-8rem)] flex flex-col">
    <!-- Header -->
    <div class="backdrop-blur-xl bg-white/5 border border-white/10 rounded-2xl p-4 mb-4">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-4">
          <NuxtLink to="/app/ai-agents" class="text-white/40 hover:text-white transition-colors">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </NuxtLink>
          <div>
            <h1 class="text-lg font-semibold text-white">{{ currentConversation.title }}</h1>
            <div class="flex items-center gap-2 mt-1">
              <!-- Participant chips -->
              <div v-for="p in currentConversation.participants.slice(0, 4)" :key="p.id" class="flex items-center gap-1">
                <div
                  v-if="p.aiAgent"
                  class="w-5 h-5 rounded-full flex items-center justify-center text-xs font-medium text-white"
                  :style="{ backgroundColor: p.aiAgent.avatarColor || '#8B5CF6' }"
                >
                  {{ p.aiAgent.name.charAt(0) }}
                </div>
                <div
                  v-else-if="p.user"
                  class="w-5 h-5 rounded-full bg-purple-500 flex items-center justify-center text-xs font-medium text-white"
                >
                  {{ p.user.displayName.charAt(0) }}
                </div>
              </div>
              <span v-if="currentConversation.participants.length > 4" class="text-white/40 text-sm">
                +{{ currentConversation.participants.length - 4 }} more
              </span>
              <button
                class="w-5 h-5 rounded-full border border-dashed border-white/30 flex items-center justify-center text-white/40 hover:text-white hover:border-white/60 transition-colors"
                @click="showAddParticipantDialog = true"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <div class="flex items-center gap-4">
          <!-- Mode selector -->
          <select
            v-model="selectedMode"
            class="orbitos-input text-sm py-1.5"
            @change="handleModeChange(selectedMode)"
          >
            <option value="OnDemand">On-Demand</option>
            <option value="Moderated">Moderated</option>
            <option value="RoundRobin">Round-Robin</option>
            <option value="Free">Free (Caution)</option>
          </select>

          <!-- Stats -->
          <div class="hidden lg:flex items-center gap-4 text-sm">
            <div class="text-white/40">
              <span class="text-white">{{ currentConversation.messageCount }}</span> messages
            </div>
            <div class="text-white/40">
              <span class="text-white">{{ currentConversation.totalTokens.toLocaleString() }}</span> tokens
            </div>
            <div class="text-white/40">
              $<span class="text-white">{{ currentConversation.totalCost.toFixed(2) }}</span>
            </div>
            <div class="text-white/40">
              <span class="text-white">{{ formatDuration }}</span>
            </div>
          </div>

          <!-- Pause/Resume button -->
          <button
            :class="[
              'px-3 py-1.5 rounded-lg text-sm font-medium transition-colors',
              currentConversation.status === 'Paused'
                ? 'bg-green-500/20 text-green-400 hover:bg-green-500/30'
                : 'bg-yellow-500/20 text-yellow-400 hover:bg-yellow-500/30'
            ]"
            @click="handlePauseResume"
          >
            {{ currentConversation.status === 'Paused' ? 'Resume' : 'Pause' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Chat container -->
    <div class="flex-1 flex gap-4 min-h-0">
      <!-- Messages area -->
      <div class="flex-1 flex flex-col backdrop-blur-xl bg-white/5 border border-white/10 rounded-2xl overflow-hidden">
        <!-- Messages list -->
        <div id="message-container" class="flex-1 overflow-y-auto p-4 space-y-4">
          <div v-if="messages.length === 0" class="flex items-center justify-center h-full text-white/40">
            <div class="text-center">
              <svg class="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
              <p>No messages yet</p>
              <p class="text-sm mt-1">Start the conversation by sending a message</p>
            </div>
          </div>

          <div
            v-for="message in messages"
            :key="message.id"
            :class="[
              'flex gap-3',
              message.senderType === 'user' ? 'flex-row-reverse' : ''
            ]"
          >
            <!-- Avatar -->
            <div
              v-if="message.senderAiAgent"
              class="w-8 h-8 rounded-full flex-shrink-0 flex items-center justify-center text-sm font-medium text-white"
              :style="{ backgroundColor: message.senderAiAgent.avatarColor || '#8B5CF6' }"
            >
              {{ message.senderAiAgent.name.charAt(0) }}
            </div>
            <div
              v-else-if="message.senderUser"
              class="w-8 h-8 rounded-full bg-purple-500 flex-shrink-0 flex items-center justify-center text-sm font-medium text-white"
            >
              {{ message.senderUser.displayName.charAt(0) }}
            </div>

            <!-- Message content -->
            <div
              :class="[
                'max-w-[70%] rounded-2xl px-4 py-3',
                message.senderType === 'user'
                  ? 'bg-purple-500/20 border border-purple-500/30'
                  : 'bg-white/10 border border-white/10',
                message.status === 'failed' ? 'border-red-500/50' : ''
              ]"
            >
              <!-- Sender info -->
              <div class="flex items-center gap-2 mb-1">
                <span class="text-sm font-medium text-white">
                  {{ message.senderAiAgent?.name || message.senderUser?.displayName }}
                </span>
                <span
                  v-if="message.senderAiAgent"
                  :class="['text-xs px-1.5 py-0.5 rounded', providerColors[message.senderAiAgent.provider] || 'bg-gray-500/20 text-gray-300']"
                >
                  {{ message.senderAiAgent.modelDisplayName }}
                </span>
                <span class="text-xs text-white/40">{{ formatTime(message.createdAt) }}</span>
              </div>

              <!-- Content -->
              <div class="text-white/90 whitespace-pre-wrap">{{ message.content }}</div>

              <!-- AI message stats -->
              <div v-if="message.senderType === 'ai' && message.status === 'sent'" class="flex items-center gap-3 mt-2 text-xs text-white/40">
                <span v-if="message.tokensUsed">{{ message.tokensUsed }} tokens</span>
                <span v-if="message.responseTimeMs">{{ (message.responseTimeMs / 1000).toFixed(1) }}s</span>
                <span v-if="message.cost">${{ message.cost.toFixed(4) }}</span>
              </div>

              <!-- Failed status -->
              <div v-if="message.status === 'failed'" class="mt-2 text-xs text-red-400">
                Failed to generate response
              </div>
            </div>
          </div>

          <!-- Typing indicator when sending -->
          <div v-if="isSending" class="flex gap-3">
            <div class="w-8 h-8 rounded-full bg-white/10 flex-shrink-0 flex items-center justify-center">
              <svg class="animate-spin h-4 w-4 text-white/60" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
            </div>
            <div class="bg-white/10 border border-white/10 rounded-2xl px-4 py-3">
              <div class="flex items-center gap-1">
                <span class="w-2 h-2 bg-white/40 rounded-full animate-bounce" style="animation-delay: 0ms" />
                <span class="w-2 h-2 bg-white/40 rounded-full animate-bounce" style="animation-delay: 150ms" />
                <span class="w-2 h-2 bg-white/40 rounded-full animate-bounce" style="animation-delay: 300ms" />
              </div>
            </div>
          </div>
        </div>

        <!-- Message input -->
        <div class="p-4 border-t border-white/10">
          <div v-if="currentConversation.status === 'Paused'" class="text-center py-4 text-yellow-400">
            Conversation is paused. Click "Resume" to continue.
          </div>
          <div v-else class="flex gap-3">
            <textarea
              ref="messageInputRef"
              v-model="messageInput"
              class="orbitos-input flex-1 resize-none"
              rows="2"
              placeholder="Type a message... Use @AgentName to mention specific AI agents"
              :disabled="isSending"
              @keydown="handleKeydown"
            />
            <button
              class="orbitos-btn-primary px-4 h-auto"
              :disabled="!messageInput.trim() || isSending"
              @click="handleSend"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
              </svg>
            </button>
          </div>
          <div v-if="mentionedAgents.length > 0" class="mt-2 text-sm text-white/60">
            Will invoke: {{ mentionedAgents.map(a => a.name).join(', ') }}
          </div>
        </div>
      </div>

      <!-- Right sidebar - Context & Stats -->
      <div class="hidden xl:block w-80 flex-shrink-0 space-y-4">
        <!-- Participants -->
        <div class="backdrop-blur-xl bg-white/5 border border-white/10 rounded-2xl p-4">
          <h3 class="text-sm font-medium text-white mb-3">Participants</h3>
          <div class="space-y-2">
            <div
              v-for="p in currentConversation.participants"
              :key="p.id"
              class="flex items-center justify-between py-2 border-b border-white/5 last:border-0"
            >
              <div class="flex items-center gap-2">
                <div
                  v-if="p.aiAgent"
                  class="w-7 h-7 rounded-full flex items-center justify-center text-xs font-medium text-white"
                  :style="{ backgroundColor: p.aiAgent.avatarColor || '#8B5CF6' }"
                >
                  {{ p.aiAgent.name.charAt(0) }}
                </div>
                <div
                  v-else-if="p.user"
                  class="w-7 h-7 rounded-full bg-purple-500 flex items-center justify-center text-xs font-medium text-white"
                >
                  {{ p.user.displayName.charAt(0) }}
                </div>
                <div>
                  <div class="text-sm text-white">{{ p.aiAgent?.name || p.user?.displayName }}</div>
                  <div class="text-xs text-white/40">{{ p.aiAgent?.roleTitle || p.role }}</div>
                </div>
              </div>
              <button
                v-if="p.role !== 'owner'"
                class="p-1 rounded-full text-white/40 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                title="Remove from conversation"
                @click="handleRemoveParticipant(p.id)"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
          <button
            class="mt-3 w-full py-2 text-sm text-purple-400 hover:text-purple-300 border border-dashed border-purple-500/30 rounded-lg transition-colors"
            @click="showAddParticipantDialog = true"
          >
            + Add Participant
          </button>
        </div>

        <!-- Session Stats -->
        <div class="backdrop-blur-xl bg-white/5 border border-white/10 rounded-2xl p-4">
          <h3 class="text-sm font-medium text-white mb-3">Session Stats</h3>
          <div class="space-y-3">
            <div class="flex justify-between">
              <span class="text-white/60 text-sm">Messages</span>
              <span class="text-white text-sm">{{ currentConversation.messageCount }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-white/60 text-sm">AI Responses</span>
              <span class="text-white text-sm">{{ currentConversation.aiResponseCount }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-white/60 text-sm">Total Tokens</span>
              <span class="text-white text-sm">{{ currentConversation.totalTokens.toLocaleString() }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-white/60 text-sm">Est. Cost</span>
              <span class="text-white text-sm">${{ currentConversation.totalCost.toFixed(4) }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-white/60 text-sm">Duration</span>
              <span class="text-white text-sm">{{ formatDuration }}</span>
            </div>
          </div>
        </div>

        <!-- Mode Info -->
        <div class="backdrop-blur-xl bg-white/5 border border-white/10 rounded-2xl p-4">
          <h3 class="text-sm font-medium text-white mb-2">Current Mode</h3>
          <div :class="['text-sm', modeInfo[currentConversation.mode]?.color || 'text-white']">
            {{ modeInfo[currentConversation.mode]?.name || currentConversation.mode }}
          </div>
          <p class="text-xs text-white/40 mt-1">
            {{ modeInfo[currentConversation.mode]?.description || '' }}
          </p>
        </div>
      </div>
    </div>

    <!-- Add Participant Dialog -->
    <UiBaseDialog
      v-if="showAddParticipantDialog"
      v-model="showAddParticipantDialog"
      size="md"
      title="Add AI Agent"
      :submit-on-enter="false"
    >
      <div v-if="availableAgentsToAdd.length === 0" class="text-center py-8 text-white/60">
        <p>No more AI agents available to add.</p>
        <NuxtLink to="/app/ai-agents" class="text-purple-400 hover:text-purple-300 text-sm mt-2 inline-block">
          Create a new agent
        </NuxtLink>
      </div>

      <div v-else class="space-y-2 max-h-80 overflow-y-auto">
        <button
          v-for="agent in availableAgentsToAdd"
          :key="agent.id"
          class="w-full flex items-center gap-3 p-3 rounded-xl border border-white/10 hover:border-purple-500/50 hover:bg-purple-500/10 transition-colors text-left"
          @click="handleAddAgent(agent.id)"
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
          <span
            :class="['text-xs px-2 py-1 rounded', providerColors[agent.provider] || 'bg-gray-500/20 text-gray-300']"
          >
            {{ agent.modelDisplayName }}
          </span>
        </button>
      </div>

      <template #footer="{ close }">
        <button
          class="w-full py-2 text-white/60 hover:text-white transition-colors"
          @click="close"
        >
          Cancel
        </button>
      </template>
    </UiBaseDialog>
  </div>
</template>
